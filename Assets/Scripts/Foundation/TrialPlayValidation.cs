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
    public sealed class TrialPlayValidation:MonoBehaviour
    {
        [Serializable] private class Report{public List<string> passed=new List<string>();public List<string> failed=new List<string>();}
        private readonly Report report=new Report();private string output;private float deadline;private bool done;
        private GameRuntime Game=>GameRuntime.Instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void Boot()
        {
            var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"-juanTrialValidate");if(i<0||i+1>=a.Length||!a.Contains("-juanDataPath"))return;
            var o=new GameObject("Verificación aislada de retos");DontDestroyOnLoad(o);o.AddComponent<TrialPlayValidation>().output=Path.GetFullPath(a[i+1]);
        }
        private void Check(bool ok,string name){(ok?report.passed:report.failed).Add(name);Debug.Log("TRIAL "+(ok?"PASS ":"FAIL ")+name);}
        private string Fingerprint(){using(var sha=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(Game.Store.FilePath)));}
        private IEnumerator Start()
        {
            Application.runInBackground=true;deadline=Time.realtimeSinceStartup+180;Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            yield return new WaitForSecondsRealtime(1);Check(CampaignAudio.Current.LoadedClips==40&&CampaignAudio.Current.Track=="menu","Banco de audio y música de menú cargados");
            yield return MeasureAudio(false);
            Check(!Game.TrialUnlocked(1)&&!Game.TrialUnlocked(2)&&!Game.TrialUnlocked(3),"Instalación limpia mantiene retos bloqueados");
            var seed=new CampaignData{sword=true,dash=true,gun=true,shield=true,sierraFinished=true,swampFinished=true,completed=true,maxHearts=6,healthQuarters=21,fruit=3,cells=2,gold=200,scrap=30,fastReload=true,reinforcedShield=true,introSeen=true,epilogueSeen=true};
            seed.rescues.AddRange(new[]{"MARA","TICO","LUMA"});Check(Game.Store.StartNew(seed,true,out _),"Fixture de campaña completa guardada");
            string campaign=Fingerprint();Check(Game.TrialUnlocked(1)&&Game.TrialUnlocked(2)&&Game.TrialUnlocked(3),"Completar campaña desbloquea tres retos");
            Game.UI.TrialMenu();yield return Capture("01-retos");Click("Correr Sierra");yield return WaitScene();yield return new WaitForSeconds(.3f);
            Check(Game.IsTrial&&Game.TrialChapter==1&&!Game.Data.sword&&!Game.Data.dash&&!Game.Cinematic,"Sierra empieza sin equipo y omite introducción");
            Check(Game.Data.maxHearts==5&&Game.Data.healthQuarters==20&&Game.Data.fruit==0&&!Game.Data.fastReload,"Vida y provisiones normalizadas");
            Check(!Game.Save(true,"No escribir"),"Guardado manual bloqueado durante reto");
            Check(Game.Transaction(d=>{d.Grant(CampaignReward.Sword);d.fruit=3;d.gold=500;d.checkpoint="CP_B02";d.x=11;d.y=.08f;d.hasPosition=true;return true;},"Checkpoint de prueba"),"Checkpoint temporal admite progresión");
            Check(Game.Data.fruit==0&&Game.Data.gold==0&&Game.Data.sword,"Recompensas conservan equipo fijo sin consumibles");
            double before=Game.Data.activeSeconds;Game.UI.Pause();yield return new WaitForSecondsRealtime(.4f);Check(Game.Data.activeSeconds==before,"Pausa excluida del reloj del reto");
            Game.UI.SystemMenu();Check(Game.UI.ScreenName=="trial-system"&&!FindObjectsByType<Button>().Any(b=>b.name=="Guardar"),"Sistema de reto sustituye guardado por reinicio");yield return Capture("02-sistema-reto");Game.UI.Play();yield return new WaitForSeconds(.3f);
            Game.Player.DamageQuarters(Game.Player.Center+Vector2.left,100);yield return new WaitForSecondsRealtime(.2f);Check(Game.UI.ScreenName=="death"&&Game.Data.deaths==1,"Derrota se registra una sola vez");
            double elapsed=Game.Data.activeSeconds;Game.Retry();yield return WaitScene();yield return new WaitForSeconds(.15f);
            Check(Game.Data.sword&&Game.Data.checkpoint=="CP_B02"&&Game.Data.healthQuarters==20&&Game.Data.deaths==1&&Game.Data.activeSeconds>=elapsed,"Reintento recupera checkpoint y conserva reloj y derrota");
            Check(Fingerprint()==campaign,"Recoger equipo y morir no modifican campaña");
            Game.CompleteTrial();Check(Game.TrialFinished&&Game.UI.ScreenName=="trial-result"&&Game.TrialResult.RankedMilliseconds==(long)Math.Round((Game.TrialResult.seconds+15)*1000,MidpointRounding.AwayFromZero),"Resultados incluyen penalización exacta");yield return Capture("03-marca");
            Check(new TrialRecords(Game.DataDirectory).Best(seed.campaignId,1)!=null,"Mejor marca persiste en archivo independiente");
            Game.StartTrial(2);yield return WaitScene();yield return new WaitForSeconds(.25f);
            Check(Game.Data.sword&&Game.Data.dash&&!Game.Data.gun&&!Game.Data.shield&&Game.Data.maxHearts==5,"Pantano comienza con espada y dash sin mejoras");
            Game.Transaction(d=>{d.gun=true;d.collected.AddRange(new[]{"P05_TARGET_1","P05_TARGET_2","P05_TARGET_3"});return true;},"Fixture de acceso al agua");
            double land=Game.Data.activeSeconds;Check(SwampLevel.Current.Dive(),"Reto de pantano admite transición al agua");yield return WaitScene();yield return new WaitForSeconds(.25f);
            Check(Game.IsTrial&&Game.TrialChapter==2&&Game.Player.aquatic&&Game.Data.activeSeconds>=land,"Tierra y agua comparten intento y reloj");
            Check(Fingerprint()==campaign,"Viaje acuático no escribe en campaña");Game.StartTrial(2);yield return WaitScene();yield return new WaitForSeconds(.2f);
            Check(!Game.Data.gun&&!Game.Player.aquatic&&Game.Data.deaths==0&&Game.Data.activeSeconds<1,"Reiniciar restablece recorrido terrestre y tiempo");
            Game.ToMenu();yield return WaitScene();Check(!Game.IsTrial&&Game.Trials.Best(seed.campaignId,2)==null&&Fingerprint()==campaign,"Cancelar intento no crea marca ni altera campaña");
            Game.StartTrial(3);yield return WaitScene();yield return new WaitForSeconds(.25f);
            Check(Game.Data.sword&&Game.Data.dash&&Game.Data.gun&&Game.Data.shield&&!Game.Data.maintenanceKey&&!Game.Data.rescues.Contains("LUMA")&&Game.Data.reloadRemaining==0&&Game.Data.MaximumShield==100,"MinerTown equipa todo y exige rescatar a Luma");
            Check(CampaignAudio.Current.Track=="mine"&&CampaignAudio.Current.EnvironmentTrack=="factory","Audio cambia al entorno industrial");
            int effects=CampaignAudio.Current.PlayedEffects;CampaignAudio.Play("shot");yield return null;Check(CampaignAudio.Current.PlayedEffects>effects,"Efectos emiten voces durante juego");
            Game.UI.Pause();effects=CampaignAudio.Current.PlayedEffects;CampaignAudio.Play("shot");yield return new WaitForSecondsRealtime(.1f);Check(CampaignAudio.Current.PlayedEffects==effects,"Pausa bloquea efectos jugables nuevos");CampaignAudio.Play("confirm",true);Check(CampaignAudio.Current.PlayedEffects>effects,"Interfaz conserva sonidos durante pausa");
            Game.Settings.master=0;Game.Settings.Apply();Check(AudioListener.volume==0,"Silencio general se aplica al motor de audio");yield return new WaitForSecondsRealtime(.3f);yield return MeasureAudio(true);Game.Settings.master=.8f;Game.Settings.Apply();Game.UI.Play();
            Game.Transaction(d=>{d.defeated.Add("DIRECTOR");d.maintenanceKey=true;d.rescues.Add("LUMA");return true;},"Fixture de cierre final");
            Check(MineLevel.Current.CloseDischarge()&&Game.TrialFinished&&!Game.Cinematic&&Game.UI.ScreenName=="trial-result","Válvula termina reto sin epílogo");
            Check(Fingerprint()==campaign,"Campaña byte por byte intacta tras tres modalidades");
            Game.LoadRecord(Game.Store.Records()[0]);yield return WaitScene();yield return new WaitForSecondsRealtime(.25f);
            Check(!Game.IsTrial&&Game.Data.gold==200&&Game.Data.fruit==3&&Game.Data.fastReload&&Game.Data.maxHearts==6,"Volver a campaña restaura inventario y mejoras originales");Finish();
        }
        private void Click(string label){var b=FindObjectsByType<Button>().FirstOrDefault(x=>x.name==label&&x.interactable);Check(b!=null,"Botón disponible: "+label);b?.onClick.Invoke();}
        private IEnumerator MeasureAudio(bool silent)
        {
            var samples=new float[1024];float peak=0;double energy=0;int count=0;bool finite=true;float end=Time.realtimeSinceStartup+1.5f;
            while(Time.realtimeSinceStartup<end){AudioListener.GetOutputData(samples,0);foreach(float sample in samples){finite&=!float.IsNaN(sample)&&!float.IsInfinity(sample);peak=Mathf.Max(peak,Mathf.Abs(sample));energy+=sample*sample;count++;}yield return null;}
            File.WriteAllText(Path.Combine(output,silent?"audio-silencio.txt":"audio-mezcla.txt"),"peak="+peak+" rms="+Math.Sqrt(energy/Math.Max(1,count))+" samples="+count);
            Check(finite&&(silent?peak<.00001f:peak>.0001f&&peak<.99f),silent?"Salida de audio realmente silenciosa con volumen cero":"Mezcla de audio produce señal finita sin saturar");
        }
        private IEnumerator WaitScene(){yield return null;float limit=Time.realtimeSinceStartup+20;while(Game.Busy&&Time.realtimeSinceStartup<limit)yield return null;yield return null;}
        private IEnumerator Capture(string label){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,label+".png"));yield return new WaitForSecondsRealtime(.1f);}
        private void Log(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)report.failed.Add(message+"\n"+trace);}
        private void Update(){if(!done&&deadline>0&&Time.realtimeSinceStartup>deadline){Check(false,"Tiempo máximo del ensayo");Finish();}}
        private void Finish(){done=true;File.WriteAllText(Path.Combine(output,"validacion-jugable.json"),JsonUtility.ToJson(report,true));Debug.Log("TRIAL PLAY RESULT: "+report.passed.Count+" PASS, "+report.failed.Count+" FAIL");Application.logMessageReceived-=Log;Application.Quit(report.failed.Count==0?0:1);}
    }
}
#endif
