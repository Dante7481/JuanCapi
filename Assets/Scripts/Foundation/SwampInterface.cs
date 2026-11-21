using UnityEngine;
using UnityEngine.UI;
namespace JuanCapi
{
    public sealed partial class GameInterface
    {
        private RectTransform targetPanel,objectivePanel;
        private Image objectiveImage,reloadFill,shieldFill;
        private SymbolGraphic objectiveArrow;
        private SymbolGraphic[] targetMarks;
        public void ResetEncounter()
        {
            if(bossPanel!=null)bossPanel.gameObject.SetActive(false);
            if(targetPanel!=null)targetPanel.gameObject.SetActive(false);
            if(objectivePanel!=null)objectivePanel.gameObject.SetActive(false);
            Lesson(null,null);PromptIcon(null);
        }
        public void TrainingTargets(bool visible,CampaignData d)
        {
            if(targetPanel==null)
            {
                targetPanel=Rect("Blancos de práctica",hud,535,116,210,64);Box(targetPanel,0,0,210,64,Ink);
                targetMarks=new SymbolGraphic[3];
                for(int i=0;i<3;i++)targetMarks[i]=Symbol(targetPanel,SymbolGraphic.Shape.Reticle,16+i*64,10,44,Gold);
            }
            targetPanel.gameObject.SetActive(visible);
            for(int i=0;i<3;i++){bool done=d.collected.Contains("P05_TARGET_"+(i+1));targetMarks[i].shape=done?SymbolGraphic.Shape.Check:SymbolGraphic.Shape.Reticle;targetMarks[i].color=done?Cyan:Gold;targetMarks[i].SetAllDirty();}
        }
        public void Objective(string icon,Vector3 direction)
        {
            if(objectivePanel==null)
            {
                objectivePanel=Rect("Rumbo",hud,1130,133,124,60);Box(objectivePanel,0,0,124,60,Ink);
                objectiveImage=Icon(objectivePanel,"valve",8,5,50,50);
                objectiveArrow=Symbol(objectivePanel,SymbolGraphic.Shape.Arrow,78,15,30,Gold);
                objectiveArrow.rectTransform.pivot=Vector2.one*.5f;objectiveArrow.rectTransform.anchoredPosition+=new Vector2(15,-15);
            }
            objectivePanel.gameObject.SetActive(icon!=null);objectiveImage.sprite=CampaignArt.Current.Icon(icon);
            objectiveArrow.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        }
        private void RefreshCombatHud(CampaignData d)
        {
            if(reloadFill==null)
            {
                reloadFill=Box(hud,980,565,72,4,Cyan);
                shieldFill=Box(hud,1172,579,72,7,Cyan);
            }
            reloadFill.gameObject.SetActive(d.gun&&d.reloadRemaining>0);
            reloadFill.rectTransform.sizeDelta=new Vector2(72*Mathf.Clamp01(1-d.reloadRemaining/(d.fastReload?4.5f:6)),4);
            shieldFill.gameObject.SetActive(d.shield);shieldFill.rectTransform.sizeDelta=new Vector2(72*d.shieldEnergy/d.MaximumShield,7);
            if(Game.Player!=null&&Game.Player.aquatic){equipmentHud[0].color=new Color(.35f,.42f,.43f,.45f);equipmentHud[2].color=equipmentHud[0].color;}
        }
        public void SwampResults()
        {
            var d=Game.Data;
            Begin("chapter","PANTANO LIBERADO","Tico encontró el conducto de mantenimiento hacia MinerTown.",Play);
            Symbol(page,SymbolGraphic.Shape.Water,246,190,88,Cyan);Symbol(page,SymbolGraphic.Shape.Check,350,216,46,Cyan);
            Symbol(page,SymbolGraphic.Shape.Arrow,470,215,40,Gold);Symbol(page,SymbolGraphic.Shape.Factory,580,190,88,Gold);Icon(page,"shield",850,172,120,120);
            Symbol(page,SymbolGraphic.Shape.Clock,375,333,46,Cyan);Label(page,FormatTime(d.swampSeconds),443,332,460,60,40,TextAnchor.MiddleLeft,Gold);
            Icon(page,"crate",370,420,50,50);Label(page,d.collected.FindAll(id=>id.StartsWith("SEC_P")&&id.EndsWith("_GOLD")).Count+" / 3",440,426,160,36,23);
            Icon(page,"record",650,420,50,50);Label(page,d.records.FindAll(id=>id=="REC_P01"||id=="REC_A01").Count+" / 2",720,426,160,36,23);
            Focus(Button(page,"Seguir explorando",245,575,375,62,Play));Button(page,"Menú principal",660,575,375,62,Game.ToMenu);
        }
    }
}
