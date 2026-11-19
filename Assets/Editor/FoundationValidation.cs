using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JuanCapi;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JuanCapi.Editor
{
    public static class FoundationValidation
    {
        [Serializable] private class Report { public List<string> passed=new List<string>(); public List<string> failed=new List<string>(); public string build; }
        [Serializable] private class LegacyEnvelope {public int version=1;public string payload,sha256;}
        private static Report report;
        private static string output;
        private static void Check(bool value,string name) { if(value)report.passed.Add(name);else report.failed.Add(name); }
        public static void RunAndBuild()
        {
            output=Argument("-juanOutput")??Path.GetFullPath(Path.Combine(Application.dataPath,"../../Entregas/Fase3"));
            Directory.CreateDirectory(output); report=new Report();
            try { DataAndStorage(); AuditScenes(); }
            catch(Exception e){report.failed.Add(e.ToString());}
            File.WriteAllText(Path.Combine(output,"validacion-datos.json"),JsonUtility.ToJson(report,true));
            if(report.failed.Count>0)throw new Exception(string.Join("\n",report.failed));
            if(Argument("-juanSkipBuild")=="true")return;
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes=new[]{"Assets/Scenes/Menu.unity","Assets/Scenes/LV1.unity"},
                locationPathName=Path.Combine(output,"Juego/Windows/JuanCapi.exe"),
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development
            });
            report.build=result.summary.result.ToString();
            File.WriteAllText(Path.Combine(output,"validacion-datos.json"),JsonUtility.ToJson(report,true));
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+result.summary.result);
            Debug.Log($"FOUNDATION VALIDATION: {report.passed.Count} PASS, {report.failed.Count} FAIL. Build {report.build}");
        }
        private static void DataAndStorage()
        {
            var d=new CampaignData();Check(d.maxHearts==3&&d.healthQuarters==12,"Inicio con tres corazones completos");
            Check(!d.Grant(CampaignReward.Dash)&&!d.Grant(CampaignReward.PlasmaGun),"Orden de habilidades protegido");
            Check(d.Grant(CampaignReward.Sword)&&!d.Grant(CampaignReward.Sword),"Espada idempotente");
            Check(d.Grant(CampaignReward.Dash)&&d.maxHearts==4,"Rastreador concede dash y contenedor");
            Check(d.Grant(CampaignReward.PlasmaGun)&&d.Grant(CampaignReward.PlasmaShield)&&d.maxHearts==5,"Cadena de recompensas del caimán");
            Check(!d.Grant(CampaignReward.PlasmaShield)&&d.maxHearts==5,"Repetir recompensa no duplica corazones");
            d.healthQuarters-=3;Check(d.healthQuarters==17,"Representación de cuartos de corazón");
            for(int i=0;i<4;i++)Check(d.Collect("PIECE_"+i,"piece"),"Pieza única "+i);
            Check(d.maxHearts==6&&d.heartPieces==0&&d.healthQuarters==24,"Cuatro piezas crean un corazón y curan");
            Check(!d.Collect("PIECE_0","piece"),"Piezas no se duplican");
            Check(d.Collect("F1","fruit",3)&&!d.Collect("F2","fruit"),"Capacidad de provisiones respeta tres");
            Check(!d.Use("fruit")&&d.fruit==3,"Vida llena no gasta fruta");
            d.healthQuarters=2;Check(d.Use("fruit")&&d.healthQuarters==14&&d.fruit==2,"Fruta restaura tres corazones");
            d.rescues.Add("MARA");d.gold=200;d.scrap=50;
            Check(d.Purchase("reload","MARA")&&d.gold==120&&d.scrap==35,"Compra cobra exactamente una vez");
            Check(!d.Purchase("reload","MARA")&&d.gold==120,"Mejora permanente no duplicable");
            Check(d.IsValid(out _),"Estado completo válido");
            var invalid=d.Clone();invalid.gold=-1;Check(!invalid.IsValid(out _),"Cantidades negativas rechazadas");
            invalid=d.Clone();invalid.x=float.NaN;Check(!invalid.IsValid(out _),"Coordenadas no finitas rechazadas");
            invalid=d.Clone();invalid.checkpoint="arbitrary";Check(!invalid.IsValid(out _),"Checkpoint desconocido rechazado");
            string root=Path.Combine(output,"PruebasDatos",Guid.NewGuid().ToString("N"));var store=new CampaignStore(root);
            Check(!store.HasRecords&&!store.IsBlocked,"Instalación limpia sin partida");
            Check(store.StartNew(d,false,out string createError),"Creación persistente de campaña: "+(createError??"OK"));
            Check(!store.StartNew(new CampaignData(),false,out _),"Nueva partida requiere reemplazo confirmado");
            Check(store.Save(d,true,"Manual",null,out _),"Guardado manual escrito");
            string manual=store.Records().Single(r=>r.kind=="Manual").id;
            for(int i=0;i<8;i++){d.gold++;store.Save(d,false,"Auto "+i,null,out _);}
            var restarted=new CampaignStore(root);
            Check(restarted.Records().Count==6&&restarted.Records().Single(r=>r.kind=="Manual").id==manual,"Cinco autos rotan sin alterar manual");
            Check(restarted.Records()[0].data.gold==128,"Cierre y reapertura conservan progreso");
            string legacyRoot=Path.Combine(root,"legacy");Directory.CreateDirectory(legacyRoot);
            var legacyArchive=new CampaignArchive{schemaVersion=1,revision=1};
            legacyArchive.automatic.Add(new SaveRecord{id="legacy",savedUtc=DateTime.UtcNow.ToString("o"),kind="Autoguardado",label="Anterior",revision=1,data=new CampaignData{sword=true,x=-28,y=-9,hasPosition=true}});
            string legacyPayload=JsonUtility.ToJson(legacyArchive);
            foreach(string field in new[]{"sierraLayout","sierraFinished","sierraSeconds"})legacyPayload=System.Text.RegularExpressions.Regex.Replace(legacyPayload,"\\\""+field+"\\\":[^,}]+,?","");
            string legacyHash;using(var sha=System.Security.Cryptography.SHA256.Create())legacyHash=Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(legacyPayload)));
            File.WriteAllText(Path.Combine(legacyRoot,"campaign.json"),JsonUtility.ToJson(new LegacyEnvelope{payload=legacyPayload,sha256=legacyHash}));
            var legacyStore=new CampaignStore(legacyRoot);Check(!legacyStore.IsBlocked&&legacyStore.Records()[0].data.sierraLayout==0&&legacyStore.Records()[0].data.sword,"Formato anterior sin campos de sierra se lee y conserva equipo");
            Check(legacyStore.Save(legacyStore.Records()[0].data,false,"Migración",null,out _)&&File.ReadAllText(legacyStore.FilePath).Contains("\"version\":2"),"Nueva escritura protege formato ampliado frente a ejecutables antiguos");
            var independent=restarted.Records();independent[0].data.gold=0;Check(restarted.Records()[0].data.gold==128,"Copias de lectura aisladas");
            string before=File.ReadAllText(store.FilePath);
            var failing=new CampaignStore(root,()=>throw new IOException("Fallo de disco simulado"));
            var purchase=d.Clone();purchase.Purchase("shield","MARA");
            Check(!failing.Save(purchase,false,"Compra",null,out _)&&File.ReadAllText(store.FilePath)==before,"Fallo antes del reemplazo conserva archivo y recursos");
            Check(new CampaignStore(root).Records()[0].data.gold==128,"Temporal incompleto ignorado al reiniciar");
            File.WriteAllText(store.FilePath,"damaged");var recovery=new CampaignStore(root);
            Check(recovery.IsBlocked&&recovery.RecoveryAvailable,"Corrupción detectada con respaldo recuperable");
            Check(!recovery.StartNew(new CampaignData(),true,out _),"No sobrescribe silenciosamente una partida dañada");
            Check(recovery.Recover(out _)&&!recovery.IsBlocked&&recovery.HasRecords,"Recuperación explícita válida");
            Check(File.ReadAllText(store.FilePath)==File.ReadAllText(store.FilePath+".bak"),"Recuperación conserva respaldo válido");
            Check(Directory.GetFiles(root,"*.damaged-*").Length==1,"Archivo dañado preservado");
            string valid=File.ReadAllText(store.FilePath);File.WriteAllText(store.FilePath,valid.Replace("\"version\":"+CampaignStore.FormatVersion,"\"version\":99"));
            var future=new CampaignStore(root);Check(future.IsBlocked&&!future.RecoveryAvailable&&!future.Save(d,true,"Future",null,out _),"Versión futura bloqueada sin degradar");
            File.WriteAllText(store.FilePath,valid);
            var replaced=new CampaignStore(root);var nextCampaign=new CampaignData();
            Check(replaced.StartNew(nextCampaign,true,out _)&&replaced.Records().Count==1&&replaced.Records()[0].data.campaignId==nextCampaign.campaignId,"Nueva campaña reemplaza historial completo atómicamente");
            var settings=new GameSettings();var attack=settings.Key("Atacar");
            Check(settings.Rebind("Atacar",KeyCode.A)&&settings.Key("Izquierda")==attack,"Reasignación intercambia duplicados");
            Check(!settings.Rebind("Atacar",KeyCode.Escape),"Escape permanece disponible");
            settings.master=.3f;Check(settings.Save(root,out _)&&Mathf.Approximately(GameSettings.Load(root).master,.3f),"Ajustes persisten separados de campaña");
        }
        private static void AuditScenes()
        {
            foreach(string path in new[]{"Assets/Scenes/Menu.unity","Assets/Scenes/LV1.unity"})
            {
                var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);int missing=0;
                foreach(var root in scene.GetRootGameObjects()) foreach(var transform in root.GetComponentsInChildren<Transform>(true)) missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                Check(missing==0,"Escena sin scripts faltantes: "+path);
            }
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
            var importer=AssetImporter.GetAtPath("Assets/Resources/Foundation/MenuForest.png") as TextureImporter;
            if(importer!=null){importer.textureType=TextureImporterType.Default;importer.maxTextureSize=2048;importer.mipmapEnabled=false;importer.SaveAndReimport();}
        }
        private static string Argument(string key)
        {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
    }
}
