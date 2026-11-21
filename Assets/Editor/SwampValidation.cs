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
    public static class SwampValidation
    {
        [Serializable] private class Report{public List<string> passed=new List<string>();public List<string> failed=new List<string>();public string build;}
        [MenuItem("Juan Capi/Nivel 2/Verificar y compilar")]
        public static void RunAndBuild()
        {
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Entregas/Fase6"));Directory.CreateDirectory(output);
            FoundationValidation.RunAndBuild();SwampBuilder.Build();var report=new Report();Action<bool,string> check=(ok,name)=>(ok?report.passed:report.failed).Add(name);
            var allIds=new List<string>();
            foreach(bool water in new[]{false,true})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+(water?"AmazoniaAcuatica":"AmazoniaTerrestre")+".unity");
                var l=UnityEngine.Object.FindFirstObjectByType<SwampLevel>();var p=UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                check(l!=null&&l.boss!=null&&l.props.All(x=>x!=null),"Director y conexiones: "+scene.name);
                check(p.aquatic==water&&p.GetComponent<WeaponCombat>()!=null,"Modo de movimiento y equipo: "+scene.name);
                check(l.boss.Maximum==(water?24:30),"Resistencia del caimán: "+scene.name);
                foreach(var go in scene.GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true))
                    check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Script: "+scene.name+"/"+t.name);
                check(UnityEngine.Object.FindObjectsByType<UnityEngine.Video.VideoPlayer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==0,"Sin video ni reproductor heredado: "+scene.name);
                check(UnityEngine.Object.FindObjectsByType<SwampEnemy>(FindObjectsSortMode.None).Length==2,"Dos enemigos de recorrido: "+scene.name);
                var ids=UnityEngine.Object.FindObjectsByType<CampaignPickup>(FindObjectsSortMode.None).Select(x=>x.persistentId);allIds.AddRange(ids);
                var d=new CampaignData{sword=true,dash=true,gun=water,scene=scene.name,checkpoint=water?"CP_A01":"CP_P01",hasPosition=true,x=-20};
                l.Prepare(d,false);check(d.swampLayout==6&&d.x==3&&d.IsValid(out _),"Migración al punto seguro: "+scene.name);
                if(water)
                {
                    check(l.rescueGate!=null&&l.tico!=null&&l.tico.transform.position.y>14.5f,"Tico dentro del refugio de aire");
                    check(l.props.Count(x=>x.kind==SwampPropKind.Latch)==2,"Dos cierres para rescatar a Tico");
                    check(l.props.Any(x=>x.kind==SwampPropKind.Receiver&&x.GetComponent<ShieldPracticeEmitter>()!=null),"Práctica física de reflexión conectada");
                }
                else check(l.props.Count(x=>x.kind==SwampPropKind.Target)==3,"Tres blancos de puntería independiente");
            }
            check(allIds.Distinct().Count()==allIds.Count,"Objetos únicos entre ambas escenas");
            check(allIds.Count(x=>x.EndsWith("_GOLD"))==3&&allIds.Count(x=>x.StartsWith("REC_"))==2,"Tres secretos y dos registros en todo el pantano");
            var invalid=new CampaignData{swampFinished=true};check(!invalid.IsValid(out _),"Final de pantano exige recompensa de escudo");
            var data=new CampaignData{sword=true,dash=true,gun=true,swampLayout=6,ammunition=0,reloadRemaining=3.2f};
            var store=new CampaignStore(Path.Combine(output,"PruebasDatos",Guid.NewGuid().ToString("N")));
            check(store.StartNew(data,false,out _)&&new CampaignStore(Path.GetDirectoryName(store.FilePath)).Records()[0].data.reloadRemaining==3.2f,"Recarga y versión persisten al reiniciar");
            File.WriteAllText(Path.Combine(output,"validacion-escenas.json"),JsonUtility.ToJson(report,true));
            if(report.failed.Count>0)throw new Exception(string.Join("\n",report.failed));
            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
            var build=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=Path.Combine(output,"Juego/Windows/JuanCapi.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            report.build=build.summary.result.ToString();File.WriteAllText(Path.Combine(output,"validacion-escenas.json"),JsonUtility.ToJson(report,true));
            if(build.summary.result!=BuildResult.Succeeded)throw new Exception("Pantano build failed: "+build.summary.result);
            Debug.Log("SWAMP VALIDATION: "+report.passed.Count+" PASS, "+report.failed.Count+" FAIL");
        }
    }
}
