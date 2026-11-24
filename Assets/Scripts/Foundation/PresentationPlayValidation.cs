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
            if(Environment.GetCommandLineArgs().Contains("-juanEnvironmentTour")){yield return EnvironmentTour();Finish();yield break;}
            foreach(string scene in new[]{"LV1","AmazoniaTerrestre","AmazoniaAcuatica","MinerTown"})
            {
                var d=new CampaignData{sword=scene!="LV1",dash=scene!="LV1",gun=scene!="LV1",shield=scene!="LV1",introSeen=true,sierraLayout=5,swampLayout=6,scene=scene,hasPosition=true,x=scene=="LV1"?2:3,y=scene=="AmazoniaAcuatica"?9:.08f,maxHearts=5,healthQuarters=20};
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
                if(scene!="AmazoniaAcuatica")
                {
                    Game.Data.equippedWeapon="sword";
                    GameInput.ValidationHeld=a=>a=="Derecha";yield return new WaitForSeconds(.12f);yield return Capture(scene+"-correr-derecha");
                    GameInput.ValidationHeld=a=>a=="Izquierda";yield return new WaitForSeconds(.12f);yield return Capture(scene+"-correr-izquierda");GameInput.ValidationHeld=a=>false;
                    yield return new WaitForSeconds(.2f);
                    GameInput.ValidationHeld=null;
                }
                var emotes=Game.Player.GetComponent<PlayerEmotes>();
                Game.Data.equippedWeapon="gun";
                for(int i=0;i<3;i++){Check(emotes.Play(i),"Emote disponible sin desbloqueo: "+scene+" "+i);yield return new WaitForSeconds(.25f);Check(driver.actor.CurrentState==PlayerEmotes.States[i],"Animación de emote: "+scene+" "+i);yield return Capture(scene+"-emote-"+i);emotes.Cancel();}
                emotes.Play(0);GameInput.ValidationHeld=a=>a=="Derecha";yield return null;yield return null;
                Check(!emotes.Active,"Moverse cancela el emote: "+scene);GameInput.ValidationHeld=null;
                if(scene=="LV1")
                {
                    Check(!Game.Data.sword&&!Game.Data.dash&&!Game.Data.gun&&!Game.Data.shield,"Emotes probados antes de obtener cualquier habilidad");
                    Check(!Game.Settings.Rebind("Dash",KeyCode.T),"T reservada para emotes");
                    Game.UI.Emotes();yield return Capture("selector-emotes");
                    var button=Game.UI.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="1  Wazaaaa");button.onClick.Invoke();yield return null;
                    Check(emotes.Active&&!Game.UI.IsOpen,"Selector de emotes reproduce y reanuda la partida");emotes.Cancel();
                }
                if(scene=="AmazoniaAcuatica")
                {
                    var waterBoss=FindObjectsByType<CaimanEncounter>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();
                    waterBoss.enabled=false;waterBoss.gameObject.SetActive(true);waterBoss.transform.position=Game.Player.transform.position+new Vector3(6,2);
                    waterBoss.actor.Face(-1);
                    foreach(string pose in new[]{"Swim","ChargeAlt"}){waterBoss.actor.Play(pose,true);yield return new WaitForSeconds(pose=="Swim"?.1f:.32f);yield return Capture("caiman-agua-"+pose);}
                    waterBoss.gameObject.SetActive(false);
                }
                if(scene=="AmazoniaTerrestre")
                {
                    bool continuous=true;for(float x=1;x<SwampLevel.ShoreX;x+=.5f){var h=Physics2D.Raycast(new Vector2(x,.1f),Vector2.down,.2f,1<<3);if(h.collider==null)continuous=false;}
                    Check(continuous,"Pantano tiene piso continuo hasta la orilla");
                    Check(Physics2D.Raycast(new Vector2(SwampLevel.ShoreX+1,.1f),Vector2.down,.4f,1<<3).collider==null,"El lago no tiene suelo invisible sobre el agua");
                    Check(GameObject.Find("Estratos de la orilla")==null,"Pantano conserva el camino del fondo sin muros añadidos");
                    var panorama=GameObject.Find("Panorama original");var fixedPosition=panorama.transform.position;
                    foreach(float x in new[]{44f,46f,90f,123f}){Game.Player.transform.position=new Vector3(x,.08f);yield return new WaitForSeconds(.6f);yield return Capture("camino-fijo-"+x);}
                    Check(panorama.transform.position==fixedPosition&&panorama.GetComponent<ParallaxArt>()==null,"El camino permanece fijo al desplazarse el jugador");
                    var backgrounds=GameObject.Find("Camino extendido del pantano").GetComponentsInChildren<SpriteRenderer>();
                    Check(backgrounds.Max(r=>r.bounds.max.x)>=128,"Fondo extendido hasta el final del nivel terrestre");
                    Check(!backgrounds.Any(r=>r.flipX)&&backgrounds.Select(r=>r.sprite.texture).Append(panorama.GetComponent<SpriteRenderer>().sprite.texture).Distinct().Count()==3,"Tres paisajes distintos sin reflejos ni repeticiones");
                }
                if(scene=="MinerTown")
                {
                    Game.Player.transform.position=new Vector3(44,.08f);yield return new WaitForSeconds(1);
                    Check(GameObject.Find("Superficie del agua").transform.position.y<0,"Agua del conducto por debajo de la pasarela");yield return Capture("mina-entrada-agua");
                }
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
            yield return DiveAnimation();
            Check(Resources.LoadAll<AudioClip>("Audio/Music").Length==7,"Siete pistas musicales externas disponibles");
            Check(Resources.LoadAll<AudioClip>("Audio/Music").All(c=>c.length>30),"Pistas musicales completas, de más de treinta segundos");
            Game.UI.Settings(Game.UI.Pause);yield return Capture("05-ajustes");
            Game.Settings.fullscreen=true;Game.Settings.Apply(true);yield return new WaitForSecondsRealtime(1);
            Check(Screen.width==Display.main.systemWidth&&Screen.height==Display.main.systemHeight,"Pantalla completa usa resolución nativa");yield return Capture("06-nativa");
            Finish();
        }
        IEnumerator EnvironmentTour()
        {
            foreach(string scene in new[]{"LV1","AmazoniaTerrestre","AmazoniaAcuatica","MinerTown"})
            {
                var d=new CampaignData{sword=true,dash=true,gun=true,shield=true,introSeen=true,sierraLayout=5,swampLayout=6,scene=scene,hasPosition=true,x=3,y=scene=="AmazoniaAcuatica"?9:.08f,maxHearts=5,healthQuarters=20};
                Game.Store.StartNew(d,true,out _);Game.LoadRecord(Game.Store.Records()[0]);yield return null;while(Game.Busy)yield return null;yield return new WaitForSeconds(.6f);
                Check(GameObject.Find("Colección de entornos V2")!=null,"Colección integrada: "+scene);
                var pieces=FindObjectsByType<SpriteRenderer>().Where(r=>r.enabled&&r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="JuanCapi/EnvironmentCutout").ToArray();
                Check(pieces.Length>0&&pieces.All(r=>r.sprite!=null),"Piezas visibles y referencias válidas: "+scene);
                Vector2[] stops=scene=="LV1"?new[]{new Vector2(7,.08f),new Vector2(33,.08f),new Vector2(67,.08f)}:
                    scene=="AmazoniaTerrestre"?new[]{new Vector2(14,.08f),new Vector2(61,.08f)}:
                    scene=="AmazoniaAcuatica"?new[]{new Vector2(34,14.4f),new Vector2(61,7)}:
                    new[]{new Vector2(7,.08f),new Vector2(27,.08f),new Vector2(39,.08f),new Vector2(46,.08f),new Vector2(60,-4),new Vector2(87,.08f),new Vector2(112,.08f),new Vector2(143,.08f)};
                foreach(var point in stops)
                {
                    Game.Player.transform.position=point;Game.Player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
                    yield return new WaitForSeconds(.8f);yield return Capture(scene+"-entorno-"+point.x);
                }
            }
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
        IEnumerator DiveAnimation()
        {
            var data=new CampaignData{sword=true,dash=true,gun=true,introSeen=true,swampLayout=6,scene="AmazoniaTerrestre",hasPosition=true,x=124,y=.08f,maxHearts=5,healthQuarters=17,ammunition=2};
            data.defeated.Add("CAIMAN_LAND");foreach(string id in new[]{"P05_TARGET_1","P05_TARGET_2","P05_TARGET_3"})data.collected.Add(id);
            Game.Store.StartNew(data,true,out _);Game.LoadRecord(Game.Store.Records()[0]);yield return null;while(Game.Busy)yield return null;yield return new WaitForSeconds(1);
            var driver=Game.Player.GetComponent<PlayerArtDriver>();int health=Game.Data.healthQuarters,ammo=Game.Data.ammunition;
            Check(SwampLevel.Current.Dive(),"Inicia inmersión al completar la fase terrestre");
            yield return new WaitForSeconds(.4f);Check(driver.actor.CurrentState=="DivePrepare","Preparación de zambullida visible");yield return Capture("juan-prepara-inmersion");
            yield return new WaitForSeconds(.4f);Check(driver.actor.CurrentState=="Dive","Juan usa la animación de zambullida");yield return Capture("juan-entra-al-agua");
            float end=Time.realtimeSinceStartup+15;while((SwampLevel.Current?.Diving==true||Game.Busy)&&Time.realtimeSinceStartup<end)yield return null;
            yield return new WaitForSeconds(.5f);
            Check(Game.Data.scene=="AmazoniaAcuatica"&&Game.Player.aquatic,"Zambullida termina en la fase acuática");
            Check(Game.Data.healthQuarters==health&&Game.Data.ammunition==ammo,"Inmersión conserva vida y munición");
            Game.Store.StartNew(data,true,out _);Game.LoadRecord(Game.Store.Records()[0]);yield return null;while(Game.Busy)yield return null;yield return new WaitForSeconds(.3f);
            Game.Player.transform.position=new Vector3(SwampLevel.ShoreX-.1f,.08f);yield return new WaitForSeconds(.2f);
            Check(SwampLevel.Current.Diving,"Caminar hasta la orilla inicia inmersión automáticamente");
            end=Time.realtimeSinceStartup+15;while((SwampLevel.Current?.Diving==true||Game.Busy)&&Time.realtimeSinceStartup<end)yield return null;
            Check(Game.Data.scene=="AmazoniaAcuatica","No se puede caminar sobre el lago terrestre");
        }
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.15f);}
        void Log(string s,string trace,LogType t){if(t==LogType.Error||t==LogType.Exception)report.failed.Add(s+trace);}
        void Update(){if(deadline>0&&Time.realtimeSinceStartup>deadline){Check(false,"Plazo del ensayo");Finish();}}
        void Finish(){deadline=0;Application.logMessageReceived-=Log;File.WriteAllText(Path.Combine(output,"validacion.json"),JsonUtility.ToJson(report,true));Application.Quit(report.failed.Count==0?0:1);}
    }
}
#endif
