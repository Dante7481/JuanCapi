using System.Collections;
using UnityEngine;

namespace JuanCapi
{
    [DefaultExecutionOrder(80)]
    public sealed class SwampLevel:MonoBehaviour
    {
        public static SwampLevel Current { get; private set; }
        public bool aquatic;
        public CaimanEncounter boss;
        public SwampProp[] props;
        public GameObject rescueGate;
        public AnimatedActor tico;
        public int Reflections { get; private set; }
        private bool arenaSaved;
        private Vector3 fallAnchor;
        private GameRuntime Game=>GameRuntime.Instance;
        private void Awake(){Current=this;}
        private void OnDestroy(){if(Game!=null)Game.Changed-=Refresh;if(Current==this)Current=null;}
        public static bool TargetsComplete(CampaignData d)=>d.collected.Contains("P05_TARGET_1")&&d.collected.Contains("P05_TARGET_2")&&d.collected.Contains("P05_TARGET_3");
        public void Prepare(CampaignData d,bool practice)
        {
            Current=this;d.region=aquatic?"Pantano · aguas profundas":"Pantano · orilla de los vertidos";
            if(practice)
            {
                d.sword=d.dash=true;d.maxHearts=4;d.healthQuarters=16;d.sierraFinished=d.introSeen=true;
                if(!d.rescues.Contains("MARA"))d.rescues.Add("MARA");
                if(aquatic){d.gun=true;d.defeated.Add("CAIMAN_LAND");}
            }
            if(d.swampLayout<6||!d.hasPosition)
            {
                d.swampLayout=6;d.hasPosition=true;d.z=0;
                d.x=aquatic?d.shield?93:d.checkpoint=="CP_A04"?64:d.rescues.Contains("TICO")?40:3:d.gun?104:d.checkpoint=="CP_P04"?79:3;
                d.y=aquatic?9:.08f;d.checkpoint=aquatic?d.shield?"CP_A05":d.rescues.Contains("TICO")?"CP_A03":"CP_A01":d.gun?"CP_P05":d.x>70?"CP_P04":"CP_P01";
            }
            if(aquatic)d.equippedWeapon="gun";
            bool won=d.defeated.Contains(aquatic?"CAIMAN_WATER":"CAIMAN_LAND")||(aquatic?d.shield:d.gun);
            arenaSaved=won||d.checkpoint==(aquatic?"CP_A04":"CP_P04");
            if(!won&&d.x>(aquatic?66:82)){d.x=aquatic?64:79;d.y=aquatic?9:.08f;}
            fallAnchor=new Vector3(d.x,d.y,0);
        }
        private IEnumerator Start()
        {
            while(Game==null||Game.Busy||Game.Data==null)yield return null;
            Game.Changed+=Refresh;Refresh();arenaSaved=Game.Data.checkpoint==(aquatic?"CP_A04":"CP_P04")||Game.Data.defeated.Contains(aquatic?"CAIMAN_WATER":"CAIMAN_LAND");
        }
        private void Refresh()
        {
            if(Game?.Data==null)return;
            foreach(var prop in props)if(prop!=null)prop.Refresh();
            if(rescueGate!=null)rescueGate.SetActive(!Game.Data.rescues.Contains("TICO"));
            if(tico!=null)tico.Play(Game.Data.rescues.Contains("TICO")?"Celebrate":"Idle");
        }
        public Vector3 SafeSavePosition(Vector3 position)
        {
            if(boss!=null&&boss.Fighting)return new Vector3(aquatic?64:79,aquatic?9:.08f,0);
            if(aquatic)return new Vector3(Mathf.Clamp(position.x,1,108),Mathf.Clamp(position.y,1,18.5f),0);
            var ground=Physics2D.Raycast(position+Vector3.up*.15f,Vector2.down,.5f,1<<3);
            if(ground.collider!=null&&ground.normal.y>.5f&&ground.collider.bounds.size.x>1.2f)position.x=Mathf.Clamp(position.x,ground.collider.bounds.min.x+.6f,ground.collider.bounds.max.x-.6f);
            return position;
        }
        public Vector2 CurrentAt(Vector3 position)=>aquatic&&position.x>17&&position.x<25&&position.y<11?Vector2.right*.65f:Vector2.zero;
        private void Update()
        {
            if(Game?.CanPlay!=true)return;var d=Game.Data;var p=Game.Player.transform.position;
            if(!aquatic&&p.y<-5){Game.Player.RespawnAt(fallAnchor);return;}
            if(!aquatic&&Game.Player.EnSuelo&&!Game.Player.InDanger)
            {
                var support=Physics2D.Raycast(p+Vector3.up*.15f,Vector2.down,.5f,1<<3);
                if(support.collider!=null&&support.normal.y>.5f)fallAnchor=SafeSavePosition(p)+Vector3.up*.05f;
            }
            if(!arenaSaved&&p.x>(aquatic?62:78)&&(aquatic?d.rescues.Contains("TICO"):d.collected.Contains("P03_CABLE")))
                arenaSaved=Game.Transaction(next=>{next.checkpoint=aquatic?"CP_A04":"CP_P04";next.x=aquatic?64:79;next.y=aquatic?9:.08f;next.z=0;next.hasPosition=true;return true;},aquatic?"Refugio del guardián":"Territorio del caimán");
            SwampProp nearest=null;float distance=2.4f;
            foreach(var prop in props)
            {
                if(prop==null||!prop.Usable)continue;float value=Vector2.Distance(Game.Player.Center,prop.transform.position);
                if(value<distance){nearest=prop;distance=value;}
            }
            Game.UI.PromptIcon(nearest!=null?nearest.icon:null);
            if(nearest!=null&&GameInput.Down("Interactuar"))nearest.Use();
            if(!aquatic&&p.x>16&&p.x<57)Game.UI.Lesson("Dash","dash");
            else if(!aquatic&&d.gun&&!TargetsComplete(d))Game.UI.Lesson("Atacar","gun");
            else if(aquatic&&p.x<10)Game.UI.Lesson("Nadar arriba","swim");
            else if(aquatic&&d.shield&&!d.collected.Contains("A06_REFLECTED"))Game.UI.Lesson("Escudo","shield");
            else Game.UI.Lesson(null,null);
            Game.UI.TrainingTargets(!aquatic&&d.gun&&!TargetsComplete(d),d);
            Vector3 objective=Objective(out string icon);Game.UI.Objective(icon,objective-Game.Player.transform.position);
        }
        private Vector3 Objective(out string icon)
        {
            var d=Game.Data;
            if(!aquatic)
            {
                if(!d.collected.Contains("P03_CABLE")){icon="sword";return new Vector3(75,1);}
                if(!d.gun){icon=d.defeated.Contains("CAIMAN_LAND")?"gun":"caiman";return new Vector3(d.defeated.Contains("CAIMAN_LAND")?105:94,1);}
                icon=TargetsComplete(d)?"valve":"gun";return new Vector3(TargetsComplete(d)?125:116,2);
            }
            if(!d.rescues.Contains("TICO")){icon="cage";return new Vector3(34,14);}
            if(!d.shield){icon=d.defeated.Contains("CAIMAN_WATER")?"shield":"caiman";return new Vector3(d.defeated.Contains("CAIMAN_WATER")?93:80,9);}
            icon=d.collected.Contains("A06_REFLECTED")?"valve":"shield";return new Vector3(d.collected.Contains("A06_REFLECTED")?107:103,9);
        }
        public bool Dive()
        {
            if(aquatic||!Game.CanPlay||!Game.Data.gun||!TargetsComplete(Game.Data))return false;
            return Game.Travel("AmazoniaAcuatica","CP_A01","Pantano · aguas profundas",new Vector3(3,9,0),true);
        }
        public void ReflectedShot(){Reflections++;}
        public bool FinishLevel()
        {
            if(!aquatic||!Game.CanPlay||!Game.Data.shield||!Game.Data.collected.Contains("A06_REFLECTED"))return false;
            if(!Game.Data.swampFinished&&!Game.Transaction(d=>{d.swampFinished=true;d.swampSeconds=System.Math.Max(0,d.activeSeconds-d.swampStarted);d.checkpoint="CP_A06";return true;},"Pantano superado"))return false;
            Game.UI.Lesson(null,null);Game.UI.SwampResults();return true;
        }
    }
}
