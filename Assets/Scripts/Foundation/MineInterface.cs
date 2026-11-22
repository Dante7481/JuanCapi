using UnityEngine;
using UnityEngine.UI;
namespace JuanCapi
{
    public sealed partial class GameInterface
    {
        private RectTransform corePanel;private SymbolGraphic coreSymbol;private Image coreBar;
        public void DirectorCore(bool visible,bool open,float remaining)
        {
            if(corePanel==null)
            {
                corePanel=Rect("Núcleo del exotraje",hud,552,108,176,58);Box(corePanel,0,0,176,58,Ink);
                Icon(corePanel,"shield",8,7,44,44);coreSymbol=Symbol(corePanel,SymbolGraphic.Shape.Lock,61,12,32,Gold);coreBar=Box(corePanel,106,26,58,6,Cyan);
            }
            corePanel.gameObject.SetActive(visible);coreSymbol.shape=open?SymbolGraphic.Shape.Check:SymbolGraphic.Shape.Lock;coreSymbol.color=open?Cyan:Gold;coreSymbol.SetAllDirty();
            coreBar.rectTransform.sizeDelta=new Vector2(58*Mathf.Clamp01(remaining/3),6);
        }
        public void CampaignResults()
        {
            var d=Game.Data;Begin("complete","GUARDIANES DEL RÍO","La descarga se detuvo. La recuperación de la cuenca continúa.",Play);
            for(int i=0;i<3;i++)
            {
                float x=140+i*370;Symbol(page,new[]{SymbolGraphic.Shape.Mountain,SymbolGraphic.Shape.Water,SymbolGraphic.Shape.Factory}[i],x,182,66,Gold);
                Symbol(page,SymbolGraphic.Shape.Check,x+82,202,35,Cyan);
                Label(page,FormatTime(i==0?d.sierraSeconds:i==1?d.swampSeconds:d.mineSeconds),x-5,265,220,40,24);
            }
            Symbol(page,SymbolGraphic.Shape.Clock,395,340,46,Cyan);Label(page,FormatTime(d.campaignSeconds),465,334,450,60,40,TextAnchor.MiddleLeft,Gold);
            int secrets=d.collected.FindAll(id=>id.StartsWith("SEC_")&&id.EndsWith("_GOLD")).Count;
            Icon(page,"crate",245,430,50,50);Label(page,secrets+" / 9",306,435,130,40,23);
            Icon(page,"record",500,430,50,50);Label(page,d.records.Count+" / 6",561,435,130,40,23);
            Icon(page,"cage",755,430,50,50);Label(page,d.rescues.Count+" / 3",816,435,130,40,23);
            Label(page,"Derrotas: "+d.deaths,440,500,400,36,20,TextAnchor.MiddleCenter);
            Focus(Button(page,"Explorar",165,565,300,60,Play));Button(page,"Créditos",490,565,300,60,Credits);Button(page,"Menú principal",815,565,300,60,Game.ToMenu);
        }
        private void Credits()
        {
            Begin("credits","JUAN CAPI · GUARDIANES DEL RÍO","",CampaignResults);
            Label(page,"Proyecto universitario",150,170,980,45,28,TextAnchor.MiddleCenter,Gold);
            Label(page,"Personajes, escenarios y animaciones de los proyectos JuanCapi y CapibaraUnderWaterBoss, con ampliaciones de arte asistidas por IA.\n\nMotor de juego: Unity.\n\nGracias por acompañar a Juan, Mara, Tico y Luma.",220,250,840,270,24,TextAnchor.MiddleCenter);
        }
    }
}
