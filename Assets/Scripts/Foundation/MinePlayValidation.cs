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
    public sealed class MinePlayValidation:MonoBehaviour
    {
        [Serializable] private class Report{public List<string> passed=new List<string>();public List<string> failed=new List<string>();}
        [Serializable] private class Snapshot{public string scene,screen,step,boss;public float x,y,energy;public int hearts,bossHealth,phase,reflected;}
        private Report report=new Report();private string output,step;private bool ending;private float deadline,nextSnapshot;private Vector3 pointer;
        private readonly HashSet<string> held=new HashSet<string>();private readonly Dictionary<string,int> pressed=new Dictionary<string,int>();
        private GameRuntime Game=>GameRuntime.Instance;private MineLevel Level=>MineLevel.Current;
        private void Check(bool ok,string label){(ok?report.passed:report.failed).Add(label);Debug.Log("MINE "+(ok?"PASS ":"FAIL ")+label);}
        private void Pulse(string action){pressed[action]=Time.frameCount+1;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-juanMineValidate");if(i<0||i+1>=args.Length||!args.Contains("-juanDataPath"))return;
            var go=new GameObject("Pruebas aisladas de MinerTown");DontDestroyOnLoad(go);go.AddComponent<MinePlayValidation>().output=Path.GetFullPath(args[i+1]);
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;Directory.CreateDirectory(output);deadline=Time.realtimeSinceStartup+850;Application.logMessageReceived+=OnLog;
            GameInput.ValidationHeld=a=>held.Contains(a);GameInput.ValidationDown=a=>pressed.TryGetValue(a,out int f)&&f==Time.frameCount;GameInput.ValidationPointer=()=>pointer;
            yield return new WaitForSecondsRealtime(.8f);Check(Game.UI.ScreenName=="menu","Arranque de campaña en menú");
            // Fixture represents a completed pantano. From travel onward progression uses normal inputs and colliders.
            var seed=new CampaignData{scene="AmazoniaAcuatica",checkpoint="CP_A06",sword=true,dash=true,gun=true,shield=true,introSeen=true,sierraLayout=5,swampLayout=6,sierraFinished=true,swampFinished=true,maxHearts=5,healthQuarters=20,fruit=3,cells=1,gold=75,scrap=15,activeSeconds=400,sierraSeconds=180,swampStarted=180,swampSeconds=220,hasPosition=true,x=107,y=8,equippedWeapon="gun"};
            seed.rescues.AddRange(new[]{"MARA","TICO"});seed.defeated.AddRange(new[]{"RASTREADOR","CAIMAN_LAND","CAIMAN_WATER"});seed.collected.Add("A06_REFLECTED");
            Check(Game.Store.StartNew(seed,true,out _),"Campaña aislada al finalizar el pantano");Game.LoadRecord(Game.Store.Records()[0]);yield return WaitScene();yield return new WaitForSeconds(.3f);
            SwampLevel.Current.FinishLevel();yield return null;Click("Ir a MinerTown");yield return WaitScene();yield return new WaitForSeconds(.3f);
            Check(Level!=null&&Game.Data.scene=="MinerTown"&&Game.Data.shield&&!Game.Player.aquatic,"Viaje desde pantano conserva equipo y recupera movimiento terrestre");
            Check(Game.Store.Records()[0].data.scene=="MinerTown"&&!Game.Data.completed,"Entrada se guarda sin adelantar victoria");step="acceso de servicio";yield return Capture("01-cantera");
            yield return Walk(7);Pulse("Interactuar");yield return new WaitForSeconds(.2f);Check(Game.Data.collected.Contains("M01_SUPPLY"),"Suministros del acceso recogidos");
            yield return Walk(9.3f);yield return ReflectReceiver("M01_REFLECT");Check(Game.Data.collected.Contains("M01_REFLECT")&&!Level.entryGate.activeSelf,"Reflexión abre la compuerta inicial");
            yield return Walk(20);Check(Game.Data.checkpoint=="CP_M02","Checkpoint al entrar al patio");yield return Walk(33.5f);Check(Game.Player.transform.position.x>32.6f,"Salto y dash cruzan el corte de cantera");
            float start=Game.Player.transform.position.x;held.Clear();yield return new WaitForSeconds(.35f);Check(Game.Player.transform.position.x>start+.25f,"Cinta desplaza a Juan sin entrada horizontal");
            if(Game.Data.equippedWeapon!="sword")Pulse("Cambiar arma");yield return new WaitForSeconds(.2f);
            var guard=FindObjectsByType<MineEnemy>(FindObjectsSortMode.None).First(e=>e.name=="Guardia del depósito");yield return FightGuard(guard);
            Check(guard.Dead,"Guardia industrial vencido atacando en recuperación");
            var press=FindFirstObjectByType<MinePress>();yield return Walk(38);var head=press.head.position;Game.UI.Pause();yield return new WaitForSecondsRealtime(.4f);
            Check(press.head.position==head,"Pausa congela prensa industrial");Game.UI.Play();yield return new WaitForSeconds(.2f);
            float wait=Time.realtimeSinceStartup+6;while(press.StateName!="Recovery"&&Time.realtimeSinceStartup<wait)yield return null;yield return Walk(45);
            Check(Game.Player.transform.position.x>43,"Prensa se atraviesa durante recuperación");yield return Capture("02-cinta-prensa");
            step="conducto inundado";yield return Walk(47.3f);yield return Swim(new Vector2(47.5f,-3.8f));Check(Game.Player.aquatic&&Game.Data.equippedWeapon=="gun","Entrada al conducto activa nado y disparador");
            int swing=Game.Player.AttackSequence;Game.Player.Attack();Pulse("Dash");yield return new WaitForSeconds(.15f);Check(!Game.Player.IsDashing&&Game.Player.AttackSequence==swing,"Conducto bloquea espada y dash");
            yield return Swim(new Vector2(59,-4.8f));yield return Swim(new Vector2(61,-6.1f));
            var target=Level.props.First(p=>p.persistentId=="M03_TARGET");yield return ShootTarget(target);
            Check(Game.Data.collected.Contains("M03_TARGET")&&!Level.waterGate.activeSelf,"Disparo diagonal abre cierre del conducto");yield return Capture("03-conducto");
            yield return Swim(new Vector2(71,-4.2f));yield return Swim(new Vector2(71,.95f));yield return Swim(new Vector2(74,.9f));
            Check(!Game.Player.aquatic&&Game.Data.checkpoint=="CP_M03","Salida recupera físicas terrestres y guarda checkpoint");
            step="rescate de Luma";yield return Walk(77.8f);if(Game.Data.equippedWeapon!="sword")Pulse("Cambiar arma");yield return new WaitForSeconds(.2f);held.Add("Atacar");yield return new WaitForSeconds(1);held.Clear();
            Check(Game.Data.collected.Contains("M04_CABLE")&&Level.lumaGate.activeSelf,"Cortar cable es necesario pero no basta para liberar a Luma");
            yield return ReflectReceiver("M04_REFLECT");Check(Game.Data.collected.Contains("M04_REFLECT")&&!Level.lumaGate.activeSelf,"Reflejar al receptor abre la celda");
            yield return Walk(86);Pulse("Interactuar");yield return new WaitForSeconds(.3f);
            Check(Game.Data.rescues.Contains("LUMA")&&Game.Data.maintenanceKey&&!Level.controlGate.activeSelf,"Luma entrega llave y abre acceso al control");
            Check(Game.Store.Records()[0].data.rescues.Contains("LUMA"),"Rescate y llave persisten juntos");yield return Capture("04-luma");
            yield return Walk(91);Pulse("Interactuar");yield return new WaitForSecondsRealtime(.2f);Check(Game.UI.ScreenName=="bank","Banco final accesible tras rescate");Game.UI.Play();
            yield return Walk(95.5f);Pulse("Interactuar");yield return new WaitForSeconds(.25f);Check(Game.Data.collected.Contains("M04_SUPPLY"),"Segunda provisión mantiene presupuesto del nivel");
            step="ascenso al control";yield return Walk(126);Check(Game.Player.transform.position.x>125&&Game.Data.checkpoint=="CP_M05","Ascenso llega a checkpoint anterior al Director");yield return Capture("05-control");
            // A fresh encounter fixture matches Retry supplies; no healing or boss-health changes are made during the fight.
            Game.Data.healthQuarters=Game.Data.MaximumHealth;Game.Data.ammunition=3;Game.Data.reloadRemaining=0;Game.Data.shieldEnergy=100;Game.NotifyChanged();
            yield return Walk(132);yield return new WaitForSeconds(1.4f);Check(Level.boss.Fighting&&Level.boss.entrance.activeSelf&&Level.boss.exit.activeSelf,"Director cierra la arena tras la llave de Luma");
            Check(Level.SafeSavePosition(Game.Player.transform.position).x==127,"Guardar en combate sitúa retorno antes de la arena");
            var bx=Level.boss.transform.position;Game.UI.Pause();double clock=Game.Data.activeSeconds;yield return new WaitForSecondsRealtime(.4f);Check(Level.boss.transform.position==bx&&Game.Data.activeSeconds==clock,"Pausa congela Director y cronómetro");Game.UI.Play();
            step="Director";float end=Time.realtimeSinceStartup+220;var phases=new HashSet<int>();var patterns=new HashSet<string>();bool captured2=false,captured3=false;
            while(!Game.Data.defeated.Contains("DIRECTOR")&&Game.CanPlay&&Time.realtimeSinceStartup<end)
            {
                held.Clear();var b=Level.boss;var p=Game.Player;int phase=b.Phase;phases.Add(phase);pointer=b.transform.position+Vector3.up*1.1f;float dx=b.transform.position.x-p.transform.position.x;
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
            held.Clear();Check(Game.Data.defeated.Contains("DIRECTOR"),"Director derrotado con ataques, escudo y proyectiles reales");
            Check(phases.Count==3&&patterns.Contains("Golpe")&&patterns.Contains("Onda")&&patterns.Contains("Reflector")&&patterns.Contains("Ráfaga")&&patterns.Contains("Embestida"),"Tres etapas y cinco patrones observados");
            Check(Level.boss.ReflectedHits>0&&Level.boss.ArmorBlocks>0,"Blindaje rechaza daño ordinario y abre núcleo al reflejar");
            Check(Level.boss.SummonPeak==2&&FindObjectsByType<MineEnemy>(FindObjectsSortMode.None).Where(e=>e.summoned).All(e=>e.Dead),"Invocación limitada a dos drones y retirada al vencer");
            if(!Game.CanPlay||!Game.Data.defeated.Contains("DIRECTOR")){Finish();yield break;}
            Check(!Game.Data.completed&&!Level.boss.exit.activeSelf,"Derrotar exotraje deja cierre final pendiente y seguro");
            var victory=Game.Store.Records()[0];Game.LoadRecord(victory);yield return WaitScene();yield return new WaitForSeconds(.25f);
            Check(Level.boss.Resolved&&!Game.Data.completed&&Game.Data.checkpoint=="CP_M06","Cargar victoria parcial no repite jefe ni completa válvulas");
            step="cierre y epílogo";yield return Walk(160);Pulse("Interactuar");yield return new WaitForSecondsRealtime(2.5f);
            Check(Game.Data.completed&&!Game.Data.epilogueSeen&&Level.EndingRunning,"Válvula guarda campaña antes del epílogo");
            var completed=Game.Store.Records()[0];Check(completed.data.completed&&!completed.data.epilogueSeen,"Copia persistente permite recuperar epílogo interrumpido");yield return Capture("08-epilogo");
            float heroX=Level.view.transform.position.x;clock=Game.Data.activeSeconds;Game.UI.Pause();yield return new WaitForSecondsRealtime(.4f);Check(Level.view.transform.position.x==heroX&&Game.Data.activeSeconds==clock,"Pausa detiene epílogo y tiempo final");Game.UI.Play();
            Game.LoadRecord(completed);yield return WaitScene();yield return new WaitForSecondsRealtime(.3f);Check(Level.EndingRunning&&Game.Data.completed&&!Game.Data.epilogueSeen,"Reanudar durante epílogo conserva victoria");
            float storyEnd=Time.realtimeSinceStartup+20;while(Level.EndingRunning&&Time.realtimeSinceStartup<storyEnd)yield return null;
            Check(Game.Data.epilogueSeen&&Game.UI.ScreenName=="complete"&&Game.Data.campaignSeconds>=Game.Data.mineSeconds,"Epílogo completo conduce a resultados permanentes");yield return Capture("09-campana-completada");
            Click("Créditos");yield return null;Check(Game.UI.ScreenName=="credits","Créditos accesibles al finalizar");Game.UI.CampaignResults();
            var final=Game.Store.Records()[0];double total=Game.Data.campaignSeconds;Game.LoadRecord(final);yield return WaitScene();yield return new WaitForSecondsRealtime(.3f);
            Check(!Level.EndingRunning&&Game.UI.ScreenName=="complete"&&Game.Store.Records()[0].id==final.id,"Continuar campaña terminada no repite jefe, epílogo ni guardados");
            Game.UI.Play();yield return new WaitForSeconds(.3f);Check(Game.Data.campaignSeconds==total&&!FindObjectsByType<CampaignProjectile>(FindObjectsSortMode.None).Any(s=>s.hostile),"Exploración final conserva resultado y desactiva amenazas");
            Finish();
        }
        private IEnumerator Walk(float x)
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
        private IEnumerator FightGuard(MineEnemy enemy)
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
        private IEnumerator ReflectReceiver(string id)
        {
            var receiver=Level.props.First(p=>p.persistentId==id);float end=Time.realtimeSinceStartup+24;
            while(!Game.Data.collected.Contains(id)&&Game.CanPlay&&Time.realtimeSinceStartup<end)
            {
                held.Clear();pointer=receiver.transform.position;
                var shot=FindObjectsByType<CampaignProjectile>(FindObjectsSortMode.None).Where(s=>s.hostile&&s.reflectable).OrderBy(s=>Vector2.Distance(s.transform.position,Game.Player.Center)).FirstOrDefault();
                if(shot!=null&&Vector2.Distance(shot.transform.position,Game.Player.Center)<1.03f)held.Add("Escudo");yield return null;
            }
            held.Clear();yield return new WaitForSeconds(.1f);
        }
        private IEnumerator Swim(Vector2 target)
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
        private IEnumerator ShootTarget(MineProp target)
        {held.Clear();pointer=target.transform.position;float end=Time.realtimeSinceStartup+15;held.Add("Atacar");while(!Game.Data.collected.Contains(target.persistentId)&&Game.CanPlay&&Time.realtimeSinceStartup<end)yield return null;held.Clear();yield return new WaitForSeconds(.2f);}
        private void Click(string name){var b=FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(x=>x.name==name&&x.interactable);Check(b!=null,"Botón disponible: "+name);b?.onClick.Invoke();}
        private IEnumerator WaitScene(){yield return null;float end=Time.realtimeSinceStartup+20;while(Game.Busy&&Time.realtimeSinceStartup<end)yield return null;yield return null;}
        private IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.15f);}
        private void OnLog(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)report.failed.Add(message+"\n"+trace);}
        private void Update()
        {
            if(ending||deadline<=0)return;
            if(Time.realtimeSinceStartup>nextSnapshot&&Game?.Player!=null&&Game.Data!=null)
            {nextSnapshot=Time.realtimeSinceStartup+3;var p=Game.Player.transform.position;File.WriteAllText(Path.Combine(output,"estado.json"),JsonUtility.ToJson(new Snapshot{screen=Game.UI.ScreenName,scene=Game.Data.scene,step=step,x=p.x,y=p.y,hearts=Game.Data.healthQuarters,energy=Game.Data.shieldEnergy,boss=Level?.boss.StateName,bossHealth=Level!=null?Level.boss.health:0,phase=Level!=null?Level.boss.Phase:0,reflected=Level!=null?Level.boss.ReflectedHits:0}));}
            if(Game?.UI.ScreenName=="death"){Check(false,"Derrota durante "+step);ending=true;StartCoroutine(EndCapture());}
            else if(Time.realtimeSinceStartup>deadline){Check(false,"Tiempo máximo agotado: "+step);Finish();}
        }
        private IEnumerator EndCapture(){yield return Capture("fallo-derrota");Finish();}
        private void Finish(){ending=true;held.Clear();GameInput.ValidationDown=GameInput.ValidationHeld=null;GameInput.ValidationPointer=null;File.WriteAllText(Path.Combine(output,"validacion-jugable.json"),JsonUtility.ToJson(report,true));Debug.Log("MINE PLAY RESULT: "+report.passed.Count+" PASS, "+report.failed.Count+" FAIL");Application.logMessageReceived-=OnLog;Application.Quit(report.failed.Count==0?0:1);}
    }
}
#endif
