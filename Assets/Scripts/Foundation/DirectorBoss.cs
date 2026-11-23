using System.Collections.Generic;
using UnityEngine;
namespace JuanCapi
{
    public sealed class DirectorBoss:SierraDamageTarget
    {
        public AnimatedActor actor;public GameObject entrance,exit;public int health=42;
        public int Phase=>health>28?1:health>14?2:3;
        public bool Fighting=>state>0&&state<6;
        public bool Resolved=>state==7;
        public bool CoreOpen=>Phase!=2||Time.time<coreUntil;
        public float CoreRemaining=>Mathf.Max(0,coreUntil-Time.time);
        public string StateName=>new[]{"Waiting","Introduction","Approach","Telegraph","Attack","Recovery","Defeated","Resolved"}[state];
        public string Pattern=>pattern==0?"Golpe":pattern==1?"Onda":pattern==2?"Reflector":pattern==3?"Ráfaga":"Embestida";
        public float TimeToStrike=>state==3?Mathf.Max(0,until-Time.time):-1;
        public int ReflectedHits {get;private set;}
        public int ArmorBlocks {get;private set;}
        public int SummonPeak {get;private set;}
        private int state,cycle,pattern,shots;private float until,started,facing=-1,coreUntil,nextSummon;private bool hit;
        private Vector2 locked;private AttackTelegraph warning;private LineRenderer armor;private Material armorMaterial;
        private readonly List<MineEnemy> drones=new List<MineEnemy>();
        private GameRuntime Game=>GameRuntime.Instance;
        private void Start()
        {
            warning=AttackTelegraph.Create(transform);actor.Face(-1);actor.Play("Idle");Gates(false);
            var ring=new GameObject("Núcleo · blindaje");ring.transform.SetParent(transform,false);ring.transform.localPosition=Vector3.up*1.7f;
            armor=ring.AddComponent<LineRenderer>();armorMaterial=new Material(Shader.Find("Sprites/Default"));armor.sharedMaterial=armorMaterial;
            armor.useWorldSpace=false;armor.loop=true;armor.positionCount=40;armor.startWidth=armor.endWidth=.045f;armor.sortingOrder=28;
            for(int i=0;i<40;i++){float a=i*Mathf.PI*2/40;armor.SetPosition(i,new Vector3(Mathf.Cos(a)*1.5f,Mathf.Sin(a)*1.65f));}
            if(Game.Data.defeated.Contains("DIRECTOR")||Game.Data.completed){health=0;state=7;GetComponent<Collider2D>().enabled=false;actor.Play("Death",true);}
        }
        private void OnDestroy(){if(armorMaterial!=null)Destroy(armorMaterial);}
        private void Gates(bool on){entrance.SetActive(on);exit.SetActive(on);}
        private void Update()
        {
            if(Game?.CanPlay!=true)return;
            Game.UI.EncounterMeter(health,42,Fighting,"DIRECTOR · EXOTRAJE",CampaignArt.Current.directorPortrait);
            Game.UI.DirectorCore(Fighting&&Phase==2,CoreOpen,CoreRemaining);
            armor.enabled=Fighting&&Phase==2;armor.startColor=armor.endColor=CoreOpen?new Color(.2f,1,1,.7f):new Color(1,.75f,.2f,.85f);
            if(state==7)return;
            if(state==6){if(Time.time>=until)Resolve();return;}
            Vector2 delta=Game.Player.Center-(Vector2)transform.position;
            if(state==0)
            {if(Game.Player.transform.position.x>131&&Game.Data.maintenanceKey&&Game.Data.rescues.Contains("LUMA")){state=1;until=Time.time+1.2f;Gates(true);actor.Play("Telegraph",true);}return;}
            if(state==1){if(Time.time>=until)Approach();return;}
            if(state==2)
            {
                facing=delta.x<0?-1:1;actor.Face(facing);
                if(Phase!=2&&Mathf.Abs(delta.x)>3.3f&&Time.time<until)
                {var p=transform.position;p.x=Mathf.Clamp(p.x+facing*2.6f*Time.deltaTime,133,153);transform.position=p;actor.Play("Run");}
                else Windup();
            }
            else if(state==3)
            {
                if(Time.time>=until)
                {state=4;started=Time.time;until=Time.time+(pattern==4?.8f:pattern==3?.65f:.3f);hit=false;shots=0;warning.Hide();actor.Play("Attack",true);}
            }
            else if(state==4)
            {
                if(pattern==0&&!hit&&Mathf.Abs(Game.Player.Center.x-(transform.position.x+facing*1.5f))<2.2f&&Game.Player.transform.position.y<1.2f)
                {hit=true;Game.Player.DamageQuarters(transform.position,4,true);}
                if(pattern==1&&!hit)
                {hit=true;CampaignProjectile.Launch((Vector2)transform.position+new Vector2(facing*2,.25f),Vector2.right*facing*7,true,false,false,gameObject,2);}
                if(pattern==2&&!hit)
                {hit=true;Fire(true);}
                if(pattern==3&&shots<3&&Time.time-started>=shots*.18f){shots++;Fire(false);}
                if(pattern==4)
                {
                    var p=transform.position;p.x=Mathf.Clamp(p.x+facing*12*Time.deltaTime,133,153);transform.position=p;
                    if(!hit&&Mathf.Abs(Game.Player.Center.x-p.x)<1.7f&&Game.Player.transform.position.y<1.3f){hit=true;Game.Player.DamageQuarters(p,2,true);}
                }
                if(Time.time>=until)Recovery();
            }
            else if(state==5&&Time.time>=until)Approach();
        }
        private void Approach(){state=2;until=Time.time+1.2f;actor.Play("Idle");}
        private void Windup()
        {
            pattern=Phase==1?cycle++%2:Phase==2?2+cycle++%2:4;
            state=3;until=Time.time+(pattern==1?.85f:1);locked=Game.Player.Center;actor.Play("Telegraph",true);
            warning.Arrow(transform.position+Vector3.up*(pattern==2||pattern==3?1.1f:.2f),Vector2.right*facing,pattern==4?8:4);
        }
        private void Fire(bool reflectable)
        {
            Vector2 origin=(Vector2)transform.position+new Vector2(facing*2,1.1f),direction=(locked-origin).normalized;
            CampaignProjectile.Launch(origin,direction*(reflectable?4:7.5f),true,reflectable,false,gameObject,2);
        }
        private void Recovery()
        {
            state=5;until=Time.time+(pattern==2?3.3f:1.5f);actor.Play("Recovery",true);
            if(Phase==3&&Time.time>=nextSummon)
            {
                drones.RemoveAll(d=>d==null||d.Dead);if(drones.Count==0)
                {drones.Add(CreateDrone(136));drones.Add(CreateDrone(150));SummonPeak=Mathf.Max(SummonPeak,drones.Count);nextSummon=Time.time+20;}
            }
        }
        private MineEnemy CreateDrone(float x)
        {
            var go=new GameObject("Dron del Director");go.transform.position=new Vector3(x,2.7f);
            var d=go.AddComponent<MineEnemy>();d.drone=d.summoned=true;d.health=3;
            var art=CampaignArt.Current;d.actor=AnimatedActor.Create("Dron",go.transform,art.enemyControllers[1],art.enemies[1],.8f);d.actor.sprite.sharedMaterial=art.enemyMaterial;
            var c=go.AddComponent<BoxCollider2D>();c.isTrigger=true;c.size=new Vector2(1.3f,1);return d;
        }
        protected override void Hit(Vector2 origin){Damage(1,false);}
        public override void PlasmaHit(Vector2 origin,int damage){Damage(damage,damage>=3);}
        private void Damage(int amount,bool reflected)
        {
            if(!Fighting||state==1||Game?.CanPlay!=true)return;
            if(Phase==2)
            {
                if(reflected){ReflectedHits++;coreUntil=Time.time+3;PlasmaEffects.Burst(transform.position+Vector3.up*1.7f,Color.cyan,1.2f);}
                else if(!CoreOpen){ArmorBlocks++;PlasmaEffects.Burst(transform.position+Vector3.up*1.7f,Color.yellow,.3f);return;}
            }
            health=Mathf.Max(0,health-amount);PlasmaEffects.Burst(transform.position+Vector3.up,Color.cyan,.6f);
            if(health==0)
            {
                state=6;until=Time.time+.8f;warning.Hide();actor.Defeat();GetComponent<Collider2D>().enabled=false;armor.enabled=false;
                foreach(var drone in drones)if(drone!=null&&!drone.Dead)drone.Retire();CampaignProjectile.ClearHostile();
            }
        }
        private void Resolve()
        {
            if(!Game.Transaction(d=>{if(d.defeated.Contains("DIRECTOR"))return false;d.defeated.Add("DIRECTOR");d.checkpoint="CP_M06";d.x=157;d.y=.08f;d.z=0;d.hasPosition=true;return true;},"Luma: ¡Ahora podemos cerrar la descarga!"))return;
            state=7;Gates(false);Game.UI.EncounterMeter(0,42,false,"",CampaignArt.Current.directorPortrait);Game.UI.DirectorCore(false,false,0);
        }
    }
}
