using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JuanCapi
{
    public sealed partial class GameInterface : MonoBehaviour
    {
        private static readonly Color Ink = new Color(.035f,.09f,.11f,.97f);
        private static readonly Color Panel = new Color(.065f,.135f,.13f,.98f);
        private static readonly Color Gold = new Color(.91f,.76f,.43f);
        private static readonly Color Cyan = new Color(.39f,.9f,.84f);
        private GameRuntime Game => GameRuntime.Instance;
        private RectTransform stage, page, hud, toastRoot;
        private Text toast, prompt;
        private HeartGraphic[] hearts;
        private readonly List<Texture2D> thumbnails = new List<Texture2D>();
        private Font font;
        private float toastUntil, displayUntil;
        private Action back, afterMessage, returnSettings;
        private string screen = "menu", tab = "Equipo", rebind;
        private int rebindFrame;
        private GameSettings oldDisplay;
        private Text displayTimer;
        private EventSystem events;
        private CanvasScaler scaler;
        public bool IsOpen => screen != "game";
        public string ScreenName => screen;
        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Interfaz", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform,false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            scaler=canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,720);
            var center=new GameObject("Zona segura",typeof(RectTransform)); center.transform.SetParent(canvasObject.transform,false); stage=center.GetComponent<RectTransform>();
            stage.anchorMin=stage.anchorMax=new Vector2(.5f,.5f); stage.sizeDelta=new Vector2(1280,720);
            hud = Rect("HUD",stage,0,0,1280,720);
            BuildVisualHud();
            toastRoot=Rect("Notificación",stage,290,650,700,46); Box(toastRoot,0,0,700,46,Ink);
            toast=Label(toastRoot,"",12,0,676,46,19,TextAnchor.MiddleCenter); toastRoot.gameObject.SetActive(false);
            var e=new GameObject("Entrada UI",typeof(EventSystem),typeof(StandaloneInputModule)); e.transform.SetParent(transform,false); events=e.GetComponent<EventSystem>();
        }
        private void Update()
        {
            scaler.matchWidthOrHeight=Screen.width/(float)Math.Max(1,Screen.height)<16f/9f?0:1;
            toastRoot.gameObject.SetActive(SwampLevel.Current?.Diving!=true&&screen!="art"&&screen!="cinematic"&&screen!="trial-result"&&screen!="complete"&&Time.unscaledTime<toastUntil);
            var d=Game.Data;
            if(d!=null)
            {
                hud.gameObject.SetActive(!IsOpen&&SwampLevel.Current?.Diving!=true);
                for(int i=0;i<20;i++) { hearts[i].gameObject.SetActive(i<d.maxHearts); hearts[i].SetQuarters(Mathf.Clamp(d.healthQuarters-i*4,0,4)); }
                RefreshVisualHud(d);
                if(d.healthQuarters<=4 && !Game.Settings.reducedFlashes)
                    foreach(var h in hearts) h.color=new Color(1,.2f,.25f,.72f+.28f*Mathf.Sin(Time.unscaledTime*4));
                else foreach(var h in hearts) h.color=new Color(.97f,.24f,.29f);
            }
            else hud.gameObject.SetActive(false);
            if (Game.Busy) return;
            if(d!=null&&screen=="game"&&Input.GetKeyDown(KeyCode.T)){Emotes();return;}
            if(screen=="emotes")
            {
                if(Input.GetKeyDown(KeyCode.T)){Play();return;}
                for(int i=0;i<3;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))){StartEmote(i);return;}
            }
            if(oldDisplay!=null)
            {
                if(displayTimer!=null) displayTimer.text=$"Confirma el cambio en {Mathf.CeilToInt(displayUntil-Time.unscaledTime)} s.";
                if(Time.unscaledTime>=displayUntil) RevertDisplay();
            }
            if(rebind!=null)
            {
                if(Time.frameCount<=rebindFrame+1) return;
                if(Input.GetKeyDown(KeyCode.Escape)) { rebind=null; Controls(); return; }
                foreach(KeyCode k in Enum.GetValues(typeof(KeyCode))) if(Input.GetKeyDown(k) && Game.Settings.Rebind(rebind,k))
                { rebind=null; PersistSettings(); Controls(); return; }
                return;
            }
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                var emote=Game.Player!=null?Game.Player.GetComponent<PlayerEmotes>():null;
                if(screen=="game"&&emote!=null&&emote.Active){emote.Cancel();return;}
                if(screen=="game") Pause(); else back?.Invoke();
                return;
            }
            if(Input.GetKeyDown(KeyCode.Tab) && d!=null)
            { if(screen=="game") Inventory(); else if(screen=="inventory") Play(); }
        }
        private void Begin(string name, string title, string subtitle, Action onBack=null)
        {
            screen=name; back=onBack; rebind=null;
            if(page!=null) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            foreach(var t in thumbnails) Destroy(t); thumbnails.Clear();
            page=Rect(name,stage,0,0,1280,720); Box(page,0,0,1280,720,Ink);
            var bg=CampaignArt.Current!=null?CampaignArt.Current.sierra.texture:Resources.Load<Texture2D>("Foundation/MenuForest");
            if(bg!=null && name=="menu")
            {
                var image=Rect("Sierra",page,-405,0,2160,720).gameObject.AddComponent<RawImage>(); image.texture=bg; image.color=new Color(.8f,.88f,.92f,1); image.raycastTarget=false;
                Box(page,0,0,590,720,new Color(.025f,.08f,.1f,.92f));
                if(CampaignArt.Current!=null)Icon(page,CampaignArt.Current.menuHero,762,272,370,370);
            }
            Label(page,title,64,40,1152,64,34,TextAnchor.MiddleLeft,Gold);
            Label(page,subtitle,66,106,1140,42,18,TextAnchor.UpperLeft,new Color(.7f,.82f,.8f));
            if(onBack!=null) Button(page,"Volver  [Esc]",1020,644,196,44,onBack);
            toastRoot.SetAsLastSibling();
            hud.gameObject.SetActive(false); Time.timeScale=0;
        }
        public void MainMenu()
        {
            Game.Store.Refresh();
            Begin("menu","JUAN CAPI","GUARDIANES DEL RÍO");
            Symbol(page,SymbolGraphic.Shape.Mountain,80,206,46,Gold);Symbol(page,SymbolGraphic.Shape.Arrow,158,220,23,Cyan);
            Symbol(page,SymbolGraphic.Shape.Water,218,206,46,Gold);Symbol(page,SymbolGraphic.Shape.Arrow,296,220,23,Cyan);Symbol(page,SymbolGraphic.Shape.Factory,356,206,46,Gold);
            var first=Button(page,"Continuar partida",68,316,380,56,()=>History(MainMenu),Game.Store.HasRecords||Game.Store.IsBlocked);
            Button(page,"Nueva partida",68,385,380,56,()=>Confirm("¿Comenzar una nueva aventura?",Game.Store.HasRecords?
                "Se reemplazará la campaña actual: guardado manual y todos los autoguardados. Esta acción no se puede deshacer desde el juego.":
                "Juan comienza en la sierra con tres corazones. La espada espera en el dron caído.",()=>Game.NewCampaign(true),MainMenu),!Game.Store.IsBlocked);
            Button(page,"Ajustes",68,454,380,56,()=>Settings(MainMenu));
            Button(page,"Salir",68,523,380,56,Quit);
            Button(page,"Contrarreloj",790,570,340,56,TrialMenu,Game.TrialUnlocked(1));
            Symbol(page,SymbolGraphic.Shape.Mouse,70,642,26,Cyan);Label(page,"/   Flechas + Enter",112,638,370,34,17,TextAnchor.MiddleLeft);
            Focus(first.interactable?first:page.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.interactable));
        }
        public void EnsureInput()
        { foreach(var e in FindObjectsByType<EventSystem>(FindObjectsSortMode.None)) if(e!=events) e.enabled=false; }
        public void Play()
        {
            if(Game.Cinematic){ResumeCinematic();return;}
            screen="game"; back=null; if(page!=null) { page.gameObject.SetActive(false); Destroy(page.gameObject); page=null; }
            Time.timeScale=1; Game.ResumeInput(); prompt.text="";PromptIcon(null);
        }
        public void Pause()
        {
            Begin("pause","PAUSA","Tu aventura se detiene mientras consultas este menú.",Play);
            Focus(Button(page,"Continuar",395,185,490,58,Play));
            Button(page,"Inventario",395,260,490,58,Inventory);
            Button(page,"Sistema",395,335,490,58,SystemMenu);
            Button(page,"Ajustes",395,410,490,58,()=>Settings(Pause));
        }
        public void SystemMenu()
        {
            if(Game.IsTrial){TrialSystem();return;}
            Begin("system","SISTEMA","Un guardado manual y cinco autoguardados de tu campaña.",Pause);
            Focus(Button(page,"Guardar",365,178,550,60,SaveConfirmation,!Game.IsPractice));
            Button(page,"Cargar partida",365,254,550,60,()=>History(SystemMenu),Game.Store.HasRecords);
            Button(page,"Volver al menú principal",365,330,550,60,()=>Confirm("¿Volver al menú?","Se perderán los avances posteriores al último guardado. Puedes cancelar y guardar antes de salir.",Game.ToMenu,SystemMenu));
            Button(page,"Salir del juego",365,406,550,60,()=>Confirm("¿Salir del juego?","Los avances sin guardar se perderán. Guarda desde Sistema antes de cerrar.",Quit,SystemMenu));
            if(Game.IsPractice) Label(page,"Sesión de prueba del editor: la campaña está protegida.",200,510,880,44,20,TextAnchor.MiddleCenter);
        }
        private void SaveConfirmation()
        {
            var old=Game.Store.Records().FirstOrDefault(r=>r.kind=="Manual");
            Begin("confirm","¿Guardar la partida?","El autoguardado no sustituye este registro manual.",SystemMenu);
            Box(page,130,180,1020,350,Panel);
            var preview=new SaveRecord{thumbnail=Game.CaptureThumbnail()};Thumbnail(page,preview,170,220,250,140);
            Label(page,Game.Data.region,460,215,640,45,26,TextAnchor.MiddleLeft,Gold);
            Label(page,DateTime.Now.ToString("dd/MM/yyyy HH:mm")+"  ·  "+FormatTime(Game.Data.activeSeconds),460,267,640,38,20);
            Label(page,old==null?"Crearás tu guardado manual.":"Reemplaza el manual de "+LocalDate(old.savedUtc)+".",460,315,640,75,20);
            Button(page,"Guardar",230,430,380,60,()=>StartCoroutine(SaveFromMenu()));
            Focus(Button(page,"Cancelar",670,430,380,60,SystemMenu));
        }
        private System.Collections.IEnumerator SaveFromMenu()
        {
            Begin("saving","GUARDANDO…","Espera a que se confirme el guardado antes de cerrar el juego.");
            yield return null;
            if(Game.Save(true,Game.Data.region))SystemMenu();
        }
        public void History(Action onBack)
        {
            Begin("history","CARGAR PARTIDA","Selecciona un registro. Cargar restaura también los objetos y el progreso de ese momento.",onBack);
            if(Game.Store.IsBlocked)
            {
                Label(page,Game.Store.Error,90,190,1100,110,22);
                if(Game.Store.RecoveryAvailable) Focus(Button(page,"Recuperar respaldo",350,340,580,64,()=>Confirm("¿Recuperar el respaldo?","Se restaurará la copia anterior válida. El archivo dañado se conservará para diagnóstico.",()=>{
                    if(Game.Store.Recover(out string error)) History(onBack); else Message(error);
                },()=>History(onBack),"Recuperar")));
                return;
            }
            var records=Game.Store.Records(); Button first=null; int row=0;
            foreach(var record in records)
            {
                var r=record; float y=163+row++*76;
                var b=Button(page,"",66,y,1148,67,()=>Confirm("¿Cargar este registro?",$"{r.kind} · {r.data.region}\n{LocalDate(r.savedUtc)} · {FormatTime(r.data.activeSeconds)}\nSe perderán los avances actuales sin guardar.",()=>Game.LoadRecord(r),()=>History(onBack),"Cargar"));
                if(first==null) first=b;
                Thumbnail(b.transform,r,8,5,100,56);
                Label(b.transform,r.kind.ToUpperInvariant(),126,5,220,26,16,TextAnchor.MiddleLeft,Gold);
                Label(b.transform,r.data.region+"  ·  "+r.label,126,30,630,30,17);
                Label(b.transform,LocalDate(r.savedUtc)+"   "+FormatTime(r.data.activeSeconds),700,4,430,25,16,TextAnchor.MiddleRight);
                for(int h=0;h<Math.Min(r.data.maxHearts,20);h++)
                {
                    var heart=Rect("Vida",b.transform,750+h*17,36,15,15).gameObject.AddComponent<HeartGraphic>();
                    heart.color=new Color(.97f,.24f,.29f); heart.quarters=Mathf.Clamp(r.data.healthQuarters-h*4,0,4); heart.raycastTarget=false;
                }
            }
            if(records.Count==0) Label(page,"Aún no hay una partida guardada.",120,220,1000,70,24,TextAnchor.MiddleCenter);
            Focus(first);
        }
        public void Inventory() { VisualInventory(); }
        private void Item(string name,string description,float y,bool enabled,Action action,string actionName)
        { Label(page,name,100,y,620,38,25,TextAnchor.MiddleLeft,Gold); Label(page,description,100,y+45,610,65,19); Button(page,actionName,740,y+15,370,52,action??(()=>{}),enabled); }
        public void Bank(string shelter)
        {
            if(Game.IsTrial){Toast("Equipo fijo: el banco no se utiliza en contrarreloj.");return;}
            var d=Game.Data; Begin("bank","BANCO DEL REFUGIO",$"Oro {d.gold} · Chatarra {d.scrap} · Cada compra se autoguarda.",Play);
            string[] ids={"reload","shield","fruit","cell"}; string[] names={"Recarga rápida · 80 oro + 15 chatarra","Escudo reforzado · 80 oro + 15 chatarra","Fruta · 15 oro","Celda · 20 oro"};
            for(int i=0;i<ids.Length;i++)
            {
                string id=ids[i]; float y=165+i*108; string problem=d.PurchaseProblem(id,shelter);
                Label(page,names[i],90,y,730,40,23,TextAnchor.MiddleLeft,Gold);
                Label(page,problem??(id=="reload"?"Recarga en 4,5 segundos.":id=="shield"?"Energía máxima de 125.":"Se añade a tus provisiones."),90,y+42,730,44,18);
                var button=Button(page,"Comprar",875,y+10,300,54,()=>{if(Game.Transaction(s=>s.Purchase(id,shelter),"Compra confirmada"))Bank(shelter);},problem==null);
                if(i==0)Focus(button);
            }
        }
        public void Settings(Action onBack)
        {
            returnSettings=onBack;
            Begin("settings","AJUSTES","Los ajustes son del dispositivo y no borran tu campaña.",()=>{PersistSettings();onBack();});
            var s=Game.Settings;
            Slider("Volumen general",s.master,165,v=>{s.master=v;s.Apply();});
            Slider("Música",s.music,215,v=>s.music=v); Slider("Efectos",s.effects,265,v=>s.effects=v);
            Slider("Ambiente",s.ambience,315,v=>s.ambience=v);
            Button(page,"Escala de texto: "+(s.uiScale>1?"125 %":"100 %"),66,362,550,50,()=>{s.uiScale=s.uiScale>1?1:1.25f;Settings(onBack);});
            Button(page,"Subtítulos: "+(s.subtitleScale>1?"Grandes":"Normales"),658,362,556,50,()=>{s.subtitleScale=s.subtitleScale>1?1:1.25f;Settings(onBack);});
            Button(page,"Destellos reducidos: "+(s.reducedFlashes?"Sí":"No"),66,426,550,50,()=>{s.reducedFlashes=!s.reducedFlashes;Settings(onBack);});
            Button(page,"Vibración de cámara: "+(s.cameraShake?"Sí":"No"),658,426,556,50,()=>{s.cameraShake=!s.cameraShake;Settings(onBack);});
            Focus(Button(page,"Controles",66,500,550,55,Controls));
            Button(page,"Pantalla",658,500,556,55,DisplaySettings);
        }
        private void Controls()
        {
            Begin("controls","CONTROLES","Selecciona una acción y pulsa una tecla o botón del mouse. Las teclas duplicadas se intercambian.",()=>Settings(returnSettings));
            for(int i=0;i<Game.Settings.bindings.Count;i++)
            {
                var binding=Game.Settings.bindings[i]; int col=i/6,row=i%6;
                var b=Button(page,binding.action+"   ["+GameInput.Label(binding.action)+"]",66+col*590,165+row*65,560,52,()=>{
                    Begin("rebind","REASIGNAR: "+binding.action,"Pulsa la nueva tecla. Esc cancela. Tab, Enter y Esc se reservan para los menús.",Controls);
                    rebind=binding.action;rebindFrame=Time.frameCount;
                });if(i==0)Focus(b);
            }
            Button(page,"Restaurar controles",66,580,560,48,()=>Confirm("¿Restaurar los controles?","Se recuperarán las teclas iniciales.",()=>{Game.Settings.bindings=GameSettings.Defaults();PersistSettings();Controls();},Controls,"Restaurar"));
        }
        private void DisplaySettings()
        {
            Begin("display","PANTALLA","Pantalla completa usa la resolución nativa. Los tamaños de abajo se aplican al modo ventana.",()=>Settings(returnSettings));
            var s=Game.Settings;
            Focus(Button(page,s.fullscreen?"Cambiar a ventana":"Cambiar a pantalla completa",190,180,900,60,()=>TryDisplay(s.width,s.height,!s.fullscreen)));
            var resolutions=new[]{new Vector2Int(1280,720),new Vector2Int(1600,900),new Vector2Int(1920,1080)};
            for(int i=0;i<resolutions.Length;i++) { var r=resolutions[i];Button(page,r.x+" × "+r.y,190,265+i*70,900,54,()=>TryDisplay(r.x,r.y,false),r.x<=Display.main.systemWidth&&r.y<=Display.main.systemHeight); }
            Button(page,"Sincronización vertical: "+(s.vsync?"Sí":"No"),190,500,900,54,()=>{s.vsync=!s.vsync;s.Apply();PersistSettings();DisplaySettings();});
        }
        private void TryDisplay(int width,int height,bool full)
        {
            oldDisplay=JsonUtility.FromJson<GameSettings>(JsonUtility.ToJson(Game.Settings));
            Game.Settings.width=width;Game.Settings.height=height;Game.Settings.fullscreen=full;Game.Settings.Apply(true);displayUntil=Time.unscaledTime+15;
            Confirm("¿Conservar estos ajustes?","",()=>{oldDisplay=null;PersistSettings();DisplaySettings();},RevertDisplay,"Conservar");
            displayTimer=Label(page,"",180,330,920,50,24,TextAnchor.MiddleCenter);
        }
        private void RevertDisplay()
        {
            if(oldDisplay==null)return;
            Game.Settings.width=oldDisplay.width;Game.Settings.height=oldDisplay.height;Game.Settings.fullscreen=oldDisplay.fullscreen;oldDisplay=null;Game.Settings.Apply(true);DisplaySettings();
        }
        private void PersistSettings() { if(!Game.Settings.Save(Game.DataDirectory,out string error)) Toast(error); }
        public void Confirm(string title,string message,Action yes,Action no,string confirm="Confirmar")
        {
            Begin("confirm",title,"",no); Box(page,130,180,1020,350,Panel);
            Label(page,message,180,210,920,180,22,TextAnchor.MiddleCenter);
            Button(page,confirm,230,430,380,60,yes);
            Focus(Button(page,"Cancelar",670,430,380,60,no));
        }
        public void Message(string message)
        {
            afterMessage=Game.Data==null?(Action)MainMenu:Game.Data.healthQuarters<=0?Death:Pause;
            Begin("error","NO SE COMPLETÓ LA ACCIÓN","",afterMessage);
            Label(page,message,110,220,1060,220,23,TextAnchor.MiddleCenter);
            Focus(Button(page,"Volver",400,500,480,60,afterMessage));
        }
        public void Death()
        {
            Begin("death","LA AVENTURA CONTINÚA",Game.IsTrial?"+15 segundos. Reintentar conserva el reloj y recupera el último checkpoint del reto.":"Has perdido todos los corazones. Puedes cargar un guardado para volver a intentarlo.");
            Focus(Button(page,Game.IsTrial?"Reintentar checkpoint":"Reintentar último guardado",300,250,680,64,Game.Retry));
            Button(page,"Elegir otro guardado",300,340,680,64,()=>History(Death),!Game.IsPractice&&!Game.IsTrial&&Game.Store.HasRecords);
            Button(page,"Volver al menú",300,430,680,64,Game.ToMenu);
        }
        public void Loading() { Begin("loading","JUAN CAPI","Cargando la aventura…"); }
        public void Toast(string value) { toast.text=value;toastUntil=Time.unscaledTime+3.5f;toastRoot.SetAsLastSibling(); }
        public void Prompt(string value) { if(prompt!=null)prompt.text=value; }
        private void Quit()
        {
            PersistSettings();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit();
            #endif
        }
        private void Thumbnail(Transform parent,SaveRecord r,float x,float y,float width,float height)
        {
            Texture texture=Resources.Load<Texture2D>("Foundation/MenuForest");
            if(!string.IsNullOrEmpty(r.thumbnail))try{var t=new Texture2D(2,2);if(t.LoadImage(Convert.FromBase64String(r.thumbnail))){thumbnails.Add(t);texture=t;}else Destroy(t);}catch{}
            var image=Rect("Vista",parent,x,y,width,height).gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
        }
        private RectTransform Rect(string name,Transform parent,float x,float y,float width,float height)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();
            rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(width,height);return rt;
        }
        private Image Box(Transform parent,float x,float y,float w,float h,Color color)
        {var image=Rect("Panel",parent,x,y,w,h).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;if(w>=100&&h>=45&&w<1280)AdventureFrame.Add(image.rectTransform);return image;}
        private Text Label(Transform parent,string value,float x,float y,float w,float h,int size,TextAnchor alignment=TextAnchor.UpperLeft,Color? color=null)
        {
            var text=Rect("Texto",parent,x,y,w,h).gameObject.AddComponent<Text>();text.font=font;text.text=value;
            text.fontSize=Mathf.RoundToInt(size*(Game.Settings?.uiScale??1));text.color=color??Color.white;text.alignment=alignment;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;text.raycastTarget=false;return text;
        }
        private Button Button(Transform parent,string text,float x,float y,float w,float h,Action action,bool enabled=true)
        {
            var rt=Rect(text.Length>0?text:"Registro",parent,x,y,w,h);var image=rt.gameObject.AddComponent<Image>();image.color=Panel;AdventureFrame.Add(rt);
            var b=rt.gameObject.AddComponent<Button>();b.targetGraphic=image;b.interactable=enabled;
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.55f,.95f,.87f);colors.selectedColor=new Color(.6f,.95f,.85f);colors.pressedColor=Gold;colors.disabledColor=new Color(.4f,.5f,.5f,.5f);b.colors=colors;
            Label(rt,text,16,0,w-32,h,20,TextAnchor.MiddleCenter,enabled?Color.white:new Color(.48f,.6f,.6f));
            var accent=Box(rt,0,0,4,h,Gold);accent.gameObject.SetActive(false);rt.gameObject.AddComponent<SelectionAccent>().indicator=accent.gameObject;
            b.onClick.AddListener(()=>{CampaignAudio.Play(text.Contains("Cancelar")||text.Contains("Volver")?"cancel":"confirm",true);action?.Invoke();});return b;
        }
        private void Slider(string title,float value,float y,Action<float> action)
        {
            var label=Label(page,title+"   "+Mathf.RoundToInt(value*100)+" %",66,y,430,42,22);
            var rt=Rect(title,page,525,y+8,650,30);var slider=rt.gameObject.AddComponent<Slider>();
            var track=Box(rt,0,0,650,30,new Color(.17f,.3f,.32f));track.raycastTarget=true;
            var fillArea=Rect("Relleno",rt,12,10,626,10);
            var fill=Box(fillArea,0,0,0,0,Cyan);fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
            var handleArea=Rect("Recorrido",rt,12,0,626,30);
            var handle=Box(handleArea,0,0,24,0,Gold);handle.rectTransform.anchorMin=Vector2.zero;handle.rectTransform.anchorMax=new Vector2(0,1);handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.anchoredPosition=Vector2.zero;handle.raycastTarget=true;
            slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
            var nav=slider.navigation;nav.mode=Navigation.Mode.Automatic;slider.navigation=nav;slider.value=value;
            slider.onValueChanged.AddListener(v=>{action(v);label.text=title+"   "+Mathf.RoundToInt(v*100)+" %";});
        }
        private void Focus(Selectable selectable) { if(selectable!=null&&selectable.interactable)events.SetSelectedGameObject(selectable.gameObject); }
        private static string LocalDate(string utc)=>DateTime.TryParse(utc,out DateTime date)?date.ToLocalTime().ToString("dd/MM/yyyy HH:mm"):"Fecha desconocida";
        public static string FormatTime(double seconds)=>TimeSpan.FromSeconds(Math.Min(seconds,TimeSpan.MaxValue.TotalSeconds-1)).ToString(@"hh\:mm\:ss");
    }
}
