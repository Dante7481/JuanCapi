using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace JuanCapi.Editor
{
    public static class MineValidation
    {
        [Serializable] private class Report{public List<string> passed=new List<string>();public List<string> failed=new List<string>();public string build;}
        [Serializable] private class LegacyEnvelope{public int version=3;public string payload,sha256;}
        [MenuItem("Juan Capi/Nivel 3/Verificar y compilar")]
        public static void RunAndBuild()
        {
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Entregas/Fase7"));Directory.CreateDirectory(output);
            FoundationValidation.RunAndBuild();MineBuilder.Build();var report=new Report();Action<bool,string> check=(ok,name)=>(ok?report.passed:report.failed).Add(name);
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/MinerTown.unity");var level=UnityEngine.Object.FindFirstObjectByType<MineLevel>();
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Script: "+t.name);
            check(level!=null&&level.boss!=null&&level.view!=null&&level.epilogueRoot!=null,"Director, cámara y epílogo conectados");
            check(level.props.All(p=>p!=null)&&level.props.Count(p=>p.kind==MinePropKind.Luma)==1&&level.props.Count(p=>p.kind==MinePropKind.FinalValve)==1,"Rescate e interacción final únicos");
            check(level.boss.health==42,"Director comienza con 42 puntos");
            check(UnityEngine.Object.FindObjectsByType<MineEnemy>(FindObjectsSortMode.None).Length==3,"Dos guardias industriales y un dron de recorrido");
            check(UnityEngine.Object.FindObjectsByType<MinePress>(FindObjectsSortMode.None).Length==1,"Prensa con aviso y retorno seguro");
            check(UnityEngine.Object.FindObjectsByType<MineEmitter>(FindObjectsSortMode.None).Length==3,"Tres emisores conectados sin scripts faltantes");
            var pickups=UnityEngine.Object.FindObjectsByType<CampaignPickup>(FindObjectsSortMode.None);
            check(pickups.Select(p=>p.persistentId).Distinct().Count()==pickups.Length,"Recursos con identificadores únicos");
            check(pickups.Count(p=>p.persistentId.EndsWith("_GOLD"))==3&&pickups.Count(p=>p.item=="record")==2,"Tres secretos y dos registros");
            check(pickups.Where(p=>p.item=="gold").Sum(p=>p.amount)==70&&pickups.Where(p=>p.item=="scrap").Sum(p=>p.amount)==15,"Presupuesto opcional: 70 oro y 15 chatarra");
            check(level.IsWater(new Vector3(58,-4))&&!level.IsWater(new Vector3(74,0)),"Límites de movimiento acuático y terrestre");
            check(level.BeltAt(new Vector3(36,.05f))==1.8f&&level.BeltAt(new Vector3(36,1))==0,"Cinta afecta solo contacto con suelo");
            var d=new CampaignData{sword=true,dash=true,gun=true,shield=true,scene="MinerTown",checkpoint="CP_M01",hasPosition=true,x=-30};level.Prepare(d,false);
            check(d.x==3&&d.mineLayout==7&&d.IsValid(out _),"Migración de posición a acceso seguro");
            var invalid=d.Clone();invalid.epilogueSeen=true;check(!invalid.IsValid(out _),"Epílogo visto exige campaña completada");
            string legacy=Path.Combine(output,"PruebasDatos",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(legacy);
            var archive=new CampaignArchive{schemaVersion=3,revision=1};archive.automatic.Add(new SaveRecord{id="old-swamp",kind="Autoguardado",revision=1,label="Pantano",savedUtc=DateTime.UtcNow.ToString("o"),data=new CampaignData{sword=true,dash=true,gun=true,shield=true,swampFinished=true,swampLayout=6}});
            string payload=JsonUtility.ToJson(archive);foreach(string field in new[]{"mineLayout","mineStarted","mineSeconds","campaignSeconds","epilogueSeen"})payload=System.Text.RegularExpressions.Regex.Replace(payload,"\\\""+field+"\\\":[^,}]+,?","");
            string hash;using(var sha=System.Security.Cryptography.SHA256.Create())hash=Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload)));
            File.WriteAllText(Path.Combine(legacy,"campaign.json"),JsonUtility.ToJson(new LegacyEnvelope{payload=payload,sha256=hash}));
            var store=new CampaignStore(legacy);check(!store.IsBlocked&&store.Records()[0].data.shield&&store.Records()[0].data.mineLayout==0,"Formato 3 conserva el progreso del pantano");
            check(store.Save(store.Records()[0].data,false,"Actualización",null,out _)&&File.ReadAllText(store.FilePath).Contains("\"version\":4"),"Nueva escritura actualiza a formato 4");
            File.WriteAllText(Path.Combine(output,"validacion-escena.json"),JsonUtility.ToJson(report,true));if(report.failed.Count>0)throw new Exception(string.Join("\n",report.failed));
            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");var build=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=Path.Combine(output,"Juego/Windows/JuanCapi.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            report.build=build.summary.result.ToString();File.WriteAllText(Path.Combine(output,"validacion-escena.json"),JsonUtility.ToJson(report,true));if(build.summary.result!=BuildResult.Succeeded)throw new Exception("MinerTown build failed: "+build.summary.result);
            Debug.Log("MINE VALIDATION: "+report.passed.Count+" PASS, "+report.failed.Count+" FAIL");
        }
    }
}
