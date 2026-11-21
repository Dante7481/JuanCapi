using UnityEngine;
namespace JuanCapi
{
    public sealed class SwampEnemy:SierraDamageTarget
    {
        public bool aquatic,charger;
        public AnimatedActor actor;
        public int health=3;
        public bool Dead=>health<=0;
        private int state;
        private float until,stunned;
        private Vector2 direction,home;
        private bool hit;
        private AttackTelegraph warning;
        private void Start(){home=transform.position;warning=AttackTelegraph.Create(transform);}
        private void Update()
        {
            var game=GameRuntime.Instance;if(Dead||game?.CanPlay!=true)return;
            Vector2 delta=game.Player.Center-(Vector2)transform.position;
            if(Time.time<stunned){actor.Play("Hurt");return;}
            if(state==0)
            {
                actor.Play("Idle");actor.Face(delta.x);
                if(delta.magnitude<10&&!Physics2D.Linecast(transform.position+Vector3.up*(aquatic?0:.8f),game.Player.Center,1<<3))
                {state=1;until=Time.time+.8f;direction=delta.normalized;actor.Play("Telegraph",true);}
            }
            else if(state==1)
            {
                warning.Arrow((Vector2)transform.position+Vector2.up*(aquatic?0:1),direction,charger?4:3);
                if(Time.time>=until)
                {
                    state=2;until=Time.time+(charger?.65f:.2f);hit=false;actor.Play("Attack",true);warning.Hide();
                    if(!charger)CampaignProjectile.Launch((Vector2)transform.position+Vector2.up*(aquatic?0:1)+direction*.8f,direction*(aquatic?5:7),true,false,false,gameObject,2);
                }
            }
            else if(state==2)
            {
                if(charger)
                {
                    Vector2 movement=direction*6*Time.deltaTime;var wall=Physics2D.CircleCast(transform.position,.55f,direction,movement.magnitude,1<<3);
                    if(!wall)transform.position+=(Vector3)movement;
                    if(!hit&&delta.magnitude<1.25f){hit=true;game.Player.DamageQuarters(transform.position,2,true);}
                    if(wall)until=Time.time;
                }
                if(Time.time>=until){state=3;until=Time.time+1.5f;actor.Play("Idle");}
            }
            else if(Time.time>=until){state=0;if(charger)transform.position=Vector2.MoveTowards(transform.position,home,.15f);}
        }
        protected override void Hit(Vector2 origin){if(!aquatic)Damage(1);}
        public override void PlasmaHit(Vector2 origin,int amount){Damage(amount);}
        private void Damage(int amount)
        {
            if(Dead||GameRuntime.Instance?.CanPlay!=true)return;health-=amount;stunned=Time.time+.25f;state=0;warning.Hide();
            PlasmaEffects.Burst(transform.position+Vector3.up*(aquatic?0:.6f),Color.cyan,.6f);
            if(Dead){GetComponent<Collider2D>().enabled=false;actor.Play("Death",true);actor.sprite.color=new Color(.5f,.6f,.6f,.5f);actor.transform.localRotation=Quaternion.Euler(0,0,80);}
        }
    }
}
