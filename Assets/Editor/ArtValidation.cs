using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using JuanCapi;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JuanCapi.Editor
{
    public static class ArtValidation
    {
        [Serializable] private class Report {public List<string> passed=new List<string>();public List<string> failed=new List<string>();public int sprites,clips,prefabs;public string build;}
        public static void RunAndBuild()
        {
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Entregas/Fase4"));Directory.CreateDirectory(output);
            var report=new Report();Action<bool,string> check=(pass,name)=>(pass?report.passed:report.failed).Add(name);
            var art=CampaignArt.Current;check(art!=null,"Catálogo de arte disponible desde Resources");
            if(art!=null)
            {
                check(art.equipment.Length==16&&art.equipment.All(x=>x!=null),"Equipo: 16 sprites enlazados");
                check(art.terrain.Length==12&&art.terrain.All(x=>x!=null),"Entornos: 12 módulos enlazados");
                check(new[]{art.sierra,art.swamp,art.underwater,art.mine}.All(x=>x!=null),"Cuatro ambientes completos en el proyecto");
                check(art.enemyMaterial!=null&&art.enemyMaterial.shader!=null,"Material de enemigos instalado");
                check(art.aimBodies.Length==8&&art.aimBodies.All(x=>x!=null),"Ocho cuerpos preparados para brazos independientes");
            }
            foreach(string path in AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/Art/Campaign","Assets/Animations"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);report.clips++;
                var bindings=AnimationUtility.GetObjectReferenceCurveBindings(clip);
                bool valid=bindings.Length>0&&bindings.All(b=>AnimationUtility.GetObjectReferenceCurve(clip,b).All(k=>k.value!=null));
                check(valid,"Animación con todos sus fotogramas: "+path);
            }
            report.sprites=AssetDatabase.FindAssets("t:Sprite",new[]{"Assets/Art/Campaign/Sprites","Assets/Animations"}).Length;
            foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Art/Campaign/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var prefab=PrefabUtility.LoadPrefabContents(path);report.prefabs++;
                check(prefab.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"Prefab completo: "+Path.GetFileName(path));
                PrefabUtility.UnloadPrefabContents(prefab);
            }
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled))
            {
                var scene=EditorSceneManager.OpenScene(entry.path);int missing=0;
                foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                check(missing==0,"Escena sin scripts faltantes: "+entry.path);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
            File.WriteAllText(Path.Combine(output,"validacion-arte.json"),JsonUtility.ToJson(report,true));
            if(report.failed.Count>0)throw new Exception(string.Join("\n",report.failed));
            FoundationValidation.RunAndBuild();
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=Path.Combine(output,"Juego/Windows/JuanCapi.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            report.build=result.summary.result.ToString();File.WriteAllText(Path.Combine(output,"validacion-arte.json"),JsonUtility.ToJson(report,true));
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+result.summary.result);
            Debug.Log("ART VALIDATION: "+report.passed.Count+" PASS, "+report.failed.Count+" FAIL");
        }
    }
}
