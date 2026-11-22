using System.Collections;
using UnityEngine;
namespace JuanCapi
{
    [DefaultExecutionOrder(-50)]
    public sealed class MineLevel:MonoBehaviour
    {
        public static MineLevel Current {get;private set;}
        public DirectorBoss boss;public MineCamera view;public MineProp[] props;
        public GameObject entryGate,waterGate,lumaGate,controlGate,epilogueRoot;
        public AnimatedActor luma;public Transform valve;public SpriteRenderer[] dischargeLights;
        public int Reflections {get;private set;}
        public bool EndingRunning {get;private set;}
        private bool checkpoint2,checkpoint3,checkpoint5,endingDone;private float endingClock;private Vector3 fallAnchor;
        private GameRuntime Game=>GameRuntime.Instance;
        private void Awake(){Current=this;}
        private void OnDestroy(){if(Game!=null)Game.Changed-=Refresh;if(Current==this)Current=null;}
        public bool IsWater(Vector3 p)=>p.x>46&&p.x<72&&p.y<.85f;
        public Vector2 CurrentAt(Vector3 p)=>IsWater(p)&&p.y<-2?Vector2.right*.45f:Vector2.zero;
        public float BeltAt(Vector3 p)=>p.x>33&&p.x<40&&p.y<.3f?1.8f:0;
        public void ReflectedShot(){Reflections++;}
        public void Prepare(CampaignData d,bool practice)
        {
            Current=this;d.region="MinerTown · cantera industrial";
            if(practice){d.sword=d.dash=d.gun=d.shield=d.introSeen=d.sierraFinished=d.swampFinished=true;d.maxHearts=5;d.healthQuarters=20;foreach(var id in new[]{"MARA","TICO"})if(!d.rescues.Contains(id))d.rescues.Add(id);}
            if(d.mineLayout<7||!d.hasPosition)
            {
                d.mineLayout=7;d.hasPosition=true;d.x=d.completed||d.defeated.Contains("DIRECTOR")?158:d.maintenanceKey?95:3;d.y=.08f;d.z=0;
                d.checkpoint=d.x>155?"CP_M06":d.maintenanceKey?"CP_M04":"CP_M01";
            }
            if(!d.defeated.Contains("DIRECTOR")&&!d.completed&&d.x>131){d.x=127;d.y=.08f;d.z=0;}
            if(IsWater(new Vector3(d.x,d.y)))d.equippedWeapon="gun";
            checkpoint2=d.x>19;checkpoint3=d.x>72||d.maintenanceKey;checkpoint5=d.checkpoint=="CP_M05"||d.defeated.Contains("DIRECTOR")||d.completed;
            fallAnchor=new Vector3(d.x,d.y,d.z);
        }
        private IEnumerator Start()
        {
            while(Game==null||Game.Busy||Game.Data==null)yield return null;
            Game.Changed+=Refresh;Refresh();epilogueRoot.SetActive(false);
            if(Game.Data.completed)
            {ClearThreats();if(Game.Data.epilogueSeen)Game.UI.CampaignResults();else StartEnding(true);}
        }
        public void Refresh()
        {
            if(Game?.Data==null)return;var d=Game.Data;
            foreach(var p in props)if(p!=null)p.Refresh();
            entryGate.SetActive(!d.collected.Contains("M01_REFLECT")&&!d.completed);
            waterGate.SetActive(!d.collected.Contains("M03_TARGET")&&!d.completed);
            lumaGate.SetActive(!(d.collected.Contains("M04_CABLE")&&d.collected.Contains("M04_REFLECT"))&&!d.completed);
            controlGate.SetActive(!d.maintenanceKey&&!d.completed);luma.Play(d.rescues.Contains("LUMA")?"Celebrate":"Idle");
            foreach(var light in dischargeLights)light.color=d.completed?new Color(.2f,.55f,.5f):new Color(1,.64f,.2f);
        }
        public Vector3 SafeSavePosition(Vector3 p)
        {
            if(boss.Fighting)return new Vector3(127,.08f,0);
            if(IsWater(p))return new Vector3(Mathf.Clamp(p.x,47,71),Mathf.Clamp(p.y,-6.5f,.8f),0);
            var ground=Physics2D.Raycast(p+Vector3.up*.15f,Vector2.down,.5f,1<<3);
            if(ground.collider!=null&&ground.normal.y>.5f&&ground.collider.bounds.size.x>1.2f)p.x=Mathf.Clamp(p.x,ground.collider.bounds.min.x+.6f,ground.collider.bounds.max.x-.6f);
            return p;
        }
        private void Update()
        {
            if(Game?.CanPlay!=true)return;var d=Game.Data;var p=Game.Player.transform.position;
            Game.Player.SetAquatic(IsWater(p));
            if(p.y<-10)
            {
                if(d.completed){Game.Player.transform.position=fallAnchor;Game.Player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;}
                else Game.Player.RespawnAt(fallAnchor);return;
            }
            if(!Game.Player.aquatic&&Game.Player.EnSuelo&&!Game.Player.InDanger)fallAnchor=SafeSavePosition(p)+Vector3.up*.05f;
            if(!checkpoint2&&p.x>19)checkpoint2=Checkpoint("CP_M02",20,"Patio de cintas");
            if(!checkpoint3&&p.x>73)checkpoint3=Checkpoint("CP_M03",74,"Salida del conducto");
            if(!checkpoint5&&p.x>125&&d.maintenanceKey)checkpoint5=Checkpoint("CP_M05",127,"Acceso al centro de control");
            MineProp nearest=null;float distance=2.4f;
            foreach(var prop in props){if(prop==null||!prop.Usable)continue;float value=Vector2.Distance(Game.Player.Center,prop.transform.position);if(value<distance){nearest=prop;distance=value;}}
            Game.UI.PromptIcon(nearest!=null?nearest.icon:null);if(nearest!=null&&GameInput.Down("Interactuar"))nearest.Use();
            if(!d.collected.Contains("M01_REFLECT")||p.x>78&&p.x<85&&!d.collected.Contains("M04_REFLECT"))Game.UI.Lesson("Escudo","shield");
            else if(p.x>21&&p.x<33)Game.UI.Lesson("Dash","dash");
            else if(Game.Player.aquatic)Game.UI.Lesson("Nadar abajo","swim");else Game.UI.Lesson(null,null);
            if(d.completed){Game.UI.Objective(null,Vector3.zero);return;}
            string icon;Vector3 objective;
            if(!d.collected.Contains("M01_REFLECT")){icon="shield";objective=new Vector3(14,1);}
            else if(p.x<72&&!d.collected.Contains("M03_TARGET")){icon="gun";objective=new Vector3(67,-5);}
            else if(!d.maintenanceKey){icon=!d.collected.Contains("M04_CABLE")?"sword":!d.collected.Contains("M04_REFLECT")?"shield":"key";objective=new Vector3(84,1);}
            else {bool won=d.defeated.Contains("DIRECTOR");icon=won?"valve":"director";objective=new Vector3(won?161:142,1);}
            Game.UI.Objective(icon,objective-p);
        }
        private bool Checkpoint(string id,float x,string label)=>Game.Transaction(d=>{d.checkpoint=id;d.x=x;d.y=.08f;d.z=0;d.hasPosition=true;return true;},label);
        private void ClearThreats()
        {
            CampaignProjectile.ClearHostile();foreach(var e in FindObjectsByType<MineEnemy>(FindObjectsSortMode.None))if(!e.Dead)e.Retire();
            foreach(var e in FindObjectsByType<SwampEnemy>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
        }
        public bool CloseDischarge()
        {
            if(!Game.CanPlay||EndingRunning||!Game.Data.defeated.Contains("DIRECTOR")||!Game.Data.maintenanceKey)return false;
            ClearThreats();StartEnding(false);return true;
        }
        private void StartEnding(bool completed)
        {EndingRunning=true;endingDone=false;endingClock=completed?2:0;Game.Cinematic=true;Game.Player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;StartCoroutine(Ending());}
        private IEnumerator Ending()
        {
            bool tableau=false;
            while(!endingDone)
            {
                if(Game.UI.ScreenName=="cinematic"||endingClock==0||Game.UI.ScreenName=="game")
                {
                    endingClock+=Time.deltaTime;
                    if(!Game.Data.completed)
                    {
                        view.Cut(new Vector3(156,3.1f,-10));Game.UI.CinematicFrame("Luma: Ya no controla las compuertas. Ahora.",()=>endingClock=2,"CIERRE DE LA DESCARGA");
                        valve.localRotation=Quaternion.Euler(0,0,-Mathf.Clamp01(endingClock/2)*90);
                        if(endingClock>=2)
                        {
                            if(!Game.Transaction(d=>{if(!d.Grant(CampaignReward.Director))return false;d.mineSeconds=System.Math.Max(0,d.activeSeconds-d.mineStarted);d.campaignSeconds=d.activeSeconds;d.x=160;d.y=.08f;d.z=0;d.hasPosition=true;return true;},"Descarga cerrada · campaña guardada"))yield return null;
                        }
                    }
                    else
                    {
                        if(!tableau){tableau=true;epilogueRoot.SetActive(true);Game.Player.gameObject.SetActive(false);view.Cut(new Vector3(210,3.1f,-10));}
                        string caption=endingClock<7?"Semanas después, el agua comenzó a aclararse. Los refugios siguieron trabajando.":endingClock<12?"Las pruebas salieron de MinerTown. Mara, Tico y Luma ayudaron a recuperar las orillas.":"Juan: Todavía queda trabajo. Pero el río vuelve a tener una oportunidad.";
                        Game.UI.CinematicFrame(caption,FinishEpilogue,"SEMANAS DESPUÉS");
                        if(endingClock>=17)FinishEpilogue();
                    }
                }
                yield return null;
            }
        }
        public void FinishEpilogue()
        {
            if(!EndingRunning||!Game.Data.completed||Game.Busy)return;
            if(!Game.Data.epilogueSeen&&!Game.Transaction(d=>{d.epilogueSeen=true;return true;},"Epílogo completado"))return;
            endingDone=true;EndingRunning=false;Game.Cinematic=false;epilogueRoot.SetActive(false);
            Game.Player.transform.position=new Vector3(160,.08f);Game.Player.gameObject.SetActive(true);view.directed=false;view.transform.position=new Vector3(155,3.1f,-10);
            Game.UI.CampaignResults();
        }
    }
}
