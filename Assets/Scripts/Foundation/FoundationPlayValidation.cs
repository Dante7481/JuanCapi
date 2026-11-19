#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JuanCapi
{
    public sealed class FoundationPlayValidation : MonoBehaviour
    {
        [Serializable] private class Report{public List<string> passed=new List<string>();public List<string> failed=new List<string>();}
        private Report report=new Report();
        private string output;
        private float deadline;
        private GameRuntime Game=>GameRuntime.Instance;
        private void Check(bool value,string name){(value?report.passed:report.failed).Add(name);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-juanValidate");
            if(index<0||index+1>=args.Length||!args.Contains("-juanDataPath"))return;
            var go=new GameObject("Validación aislada");DontDestroyOnLoad(go);go.AddComponent<FoundationPlayValidation>().output=Path.GetFullPath(args[index+1]);
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;
            Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            deadline=Time.realtimeSinceStartup+100;
            yield return null;yield return new WaitForSecondsRealtime(1);
            Check(Game.UI.ScreenName=="menu","Menú visible al arrancar el ejecutable");
            yield return Capture("01-menu");
            Game.NewCampaign(true);yield return WaitScene();
            yield return new WaitForSecondsRealtime(.7f);
            while(SierraLevel.Current!=null&&SierraLevel.Current.IntroRunning)yield return null;
            Check(Game.Player!=null&&Game.Data!=null&&Game.Data.healthQuarters==12,"Nueva campaña llega al nivel con tres corazones");
            Check(!Game.Data.sword&&!Game.Data.dash&&!Game.Data.gun&&!Game.Data.shield,"Equipo inicial bloqueado");
            Game.Player.DamageQuarters(Game.Player.transform.position+Vector3.right,1);
            Check(Game.Data.healthQuarters==11,"Daño real de un cuarto de corazón");
            Game.Player.DamageQuarters(Game.Player.transform.position+Vector3.right,4);
            Check(Game.Data.healthQuarters==11,"Invulnerabilidad evita daño doble");
            yield return new WaitForSecondsRealtime(.8f);
            yield return Capture("02-corazones");
            Game.UI.Pause();double before=Game.Data.activeSeconds;var position=Game.Player.transform.position;
            yield return new WaitForSecondsRealtime(.4f);
            Check(Game.Data.activeSeconds==before&&Game.Player.transform.position==position,"Pausa congela reloj y físicas");
            Game.UI.SystemMenu();yield return null;Click("Guardar");yield return null;
            Check(EventSystem.current.currentSelectedGameObject.name=="Cancelar","Confirmación selecciona Cancelar inicialmente");
            yield return Capture("09-confirmar-guardado");
            Click("Guardar");yield return new WaitForSecondsRealtime(.15f);
            Check(Game.Store.Records().Count(r=>r.kind=="Manual")==1,"Guardar desde interfaz crea un único manual");
            Game.UI.Inventory();yield return Capture("03-inventario");
            Game.UI.Settings(Game.UI.Pause);yield return Capture("04-ajustes");
            Game.Settings.uiScale=1.25f;Game.UI.Settings(Game.UI.Pause);yield return Capture("07-ajustes-grandes");
            Game.UI.Inventory();yield return Capture("08-inventario-grande");
            Game.Settings.uiScale=1;Game.UI.Settings(Game.UI.Pause);
            Click("Pantalla");yield return null;Click("1280 × 720");yield return null;
            Check(Game.UI.ScreenName=="confirm","Cambio de resolución pide confirmación");
            yield return new WaitForSecondsRealtime(15.5f);
            Check(Game.UI.ScreenName=="display","Resolución revierte sin confirmación a los 15 segundos");
            Game.UI.Play();yield return null;
            Check(Game.Transaction(d=>d.Grant(CampaignReward.Sword),"Espada de prueba"),"Recompensa en sesión se autoguarda");
            var manual=Game.Store.Records().Single(r=>r.kind=="Manual");
            Check(!manual.data.sword,"Autoguardado no cambia el manual anterior");
            int goldBefore=Game.Data.gold;
            using(var locked=new FileStream(Game.Store.FilePath,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                Check(!Game.Transaction(d=>{d.gold+=10;return true;},"Escritura bloqueada"),"Fallo de escritura muestra error en transacción real");
                Check(Game.Data.gold==goldBefore,"Fallo de escritura conserva estado de sesión");
            }
            Game.UI.Play();yield return null;
            Game.UI.History(Game.UI.Pause);yield return Capture("05-guardados");
            var record=Game.Store.Records()[0];Game.LoadRecord(record);yield return WaitScene();
            Check(Game.Data.sword&&Game.Data.healthQuarters==11,"Cargar restaura vida fraccionada y equipo");
            Game.UI.Inventory();Game.UI.Play();yield return null;
            Check(Time.timeScale==1&&!Game.UI.IsOpen,"Cerrar inventario devuelve control");
            Game.Data.healthQuarters=0;yield return null;yield return null;
            Check(Game.UI.ScreenName=="death"&&Game.Data.deaths==1,"Derrota abre opciones una sola vez");
            Game.Retry();yield return WaitScene();
            Check(Game.Data.healthQuarters==12&&Game.Data.deaths==1,"Reintento conserva muerte y restaura tres corazones");
            var pickup=FindObjectsByType<CampaignPickup>(FindObjectsSortMode.None).FirstOrDefault(p=>p.item=="piece");
            if(pickup!=null)
            {
                Game.Player.transform.position=pickup.transform.position;yield return new WaitForSecondsRealtime(.15f);
                Check(Game.Data.heartPieces==1&&!pickup.gameObject.activeSelf,"Pieza de corazón se recoge mediante físicas reales");
            }
            else Check(false,"Existe pieza de corazón en el nivel de integración");
            Game.UI.History(Game.UI.Pause);yield return Capture("06-historial-final");
            Check(FindObjectsByType<GameRuntime>(FindObjectsSortMode.None).Length==1,"Una sola instancia de servicios tras recargar");
            Check(FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Count(e=>e.enabled)==1,"Un solo EventSystem activo");
            File.WriteAllText(Path.Combine(output,"validacion-jugable.json"),JsonUtility.ToJson(report,true));
            Application.logMessageReceived-=Log;
            Application.Quit(report.failed.Count==0?0:1);
        }
        private IEnumerator WaitScene(){yield return null;float until=Time.realtimeSinceStartup+30;while(Game.Busy&&Time.realtimeSinceStartup<until)yield return null;yield return null;Check(!Game.Busy,"Carga termina sin bloqueo");}
        private IEnumerator Capture(string name){yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        private void Click(string label)
        {
            var button=FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.gameObject.name==label&&b.interactable);
            if(button==null){report.failed.Add("Botón no encontrado: "+label);return;}button.onClick.Invoke();
        }
        private void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.failed.Add(message+"\n"+stack);}
        private void Update()
        {if(deadline>0&&Time.realtimeSinceStartup>deadline){report.failed.Add("Tiempo máximo de validación superado");File.WriteAllText(Path.Combine(output,"validacion-jugable.json"),JsonUtility.ToJson(report,true));Application.Quit(1);}}
    }
}
#endif
