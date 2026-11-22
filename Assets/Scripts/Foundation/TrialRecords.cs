using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace JuanCapi
{
    [Serializable] public sealed class TrialRecord
    {
        public string campaignId,dateUtc; public int chapter,rules=1,deaths; public double seconds;
        public long RankedMilliseconds=> (long)Math.Round((seconds+15*deaths)*1000,MidpointRounding.AwayFromZero);
        public string Medal=>RankedMilliseconds<=Gold(chapter)*1000?"Oro":RankedMilliseconds<=Silver(chapter)*1000?"Plata":RankedMilliseconds<=Bronze(chapter)*1000?"Bronce":"Completado";
        public static int Gold(int chapter)=>chapter==2?240:180;
        public static int Silver(int chapter)=>chapter==2?360:260;
        public static int Bronze(int chapter)=>chapter==2?480:360;
    }
    public sealed class TrialRecords
    {
        [Serializable] private sealed class Archive { public int version=1;public List<TrialRecord> records=new List<TrialRecord>(); }
        private Archive archive=new Archive();private readonly string path;public string Error {get;private set;}
        public TrialRecords(string directory)
        {
            path=Path.Combine(directory,"trials.json");
            if(!File.Exists(path))return;
            try {archive=JsonUtility.FromJson<Archive>(File.ReadAllText(path));Validate(archive);}
            catch(Exception e){archive=new Archive();Error="No se pudieron leer las marcas: "+e.Message;}
        }
        private static void Validate(Archive data)
        {
            if(data==null||data.version!=1||data.records==null||data.records.Count>10000)throw new IOException("Formato de marcas incompatible.");
            var keys=new HashSet<string>();
            foreach(var r in data.records)
                if(r==null||string.IsNullOrEmpty(r.campaignId)||r.chapter<1||r.chapter>3||r.rules!=1||r.deaths<0||r.deaths>100000||r.seconds>604800||r.seconds<0||double.IsNaN(r.seconds)||double.IsInfinity(r.seconds)||!DateTime.TryParse(r.dateUtc,out _)||!keys.Add(r.campaignId+":"+r.chapter))throw new IOException("Marca inválida.");
        }
        public TrialRecord Best(string campaign,int chapter)=>archive.records.Find(r=>r.campaignId==campaign&&r.chapter==chapter);
        public bool Record(TrialRecord value,out bool improved,out string error)
        {
            improved=false;error=Error;if(Error!=null)return false;
            try {Validate(new Archive{records=new List<TrialRecord>{value}});}catch(Exception e){error=e.Message;return false;}
            var previous=Best(value.campaignId,value.chapter);
            if(previous!=null&&previous.RankedMilliseconds<=value.RankedMilliseconds)return true;
            try
            {
                var next=JsonUtility.FromJson<Archive>(JsonUtility.ToJson(archive));next.records.RemoveAll(r=>r.campaignId==value.campaignId&&r.chapter==value.chapter);next.records.Add(value);Validate(next);
                Directory.CreateDirectory(Path.GetDirectoryName(path));string temporary=path+".tmp";
                using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))
                {var bytes=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(next,true));stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);
                archive=next;improved=true;return true;
            }
            catch(Exception e){error="No se guardó la marca: "+e.Message;return false;}
        }
    }
}
