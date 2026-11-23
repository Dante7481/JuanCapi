#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace JuanCapi
{
    public sealed class PresentationPlayValidation:MonoBehaviour
    {
        [Serializable] class Report {public List<string> passed=new List<string>(),failed=new List<string>();}
        readonly Report report=new Report();string output;float deadline;GameRuntime Game=>GameRuntime.Instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot()
        {var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"-juanPresentationValidate");if(i<0||!a.Contains("-juanDataPath"))return;var go=new GameObject("Presentación · pruebas aisladas");DontDestroyOnLoad(go);go.AddComponent<PresentationPlayValidation>().output=a[i+1];}
        void Check(bool ok,string message){(ok?report.passed:report.failed).Add(message);}
        IEnumerator Start()
        {
            deadline=Time.realtimeSinceStartup+180;Application.runInBackground=true;Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            yield return new WaitForSecondsRealtime(1);yield return Capture("00-menu");
            foreach(string scene in new[]{"LV1","AmazoniaTerrestre","AmazoniaAcuatica","MinerTown"})
            {
                var d=new CampaignData{sword=true,dash=true,gun=true,shield=true,introSeen=true,scene=scene,hasPosition=true,x=scene=="LV1"?2:3,y=scene=="AmazoniaAcuatica"?9:.08f,maxHearts=5,healthQuarters=20};
                Game.Store.StartNew(d,true,out _);Game.LoadRecord(Game.Store.Records()[0]);yield return null;while(Game.Busy)yield return null;yield return new WaitForSeconds(1.2f);
                var driver=Game.Player.GetComponent<PlayerArtDriver>();Check(driver.actor!=null,"Arte instalado: "+scene);
                if(scene!="AmazoniaAcuatica")
                {
                    var foot=Resources.Load<SpriteFootprints>("Campaign/Footprints").Bottom(driver.actor.sprite.sprite);
                    float world=driver.actor.sprite.transform.TransformPoint(new Vector3(0,foot,0)).y;
                    Check(Mathf.Abs(world-Game.Player.GetComponent<Collider2D>().bounds.min.y)<.04f,"Pies coinciden con base del collider: "+scene);
                    File.AppendAllText(Path.Combine(output,"contacto.txt"),scene+" position="+Game.Player.transform.position+" visualFoot="+world+" collider="+Game.Player.GetComponent<Collider2D>().bounds+" sprite="+driver.actor.sprite.sprite.name+" grounded="+Game.Player.EnSuelo+"\n");
                    Check(FindObjectsByType<PlatformEffector2D>().Length>0,"Plataformas atravesables desde abajo: "+scene);
                }
                Game.UI.Lesson("Atacar","sword");Game.UI.PromptIcon("drone");yield return Capture(scene+"-reposo");
                Game.UI.Lesson("Atacar","sword");var hint=Game.UI.GetComponentsInChildren<RectTransform>().LastOrDefault(t=>t.name=="Gesto");Check(hint!=null&&hint.anchoredPosition.x==24&&hint.anchoredPosition.y==-150,"Ayuda bajo corazones: "+scene);
                GameInput.ValidationDown=a=>a=="Atacar";yield return null;GameInput.ValidationDown=null;yield return new WaitForSeconds(.2f);yield return Capture(scene+"-ataque");
                var enemy=FindObjectsByType<SierraDamageTarget>().FirstOrDefault(e=>e is SierraEnemy||e is SwampEnemy||e is MineEnemy);
                if(enemy!=null){var actor=enemy.GetComponentInChildren<AnimatedActor>();actor.Defeat();yield return new WaitForSeconds(1.3f);Check(!actor.gameObject.activeSelf,"Desaparición tras animación de derrota: "+scene);}
                if(scene=="LV1")
                {
                    // Jump from below the first ledge, then land on its top using the real body.
                    Game.Player.transform.position=new Vector3(6.4f,.03f);Game.Player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;yield return new WaitForSeconds(.15f);
                    GameInput.ValidationDown=a=>a=="Saltar";yield return null;GameInput.ValidationDown=null;
                    float max=0;for(float t=0;t<1.3f;t+=Time.deltaTime){max=Mathf.Max(max,Game.Player.transform.position.y);yield return null;}
                    Check(max>1.1f&&Mathf.Abs(Game.Player.transform.position.y-.8f)<.1f,"Salta a través de cornisa y aterriza encima");yield return Capture("01-cornisa");
                }
            }
            yield return CombatSprites();
            Game.UI.Settings(Game.UI.Pause);yield return Capture("05-ajustes");
            Game.Settings.fullscreen=true;Game.Settings.Apply(true);yield return new WaitForSecondsRealtime(1);
            Check(Screen.width==Display.main.systemWidth&&Screen.height==Display.main.systemHeight,"Pantalla completa usa resolución nativa");yield return Capture("06-nativa");
            Finish();
        }
        IEnumerator CombatSprites()
        {
            var data=new CampaignData{sword=true,dash=true,introSeen=true,scene="AmazoniaTerrestre",hasPosition=true,x=91,y=.08f,maxHearts=5,healthQuarters=20};
            Game.Store.StartNew(data,true,out _);Game.LoadRecord(Game.Store.Records()[0]);
            yield return null;while(Game.Busy)yield return null;yield return new WaitForSeconds(1);
            var boss=FindAnyObjectByType<CaimanEncounter>();
            Check(boss!=null,"Caimán presente en la revisión visual");if(boss==null)yield break;
            boss.enabled=false;boss.transform.position=new Vector3(96,0,0);
            Game.Player.transform.position=new Vector3(91,.08f,0);Game.Player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
            yield return new WaitForSeconds(1);
            var actor=Game.Player.GetComponent<PlayerArtDriver>().actor;
            foreach(int side in new[]{1,-1})
            {
                GameInput.ValidationHeld=a=>a==(side>0?"Derecha":"Izquierda");yield return new WaitForSeconds(.05f);
                GameInput.ValidationHeld=a=>false;yield return new WaitForSeconds(.1f);
                boss.actor.Face(-side);boss.actor.Play("Idle",true);
                GameInput.ValidationDown=a=>a=="Atacar";yield return null;GameInput.ValidationDown=null;
                yield return new WaitForSeconds(.29f);
                Check(actor.sprite.sprite.name=="Melee_3"&&actor.sprite.sprite.triangles.Length>6,"Espadazo completo con geometría aislada, dirección "+side);
                Check(boss.actor.sprite.sprite.name.StartsWith("CaimanTerrestre_Idle")&&boss.actor.sprite.sprite.triangles.Length>6,"Caimán usa silueta completa, dirección "+side);
                yield return Capture("07-espada-caiman-"+side);
                yield return new WaitForSeconds(.4f);
                boss.actor.Play("Claw",true);yield return new WaitForSeconds(.13f);
                yield return Capture("08-garra-"+side);
            }
            GameInput.ValidationHeld=null;
        }
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.15f);}
        void Log(string s,string trace,LogType t){if(t==LogType.Error||t==LogType.Exception)report.failed.Add(s+trace);}
        void Update(){if(deadline>0&&Time.realtimeSinceStartup>deadline){Check(false,"Plazo del ensayo");Finish();}}
        void Finish(){deadline=0;Application.logMessageReceived-=Log;File.WriteAllText(Path.Combine(output,"validacion.json"),JsonUtility.ToJson(report,true));Application.Quit(report.failed.Count==0?0:1);}
    }
}
#endif
