#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace JuanCapi
{
    public sealed class ArtPlayValidation : MonoBehaviour
    {
        [Serializable] private class Report{public List<string> passed=new List<string>();public List<string> failed=new List<string>();}
        private readonly Report report=new Report();private string output;private float deadline;
        private GameRuntime Game=>GameRuntime.Instance;
        private void Check(bool pass,string name)=>(pass?report.passed:report.failed).Add(name);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-juanArtReview");
            if(index<0||index+1>=args.Length||!args.Contains("-juanDataPath"))return;
            var go=new GameObject("Revisión de arte aislada");DontDestroyOnLoad(go);go.AddComponent<ArtPlayValidation>().output=Path.GetFullPath(args[index+1]);
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(output);deadline=Time.realtimeSinceStartup+180;Application.logMessageReceived+=Log;
            yield return new WaitForSecondsRealtime(1);
            Check(Game.UI.ScreenName=="menu","Menú de inicio disponible");yield return Capture("01-menu");
            Game.NewCampaign(true);yield return WaitScene();yield return new WaitForSecondsRealtime(.8f);
            var driver=Game.Player.GetComponent<PlayerArtDriver>();
            Check(driver!=null&&driver.actor!=null&&driver.actor.animator.runtimeAnimatorController==CampaignArt.Current.playerLand,"Juan usa las animaciones importadas");
            Check(FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None).Where(r=>r.GetComponent<TilemapCollider2D>()!=null).All(r=>!r.enabled),"El escenario visible usa los nuevos módulos");
            Check(Game.Data.region=="Quebrada de la sierra","Guardado identifica el nuevo entorno");yield return Capture("02-sierra-jugable");
            Game.Transaction(d=>d.Grant(CampaignReward.Sword),"Espada");Game.Player.Attack();yield return new WaitForSecondsRealtime(.2f);
            Check(driver.actor.CurrentState=="Melee","Ataque cambia a la animación de espada");yield return Capture("03-espada");
            yield return new WaitForSecondsRealtime(.4f);Game.UI.Inventory();yield return Capture("04-inventario");
            Game.Settings.uiScale=1.25f;Game.UI.Inventory();yield return Capture("05-inventario-125");Game.Settings.uiScale=1;
            Game.UI.Play();Game.Settings.Rebind("Atacar",KeyCode.J);yield return new WaitForSecondsRealtime(.1f);
            Check(FindObjectsByType<InputHintView>(FindObjectsSortMode.None).Where(v=>v.action=="Atacar").All(v=>v.label.text=="J"&&!v.mouse.gameObject.activeSelf),"Pista gráfica respeta la reasignación de ataque");
            Game.Settings.Rebind("Atacar",KeyCode.Mouse0);
            Game.Transaction(d=>d.Grant(CampaignReward.Dash),"Impulsor");Game.Transaction(d=>d.Grant(CampaignReward.PlasmaGun),"Disparador");Game.Transaction(d=>d.Grant(CampaignReward.PlasmaShield),"Escudo");
            Game.Data.equippedWeapon="gun";Game.NotifyChanged();yield return null;
            Game.Player.GetComponent<Rigidbody2D>().simulated=false;driver.enabled=false;
            for(int facing=0;facing<2;facing++)
            {
                var scale=Game.Player.transform.localScale;scale.x=Mathf.Abs(scale.x)*(facing==0?1:-1);Game.Player.transform.localScale=scale;
                for(int i=0;i<8;i++)
                {
                    var shoulder=driver.transform.Find("Arte de Juan/Hombro · apuntado libre");
                    float a=i*Mathf.PI/4;var dir=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);driver.AimAt(shoulder.position+dir*5);
                    Check(Vector3.Dot((driver.Muzzle-shoulder.position).normalized,dir)>.999f,"Cañón orientado a "+(i*45)+"° con cuerpo "+(facing==0?"derecha":"izquierda"));
                    if(facing==0)yield return Capture("apuntado-"+(i*45).ToString("000"));
                }
            }
            Game.UI.Inventory();yield return Capture("06-equipo-completo");
            foreach(string tab in new[]{"Módulos","Provisiones","Misión"})
            {
                var button=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.gameObject.name==tab);button.onClick.Invoke();yield return Capture("inventario-"+tab);
            }
            foreach(string scene in new[]{"Sierra","Pantano","PantanoAcuatico","MinerTown"})
            {
                var op=SceneManager.LoadSceneAsync(scene);while(!op.isDone)yield return null;yield return new WaitForSecondsRealtime(.6f);
                Check(Game.UI.ScreenName=="art"&&Game.Data==null,"Revisión visual aislada: "+scene);yield return Capture("ambiente-"+scene);
            }
            var stage=FindFirstObjectByType<ArtPreviewScene>();Destroy(stage.gameObject);yield return null;
            Camera.main.transform.position=new Vector3(0,3,-10);Camera.main.orthographicSize=5;Camera.main.backgroundColor=new Color(.045f,.08f,.1f);
            var art=CampaignArt.Current;var gallery=new GameObject("Catálogo");
            for(int i=0;i<6;i++)
            {
                var actor=AnimatedActor.Create("Enemigo "+i,gallery.transform,art.enemyControllers[i],art.enemies[i]);actor.sprite.sharedMaterial=art.enemyMaterial;
                actor.transform.position=new Vector3(-6+(i%3)*6,i<3?4:0,0);
            }
            yield return new WaitForSecondsRealtime(.2f);yield return Capture("enemigos-fondo-oscuro");
            Camera.main.backgroundColor=new Color(.82f,.86f,.83f);yield return Capture("enemigos-fondo-claro");
            Destroy(gallery);yield return null;
            File.WriteAllText(Path.Combine(output,"validacion-visual.json"),JsonUtility.ToJson(report,true));Application.logMessageReceived-=Log;Application.Quit(report.failed.Count==0?0:1);
        }
        private IEnumerator WaitScene(){yield return null;while(Game.Busy)yield return null;yield return null;}
        private IEnumerator Capture(string name){yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        private void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.failed.Add(text+"\n"+stack);}
        private void Update(){if(deadline>0&&Time.realtimeSinceStartup>deadline){report.failed.Add("Revisión excedió el tiempo máximo");File.WriteAllText(Path.Combine(output,"validacion-visual.json"),JsonUtility.ToJson(report,true));Application.Quit(1);}}
    }
}
#endif
