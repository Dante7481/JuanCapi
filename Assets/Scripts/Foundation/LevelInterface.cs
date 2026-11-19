using System;
using UnityEngine;
using UnityEngine.UI;

namespace JuanCapi
{
    public sealed partial class GameInterface
    {
        private string cinematicCaption;
        private Action cinematicSkip;
        private Text cinematicText;
        private RectTransform bossPanel, lessonPanel;
        private Image bossFill;
        private string lesson;
        public void CinematicFrame(string caption,Action skip)
        {
            cinematicCaption=caption;cinematicSkip=skip;
            if(screen=="cinematic") { if(cinematicText!=null)cinematicText.text=caption;return; }
            ResumeCinematic();
        }
        private void ResumeCinematic()
        {
            screen="cinematic";back=Pause;
            if(page!=null){page.gameObject.SetActive(false);Destroy(page.gameObject);}
            page=Rect("Introducción",stage,0,0,1280,720);
            Box(page,0,0,1280,64,Ink);Box(page,0,600,1280,120,Ink);
            Label(page,"QUEBRADA DE LA SIERRA",48,12,900,42,22,TextAnchor.MiddleLeft,Gold);
            cinematicText=Label(page,cinematicCaption,100,610,1080,48,Mathf.RoundToInt(22*Game.Settings.subtitleScale),TextAnchor.MiddleCenter);
            Focus(Button(page,"Omitir",1030,666,170,38,()=>cinematicSkip?.Invoke()));
            Button(page,"Pausa",48,666,170,38,Pause);
            Time.timeScale=1;hud.gameObject.SetActive(false);Game.ResumeInput();
        }
        public void BossMeter(float remaining,float maximum,bool visible)
        {
            if(bossPanel==null)
            {
                bossPanel=Rect("Rastreador",hud,428,28,424,70);Box(bossPanel,0,0,424,70,Ink);
                Icon(bossPanel,CampaignArt.Current.rastreadorPortrait,8,3,68,64);
                Label(bossPanel,"RASTREADOR",85,6,320,28,18,TextAnchor.MiddleLeft,Gold);
                Box(bossPanel,85,42,320,12,new Color(.25f,.17f,.14f));bossFill=Box(bossPanel,85,42,320,12,new Color(.94f,.52f,.23f));
            }
            bossPanel.gameObject.SetActive(visible);bossFill.rectTransform.sizeDelta=new Vector2(320*Mathf.Clamp01(remaining/maximum),12);
        }
        public void Lesson(string action,string icon)
        {
            string next=action+":"+icon;if(next==lesson)return;lesson=next;
            if(lessonPanel!=null)Destroy(lessonPanel.gameObject);
            if(string.IsNullOrEmpty(action))return;
            lessonPanel=Rect("Gesto",hud,542,462,196,92);Box(lessonPanel,0,0,196,92,Ink);
            if(icon=="jump") {var arrow=Symbol(lessonPanel,SymbolGraphic.Shape.Arrow,27,14,47,Cyan);arrow.rectTransform.localRotation=Quaternion.Euler(0,0,90);}
            else Icon(lessonPanel,icon,12,6,78,78);
            KeyHint(lessonPanel,action,104,32,80);
            if(action=="Dash") {lessonPanel.sizeDelta=new Vector2(196,120);KeyHint(lessonPanel,"Saltar",104,74,80);}
        }
        public void SierraResults()
        {
            var d=Game.Data;
            Begin("chapter","QUEBRADA SUPERADA","El rastro de los cargamentos continúa hacia el pantano.",Play);
            Symbol(page,SymbolGraphic.Shape.Mountain,246,190,88,Gold);Symbol(page,SymbolGraphic.Shape.Check,350,216,46,Cyan);
            Symbol(page,SymbolGraphic.Shape.Arrow,470,215,40,Gold);Symbol(page,SymbolGraphic.Shape.Water,580,190,88,Cyan);
            Icon(page,"dash",850,172,120,120);
            Symbol(page,SymbolGraphic.Shape.Clock,375,333,46,Cyan);Label(page,FormatTime(d.sierraSeconds),443,332,460,60,40,TextAnchor.MiddleLeft,Gold);
            int secrets=d.collected.FindAll(id=>id.StartsWith("SEC_B")&&id.EndsWith("_GOLD")).Count;
            Icon(page,"crate",370,420,50,50);Label(page,secrets+" / 3",440,426,160,36,23);
            Icon(page,"record",650,420,50,50);Label(page,d.records.FindAll(id=>id.StartsWith("REC_B")).Count+" / 2",720,426,160,36,23);
            Label(page,"Tu progreso está guardado. Puedes volver a explorar la quebrada.",200,500,880,48,20,TextAnchor.MiddleCenter);
            Focus(Button(page,"Seguir explorando",245,575,375,62,Play));Button(page,"Menú principal",660,575,375,62,Game.ToMenu);
        }
    }
}
