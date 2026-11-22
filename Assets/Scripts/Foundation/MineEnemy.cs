using UnityEngine;
namespace JuanCapi
{
    public sealed class MineEnemy:SierraDamageTarget
    {
        public bool drone,summoned;public AnimatedActor actor;public int health=9;
        public bool Dead=>health<=0;public string StateName=>new[]{"Patrol","Telegraph","Attack","Recovery"}[state];
        public float TimeToStrike=>state==1?Mathf.Max(0,until-Time.time):-1;
        private int state;private float until,started,facing=1;private bool hit;private Vector3 home,origin,target;private AttackTelegraph warning;
        private GameRuntime Game=>GameRuntime.Instance;
        private void Start(){home=transform.position;warning=AttackTelegraph.Create(transform);}
        private void Update()
        {
            if(Dead||Game?.CanPlay!=true)return;
            if(Game.Data.completed){Retire();return;}
            var p=Game.Player.Center;var delta=p-(Vector2)transform.position;
            if(state==0)
            {
                actor.Play("Idle");facing=delta.x<0?-1:1;actor.Face(facing);
                if(delta.magnitude>10||Camera.main==null||Mathf.Abs(transform.position.x-Camera.main.transform.position.x)>Camera.main.orthographicSize*Camera.main.aspect)return;
                if(!drone&&Mathf.Abs(delta.x)>3)
                {var pos=transform.position;pos.x=Mathf.Clamp(pos.x+facing*2*Time.deltaTime,home.x-3,home.x+3);transform.position=pos;actor.Play("Run");return;}
                state=1;until=Time.time+(drone?.8f:1);actor.Play("Telegraph",true);origin=transform.position;
                target=new Vector3(Mathf.Clamp(p.x,home.x-3,home.x+3),home.y-(drone?2:0));
            }
            else if(state==1)
            {
                warning.Arrow(transform.position+Vector3.up*(drone?0:.3f),drone?(Vector2)(target-origin):Vector2.right*facing,drone?2:3.5f);
                if(Time.time>=until){state=2;started=Time.time;until=Time.time+(drone?.65f:.3f);hit=false;warning.Hide();actor.Play("Attack",true);}
            }
            else if(state==2)
            {
                if(drone)transform.position=Vector3.Lerp(origin,target,Mathf.Clamp01((Time.time-started)/.55f));
                bool contact=drone?Vector2.Distance(p,transform.position)<1.2f:Mathf.Abs(p.x-(transform.position.x+facing*1.3f))<1.9f&&Mathf.Abs(Game.Player.transform.position.y-home.y)<1;
                if(!hit&&contact){hit=true;Game.Player.DamageQuarters(transform.position,drone?2:4,true);}
                if(Time.time>=until){state=3;until=Time.time+1.4f;actor.Play("Idle");}
            }
            else
            {if(drone)transform.position=Vector3.MoveTowards(transform.position,home,2*Time.deltaTime);if(Time.time>=until)state=0;}
        }
        protected override void Hit(Vector2 origin){Damage(origin,1);}
        public override void PlasmaHit(Vector2 origin,int amount){Damage(origin,amount);}
        private void Damage(Vector2 source,int amount)
        {
            if(Dead||Game?.CanPlay!=true)return;
            if(!drone&&state!=3&&Mathf.Sign(source.x-transform.position.x)==facing){PlasmaEffects.Burst(transform.position+Vector3.up,Color.yellow,.3f);return;}
            health=Mathf.Max(0,health-amount);PlasmaEffects.Burst(transform.position+Vector3.up*(drone?0:1),Color.cyan,.5f);
            if(Dead)Retire();
        }
        public void Retire(){health=0;if(warning!=null)warning.Hide();GetComponent<Collider2D>().enabled=false;actor.Play("Death",true);actor.sprite.color=new Color(.45f,.52f,.56f,.5f);enabled=false;}
    }
}
