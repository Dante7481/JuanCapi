using UnityEngine;

namespace JuanCapi
{
    public sealed class RastreadorBoss:SierraDamageTarget
    {
        public AnimatedActor actor;
        public GameObject leftGate,rightGate;
        public int health=24;
        public bool Fighting=>state>0&&state<7;
        public bool Resolved=>state==8;
        public string Pattern=>pattern==0?"Barrido":pattern==1?"Salto pesado":"Impulso";
        public float TimeToStrike=>state==3?Mathf.Max(0,until-Time.time):-1;
        public string StateName=>new[]{"Dormant","Intro","Approach","Windup","Strike","Recovery","Defeat","Reward","Resolved"}[state];
        private int state,pattern,cycle;
        private float until,started,facing=-1;
        private Vector3 origin,landing;
        private SierraThreat warning;
        private GameRuntime Game=>GameRuntime.Instance;
        private void Start()
        {
            warning=SierraThreat.Create(transform);
            if(Game.Data.dash){state=8;health=0;gameObject.SetActive(false);Gates(false);return;}
            actor.Face(-1);actor.Play("Idle");Gates(false);
        }
        private void Gates(bool closed){leftGate.SetActive(closed);rightGate.SetActive(closed);}
        private void Update()
        {
            if(Game?.CanPlay!=true)return;
            Game.UI.BossMeter(health,24,Fighting);
            if(state==0)
            {
                if(Game.Player.transform.position.x>77&&Game.Data.rescues.Contains("MARA"))
                {state=1;until=Time.time+1.4f;Gates(true);actor.Play("Telegraph",true);}
                return;
            }
            if(state==8)return;
            if(state==7){TryReward();return;}
            if(state==6)
            {
                if(Time.time>=until){state=7;TryReward();}
                return;
            }
            if(state==1){if(Time.time>=until)Approach();return;}
            var player=Game.Player.transform.position;
            if(state==2)
            {
                facing=player.x<transform.position.x?-1:1;actor.Face(facing);
                if(Mathf.Abs(player.x-transform.position.x)>2.8f&&Time.time<until)
                {var p=transform.position;p.x=Mathf.Clamp(p.x+facing*2.8f*Time.deltaTime,78,93);transform.position=p;actor.Play("Run");}
                else Windup();
            }
            else if(state==3)
            {
                if(Time.time>=until)
                {state=4;started=Time.time;until=Time.time+(pattern==0?.22f:pattern==1?.65f:.5f);actor.Play("Attack",true);}
            }
            else if(state==4)
            {
                if(pattern==1)
                {
                    float t=Mathf.Clamp01((Time.time-started)/.65f);transform.position=Vector3.Lerp(origin,landing,t)+Vector3.up*(4*t*(1-t)*3.3f);
                }
                else if(pattern==2)
                {var p=transform.position;p.x=Mathf.Clamp(p.x+facing*15*Time.deltaTime,78,93);transform.position=p;}
                bool contact=pattern==0?Mathf.Abs(player.x-(origin.x+facing*1.3f))<1.9f&&player.y<.85f:
                    pattern==1?Time.time>=until&&Mathf.Abs(player.x-landing.x)<2&&player.y<1.6f:
                    Mathf.Abs(player.x-transform.position.x)<1.25f&&player.y<.9f;
                if(contact)Game.Player.DamageQuarters(transform.position,pattern==1?4:2);
                if(Time.time>=until)
                {
                    if(pattern==1){transform.position=landing;PlasmaEffects.Burst(landing,new Color(1,.67f,.25f),2);}
                    state=5;until=Time.time+(pattern==1?1.4f:pattern==2?1.2f:1f);warning.Hide();actor.Play("Recovery",true);
                }
            }
            else if(state==5&&Time.time>=until)Approach();
        }
        private void Approach(){state=2;until=Time.time+1.1f;}
        private void Windup()
        {
            state=3;pattern=cycle++%(health<=12?3:2);origin=transform.position;
            facing=Game.Player.transform.position.x<origin.x?-1:1;actor.Face(facing);actor.Play("Telegraph",true);
            until=Time.time+(pattern==0?.7f:pattern==1?.9f:.85f);
            landing=new Vector3(Mathf.Clamp(Game.Player.transform.position.x,78,93),0,0);
            if(pattern==0)warning.Show((Vector2)origin+new Vector2(facing*1.3f,.12f),3.8f,.3f);
            else if(pattern==1)warning.Show((Vector2)landing+Vector2.up*.12f,4,.45f);
            else warning.Show(new Vector2(origin.x+facing*3.6f,.12f),7.5f,.3f);
        }
        protected override void Hit(Vector2 originPoint)
        {
            if(Game?.CanPlay!=true||state<2||state>5||health<=0)return;
            health--;PlasmaEffects.Burst(transform.position+Vector3.up,new Color(.35f,1,1),.7f);
            if(health==0)
            {
                state=6;until=Time.time+1.1f;warning.Hide();actor.Play("Death",true);
                transform.position=new Vector3(transform.position.x,0,0);GetComponent<Collider2D>().enabled=false;
                Game.UI.BossMeter(0,24,false);
            }
        }
        private void TryReward()
        {
            if(!Game.Transaction(d=>
            {
                if(!d.Grant(CampaignReward.Dash))return false;
                if(!d.defeated.Contains("RASTREADOR"))d.defeated.Add("RASTREADOR");
                d.x=96;d.y=.08f;d.z=0;d.hasPosition=true;return true;
            },"Impulsor recuperado · dash"))return;
            state=8;Gates(false);SierraLevel.Current.ShowDashReward(transform.position+Vector3.up);
            Game.UI.BossMeter(0,24,false);actor.transform.localRotation=Quaternion.Euler(0,0,25);
        }
    }
}
