#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace JuanCapi
{
    public sealed class SierraPlayValidation:MonoBehaviour
    {
        [Serializable] private class Report {public List<string> passed=new List<string>();public List<string> failed=new List<string>();}
        private Report report=new Report();
        private string output;
        private float deadlineAll;
        private float nextSnapshot;
        private bool ending;
        [Serializable] private class Snapshot{public string screen,boss;public float x,y;public int hearts,bossHealth;}
        private readonly HashSet<string> held=new HashSet<string>();
        private readonly Dictionary<string,int> pressed=new Dictionary<string,int>();
        private GameRuntime Game=>GameRuntime.Instance;
        private SierraLevel Level=>SierraLevel.Current;
        private void Check(bool pass,string name){(pass?report.passed:report.failed).Add(name);Debug.Log("SIERRA "+(pass?"PASS ":"FAIL ")+name);}
        private void Pulse(string action){pressed[action]=Time.frameCount+1;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-juanSierraValidate");
            if(index<0||index+1>=args.Length||!args.Contains("-juanDataPath"))return;
            var go=new GameObject("Pruebas de sierra aisladas");DontDestroyOnLoad(go);go.AddComponent<SierraPlayValidation>().output=Path.GetFullPath(args[index+1]);
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;deadlineAll=Time.realtimeSinceStartup+300;
            Directory.CreateDirectory(output);Application.logMessageReceived+=OnLog;
            GameInput.ValidationHeld=action=>held.Contains(action);GameInput.ValidationDown=action=>pressed.TryGetValue(action,out int frame)&&frame==Time.frameCount;
            yield return new WaitForSecondsRealtime(.8f);Check(Game.UI.ScreenName=="menu","Arranque en menú");
            Game.NewCampaign(true);yield return WaitScene();yield return new WaitForSecondsRealtime(.5f);
            Check(Level!=null&&Level.IntroRunning&&Game.Cinematic&&!Game.CanPlay,"Nueva partida inicia cinemática y bloquea movimiento");
            yield return Capture("01-introduccion-mara");
            var drone=Level.drone.position;double clock=Game.Data.activeSeconds;
            Game.UI.Pause();yield return new WaitForSecondsRealtime(.5f);
            Check(Level.drone.position==drone&&Game.Data.activeSeconds==clock,"Pausa detiene cinemática y cronómetro");
            Game.UI.Play();yield return new WaitForSecondsRealtime(5.5f);yield return Capture("02-intercepcion");
            yield return new WaitForSecondsRealtime(6);yield return Capture("03-dron-juan");
            float timeout=Time.realtimeSinceStartup+12;
            while(Level.IntroRunning&&Time.realtimeSinceStartup<timeout)yield return null;
            Check(Game.Data.introSeen&&!Game.Data.sword&&Game.CanPlay,"Introducción completa deja herramienta por recoger");
            Check(Game.Data.activeSeconds<2,"Introducción y pausa excluidas del tiempo de juego");
            yield return WalkTo(10.4f,true);Check(Game.Player.transform.position.x>10,"Caminar y saltar primer desnivel con entrada del jugador");
            Pulse("Interactuar");yield return new WaitForSeconds(.25f);
            Check(Game.Data.sword&&!Game.Data.dash&&!Game.Data.gun,"Dron entrega espada y mantiene habilidades bloqueadas");
            Check(Game.Store.Records().First().data.sword,"Espada confirmada en autoguardado");
            yield return Capture("04-espada");
            Game.Save(true,"Antes de liberar a Mara");var manual=Game.Store.Records().Single(r=>r.kind=="Manual");
            yield return WalkTo(16.4f,true);yield return Strike(3);
            Check(Game.Data.collected.Contains("B02_BARRIER"),"Espada corta obstáculo con hitbox real");
            yield return WalkTo(25.5f,true);
            var guard=FindObjectsByType<SierraEnemy>(FindObjectsSortMode.None).First(e=>e.persistentId=="MARA_GUARD");
            float guardDeadline=Time.time+15,nextGuardSwing=0;
            while(!guard.Dead&&Game.CanPlay&&Time.time<guardDeadline)
            {
                float dx=guard.transform.position.x-Game.Player.transform.position.x;
                held.Clear();if(Mathf.Abs(dx)>1.35f||Mathf.Sign(dx)!=Game.Player.Facing)held.Add(dx>0?"Derecha":"Izquierda");
                if(Time.time>=nextGuardSwing){Pulse("Atacar");nextGuardSwing=Time.time+.5f;}yield return null;
            }
            held.Clear();Check(guard.Dead&&Level.GuardDefeated,"Guardia derrotado mediante espada");
            if(!Game.CanPlay){Finish();yield break;}
            yield return WalkTo(31.7f,false);yield return Strike(2);
            Check(Game.Data.rescues.Contains("MARA")&&!Level.maraGate.activeSelf,"Cortar jaula rescata a Mara y abre paso");
            Check(!manual.data.rescues.Contains("MARA"),"Rescate no altera el guardado manual anterior");
            yield return Capture("05-mara");
            yield return WalkTo(35.7f,false);Pulse("Interactuar");yield return new WaitForSecondsRealtime(.2f);
            Check(Game.UI.ScreenName=="bank","Banco disponible tras rescatar a Mara");Game.UI.Play();
            yield return WalkTo(38.3f,false);Pulse("Interactuar");yield return new WaitForSeconds(.2f);
            Check(Game.Data.collected.Contains("B01_SUPPLIES")&&Game.Data.gold>=40&&Game.Data.scrap>=10,"Contenedor único entrega presupuesto de recursos");
            var before=Game.Data.gold;var supplies=Level.props.First(p=>p.kind==SierraPropKind.Supplies);Check(!supplies.Use()&&before==Game.Data.gold,"Suministros no se duplican");
            Game.Data.healthQuarters=Game.Data.MaximumHealth;Game.NotifyChanged();
            yield return WalkTo(72.5f,true);Check(Game.Player.transform.position.x>71,"Recorrido de quebrada y huecos transitable sin dash");
            Check(Game.Data.checkpoint=="CP_B04","Punto de control anterior al jefe");
            if(!Game.CanPlay){Finish();yield break;}
            yield return Capture("06-puesto");
            // Start the isolated encounter with three hearts; healing uses only fruit collected on the actual route.
            Game.Data.healthQuarters=Game.Data.MaximumHealth;Game.NotifyChanged();
            yield return WalkTo(78.2f,false);yield return new WaitForSeconds(1.6f);
            Check(Level.boss.Fighting&&Level.boss.leftGate.activeSelf&&Level.boss.rightGate.activeSelf,"Arena se cierra al entrar");
            var safe=Level.SafeSavePosition(Game.Player.transform.position);Check(safe.x==73,"Guardado dentro de arena vuelve a entrada segura");
            yield return Capture("07-rastreador");
            int firstHealth=Level.boss.health;Game.UI.Pause();float bx=Level.boss.transform.position.x;yield return new WaitForSecondsRealtime(.4f);
            Check(Level.boss.health==firstHealth&&Level.boss.transform.position.x==bx,"Pausa congela encuentro de jefe");Game.UI.Play();
            float deadline=Time.realtimeSinceStartup+115;float nextAttack=0,flee=1;string previous="";var patterns=new HashSet<string>();
            while(!Game.Data.dash&&Game.CanPlay&&Time.realtimeSinceStartup<deadline)
            {
                var boss=Level.boss;var player=Game.Player;float dx=boss.transform.position.x-player.transform.position.x;held.Clear();
                if(boss.StateName=="Windup")
                {
                    patterns.Add(boss.Pattern);
                    if(previous!="Windup"&&boss.Pattern=="Salto pesado")flee=player.transform.position.x<80?1:player.transform.position.x>91?-1:dx>=0?-1:1;
                    if(boss.Pattern=="Salto pesado")held.Add(flee>0?"Derecha":"Izquierda");
                    else if(player.EnSuelo&&boss.TimeToStrike+(boss.Pattern=="Impulso"?Mathf.Max(0,Mathf.Abs(dx)-1.25f)/15:0)<.3f)Pulse("Saltar");
                }
                else if(boss.StateName=="Strike")
                {if(boss.Pattern=="Salto pesado")held.Add(flee>0?"Derecha":"Izquierda");else if(player.EnSuelo&&(boss.Pattern!="Impulso"||Mathf.Abs(dx)<5))Pulse("Saltar");}
                else if(Mathf.Abs(dx)>1.45f)held.Add(dx>0?"Derecha":"Izquierda");
                else if(Mathf.Sign(dx)!=player.Facing)held.Add(dx>0?"Derecha":"Izquierda");
                if(player.EnSuelo&&boss.StateName=="Approach"&&Physics2D.Raycast(player.transform.position+Vector3.up*.3f,new Vector2(Mathf.Sign(dx),0),1.1f,1<<3).collider!=null)Pulse("Saltar");
                if(Mathf.Abs(dx)<2.1f&&Time.time>=nextAttack){Pulse("Atacar");nextAttack=Time.time+.5f;}
                if(Game.Data.healthQuarters<=4&&Game.Data.fruit>0)Pulse("Consumible");
                previous=boss.StateName;yield return null;
            }
            held.Clear();Check(Game.Data.dash,"Jefe vencible con espada y provisiones recogidas, sin compras ni mejoras");
            Check(patterns.Contains("Barrido")&&patterns.Contains("Salto pesado")&&patterns.Contains("Impulso"),"Ejecutados tres patrones, impulso en segunda mitad");
            if(!Game.Data.dash){yield return Capture("fallo-combate");Finish();yield break;}
            Check(Game.Data.maxHearts==4&&Game.Data.healthQuarters==16&&Game.Data.defeated.Contains("RASTREADOR"),"Victoria entrega dash, corazón y marca de jefe en una transacción");
            yield return Capture("08-recompensa");
            var victory=Game.Store.Records().First();Game.LoadRecord(victory);yield return WaitScene();yield return new WaitForSeconds(.5f);
            Check(!Level.boss.gameObject.activeSelf&&!Level.boss.leftGate.activeSelf,"Cargar victoria no repite jefe ni bloquea arena");
            yield return WalkTo(103.7f,false);
            held.Add("Derecha");Pulse("Dash");Pulse("Saltar");yield return new WaitForSeconds(1.1f);held.Clear();
            Check(Game.Player.transform.position.x>112,"Dash y salto permiten cruzar puente roto");
            yield return Capture("09-puente");
            yield return WalkTo(121,true);Pulse("Interactuar");yield return new WaitForSecondsRealtime(.25f);
            Check(Game.Data.sierraFinished&&Game.UI.ScreenName=="chapter"&&Game.Data.sierraSeconds>0,"Salida registra tiempo y muestra resultado");
            yield return Capture("10-resultado");
            int hearts=Game.Data.maxHearts;Check(!Game.Data.Grant(CampaignReward.Dash)&&Game.Data.maxHearts==hearts,"Recompensa del jefe idempotente");
            Game.NewCampaign(true);yield return WaitScene();yield return new WaitForSecondsRealtime(.5f);Level.CompleteIntro(true);yield return null;
            Check(Game.Data.introSeen&&Game.Data.sword&&Game.CanPlay,"Omitir introducción entrega herramienta y devuelve control");
            Game.Save(false,"Introducción omitida");var skipped=Game.Store.Records().First();Game.LoadRecord(skipped);yield return WaitScene();yield return new WaitForSeconds(.3f);
            Check(!Level.IntroRunning&&Game.Data.sword,"Continuar no repite introducción");
            int healthBeforeFall=Game.Data.healthQuarters;Game.Player.transform.position=new Vector3(14,-6,0);yield return new WaitForSeconds(.2f);
            Check(Game.Data.healthQuarters==healthBeforeFall-2&&Game.Player.transform.position.y>-1,"Caída recupera superficie segura y resta medio corazón");
            yield return new WaitForSeconds(.8f);Game.Player.Attack();Game.Player.DamageQuarters(Game.Player.transform.position+Vector3.left,1);yield return new WaitForSeconds(.2f);
            Check(!Game.Player.SwingActive,"Recibir daño cancela el golpe pendiente");
            var edgeSafe=Level.SafeSavePosition(new Vector3(12.95f,.01f,0));Check(edgeSafe.x<=12.41f,"Guardado en borde conserva margen de apoyo");
            Finish();
        }
        private IEnumerator WalkTo(float x,bool jump)
        {
            float deadline=Time.realtimeSinceStartup+20;float nextJump=0;
            while(Game.CanPlay&&Mathf.Abs(Game.Player.transform.position.x-x)>.25f&&Time.realtimeSinceStartup<deadline)
            {
                float px=Game.Player.transform.position.x;held.Clear();held.Add(px<x?"Derecha":"Izquierda");
                if(jump&&Game.Player.EnSuelo&&Time.time>=nextJump)
                {
                    // Probe obstacles and gaps; all actual movement still goes through the player's input and physics.
                    float direction=px<x?1:-1;var origin=Game.Player.transform.position;
                    bool wall=Physics2D.Raycast(origin+Vector3.up*.6f,Vector2.right*direction,1.05f,1<<3).collider!=null||Physics2D.Raycast(origin+Vector3.up*1.1f,Vector2.right*direction,1.05f,1<<3).collider!=null;
                    bool gap=Physics2D.Raycast(origin+new Vector3(direction*1.05f,.2f),Vector2.down,1.4f,1<<3).collider==null;
                    bool edge=wall||gap;
                    if(edge){Pulse("Saltar");nextJump=Time.time+.45f;}
                }
                if(Game.Data.sword&&px>24&&px<71)
                {
                    var enemy=FindObjectsByType<SierraEnemy>(FindObjectsSortMode.None).Where(e=>!e.Dead&&Mathf.Abs(e.transform.position.y-Game.Player.transform.position.y)<2.2f).OrderBy(e=>Mathf.Abs(e.transform.position.x-px)).FirstOrDefault();
                    if(enemy!=null&&Mathf.Abs(enemy.transform.position.x-px)<2.1f)
                    {
                        float dx=enemy.transform.position.x-px;held.Clear();
                        if(Mathf.Abs(dx)>1.35f||Mathf.Sign(dx)!=Game.Player.Facing)held.Add(dx>0?"Derecha":"Izquierda");
                        Pulse("Atacar");
                    }
                }
                yield return null;
            }
            held.Clear();yield return new WaitForSecondsRealtime(.1f);
        }
        private IEnumerator Strike(int count){for(int i=0;i<count;i++){Pulse("Atacar");yield return new WaitForSeconds(.52f);}}
        private IEnumerator WaitScene(){yield return null;float end=Time.realtimeSinceStartup+15;while(Game.Busy&&Time.realtimeSinceStartup<end)yield return null;yield return null;}
        private IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.15f);}
        private void OnLog(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)report.failed.Add(message+"\n"+trace);}
        private void Update()
        {
            if(ending||deadlineAll<=0)return;
            if(Time.realtimeSinceStartup>=nextSnapshot&&Game?.Player!=null)
            {
                nextSnapshot=Time.realtimeSinceStartup+3;var p=Game.Player.transform.position;
                File.WriteAllText(Path.Combine(output,"estado.json"),JsonUtility.ToJson(new Snapshot{screen=Game.UI.ScreenName,x=p.x,y=p.y,hearts=Game.Data.healthQuarters,boss=Level.boss.StateName,bossHealth=Level.boss.health}));
            }
            if(Game?.UI.ScreenName=="death"){ending=true;Check(false,"Derrota durante recorrido de prueba");StartCoroutine(EndAfterCapture());}
            else if(Time.realtimeSinceStartup>deadlineAll){deadlineAll=0;Check(false,"Tiempo máximo de prueba agotado: "+Game.UI.ScreenName);Finish();}
        }
        private IEnumerator EndAfterCapture(){yield return Capture("fallo-derrota");Finish();}
        private void Finish()
        {
            held.Clear();GameInput.ValidationDown=GameInput.ValidationHeld=null;
            File.WriteAllText(Path.Combine(output,"validacion-jugable.json"),JsonUtility.ToJson(report,true));
            Debug.Log("SIERRA PLAY RESULT: "+report.passed.Count+" PASS, "+report.failed.Count+" FAIL");
            Application.logMessageReceived-=OnLog;Application.Quit(report.failed.Count==0?0:1);
        }
    }
}
#endif
