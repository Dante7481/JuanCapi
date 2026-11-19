using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JuanCapi.Editor
{
    public static class SierraValidation
    {
        [Serializable] private class Report{public List<string> passed=new List<string>();public List<string> failed=new List<string>();public string build;}
        [MenuItem("Juan Capi/Nivel 1/Construir y verificar entrega")]
        public static void RunAndBuild()
        {
            SierraBuilder.BuildLevel();var report=new Report();
            Action<bool,string> check=(ok,name)=>(ok?report.passed:report.failed).Add(name);
            var level=UnityEngine.Object.FindFirstObjectByType<SierraLevel>();
            check(level!=null&&level.boss!=null&&level.view!=null,"Director de nivel, jefe y cámara conectados");
            foreach(var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach(var t in go.GetComponentsInChildren<Transform>(true))check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Scripts: "+t.name);
            var ids=UnityEngine.Object.FindObjectsByType<CampaignPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(p=>p.persistentId).ToArray();
            check(ids.Distinct().Count()==ids.Length,"Identificadores de objetos únicos");check(ids.Count(x=>x.StartsWith("REC_B"))==2,"Dos pruebas narrativas");
            check(ids.Count(x=>x.StartsWith("SEC_B")&&x.EndsWith("_GOLD"))==3,"Tres secretos con presupuesto 15/20/15");
            var migration=new CampaignData{sword=true,rescues=new List<string>{"MARA"},x=-28,y=-9,hasPosition=true};level.Prepare(migration);
            check(migration.sierraLayout==5&&migration.x==34&&migration.rescues.Contains("MARA"),"Guardado anterior migra al taller sin perder rescate");
            var won=new CampaignData{sword=true,dash=true};level.Prepare(won);check(won.x==96&&won.defeated.Contains("RASTREADOR"),"Dash anterior migra después del jefe");
            var invalid=new CampaignData{sierraFinished=true};check(!invalid.IsValid(out _),"No acepta nivel terminado sin recompensa");
            var boss=level.boss;check(boss.health==24,"Rastreador comienza con 24 puntos");
            foreach(var prop in level.props)check(prop!=null,"Interacción conectada");
            string output=SierraBuilder.Output;File.WriteAllText(Path.Combine(output,"validacion-escena.json"),JsonUtility.ToJson(report,true));
            if(report.failed.Count>0)throw new Exception(string.Join("\n",report.failed));
            FoundationValidation.RunAndBuild();
            var build=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=Path.Combine(output,"Juego/Windows/JuanCapi.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            report.build=build.summary.result.ToString();File.WriteAllText(Path.Combine(output,"validacion-escena.json"),JsonUtility.ToJson(report,true));
            if(build.summary.result!=BuildResult.Succeeded)throw new Exception("Sierra build failed: "+build.summary.result);
            Debug.Log("SIERRA VALIDATION: "+report.passed.Count+" PASS, "+report.failed.Count+" FAIL");
        }
    }
}
