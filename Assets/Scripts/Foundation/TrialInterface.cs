using System;
using System.Linq;
using UnityEngine;
namespace JuanCapi
{
    public sealed partial class GameInterface
    {
        private RectTransform trialHud;private UnityEngine.UI.Text trialPenalty;
        private void RefreshTrialHud()
        {
            if(trialHud==null){trialHud=Rect("Penalización del reto",hud,987,78,260,42);Box(trialHud,0,0,260,42,Ink);trialPenalty=Label(trialHud,"",8,0,244,42,18,TextAnchor.MiddleCenter,Gold);}
            trialHud.gameObject.SetActive(Game.IsTrial);if(Game.IsTrial)trialPenalty.text="CONTRARRELOJ   +"+(Game.Data.deaths*15)+" s";
        }
        public void TrialMenu()
        {
            Begin("trials","CONTRARRELOJ","Equipo fijo · sin consumibles · cada derrota añade 15 segundos",MainMenu);
            string campaign=Game.Store.Records().FirstOrDefault()?.data.campaignId;
            for(int i=1;i<=3;i++)
            {
                int chapter=i;float x=85+(i-1)*410;bool unlocked=Game.TrialUnlocked(i);var best=Game.Trials.Best(campaign,i);
                Symbol(page,new[]{SymbolGraphic.Shape.Mountain,SymbolGraphic.Shape.Water,SymbolGraphic.Shape.Factory}[i-1],x+100,195,85,Gold);
                Label(page,new[]{"Sierra","Pantano completo","MinerTown"}[i-1],x,302,340,45,26,TextAnchor.MiddleCenter);
                Label(page,unlocked?(best==null?"Sin marca":PreciseTime(best.RankedMilliseconds)+" · "+best.Medal):"Completa este capítulo en campaña",x,365,340,65,20,TextAnchor.MiddleCenter);
                Label(page,"Oro  "+FormatTime(TrialRecord.Gold(i))+"\nPlata  "+FormatTime(TrialRecord.Silver(i))+"\nBronce  "+FormatTime(TrialRecord.Bronze(i)),x,440,340,100,19,TextAnchor.MiddleCenter);
                var button=Button(page,"Correr "+new[]{"Sierra","Pantano","MinerTown"}[i-1],x,565,340,55,()=>Game.StartTrial(chapter),unlocked);if(i==1&&unlocked)Focus(button);
            }
            if(Game.Trials.Error!=null)Toast(Game.Trials.Error);
        }
        private static string PreciseTime(long ms)=>TimeSpan.FromMilliseconds(ms).ToString(@"hh\:mm\:ss\.fff");
        public void TrialResults()
        {
            var r=Game.TrialResult;if(r==null)return;
            Begin("trial-result","RETO COMPLETADO",Game.TrialSaveError??(Game.TrialImproved?"Nueva mejor marca":"Tu mejor marca se conserva"));
            var medalColor=r.Medal=="Oro"?Gold:r.Medal=="Plata"?new Color(.78f,.85f,.9f):r.Medal=="Bronce"?new Color(.78f,.49f,.29f):Cyan;
            Symbol(page,r.Medal=="Completado"?SymbolGraphic.Shape.Check:SymbolGraphic.Shape.Medal,570,170,110,medalColor);
            Label(page,r.Medal.ToUpperInvariant(),240,295,800,60,38,TextAnchor.MiddleCenter,Gold);
            Label(page,"Tiempo activo   "+PreciseTime((long)Math.Round(r.seconds*1000))+"\nPenalización   +"+(r.deaths*15)+" s ("+r.deaths+(r.deaths==1?" derrota)":" derrotas)"),230,370,820,90,24,TextAnchor.MiddleCenter);
            Label(page,PreciseTime(r.RankedMilliseconds),260,475,760,60,36,TextAnchor.MiddleCenter,Cyan);
            Focus(Button(page,"Reiniciar reto",240,570,370,60,()=>Game.StartTrial(Game.TrialChapter)));
            Button(page,"Menú principal",660,570,370,60,Game.ToMenu);
            if(Game.TrialSaveError!=null)Button(page,"Reintentar guardar marca",430,645,420,45,Game.RetryTrialRecord);
        }
        private void TrialSystem()
        {
            Begin("trial-system","CONTRARRELOJ","La campaña permanece intacta. Salir cancela el intento actual.",Pause);
            Focus(Button(page,"Reintentar checkpoint",365,200,550,60,()=>Confirm("¿Reintentar?","Se mantiene el tiempo acumulado y se añaden 15 segundos de penalización.",()=>{Game.Data.deaths++;Game.Retry();},TrialSystem)));
            Button(page,"Reiniciar reto",365,285,550,60,()=>Confirm("¿Reiniciar el reto?","El intento comienza desde cero. Se conserva tu mejor marca.",()=>Game.StartTrial(Game.TrialChapter),TrialSystem));
            Button(page,"Menú principal",365,370,550,60,()=>Confirm("¿Cancelar el intento?","La carrera actual no se guardará.",Game.ToMenu,TrialSystem));
        }
    }
}
