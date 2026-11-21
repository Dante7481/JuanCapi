using UnityEngine;

namespace JuanCapi
{
    public sealed class CaimanEncounter:SierraDamageTarget
    {
        public bool aquatic;
        public AnimatedActor actor;
        public GameObject entrance,exit;
        public int health;
        public int Maximum=>aquatic?24:30;
        public bool Fighting=>state>=1&&state<=5;
        public bool Resolved=>state==8;
        public string StateName=>new[]{"Waiting","Introduction","Approach","Telegraph","Attack","Recovery","Defeated","Retreat","Resolved"}[state];
        public string Pattern=>aquatic?"Embestida":pattern==0?"Garra":pattern==1?"Empujón":"Piedra";
        public float TimeToStrike=>state==3?Mathf.Max(0,until-Time.time):-1;
        public Vector2 ChargeDirection { get; private set; }
        private int state,pattern,cycle,charges;
        private float until,started,facing=-1;
        private bool hit,thrown;
        private Vector2 lockedTarget;
        private AttackTelegraph warning;
        private GameRuntime Game=>GameRuntime.Instance;
        private void Start()
        {
            health=Maximum;warning=AttackTelegraph.Create(transform);actor.Face(-1);actor.Play("Idle");Gates(false);
            string flag=aquatic?"CAIMAN_WATER":"CAIMAN_LAND";
            if(Game.Data.defeated.Contains(flag)||aquatic&&Game.Data.shield||!aquatic&&Game.Data.gun)
            {state=8;health=0;gameObject.SetActive(false);return;}
        }
        private void Gates(bool active){entrance.SetActive(active);exit.SetActive(active);}
        private void Update()
        {
            if(Game?.CanPlay!=true)return;
            Game.UI.EncounterMeter(health,Maximum,Fighting,aquatic?"CAIMÁN · AGUAS PROFUNDAS":"CAIMÁN · GUARDIÁN DEL PANTANO",CampaignArt.Current.caimanPortrait);
            if(state==8)return;
            if(state==6){if(Time.time>=until)ConfirmVictory();return;}
            if(state==7)
            {
                float t=Time.time-started;actor.Face(1);actor.Play(t<2.8f?"Escape":"Death");
                transform.position=Vector3.MoveTowards(transform.position,new Vector3(109,t<2.8f?0:-3,0),7*Time.deltaTime);
                if(t>=4){state=8;gameObject.SetActive(false);}return;
            }
            Vector2 player=Game.Player.Center,position=transform.position,delta=player-position;
            if(state==0)
            {
                bool arrived=aquatic?player.x>66&&Game.Data.rescues.Contains("TICO"):player.x>82&&Game.Data.collected.Contains("P03_CABLE");
                if(arrived){state=1;until=Time.time+1.1f;Gates(true);actor.Play("Appear",true);}return;
            }
            if(state==1){if(Time.time>=until)Approach();return;}
            if(state==2)
            {
                Face(delta);actor.Play(aquatic?health<=12?"Enraged":"Swim":"Run");
                if(aquatic)
                {
                    Vector2 desired=delta.magnitude<5?-delta.normalized:delta.normalized;
                    MoveWater(Steer(desired)*3.1f*Time.deltaTime);
                    if(Time.time>=until||delta.magnitude<8){charges=health<=12?2:1;Windup();}
                }
                else
                {
                    var p=transform.position;p.x=Mathf.Clamp(p.x+Mathf.Sign(delta.x)*2.8f*Time.deltaTime,84,102);transform.position=p;
                    if(Time.time>=until||Mathf.Abs(delta.x)<3.2f)Windup();
                }
            }
            else if(state==3)
            {
                if(aquatic)
                {
                    if(until-Time.time>.2f)ChargeDirection=delta.normalized;
                    Face(ChargeDirection);warning.Arrow(position,ChargeDirection,Mathf.Min(9,delta.magnitude+2));
                }
                if(Time.time>=until)
                {
                    state=4;started=Time.time;until=Time.time+(aquatic?.8f:pattern==0?.22f:pattern==1?.45f:.7f);hit=thrown=false;
                    actor.Play(aquatic?health<=12?"ChargeAlt":"Charge":pattern==0?"Claw":pattern==1?"Push":"Rock",true);warning.Hide();
                }
            }
            else if(state==4)
            {
                if(aquatic)
                {
                    bool blocked=MoveWater(ChargeDirection*11*Time.deltaTime);
                    Vector2 separation=player-(Vector2)transform.position;
                    if(!hit&&new Vector2(separation.x/2.0f,separation.y/1.2f).sqrMagnitude<1){hit=true;Game.Player.DamageQuarters(transform.position,2,true);}
                    if(blocked||Time.time>=until)Recovery();
                }
                else
                {
                    if(pattern==1){var p=transform.position;p.x=Mathf.Clamp(p.x+facing*8*Time.deltaTime,84,102);transform.position=p;}
                    if(pattern==2&&!thrown&&Time.time-started>.22f)
                    {
                        thrown=true;Vector2 origin=(Vector2)transform.position+new Vector2(facing*1.8f,1.8f);
                        Vector2 travel=lockedTarget-origin;float flight=Mathf.Clamp(Mathf.Abs(travel.x)/8,.6f,1.5f);
                        CampaignProjectile.Launch(origin,new Vector2(travel.x/flight,travel.y/flight+.5f*9.81f*1.3f*flight),true,false,true,gameObject,2);
                    }
                    if(pattern!=2&&!hit&&Mathf.Abs(player.x-(transform.position.x+facing*1.3f))<1.9f&&Game.Player.transform.position.y<.9f)
                    {hit=true;Game.Player.DamageQuarters(transform.position,2,true);}
                    if(Time.time>=until)Recovery();
                }
            }
            else if(state==5&&Time.time>=until)
            {if(aquatic&&charges>1){charges--;Windup();}else Approach();}
        }
        private void Approach(){state=2;until=Time.time+(aquatic?1.2f:health<=15?1.1f:1.3f);}
        private void Windup()
        {
            state=3;var delta=Game.Player.Center-(Vector2)transform.position;ChargeDirection=delta.normalized;
            facing=delta.x<0?-1:1;Face(delta);lockedTarget=Game.Player.Center;
            pattern=cycle++%3;float preparation=aquatic?(health<=12?.75f:.9f):pattern==0?.7f:pattern==1?.85f:1;
            until=Time.time+preparation;actor.Play(!aquatic&&pattern==2?"RockPrepare":"Telegraph",true);
            if(!aquatic)warning.Arrow((Vector2)transform.position+Vector2.up*.35f,Vector2.right*facing,pattern==1?4.8f:pattern==0?3.3f:7);
        }
        private void Recovery()
        {state=5;warning.Hide();actor.Play("Recovery",true);until=Time.time+(aquatic?(charges>1?.25f:health<=12?1.7f:1.4f):pattern==0?1:pattern==1?1.3f:1.4f);}
        private void Face(Vector2 direction)
        {
            if(direction.sqrMagnitude<.001f)return;actor.Face(direction.x);
            if(aquatic)actor.transform.localRotation=Quaternion.Euler(0,0,Mathf.Clamp(Mathf.Atan2(direction.y,Mathf.Abs(direction.x))*Mathf.Rad2Deg*Mathf.Sign(direction.x),-32,32));
        }
        private Vector2 Steer(Vector2 direction)
        {
            if(!Physics2D.CircleCast(transform.position,1,direction,2,1<<3))return direction;
            Vector2 best=direction;float score=-100;
            foreach(float a in new[]{-80f,-40,40,80,120,-120,180})
            {
                Vector2 d=Quaternion.Euler(0,0,a)*direction;var h=Physics2D.CircleCast(transform.position,1,d,3,1<<3);
                float value=(h.collider!=null?h.distance:3)+Vector2.Dot(direction,d)*.4f;if(value>score){score=value;best=d;}
            }
            return best;
        }
        private bool MoveWater(Vector2 movement)
        {
            Vector2 current=transform.position;var cast=Physics2D.CircleCast(current,1,movement.normalized,movement.magnitude,1<<3);
            bool blocked=cast.collider!=null;if(blocked)movement=movement.normalized*Mathf.Max(0,cast.distance-.03f);
            Vector2 next=current+movement,clamped=new Vector2(Mathf.Clamp(next.x,67.5f,89),Mathf.Clamp(next.y,2.3f,16.5f));
            transform.position=clamped;return blocked||(clamped-next).sqrMagnitude>.0001f;
        }
        protected override void Hit(Vector2 origin){if(!aquatic)Damage(1);}
        public override void PlasmaHit(Vector2 origin,int amount){if(aquatic)Damage(amount);}
        private void Damage(int amount)
        {
            if(Game?.CanPlay!=true||!Fighting||state==1||health<=0)return;
            health=Mathf.Max(0,health-amount);PlasmaEffects.Burst(transform.position+Vector3.up*(aquatic?0:1),Color.cyan,.65f);
            if(health==0){state=6;until=Time.time+.65f;actor.Play("Hurt",true);warning.Hide();CampaignProjectile.ClearHostile();GetComponent<Collider2D>().enabled=false;}
        }
        private void ConfirmVictory()
        {
            string id=aquatic?"CAIMAN_WATER":"CAIMAN_LAND";
            if(!Game.Transaction(d=>{if(d.defeated.Contains(id))return false;d.defeated.Add(id);d.x=aquatic?92:104;d.y=aquatic?8:.08f;d.z=0;d.hasPosition=true;return true;},aquatic?"La abrazadera se ha desprendido":"El caimán huye hacia el agua"))return;
            Gates(false);Game.UI.EncounterMeter(0,Maximum,false,"",CampaignArt.Current.caimanPortrait);
            if(aquatic){state=8;actor.Play("Idle");actor.sprite.color=new Color(.7f,.9f,.85f,.75f);GetComponent<Collider2D>().enabled=false;}
            else {state=7;started=Time.time;actor.Play("Escape",true);}
        }
    }
}
