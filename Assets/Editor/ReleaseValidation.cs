using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace JuanCapi.Editor
{
    public static class ReleaseValidation
    {
        [Serializable] private class Report {public List<string> passed=new List<string>();public List<string> failed=new List<string>();public string build;}
        public static void RunAndBuild()
        {
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Entregas/Fase8"));Directory.CreateDirectory(output);
            if(Environment.GetCommandLineArgs().Contains("-juanRebuildSierra"))SierraBuilder.BuildLevel();
            FoundationValidation.RunAndBuild();var report=new Report();Action<bool,string> check=(v,s)=>(v?report.passed:report.failed).Add(s);
            var clips=Resources.LoadAll<AudioClip>("Audio");check(clips.Length==40,"40 pistas y efectos originales importados");
            foreach(var c in clips)check(c.samples>0&&c.channels==2&&c.length>.04f,"Audio válido: "+c.name);
            check(EditorBuildSettings.scenes.Count(s=>s.enabled)==9,"Nueve escenas incluidas");
            string dir=Path.Combine(output,"PruebasDatos",Guid.NewGuid().ToString("N"));var records=new TrialRecords(dir);
            var first=new TrialRecord{campaignId="trial-test",chapter=1,dateUtc=DateTime.UtcNow.ToString("o"),seconds=120.125,deaths=2};
            check(first.RankedMilliseconds==150125,"Penalización de 15 segundos y precisión de milisegundos");
            check(records.Record(first,out bool improved,out _)&&improved,"Primera marca se escribe");
            var tie=new TrialRecord{campaignId=first.campaignId,chapter=1,dateUtc=DateTime.UtcNow.AddDays(1).ToString("o"),seconds=150.125};
            check(records.Record(tie,out improved,out _)&&!improved&&records.Best(first.campaignId,1).dateUtc==first.dateUtc,"Empate conserva fecha original");
            tie.seconds=100;check(records.Record(tie,out improved,out _)&&improved,"Mejora reemplaza marca");
            var reload=new TrialRecords(dir);check(reload.Best(first.campaignId,1).seconds==100,"Marca persiste al abrir de nuevo");
            File.WriteAllText(Path.Combine(dir,"trials.json"),"invalid");reload=new TrialRecords(dir);
            check(!reload.Record(tie,out _,out _)&&File.ReadAllText(Path.Combine(dir,"trials.json"))=="invalid","Archivo de marcas dañado no se sobrescribe");
            check(new TrialRecord{chapter=1,seconds=180}.Medal=="Oro"&&new TrialRecord{chapter=1,seconds=180.001}.Medal=="Plata","Umbral de medalla preciso");
            File.WriteAllText(Path.Combine(output,"validacion-recursos.json"),JsonUtility.ToJson(report,true));if(report.failed.Count>0)throw new Exception(string.Join("\n",report.failed));
            bool release=Environment.GetCommandLineArgs().Contains("-juanRelease");
            var build=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=Path.Combine(output,release?"Juego/Windows/JuanCapi.exe":"Pruebas/Windows/JuanCapi.exe"),target=BuildTarget.StandaloneWindows64,options=release?BuildOptions.None:BuildOptions.Development});
            report.build=build.summary.result.ToString();File.WriteAllText(Path.Combine(output,"validacion-recursos.json"),JsonUtility.ToJson(report,true));if(build.summary.result!=BuildResult.Succeeded)throw new Exception("Release validation build failed");
            Debug.Log("RELEASE VALIDATION: "+report.passed.Count+" PASS, "+report.failed.Count+" FAIL");
        }
    }
}
