using System;
using UnityEngine;
using UnityEngine.UI;

namespace JuanCapi
{
    public sealed partial class GameInterface
    {
        private Text goldCount,scrapCount,clockCount,provisionCount;
        private Image[] equipmentHud;
        private GameObject[] equipmentLocks;
        private Image[] ammunition;
        private Image provisionIcon;
        private InputHintView[] weaponHints;
        private GameObject[] equippedMarks;
        private SymbolGraphic reticle;
        private RectTransform interactionHint;
        private Image interactionImage;
        private Text interactionKey;
        private SymbolGraphic[] route;
        private Image Icon(Transform parent,Sprite sprite,float x,float y,float w,float h,Color? color=null)
        {
            var image=Rect("Icono",parent,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;image.color=color??Color.white;return image;
        }
        private Image Icon(Transform parent,string key,float x,float y,float w,float h,Color? color=null)=>Icon(parent,CampaignArt.Current?.Icon(key),x,y,w,h,color);
        private SymbolGraphic Symbol(Transform parent,SymbolGraphic.Shape shape,float x,float y,float size,Color? tint=null)
        {var symbol=Rect(shape.ToString(),parent,x,y,size,size).gameObject.AddComponent<SymbolGraphic>();symbol.shape=shape;symbol.color=tint??Gold;symbol.raycastTarget=false;return symbol;}
        private InputHintView KeyHint(Transform parent,string action,float x,float y,float width=72)
        {
            var panel=Box(parent,x,y,width,28,new Color(.03f,.09f,.11f,.95f));
            var label=Label(panel.transform,"",3,0,width-6,28,14,TextAnchor.MiddleCenter);label.resizeTextForBestFit=true;label.resizeTextMinSize=10;label.resizeTextMaxSize=16;
            var symbol=Symbol(panel.transform,SymbolGraphic.Shape.Mouse,width*.5f-12,2,24,Cyan);
            var view=panel.gameObject.AddComponent<InputHintView>();view.action=action;view.label=label;view.mouse=symbol;
            return view;
        }
        private void BuildVisualHud()
        {
            Box(hud,24,20,365,116,new Color(.025f,.08f,.09f,.83f));
            hearts=new HeartGraphic[20];
            for(int i=0;i<20;i++)
            {
                var h=Rect("Corazón "+(i+1),hud,39+(i%10)*33,31+(i/10)*28,29,26).gameObject.AddComponent<HeartGraphic>();
                h.color=new Color(.97f,.24f,.29f);h.raycastTarget=false;hearts[i]=h;
            }
            Icon(hud,"gold",39,94,30,28);goldCount=Label(hud,"0",75,92,90,30,19,TextAnchor.MiddleLeft);
            Icon(hud,"scrap",175,94,30,28);scrapCount=Label(hud,"0",211,92,90,30,19,TextAnchor.MiddleLeft);
            Box(hud,1075,20,180,52,new Color(.025f,.08f,.09f,.83f));Symbol(hud,SymbolGraphic.Shape.Clock,1088,30,30,Cyan);
            clockCount=Label(hud,"00:00:00",1127,26,120,40,19,TextAnchor.MiddleLeft);
            route=new SymbolGraphic[3];
            for(int i=0;i<3;i++){route[i]=Symbol(hud,new[]{SymbolGraphic.Shape.Mountain,SymbolGraphic.Shape.Water,SymbolGraphic.Shape.Factory}[i],1090+i*59,84,30);if(i<2)Symbol(hud,SymbolGraphic.Shape.Arrow,1125+i*59,92,16,new Color(.6f,.7f,.7f,.6f));}
            equipmentHud=new Image[4];equipmentLocks=new GameObject[4];
            weaponHints=new InputHintView[4];equippedMarks=new GameObject[2];
            string[] ids={"sword","gun","dash","shield"},actions={"Atacar","Cambiar arma","Dash","Escudo"};
            for(int i=0;i<4;i++)
            {
                float x=878+i*96;Box(hud,x,598,82,88,new Color(.025f,.08f,.09f,.86f));
                equipmentHud[i]=Icon(hud,ids[i],x+9,603,64,55);weaponHints[i]=KeyHint(hud,actions[i],x+5,659,72);
                if(i<2)equippedMarks[i]=Box(hud,x+6,594,70,4,Gold).gameObject;
                equipmentLocks[i]=Symbol(hud,SymbolGraphic.Shape.Lock,x+29,619,25,new Color(.75f,.83f,.82f)).gameObject;
            }
            ammunition=new Image[3];for(int i=0;i<3;i++)ammunition[i]=Icon(hud,"cell",983+i*16,574,12,22);
            Box(hud,24,604,105,82,new Color(.025f,.08f,.09f,.86f));provisionIcon=Icon(hud,"fruit",30,610,52,44);
            provisionCount=Label(hud,"0",85,614,32,36,20,TextAnchor.MiddleCenter);KeyHint(hud,"Consumible",40,658,72);
            Symbol(hud,SymbolGraphic.Shape.Pack,154,642,28,Cyan);Label(hud,"Tab",188,640,65,30,17,TextAnchor.MiddleLeft);
            Symbol(hud,SymbolGraphic.Shape.Pause,262,644,24,Cyan);Label(hud,"Esc",292,640,60,30,17,TextAnchor.MiddleLeft);
            interactionHint=Rect("Interacción",hud,571,580,138,74);Box(interactionHint,0,0,138,74,Ink);
            interactionImage=Icon(interactionHint,"drone",6,7,60,60);interactionKey=Label(interactionHint,"E",76,10,50,54,25,TextAnchor.MiddleCenter,Gold);
            interactionHint.gameObject.SetActive(false);
            prompt=Label(hud,"",280,535,720,34,18,TextAnchor.MiddleCenter);
            reticle=Symbol(hud,SymbolGraphic.Shape.Reticle,0,0,32,Cyan);reticle.rectTransform.pivot=Vector2.one*.5f;reticle.gameObject.SetActive(false);
        }
        private void RefreshVisualHud(CampaignData d)
        {
            goldCount.text=d.gold.ToString();scrapCount.text=d.scrap.ToString();clockCount.text=FormatTime(d.activeSeconds);
            bool[] unlocked={d.sword,d.gun,d.dash,d.shield};
            for(int i=0;i<4;i++){equipmentHud[i].color=unlocked[i]?Color.white:new Color(.35f,.42f,.43f,.45f);equipmentLocks[i].SetActive(!unlocked[i]);}
            bool firing=d.gun&&d.equippedWeapon=="gun";
            equippedMarks[0].SetActive(d.sword&&!firing);equippedMarks[1].SetActive(firing);
            weaponHints[0].action=firing?"Cambiar arma":"Atacar";weaponHints[1].action=firing?"Atacar":"Cambiar arma";
            reticle.gameObject.SetActive(firing);if(firing)reticle.rectTransform.position=Input.mousePosition;
            for(int i=0;i<3;i++){ammunition[i].gameObject.SetActive(d.gun);ammunition[i].color=i<d.ammunition?Color.white:new Color(.35f,.42f,.43f,.5f);}
            provisionIcon.sprite=CampaignArt.Current?.Icon(d.assignedConsumable);provisionCount.text=(d.assignedConsumable=="fruit"?d.fruit:d.cells).ToString();
            int region=d.scene=="LV1"?0:d.scene.Contains("Miner")?2:1;
            for(int i=0;i<3;i++)route[i].color=i==region?Gold:new Color(.46f,.61f,.61f,.5f);
        }
        public void PromptIcon(string key)
        {
            interactionHint.gameObject.SetActive(key!=null);
            if(key==null)return;
            interactionImage.sprite=CampaignArt.Current?.Icon(key);interactionKey.text=GameInput.Label("Interactuar");
        }
        public void ArtPreview()
        {
            screen="art";back=Game.ToMenu;if(page!=null){page.gameObject.SetActive(false);Destroy(page.gameObject);page=null;}
            hud.gameObject.SetActive(false);toastUntil=0;toastRoot.gameObject.SetActive(false);Time.timeScale=1;
        }
        private void ResourceStrip(Transform parent,float y)
        {
            var d=Game.Data;Icon(parent,"gold",68,y,30,28);Label(parent,d.gold.ToString(),106,y,95,32,19);
            Icon(parent,"scrap",215,y,30,28);Label(parent,d.scrap.ToString(),253,y,95,32,19);
            var heart=Rect("Piezas",parent,365,y,28,26).gameObject.AddComponent<HeartGraphic>();heart.color=new Color(1,.35f,.5f);heart.quarters=d.heartPieces;
            Label(parent,d.heartPieces+" / 4",405,y,100,32,19);
        }
        private void VisualInventory()
        {
            var d=Game.Data;if(d==null)return;
            Begin("inventory","INVENTARIO","",Play);ResourceStrip(page,112);
            string[] tabs={"Equipo","Módulos","Provisiones","Misión"};
            for(int i=0;i<tabs.Length;i++){string value=tabs[i];var b=Button(page,value,66+i*290,166,275,48,()=>{tab=value;Inventory();});if(value==tab)Focus(b);}
            if(tab=="Equipo")
            {
                ArtCard(66,"sword","Espada de plasma",d.sword?"Ataque de corto alcance.":"En el dron de la quebrada.",d.sword,d.sword,d.equippedWeapon=="sword"&&d.sword?"Equipada":"Equipar",()=>{d.equippedWeapon="sword";Game.NotifyChanged();Inventory();});
                ArtCard(664,"gun","Disparador de plasma",d.gun?"Apunta con el mouse.":"Tras el caimán terrestre.",d.gun,d.gun,d.equippedWeapon=="gun"?"Equipado":"Equipar",()=>{d.equippedWeapon="gun";Game.NotifyChanged();Inventory();});
                if(d.gun)for(int i=0;i<3;i++)Icon(page,"cell",980+i*35,448,24,37,i<d.ammunition?Color.white:new Color(.3f,.4f,.4f));
            }
            else if(tab=="Módulos")
            {
                ArtCard(66,"dash","Impulsor",d.dash?"Un impulso para cruzar peligros.":"Lo lleva el Rastreador.",d.dash,false,d.dash?"Disponible":"Bloqueado",null);
                ArtCard(664,"shield","Escudo de plasma",d.shield?"Protege el frente de Juan.":"Tras el caimán acuático.",d.shield,false,d.shield?"Disponible":"Bloqueado",null);
                if(d.shield){Box(page,850,456,300,12,new Color(.12f,.25f,.26f));Box(page,850,456,300*d.shieldEnergy/d.MaximumShield,12,Cyan);}
            }
            else if(tab=="Provisiones")
            {
                ArtCard(66,"fruit","Fruta medicinal",d.fruit+" / 3",true,d.fruit>0&&d.healthQuarters<d.MaximumHealth,"Usar",()=>{d.Use("fruit");Game.NotifyChanged();Inventory();});
                for(int i=0;i<3;i++){var h=Rect("Curación",page,279+i*35,443,30,27).gameObject.AddComponent<HeartGraphic>();h.color=new Color(1,.3f,.35f);h.quarters=4;}
                ArtCard(664,"cell","Celda de reserva",d.cells+" / 3    ·    +50",true,d.cells>0&&d.shield&&d.shieldEnergy<d.MaximumShield,"Usar",()=>{d.Use("cell");Game.NotifyChanged();Inventory();});
                Button(page,d.assignedConsumable=="fruit"?"Asignada  ["+GameInput.Label("Consumible")+"]":"Asignar",100,549,482,44,()=>{d.assignedConsumable="fruit";Inventory();});
                Button(page,d.assignedConsumable=="cell"?"Asignada  ["+GameInput.Label("Consumible")+"]":"Asignar",698,549,482,44,()=>{d.assignedConsumable="cell";Inventory();});
            }
            else
            {
                Box(page,66,240,1148,360,Panel);
                string[] names={"Mara","Tico","Luma"},ids={"MARA","TICO","LUMA"};
                for(int i=0;i<3;i++)
                {
                    bool rescued=d.rescues.Contains(ids[i]);float x=115+i*205;
                    Icon(page,CampaignArt.Current.allies[i],x,265,140,175,rescued?Color.white:new Color(.27f,.35f,.35f));
                    Label(page,names[i],x,447,140,38,22,TextAnchor.MiddleCenter,Gold);
                    Symbol(page,rescued?SymbolGraphic.Shape.Check:SymbolGraphic.Shape.Lock,x+54,493,30,rescued?Cyan:Color.gray);
                }
                for(int i=0;i<6;i++)Icon(page,"record",788+(i%3)*112,275+(i/3)*105,76,76,i<d.records.Count?Color.white:new Color(.25f,.35f,.35f));
                Icon(page,"key",881,498,76,56,d.maintenanceKey?Color.white:new Color(.25f,.35f,.35f));
            }
        }
        private void ArtCard(float x,string key,string title,string description,bool owned,bool enabled,string action,Action click)
        {
            Box(page,x,240,550,365,Panel);Label(page,title,x+24,261,502,44,24,TextAnchor.MiddleLeft,Gold);
            Icon(page,key,x+28,327,160,160,owned?Color.white:new Color(.3f,.4f,.4f));
            if(!owned)Symbol(page,SymbolGraphic.Shape.Lock,x+89,382,39,new Color(.8f,.85f,.85f));
            Label(page,description,x+213,338,306,102,20,TextAnchor.MiddleLeft);
            if(owned)Symbol(page,SymbolGraphic.Shape.Check,x+482,270,24,Cyan);
            Button(page,action,x+34,492,482,48,click??(()=>{}),enabled);
        }
    }
}
