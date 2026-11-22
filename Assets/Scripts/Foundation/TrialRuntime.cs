using System;
using System.Linq;
namespace JuanCapi
{
    public sealed partial class GameRuntime
    {
        public int TrialChapter {get;private set;}
        public bool IsTrial=>TrialChapter>0;
        public bool TrialFinished {get;private set;}
        public TrialRecords Trials {get;private set;}
        public TrialRecord TrialResult {get;private set;}
        public bool TrialImproved {get;private set;}
        public string TrialSaveError {get;private set;}
        private CampaignData trialCheckpoint;
        public bool TrialUnlocked(int chapter)
        {return chapter>=1&&chapter<=3&&!Store.IsBlocked&&Store.Records().Any(r=>chapter==1?r.data.sierraFinished:chapter==2?r.data.swampFinished:r.data.completed);}
        public void StartTrial(int chapter)
        {
            if(Busy||!TrialUnlocked(chapter))return;
            TrialChapter=chapter;TrialFinished=false;TrialResult=null;TrialSaveError=null;TrialImproved=false;IsPractice=false;
            Data=new CampaignData{campaignId=Store.Records()[0].data.campaignId,introSeen=true,maxHearts=5,healthQuarters=20};
            if(chapter>1){Data.sword=Data.dash=Data.sierraFinished=true;Data.rescues.Add("MARA");}
            if(chapter==3){Data.gun=Data.shield=Data.swampFinished=true;Data.rescues.Add("TICO");}
            Data.scene=chapter==1?"LV1":chapter==2?"AmazoniaTerrestre":"MinerTown";
            Data.checkpoint=chapter==1?"CP_B01":chapter==2?"CP_P01":"CP_M01";
            trialCheckpoint=Data.Clone();LoadScene(Data.scene);
        }
        private void NormalizeTrial(CampaignData d)
        {if(!IsTrial)return;d.maxHearts=5;d.healthQuarters=Math.Min(20,d.healthQuarters);d.heartPieces=d.fruit=d.cells=d.gold=d.scrap=0;d.fastReload=d.reinforcedShield=false;d.shieldEnergy=Math.Min(100,d.shieldEnergy);}
        private void TrialCheckpoint(CampaignData d)
        {if(IsTrial){NormalizeTrial(d);trialCheckpoint=d.Clone();}}
        public void CompleteTrial()
        {
            if(!IsTrial||TrialFinished||Busy)return;
            TrialFinished=true;Cinematic=false;
            TrialResult=new TrialRecord{campaignId=Data.campaignId,chapter=TrialChapter,seconds=Data.activeSeconds,deaths=Data.deaths,dateUtc=DateTime.UtcNow.ToString("o")};
            CampaignAudio.Play("victory",true);
            Trials.Record(TrialResult,out bool improved,out string error);TrialImproved=improved;TrialSaveError=error;UI.TrialResults();
        }
        public void RetryTrialRecord()
        {if(TrialResult==null)return;CampaignAudio.Play("victory",true);
            Trials.Record(TrialResult,out bool improved,out string error);TrialImproved=improved||TrialImproved;TrialSaveError=error;UI.TrialResults();}
        private void RetryTrial()
        {
            if(trialCheckpoint==null||TrialFinished)return;
            var d=trialCheckpoint.Clone();d.activeSeconds=Data.activeSeconds;d.deaths=Data.deaths;d.healthQuarters=20;d.ammunition=3;d.reloadRemaining=0;d.shieldEnergy=100;
            Data=d;LoadScene(d.scene);
        }
        private void CancelTrial(){TrialChapter=0;TrialFinished=false;trialCheckpoint=null;TrialResult=null;}
    }
}
