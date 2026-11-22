#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;using System.Collections;using System.Collections.Generic;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.UI;
namespace JuanCapi
{
    public sealed class JourneyValidation:MonoBehaviour
    {
        [Serializable] private class Report{public List<string> passed=new List<string>();public List<string> failed=new List<string>();public double seconds;public int deaths;}
        private Report report=new Report();private string output,step,campaignBytes;private int trial;private float deadline,nextSnapshot;private bool ending;private Vector3 pointer;
        private HashSet<string> held=new HashSet<string>();private Dictionary<string,int> pressed=new Dictionary<string,int>();
        private GameRuntime Game=>GameRuntime.Instance;
        private void Pulse(string action){pressed[action]=Time.frameCount+1;}
        private void Check(bool ok,string label){(ok?report.passed:report.failed).Add(label);Debug.Log("JOURNEY "+(ok?"PASS ":"FAIL ")+label);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Boot()
        {var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"-juanJourneyValidate");if(i<0||i+1>=a.Length||!a.Contains("-juanDataPath"))return;var go=new GameObject("Recorrido integral aislado");DontDestroyOnLoad(go);var v=go.AddComponent<JourneyValidation>();v.output=Path.GetFullPath(a[i+1]);int j=Array.IndexOf(a,"-juanTrialChapter");if(j>=0&&j+1<a.Length)int.TryParse(a[j+1],out v.trial);}
        private IEnumerator Start()
        {
            Application.runInBackground=true;Directory.CreateDirectory(output);deadline=Time.realtimeSinceStartup+1300;Application.logMessageReceived+=Log;
            GameInput.ValidationHeld=a=>held.Contains(a);GameInput.ValidationDown=a=>pressed.TryGetValue(a,out int f)&&f==Time.frameCount;GameInput.ValidationPointer=()=>pointer;
            yield return new WaitForSecondsRealtime(.8f);
            if(trial>0){var seed=new CampaignData{sword=true,dash=true,gun=true,shield=true,sierraFinished=true,swampFinished=true,completed=true};Game.Store.StartNew(seed,true,out _);campaignBytes=File.ReadAllText(Game.Store.FilePath);Game.StartTrial(trial);}
            else Game.NewCampaign(true);
            yield return WaitScene();yield return new WaitForSeconds(.3f);
            if(trial==0){while(SierraLevel.Current.IntroRunning)yield return null;}
            if(trial==0||trial==1){step="Sierra";yield return Route1();Check(Game.Data.sierraFinished,"Sierra finalizada mediante controles normales");if(!Game.Data.sierraFinished){Finish();yield break;}if(trial==0){Click("Ir al pantano");yield return WaitScene();yield return new WaitForSeconds(.3f);}}
            if(trial==0||trial==2){step="Pantano";yield return Route2();Check(Game.Data.swampFinished,"Pantano terrestre y acuático finalizado");if(!Game.Data.swampFinished){Finish();yield break;}if(trial==0){Click("Ir a MinerTown");yield return WaitScene();yield return new WaitForSeconds(.3f);}}
            if(trial==0||trial==3){step="MinerTown";yield return Route3();Check(trial>0?Game.TrialFinished:Game.Data.epilogueSeen,"Director y cierre final completados");}
            Finish();
        }
        private void Click(string name){var b=FindObjectsByType<Button>().FirstOrDefault(x=>x.name==name&&x.interactable);Check(b!=null,"Transición disponible: "+name);b?.onClick.Invoke();}
        private IEnumerator WaitScene(){yield return null;float end=Time.realtimeSinceStartup+20;while(Game.Busy&&Time.realtimeSinceStartup<end)yield return null;yield return null;}
        private IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.1f);}
        private void Log(string m,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)report.failed.Add(m+"\n"+stack);}
        private void Update()
        {
            if(ending||deadline<=0)return;
            if(Time.realtimeSinceStartup>nextSnapshot&&Game?.Player!=null){nextSnapshot=Time.realtimeSinceStartup+3;File.WriteAllText(Path.Combine(output,"estado.txt"),step+" | "+Game.UI.ScreenName+" | "+Game.Player.transform.position+" | HP "+Game.Data.healthQuarters+" | "+Game.Data.activeSeconds);}
            if(Game?.UI.ScreenName=="death"){Check(false,"Derrota en recorrido sin restauración artificial de vida: "+step);Finish();}
            else if(Time.realtimeSinceStartup>deadline){Check(false,"Tiempo máximo: "+step);Finish();}
        }
        private void Finish()
        {
            if(ending)return;ending=true;held.Clear();GameInput.ValidationHeld=GameInput.ValidationDown=null;GameInput.ValidationPointer=null;
            Check(trial>0?Game.TrialFinished:Game.Data?.completed==true,"Recorrido alcanza su condición final");
            if(trial>0){Check(File.ReadAllText(Game.Store.FilePath)==campaignBytes,"Campaña permanece idéntica durante recorrido real");Check(Game.Data.fruit==0&&!Game.Data.fastReload&&Game.Data.maxHearts==5,"Equipo normalizado durante recorrido completo");}
            report.seconds=Game.Data?.activeSeconds??0;report.deaths=Game.Data?.deaths??0;
            File.WriteAllText(Path.Combine(output,"validacion-jugable.json"),JsonUtility.ToJson(report,true));Debug.Log("JOURNEY RESULT: "+report.passed.Count+" PASS, "+report.failed.Count+" FAIL");Application.logMessageReceived-=Log;Application.Quit(report.failed.Count==0?0:1);
        }
        private IEnumerator Route1()
        {
            yield return R1WalkTo(10.4f,true);
            Pulse("Interactuar");yield return new WaitForSeconds(.25f);


            yield return Capture("04-espada");
            yield return R1WalkTo(16.4f,true);yield return R1Strike(3);

            yield return R1WalkTo(25.5f,true);
            var guard=FindObjectsByType<SierraEnemy>(FindObjectsSortMode.None).First(e=>e.persistentId=="MARA_GUARD");
            float guardDeadline=Time.time+15,nextGuardSwing=0;
            while(!guard.Dead&&Game.CanPlay&&Time.time<guardDeadline)
            {
                float dx=guard.transform.position.x-Game.Player.transform.position.x;
                held.Clear();if(Mathf.Abs(dx)>1.35f||Mathf.Sign(dx)!=Game.Player.Facing)held.Add(dx>0?"Derecha":"Izquierda");
                if(Time.time>=nextGuardSwing){Pulse("Atacar");nextGuardSwing=Time.time+.5f;}yield return null;
            }
            held.Clear();
            if(!Game.CanPlay){Finish();yield break;}
            yield return R1WalkTo(31.7f,false);yield return R1Strike(2);


            yield return Capture("05-mara");
            yield return R1WalkTo(35.7f,false);Pulse("Interactuar");yield return new WaitForSecondsRealtime(.2f);
            Game.UI.Play();
            yield return R1WalkTo(38.3f,false);Pulse("Interactuar");yield return new WaitForSeconds(.2f);

            var before=Game.Data.gold;var supplies=SierraLevel.Current.props.First(p=>p.kind==SierraPropKind.Supplies);

            yield return R1WalkTo(72.5f,true);

            if(!Game.CanPlay){Finish();yield break;}
            yield return Capture("06-puesto");

            yield return R1WalkTo(78.2f,false);yield return new WaitForSeconds(1.6f);

            var safe=SierraLevel.Current.SafeSavePosition(Game.Player.transform.position);
            yield return Capture("07-rastreador");
            int firstHealth=SierraLevel.Current.boss.health;Game.UI.Pause();float bx=SierraLevel.Current.boss.transform.position.x;yield return new WaitForSecondsRealtime(.4f);
            Game.UI.Play();
            float deadline=Time.realtimeSinceStartup+115;float nextAttack=0,flee=1;string previous="";var patterns=new HashSet<string>();
            while(!Game.Data.dash&&Game.CanPlay&&Time.realtimeSinceStartup<deadline)
            {
                var boss=SierraLevel.Current.boss;var player=Game.Player;float dx=boss.transform.position.x-player.transform.position.x;held.Clear();
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
            held.Clear();

            if(!Game.Data.dash){yield return Capture("fallo-combate");Finish();yield break;}

            yield return Capture("08-recompensa");
            yield return R1WalkTo(99,true);
            while(Game.CanPlay&&!Game.Player.EnSuelo)yield return null;
            yield return R1WalkTo(103.1f,false);
            held.Add("Derecha");Pulse("Dash");Pulse("Saltar");yield return new WaitForSeconds(1.1f);held.Clear();

            yield return Capture("09-puente");
            yield return R1WalkTo(121,true);Pulse("Interactuar");yield return new WaitForSecondsRealtime(.25f);

            yield return Capture("10-resultado");

        }
        private IEnumerator R1WalkTo(float x,bool jump)
        {
            float deadline=Time.realtimeSinceStartup+20;float nextJump=0;
            while(Game.CanPlay&&Mathf.Abs(Game.Player.transform.position.x-x)>.25f&&Time.realtimeSinceStartup<deadline)
            {
                float px=Game.Player.transform.position.x;held.Clear();held.Add(px<x?"Derecha":"Izquierda");
                if(jump&&Game.Player.EnSuelo&&Time.time>=nextJump)
                {
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
        private IEnumerator R1Strike(int count){for(int i=0;i<count;i++){Pulse("Atacar");yield return new WaitForSeconds(.52f);}}
        private IEnumerator Route2()
        {
            step="recorrido terrestre";yield return Capture("01-orilla");
            yield return R2Walk(15.4f);Pulse("Interactuar");yield return new WaitForSeconds(.25f);
            yield return R2Walk(59);
            yield return R2Walk(73.5f);held.Add("Atacar");yield return new WaitForSeconds(1.1f);held.Clear();

            yield return R2Walk(79);

            yield return R2Walk(83);yield return new WaitForSeconds(1.3f);

            yield return Capture("02-caiman-terrestre");
            var bossPos=SwampLevel.Current.boss.transform.position;int bossHealth=SwampLevel.Current.boss.health;Game.UI.Pause();yield return new WaitForSecondsRealtime(.4f);
            Game.UI.Play();
            step="caimán terrestre";float end=Time.realtimeSinceStartup+100;var patterns=new HashSet<string>();
            while(!Game.Data.defeated.Contains("CAIMAN_LAND")&&Game.CanPlay&&Time.realtimeSinceStartup<end)
            {
                held.Clear();var b=SwampLevel.Current.boss;var p=Game.Player;float dx=b.transform.position.x-p.transform.position.x;
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
            held.Clear();

            if(!Game.CanPlay||!Game.Data.defeated.Contains("CAIMAN_LAND")){Finish();yield break;}

            yield return R2Walk(105);Pulse("Interactuar");yield return new WaitForSeconds(.3f);


            step="práctica de puntería";yield return R2Walk(111);held.Clear();yield return new WaitForSeconds(.2f);
            foreach(var target in SwampLevel.Current.props.Where(p=>p.kind==SwampPropKind.Target))yield return R2ShootTarget(target);

            yield return Capture("03-disparador");
            yield return R2Walk(124);held.Clear();pointer=Game.Player.Center+Vector2.left*8;held.Add("Atacar");yield return new WaitForSeconds(.2f);held.Clear();
            int beforeWaterAmmo=Game.Data.ammunition,beforeWaterHealth=Game.Data.healthQuarters;Pulse("Interactuar");yield return WaitScene();yield return new WaitForSeconds(.3f);


            step="recorrido acuático";yield return Capture("04-inmersion");
            float y=Game.Player.transform.position.y;held.Add("Nadar arriba");yield return new WaitForSeconds(.4f);held.Clear();
            int sequence=Game.Player.AttackSequence;Pulse("Saltar");Pulse("Dash");Game.Player.Attack();yield return new WaitForSeconds(.2f);

            held.Add("Derecha");held.Add("Nadar arriba");yield return new WaitForSeconds(.55f);
            var swimVelocity=Game.Player.GetComponent<Rigidbody2D>().linearVelocity-Vector2.down*.12f;
            held.Clear();
            yield return R2Swim(new Vector2(27,11.5f));yield return R2Swim(new Vector2(29,12.3f));
            var left=SwampLevel.Current.props.First(p=>p.persistentId=="A03_LATCH_1");yield return R2ShootTarget(left);
            yield return R2Swim(new Vector2(38.5f,14));var right=SwampLevel.Current.props.First(p=>p.persistentId=="A03_LATCH_2");yield return R2ShootTarget(right);
            yield return R2Swim(new Vector2(34,12));Pulse("Interactuar");yield return new WaitForSeconds(.3f);

            yield return Capture("05-tico-campana");
            yield return R2Swim(new Vector2(40,10));yield return R2Swim(new Vector2(60,9));yield return R2Swim(new Vector2(64,9));


            yield return R2Swim(new Vector2(67,9));yield return new WaitForSeconds(1.3f);
            yield return Capture("06-caiman-acuatico");step="caimán acuático";end=Time.realtimeSinceStartup+160;bool enraged=false;float moveAngle=0;
            while(!Game.Data.defeated.Contains("CAIMAN_WATER")&&Game.CanPlay&&Time.realtimeSinceStartup<end)
            {
                held.Clear();var b=SwampLevel.Current.boss;pointer=b.transform.position;held.Add("Atacar");if(b.health<=12)enraged=true;
                moveAngle+=Time.deltaTime*.55f;
                Vector2 target=new Vector2(78+Mathf.Cos(moveAngle)*8,8.5f+Mathf.Sin(moveAngle)*4);
                if(b.StateName=="Telegraph"&&b.TimeToStrike<.24f||b.StateName=="Attack")
                {
                    var normal=new Vector2(-b.ChargeDirection.y,b.ChargeDirection.x);var pos=Game.Player.Center;
                    if(pos.y+normal.y*2>15||pos.y+normal.y*2<3||pos.x+normal.x*2>88||pos.x+normal.x*2<68)normal=-normal;
                    target=pos+normal*3;
                }
                R2MoveToward(target,.2f);
                if(Game.Data.healthQuarters<=6&&Game.Data.fruit>0)Pulse("Consumible");yield return null;
            }
            held.Clear();
            if(!Game.CanPlay||!Game.Data.defeated.Contains("CAIMAN_WATER")){Finish();yield break;}
            int hearts=Game.Data.maxHearts;yield return R2Swim(new Vector2(93,7.3f));Pulse("Interactuar");yield return new WaitForSeconds(.3f);

            var shield=SwampLevel.Current.props.First(p=>p.kind==SwampPropKind.Shield);
            step="escudo y reflexión";yield return R2Swim(new Vector2(98,8.22f));held.Clear();pointer=new Vector3(103,9);yield return new WaitForSeconds(.2f);
            end=Time.realtimeSinceStartup+18;
            while(!Game.Data.collected.Contains("A06_REFLECTED")&&Time.realtimeSinceStartup<end&&Game.CanPlay)
            {
                held.Clear();pointer=new Vector3(103,9);R2MoveToward(new Vector2(98,9),.12f);
                var incoming=FindObjectsByType<CampaignProjectile>(FindObjectsSortMode.None).Where(s=>s.hostile&&s.reflectable).OrderBy(s=>Vector2.Distance(s.transform.position,Game.Player.Center)).FirstOrDefault();
                if(incoming!=null&&Vector2.Distance(incoming.transform.position,Game.Player.Center)<1.32f)held.Add("Escudo");yield return null;
            }
            held.Clear();yield return Capture("07-escudo");
            yield return R2Swim(new Vector2(107,8.3f));Pulse("Interactuar");yield return new WaitForSecondsRealtime(.3f);
            yield return Capture("08-pantano-liberado");

        }
        private IEnumerator R2Walk(float target)
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
        private IEnumerator R2Swim(Vector2 target)
        {
            float end=Time.realtimeSinceStartup+25;while(Game.CanPlay&&Vector2.Distance(Game.Player.Center,target)>.3f&&Time.realtimeSinceStartup<end)
            {
                held.Clear();R2MoveToward(target,.14f);
                var enemy=FindObjectsByType<SwampEnemy>(FindObjectsSortMode.None).Where(e=>!e.Dead).OrderBy(e=>Vector2.Distance(e.transform.position,Game.Player.Center)).FirstOrDefault();
                if(enemy!=null&&Vector2.Distance(enemy.transform.position,Game.Player.Center)<10){pointer=enemy.transform.position;held.Add("Atacar");}
                if(Game.Data.healthQuarters<=6&&Game.Data.fruit>0)Pulse("Consumible");yield return null;
            }
            held.Clear();yield return new WaitForSeconds(.1f);
        }
        private void R2MoveToward(Vector2 point,float tolerance)
        {var delta=point-Game.Player.Center;if(Mathf.Abs(delta.x)>tolerance)held.Add(delta.x>0?"Derecha":"Izquierda");if(Mathf.Abs(delta.y)>tolerance)held.Add(delta.y>0?"Nadar arriba":"Nadar abajo");}
        private IEnumerator R2ShootTarget(SwampProp p)
        {float end=Time.realtimeSinceStartup+12;held.Clear();pointer=p.transform.position;held.Add("Atacar");while(!Game.Data.collected.Contains(p.persistentId)&&Game.CanPlay&&Time.realtimeSinceStartup<end)yield return null;held.Clear();yield return new WaitForSeconds(.15f);}
        private IEnumerator Route3()
        {
step="acceso de servicio";yield return Capture("01-cantera");
            yield return R3Walk(7);Pulse("Interactuar");yield return new WaitForSeconds(.2f);
            yield return R3Walk(9.3f);yield return R3ReflectReceiver("M01_REFLECT");
            yield return R3Walk(20);yield return R3Walk(33.5f);
            float start=Game.Player.transform.position.x;held.Clear();yield return new WaitForSeconds(.35f);
            if(Game.Data.equippedWeapon!="sword")Pulse("Cambiar arma");yield return new WaitForSeconds(.2f);
            var guard=FindObjectsByType<MineEnemy>(FindObjectsSortMode.None).First(e=>e.name=="Guardia del depósito");yield return R3FightGuard(guard);

            var press=FindFirstObjectByType<MinePress>();yield return R3Walk(38);var head=press.head.position;Game.UI.Pause();yield return new WaitForSecondsRealtime(.4f);
            Game.UI.Play();yield return new WaitForSeconds(.2f);
            float wait=Time.realtimeSinceStartup+6;while(press.StateName!="Recovery"&&Time.realtimeSinceStartup<wait)yield return null;yield return R3Walk(45);
            yield return Capture("02-cinta-prensa");
            step="conducto inundado";yield return R3Walk(47.3f);yield return R3Swim(new Vector2(47.5f,-3.8f));
            int swing=Game.Player.AttackSequence;Game.Player.Attack();Pulse("Dash");yield return new WaitForSeconds(.15f);
            yield return R3Swim(new Vector2(59,-4.8f));yield return R3Swim(new Vector2(61,-6.1f));
            var target=MineLevel.Current.props.First(p=>p.persistentId=="M03_TARGET");yield return R3ShootTarget(target);
            yield return Capture("03-conducto");
            yield return R3Swim(new Vector2(71,-4.2f));yield return R3Swim(new Vector2(71,.95f));yield return R3Swim(new Vector2(74,.9f));

            step="rescate de Luma";yield return R3Walk(77.8f);if(Game.Data.equippedWeapon!="sword")Pulse("Cambiar arma");yield return new WaitForSeconds(.2f);held.Add("Atacar");yield return new WaitForSeconds(1);held.Clear();

            yield return R3ReflectReceiver("M04_REFLECT");
            yield return R3Walk(86);Pulse("Interactuar");yield return new WaitForSeconds(.3f);

            yield return Capture("04-luma");
            yield return R3Walk(91);Pulse("Interactuar");yield return new WaitForSecondsRealtime(.2f);Game.UI.Play();
            yield return R3Walk(95.5f);Pulse("Interactuar");yield return new WaitForSeconds(.25f);
            step="ascenso al control";yield return R3Walk(126);yield return Capture("05-control");

            yield return R3Walk(132);yield return new WaitForSeconds(1.4f);

            var bx=MineLevel.Current.boss.transform.position;Game.UI.Pause();double clock=Game.Data.activeSeconds;yield return new WaitForSecondsRealtime(.4f);Game.UI.Play();
            step="Director";float end=Time.realtimeSinceStartup+220;var phases=new HashSet<int>();var patterns=new HashSet<string>();bool captured2=false,captured3=false;
            while(!Game.Data.defeated.Contains("DIRECTOR")&&Game.CanPlay&&Time.realtimeSinceStartup<end)
            {
                held.Clear();var b=MineLevel.Current.boss;var p=Game.Player;int phase=b.Phase;phases.Add(phase);pointer=b.transform.position+Vector3.up*1.1f;float dx=b.transform.position.x-p.transform.position.x;
                if(b.StateName=="Telegraph")patterns.Add(b.Pattern);
                if(phase==2)
                {
                    if(!captured2){captured2=true;yield return Capture("06-director-blindado");}
                    if(Game.Data.equippedWeapon!="gun")Pulse("Cambiar arma");
                    float desired=Mathf.Clamp(b.transform.position.x+(dx>0?-6:6),131.5f,153.5f);float offset=desired-p.transform.position.x;
                    if(Mathf.Abs(offset)>.25f)held.Add(offset>0?"Derecha":"Izquierda");
                    var shot=FindObjectsByType<CampaignProjectile>(FindObjectsSortMode.None).Where(s=>s.hostile).OrderBy(s=>Vector2.Distance(s.transform.position,p.Center)).FirstOrDefault();
                    if(shot!=null&&Vector2.Distance(shot.transform.position,p.Center)<1.03f)held.Add("Escudo");
                    else if(Game.Data.equippedWeapon=="gun")held.Add("Atacar");
                }
                else
                {
                    if(phase==3&&!captured3){captured3=true;yield return Capture("07-director-embestida");}
                    if(Game.Data.equippedWeapon!="sword")Pulse("Cambiar arma");
                    if(b.StateName=="Telegraph"&&b.TimeToStrike<.3f&&p.EnSuelo)Pulse("Saltar");
                    if(b.StateName=="Attack"&&p.EnSuelo)Pulse("Saltar");
                    if(Mathf.Abs(dx)>1.35f||Mathf.Sign(dx)!=p.Facing)held.Add(dx>0?"Derecha":"Izquierda");
                    if(p.EnSuelo&&Physics2D.Raycast(p.transform.position+Vector3.up*.3f,new Vector2(Mathf.Sign(dx),0),1,1<<3))Pulse("Saltar");
                    if(Mathf.Abs(dx)<2.8f&&Game.Data.equippedWeapon=="sword")held.Add("Atacar");
                }
                if(Game.Data.healthQuarters<=6&&Game.Data.fruit>0)Pulse("Consumible");yield return null;
            }
            held.Clear();



            if(!Game.CanPlay||!Game.Data.defeated.Contains("DIRECTOR")){Finish();yield break;}


            yield return R3Walk(160);Pulse("Interactuar");yield return new WaitForSecondsRealtime(3);
            if(!Game.IsTrial){float limit=Time.realtimeSinceStartup+20;while(MineLevel.Current.EndingRunning&&Time.realtimeSinceStartup<limit)yield return null;}

        }
        private IEnumerator R3Walk(float x)
        {
            float end=Time.realtimeSinceStartup+35;while(Game.CanPlay&&Mathf.Abs(Game.Player.transform.position.x-x)>.25f&&Time.realtimeSinceStartup<end)
            {
                held.Clear();var p=Game.Player;float px=p.transform.position.x,dir=Mathf.Sign(x-px);held.Add(dir>0?"Derecha":"Izquierda");
                if(p.EnSuelo)
                {
                    if(dir>0&&px>24.2f&&px<25.15f){Pulse("Saltar");Pulse("Dash");}
                    else if(Physics2D.Raycast(p.transform.position+Vector3.up*.6f,new Vector2(dir,0),1.1f,1<<3)||Physics2D.Raycast(p.transform.position+Vector3.up*1.1f,new Vector2(dir,0),1.1f,1<<3))Pulse("Saltar");
                }
                if(Game.Data.equippedWeapon=="sword")held.Add("Atacar");if(Game.Data.healthQuarters<=6&&Game.Data.fruit>0)Pulse("Consumible");yield return null;
            }
            held.Clear();yield return new WaitForSeconds(.15f);
        }
        private IEnumerator R3FightGuard(MineEnemy enemy)
        {
            float end=Time.realtimeSinceStartup+50;while(Game.CanPlay&&!enemy.Dead&&Time.realtimeSinceStartup<end)
            {
                held.Clear();float dx=enemy.transform.position.x-Game.Player.transform.position.x;
                if(Mathf.Abs(dx)>1.4f||Mathf.Sign(dx)!=Game.Player.Facing)held.Add(dx>0?"Derecha":"Izquierda");
                if(Game.Player.EnSuelo&&(enemy.StateName=="Telegraph"&&enemy.TimeToStrike<.3f||enemy.StateName=="Attack"))Pulse("Saltar");
                if(Mathf.Abs(dx)<2.5f)held.Add("Atacar");if(Game.Data.healthQuarters<=6&&Game.Data.fruit>0)Pulse("Consumible");yield return null;
            }
            held.Clear();
        }
        private IEnumerator R3ReflectReceiver(string id)
        {
            var receiver=MineLevel.Current.props.First(p=>p.persistentId==id);float end=Time.realtimeSinceStartup+24;
            while(!Game.Data.collected.Contains(id)&&Game.CanPlay&&Time.realtimeSinceStartup<end)
            {
                held.Clear();pointer=receiver.transform.position;
                var shot=FindObjectsByType<CampaignProjectile>(FindObjectsSortMode.None).Where(s=>s.hostile&&s.reflectable).OrderBy(s=>Vector2.Distance(s.transform.position,Game.Player.Center)).FirstOrDefault();
                if(shot!=null&&Vector2.Distance(shot.transform.position,Game.Player.Center)<1.03f)held.Add("Escudo");yield return null;
            }
            held.Clear();yield return new WaitForSeconds(.1f);
        }
        private IEnumerator R3Swim(Vector2 target)
        {
            float end=Time.realtimeSinceStartup+25;while(Game.CanPlay&&Vector2.Distance(Game.Player.Center,target)>.3f&&Time.realtimeSinceStartup<end)
            {
                held.Clear();var delta=target-Game.Player.Center;if(Mathf.Abs(delta.x)>.14f)held.Add(delta.x>0?"Derecha":"Izquierda");if(Mathf.Abs(delta.y)>.14f)held.Add(delta.y>0?"Nadar arriba":"Nadar abajo");
                var enemy=FindObjectsByType<SwampEnemy>(FindObjectsSortMode.None).FirstOrDefault(e=>!e.Dead);
                if(enemy!=null&&Vector2.Distance(enemy.transform.position,Game.Player.Center)<10){pointer=enemy.transform.position;held.Add("Atacar");}
                if(Game.Data.healthQuarters<=6&&Game.Data.fruit>0)Pulse("Consumible");yield return null;
            }
            held.Clear();yield return new WaitForSeconds(.1f);
        }
        private IEnumerator R3ShootTarget(MineProp target)
        {held.Clear();pointer=target.transform.position;float end=Time.realtimeSinceStartup+15;held.Add("Atacar");while(!Game.Data.collected.Contains(target.persistentId)&&Game.CanPlay&&Time.realtimeSinceStartup<end)yield return null;held.Clear();yield return new WaitForSeconds(.2f);}
    }
}
#endif
