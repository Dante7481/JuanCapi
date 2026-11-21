#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace JuanCapi
{
    public sealed class SwampPlayValidation:MonoBehaviour
    {
        [Serializable] private class Report{public List<string> passed=new List<string>();public List<string> failed=new List<string>();}
        [Serializable] private class Snapshot{public string screen,scene,boss,step;public float x,y,reload;public int hearts,bossHealth,ammo;}
        private Report report=new Report();private string output,step;private float deadline,nextSnapshot;private bool ending;
        private Vector3 pointer;
        private readonly HashSet<string> held=new HashSet<string>();private readonly Dictionary<string,int> pressed=new Dictionary<string,int>();
        private GameRuntime Game=>GameRuntime.Instance;private SwampLevel Level=>SwampLevel.Current;
        private void Check(bool ok,string name){(ok?report.passed:report.failed).Add(name);Debug.Log("SWAMP "+(ok?"PASS ":"FAIL ")+name);}
        private void Pulse(string action){pressed[action]=Time.frameCount+1;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-juanSwampValidate");if(i<0||i+1>=args.Length||!args.Contains("-juanDataPath"))return;
            var go=new GameObject("Pruebas aisladas del pantano");DontDestroyOnLoad(go);go.AddComponent<SwampPlayValidation>().output=Path.GetFullPath(args[i+1]);
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;Directory.CreateDirectory(output);deadline=Time.realtimeSinceStartup+600;Application.logMessageReceived+=OnLog;
            GameInput.ValidationHeld=a=>held.Contains(a);GameInput.ValidationDown=a=>pressed.TryGetValue(a,out int f)&&f==Time.frameCount;GameInput.ValidationPointer=()=>pointer;
            yield return new WaitForSecondsRealtime(.8f);Check(Game.UI.ScreenName=="menu","Arranque en menú");
            // Fixture is the already completed first chapter. From here all progression uses actual interactions and combat.
            var seed=new CampaignData{sword=true,dash=true,introSeen=true,sierraLayout=5,sierraFinished=true,sierraSeconds=180,maxHearts=4,healthQuarters=16,fruit=2,gold=50,scrap=10,activeSeconds=180,checkpoint="CP_B06",hasPosition=true,x=122,y=.08f};
            seed.rescues.Add("MARA");seed.defeated.Add("RASTREADOR");
            Check(Game.Store.StartNew(seed,true,out _),"Campaña aislada al finalizar la sierra");Game.LoadRecord(Game.Store.Records()[0]);yield return WaitScene();yield return new WaitForSeconds(.25f);
            Check(Game.Data.checkpoint=="CP_B06"&&Game.Store.Records()[0].data.checkpoint=="CP_B06","Cargar victoria de sierra no reactiva un checkpoint anterior");
            SierraLevel.Current.FinishLevel();yield return null;
            var continueButton=FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name=="Ir al pantano");
            Check(continueButton!=null,"Resultado de sierra ofrece continuidad al pantano");continueButton?.onClick.Invoke();yield return WaitScene();yield return new WaitForSeconds(.3f);
            Check(Level!=null&&!Level.aquatic&&Game.Data.sword&&Game.Data.dash&&!Game.Data.gun,"Viaje conserva espada y dash; disparador bloqueado");
            Check(Game.Store.Records()[0].data.scene=="AmazoniaTerrestre","Entrada de pantano guardada antes de cargar");
            step="recorrido terrestre";yield return Capture("01-orilla");
            yield return Walk(15.4f);Pulse("Interactuar");yield return new WaitForSeconds(.25f);Check(Game.Data.collected.Contains("P01_SUPPLY"),"Contenedor terrestre se recoge una sola vez");
            yield return Walk(59);Check(Game.Player.transform.position.x>55.3f,"Dos huecos de orilla cruzados con salto y dash");
            yield return Walk(73.5f);held.Add("Atacar");yield return new WaitForSeconds(1.1f);held.Clear();
            Check(Game.Data.collected.Contains("P03_CABLE"),"Espada corta cable y abre paso al caimán");
            yield return Walk(79);Check(Game.Data.checkpoint=="CP_P04","Autoguardado antes del primer encuentro");
            // Encounter begins with the actual inventory earned on the route, with a fresh full-heart checkpoint fixture.
            Game.Data.healthQuarters=Game.Data.MaximumHealth;Game.NotifyChanged();
            yield return Walk(83);yield return new WaitForSeconds(1.3f);Check(Level.boss.Fighting,"Caimán terrestre activa y cierra arena");
            Check(Level.SafeSavePosition(Game.Player.transform.position).x==79,"Guardado durante jefe regresa fuera de la arena");
            yield return Capture("02-caiman-terrestre");
            var bossPos=Level.boss.transform.position;int bossHealth=Level.boss.health;Game.UI.Pause();yield return new WaitForSecondsRealtime(.4f);
            Check(Level.boss.transform.position==bossPos&&Level.boss.health==bossHealth,"Pausa congela jefe terrestre");Game.UI.Play();
            step="caimán terrestre";float end=Time.realtimeSinceStartup+100;var patterns=new HashSet<string>();
            while(!Game.Data.defeated.Contains("CAIMAN_LAND")&&Game.CanPlay&&Time.realtimeSinceStartup<end)
            {
                held.Clear();var b=Level.boss;var p=Game.Player;float dx=b.transform.position.x-p.transform.position.x;
                if(b.StateName=="Telegraph")
                {
                    patterns.Add(b.Pattern);
                    if(p.EnSuelo&&b.TimeToStrike<.3f)Pulse("Saltar");
                }
                if(b.StateName=="Attack"&&p.EnSuelo)Pulse("Saltar");
                if(Mathf.Abs(dx)>1.35f||Mathf.Sign(dx)!=p.Facing)held.Add(dx>0?"Derecha":"Izquierda");
                if(p.EnSuelo&&Physics2D.Raycast(p.transform.position+Vector3.up*.3f,new Vector2(Mathf.Sign(dx),0),1,1<<3))Pulse("Saltar");
                if(Mathf.Abs(dx)<2.7f)held.Add("Atacar");
                if(Game.Data.healthQuarters<=6&&Game.Data.fruit>0)Pulse("Consumible");yield return null;
            }
            held.Clear();Check(Game.Data.defeated.Contains("CAIMAN_LAND"),"Caimán terrestre derrotado exclusivamente con espada");
            Check(patterns.Count==3,"Garra, empujón y piedra observados");
            if(!Game.CanPlay||!Game.Data.defeated.Contains("CAIMAN_LAND")){Finish();yield break;}
            Check(!Game.Data.gun&&!Game.Cinematic&&FindObjectsByType<UnityEngine.Video.VideoPlayer>(FindObjectsSortMode.None).Length==0,"Huida mantiene control sin video y aún exige recoger arma");
            yield return Walk(105);Pulse("Interactuar");yield return new WaitForSeconds(.3f);
            Check(Game.Data.gun&&Game.Data.ammunition==3&&Game.Data.equippedWeapon=="gun","Condensador entrega disparador con tres cargas");
            Check(Game.Store.Records()[0].data.gun,"Arma confirmada en guardado persistente");
            step="práctica de puntería";yield return Walk(111);held.Clear();yield return new WaitForSeconds(.2f);
            foreach(var target in Level.props.Where(p=>p.kind==SwampPropKind.Target))yield return ShootTarget(target);
            Check(SwampLevel.TargetsComplete(Game.Data),"Mouse alcanza tres blancos a distintas alturas");
            yield return Capture("03-disparador");
            pointer=Game.Player.Center+Vector2.left*8;held.Add("Atacar");end=Time.realtimeSinceStartup+9;
            while(Game.Data.reloadRemaining<=0&&Time.realtimeSinceStartup<end)yield return null;held.Clear();
            Check(Game.Data.ammunition==0&&Game.Data.reloadRemaining>0,"Tres disparos agotan cargador e inician recarga");
            float reload=Game.Data.reloadRemaining;Game.UI.Inventory();yield return new WaitForSecondsRealtime(.5f);
            Check(Game.Data.reloadRemaining==reload,"Inventario pausa recarga");Game.UI.Play();yield return null;held.Clear();
            Game.Data.equippedWeapon="sword";yield return new WaitForSeconds(.3f);Check(Game.Data.reloadRemaining<reload&&Game.Data.reloadRemaining>0,"Cambiar a espada no reinicia recarga");
            Game.Data.equippedWeapon="gun";yield return new WaitForSeconds(Game.Data.reloadRemaining+.15f);Check(Game.Data.ammunition==3&&Game.Data.reloadRemaining==0,"Recarga termina automáticamente");
            int ammunition=Game.Data.ammunition;Check(!Game.Player.Combat.TryFire(new Vector3(float.NaN,1))&&ammunition==Game.Data.ammunition,"Puntería no finita no consume munición");
            yield return Walk(124);held.Clear();pointer=Game.Player.Center+Vector2.left*8;held.Add("Atacar");yield return new WaitForSeconds(.2f);held.Clear();
            int beforeWaterAmmo=Game.Data.ammunition,beforeWaterHealth=Game.Data.healthQuarters;Pulse("Interactuar");yield return WaitScene();yield return new WaitForSeconds(.3f);
            Check(Level.aquatic&&Game.Player.aquatic&&Game.Data.ammunition==beforeWaterAmmo&&Game.Data.healthQuarters==beforeWaterHealth,"Entrada acuática conserva estado de la sesión");
            var entry=Game.Store.Records().First(r=>r.data.scene=="AmazoniaAcuatica");
            Check(entry.data.ammunition==3&&entry.data.healthQuarters==entry.data.MaximumHealth,"Checkpoint acuático deja recuperación completa al reintentar");
            step="recorrido acuático";yield return Capture("04-inmersion");
            float y=Game.Player.transform.position.y;held.Add("Nadar arriba");yield return new WaitForSeconds(.4f);held.Clear();Check(Game.Player.transform.position.y>y+1,"Nado vertical con controles de jugador");
            int sequence=Game.Player.AttackSequence;Pulse("Saltar");Pulse("Dash");Game.Player.Attack();yield return new WaitForSeconds(.2f);
            Check(!Game.Player.IsDashing&&Game.Player.AttackSequence==sequence,"Agua desactiva salto, dash y espada");
            held.Add("Derecha");held.Add("Nadar arriba");yield return new WaitForSeconds(.55f);
            var swimVelocity=Game.Player.GetComponent<Rigidbody2D>().linearVelocity-Vector2.down*.12f;
            Check(swimVelocity.magnitude<=5.31f&&swimVelocity.magnitude>5.1f,"Diagonal acuática normalizada sin aumento de velocidad");held.Clear();
            yield return Swim(new Vector2(27,11.5f));yield return Swim(new Vector2(29,12.3f));
            var left=Level.props.First(p=>p.persistentId=="A03_LATCH_1");yield return ShootTarget(left);
            yield return Swim(new Vector2(38.5f,14));var right=Level.props.First(p=>p.persistentId=="A03_LATCH_2");yield return ShootTarget(right);
            yield return Swim(new Vector2(34,12));Pulse("Interactuar");yield return new WaitForSeconds(.3f);
            Check(Game.Data.rescues.Contains("TICO")&&!Level.rescueGate.activeSelf,"Dos cierres y válvula rescatan a Tico y abren compuerta");
            yield return Capture("05-tico-campana");
            yield return Swim(new Vector2(40,10));yield return Swim(new Vector2(60,9));yield return Swim(new Vector2(64,9));
            Check(Game.Data.checkpoint=="CP_A04","Autoguardado seguro antes de caimán acuático");
            // Test encounter starts from the same replenishment used by Retry at this checkpoint.
            Game.Data.healthQuarters=Game.Data.MaximumHealth;Game.Data.ammunition=3;Game.Data.reloadRemaining=0;Game.NotifyChanged();
            yield return Swim(new Vector2(67,9));yield return new WaitForSeconds(1.3f);Check(Level.boss.Fighting,"Encuentro acuático solo después de rescatar a Tico");
            yield return Capture("06-caiman-acuatico");step="caimán acuático";end=Time.realtimeSinceStartup+160;bool enraged=false;float moveAngle=0;
            while(!Game.Data.defeated.Contains("CAIMAN_WATER")&&Game.CanPlay&&Time.realtimeSinceStartup<end)
            {
                held.Clear();var b=Level.boss;pointer=b.transform.position;held.Add("Atacar");if(b.health<=12)enraged=true;
                // Circle through open middle water. Telegraph locks its direction before each charge.
                moveAngle+=Time.deltaTime*.55f;
                Vector2 target=new Vector2(78+Mathf.Cos(moveAngle)*8,8.5f+Mathf.Sin(moveAngle)*4);
                if(b.StateName=="Telegraph"&&b.TimeToStrike<.24f||b.StateName=="Attack")
                {
                    var normal=new Vector2(-b.ChargeDirection.y,b.ChargeDirection.x);var pos=Game.Player.Center;
                    if(pos.y+normal.y*2>15||pos.y+normal.y*2<3||pos.x+normal.x*2>88||pos.x+normal.x*2<68)normal=-normal;
                    target=pos+normal*3;
                }
                MoveToward(target,.2f);
                if(Game.Data.healthQuarters<=6&&Game.Data.fruit>0)Pulse("Consumible");yield return null;
            }
            held.Clear();Check(Game.Data.defeated.Contains("CAIMAN_WATER")&&enraged,"Caimán acuático derrotado con plasma y fase enfurecida observada");
            if(!Game.CanPlay||!Game.Data.defeated.Contains("CAIMAN_WATER")){Finish();yield break;}
            int hearts=Game.Data.maxHearts;yield return Swim(new Vector2(93,7.3f));Pulse("Interactuar");yield return new WaitForSeconds(.3f);
            Check(Game.Data.shield&&Game.Data.maxHearts==hearts+1&&Game.Data.cells>0,"Abrazadera entrega escudo, contenedor y celda");
            var shield=Level.props.First(p=>p.kind==SwampPropKind.Shield);Check(!shield.Use()&&Game.Data.maxHearts==hearts+1,"Recompensa no se duplica");
            step="escudo y reflexión";yield return Swim(new Vector2(98,8.22f));held.Clear();pointer=new Vector3(103,9);yield return new WaitForSeconds(.2f);
            int hp=Game.Data.healthQuarters;held.Add("Escudo");yield return new WaitForSeconds(.3f);
            var shot=CampaignProjectile.Launch(Game.Player.Center+Vector2.right*1.2f,Vector2.left*4,true,false,false,null,2);yield return new WaitForSeconds(.45f);held.Clear();
            Check(Game.Data.healthQuarters==hp&&Game.Data.shieldEnergy<75,"Escudo frontal bloquea y consume energía");
            yield return new WaitForSeconds(2.5f);float energy=Game.Data.shieldEnergy;Game.UI.Pause();yield return new WaitForSecondsRealtime(.3f);Check(Game.Data.shieldEnergy==energy,"Pausa congela recuperación del escudo");Game.UI.Play();
            end=Time.realtimeSinceStartup+18;
            while(!Game.Data.collected.Contains("A06_REFLECTED")&&Time.realtimeSinceStartup<end&&Game.CanPlay)
            {
                held.Clear();pointer=new Vector3(103,9);MoveToward(new Vector2(98,9),.12f);
                var incoming=FindObjectsByType<CampaignProjectile>(FindObjectsSortMode.None).Where(s=>s.hostile&&s.reflectable).OrderBy(s=>Vector2.Distance(s.transform.position,Game.Player.Center)).FirstOrDefault();
                if(incoming!=null&&Vector2.Distance(incoming.transform.position,Game.Player.Center)<1.32f)held.Add("Escudo");yield return null;
            }
            held.Clear();Check(Game.Data.collected.Contains("A06_REFLECTED")&&Level.Reflections>0,"Bloqueo preciso devuelve proyectil al receptor real");yield return Capture("07-escudo");
            yield return new WaitForSeconds(5);pointer=Game.Player.Center+Vector2.right*6;held.Add("Escudo");yield return new WaitForSeconds(.2f);
            int rearHealth=Game.Data.healthQuarters;CampaignProjectile.Launch(Game.Player.Center+Vector2.left*1.4f,Vector2.right*4,true,false,false,null,2);yield return new WaitForSeconds(.5f);
            Check(Game.Data.healthQuarters==rearHealth-2,"Impacto por la espalda atraviesa el arco frontal");held.Clear();yield return new WaitForSeconds(5);
            held.Add("Escudo");float breakDeadline=Time.realtimeSinceStartup+6;
            while(Game.Player.Combat.ShieldRecovery<=0&&Time.realtimeSinceStartup<breakDeadline)yield return null;
            Check(Game.Player.Combat.ShieldRecovery>1&&Game.Data.shieldEnergy==0&&!Game.Player.Combat.Blocking,"Agotar energía rompe el escudo y bloquea reactivación");held.Clear();yield return new WaitForSeconds(2.2f);
            Check(Game.Data.shieldEnergy>25&&Game.Player.Combat.ShieldRecovery==0,"Escudo recupera energía y sale de ruptura");
            held.Add("Escudo");yield return new WaitForSeconds(.1f);Game.Data.shieldEnergy=.005f;yield return new WaitForSeconds(.1f);
            Check(Game.Player.Combat.ShieldRecovery>1&&Game.Data.shieldEnergy==0&&!Game.Player.Combat.Blocking,"Energía residual también activa ruptura al consumirse");held.Clear();
            yield return Swim(new Vector2(107,8.3f));Pulse("Interactuar");yield return new WaitForSecondsRealtime(.3f);
            Check(Game.Data.swampFinished&&Game.UI.ScreenName=="chapter"&&Game.Data.swampSeconds>0,"Salida registra tiempo y muestra resultado de capítulo");yield return Capture("08-pantano-liberado");
            var saved=Game.Store.Records().First();Game.LoadRecord(saved);yield return WaitScene();yield return new WaitForSeconds(.2f);
            Check(Game.Data.swampFinished&&Game.Data.shield&&Game.Data.rescues.Contains("TICO")&&!Level.boss.gameObject.activeSelf,"Reanudar conserva victoria, equipo y rescate sin repetir jefe");
            Check(Game.Store.Records()[0].id==saved.id&&Game.Data.checkpoint=="CP_A06","Reanudar victoria no crea autoguardado ni retrocede checkpoint");
            Finish();
        }
        private IEnumerator Walk(float target)
        {
            float end=Time.realtimeSinceStartup+35;while(Game.CanPlay&&Mathf.Abs(Game.Player.transform.position.x-target)>.25f&&Time.realtimeSinceStartup<end)
            {
                held.Clear();var p=Game.Player;float dir=Mathf.Sign(target-p.transform.position.x);held.Add(dir>0?"Derecha":"Izquierda");
                float x=p.transform.position.x;
                if(p.EnSuelo)
                {
                    bool gap=dir>0&&(x>20.2f&&x<21.1f||x>46.2f&&x<47.1f);
                    if(gap){Pulse("Saltar");Pulse("Dash");}
                    else if(Physics2D.Raycast(p.transform.position+Vector3.up*.3f,new Vector2(dir,0),1.1f,1<<3))Pulse("Saltar");
                }
                if(!Game.Data.gun)held.Add("Atacar");if(Game.Data.healthQuarters<=6&&Game.Data.fruit>0)Pulse("Consumible");yield return null;
            }
            held.Clear();yield return new WaitForSeconds(.15f);
        }
        private IEnumerator Swim(Vector2 target)
        {
            float end=Time.realtimeSinceStartup+25;while(Game.CanPlay&&Vector2.Distance(Game.Player.Center,target)>.3f&&Time.realtimeSinceStartup<end)
            {
                held.Clear();MoveToward(target,.14f);
                var enemy=FindObjectsByType<SwampEnemy>(FindObjectsSortMode.None).Where(e=>!e.Dead).OrderBy(e=>Vector2.Distance(e.transform.position,Game.Player.Center)).FirstOrDefault();
                if(enemy!=null&&Vector2.Distance(enemy.transform.position,Game.Player.Center)<10){pointer=enemy.transform.position;held.Add("Atacar");}
                if(Game.Data.healthQuarters<=6&&Game.Data.fruit>0)Pulse("Consumible");yield return null;
            }
            held.Clear();yield return new WaitForSeconds(.1f);
        }
        private void MoveToward(Vector2 point,float tolerance)
        {var delta=point-Game.Player.Center;if(Mathf.Abs(delta.x)>tolerance)held.Add(delta.x>0?"Derecha":"Izquierda");if(Mathf.Abs(delta.y)>tolerance)held.Add(delta.y>0?"Nadar arriba":"Nadar abajo");}
        private IEnumerator ShootTarget(SwampProp p)
        {float end=Time.realtimeSinceStartup+12;held.Clear();pointer=p.transform.position;held.Add("Atacar");while(!Game.Data.collected.Contains(p.persistentId)&&Game.CanPlay&&Time.realtimeSinceStartup<end)yield return null;held.Clear();yield return new WaitForSeconds(.15f);}
        private IEnumerator WaitScene(){yield return null;float end=Time.realtimeSinceStartup+20;while(Game.Busy&&Time.realtimeSinceStartup<end)yield return null;yield return null;}
        private IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.15f);}
        private void OnLog(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)report.failed.Add(message+"\n"+trace);}
        private void Update()
        {
            if(ending||deadline<=0)return;
            if(Time.realtimeSinceStartup>nextSnapshot&&Game?.Player!=null&&Game.Data!=null)
            {nextSnapshot=Time.realtimeSinceStartup+3;var p=Game.Player.transform.position;File.WriteAllText(Path.Combine(output,"estado.json"),JsonUtility.ToJson(new Snapshot{screen=Game.UI.ScreenName,scene=Game.Data.scene,step=step,x=p.x,y=p.y,hearts=Game.Data.healthQuarters,ammo=Game.Data.ammunition,reload=Game.Data.reloadRemaining,boss=Level?.boss.StateName,bossHealth=Level!=null?Level.boss.health:0}));}
            if(Game?.UI.ScreenName=="death"){Check(false,"Derrota durante "+step);ending=true;StartCoroutine(EndCapture());}
            else if(Time.realtimeSinceStartup>deadline){Check(false,"Tiempo máximo agotado: "+step);Finish();}
        }
        private IEnumerator EndCapture(){yield return Capture("fallo-derrota");Finish();}
        private void Finish(){ending=true;held.Clear();GameInput.ValidationDown=GameInput.ValidationHeld=null;GameInput.ValidationPointer=null;File.WriteAllText(Path.Combine(output,"validacion-jugable.json"),JsonUtility.ToJson(report,true));Debug.Log("SWAMP PLAY RESULT: "+report.passed.Count+" PASS, "+report.failed.Count+" FAIL");Application.logMessageReceived-=OnLog;Application.Quit(report.failed.Count==0?0:1);}
    }
}
#endif
