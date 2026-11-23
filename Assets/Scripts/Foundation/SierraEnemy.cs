using UnityEngine;

namespace JuanCapi
{
    public sealed class SierraEnemy:SierraDamageTarget
    {
        public string persistentId;
        public bool flying;
        public float left,right;
        public AnimatedActor actor;
        public int health=3;
        public bool Dead=>health<=0;
        private float homeY,phaseUntil,stunnedUntil;
        private int state;
        private float facing=1;
        private Vector2 attackOrigin;
        private SierraThreat warning;
        private void Start(){homeY=transform.position.y;warning=SierraThreat.Create(transform);}
        private void Update()
        {
            var game=GameRuntime.Instance;if(Dead||game?.CanPlay!=true)return;
            var p=game.Player.transform.position;float dx=p.x-transform.position.x;
            if(Time.time<stunnedUntil){actor.Play("Hurt");warning.Hide();return;}
            if(state==0)
            {
                if(Mathf.Abs(dx)<2.1f&&Mathf.Abs(p.y-transform.position.y)<2)
                {state=1;phaseUntil=Time.time+.65f;facing=Mathf.Sign(dx);attackOrigin=transform.position;actor.Play("Telegraph",true);}
                else
                {
                    if(Mathf.Abs(dx)<7)facing=Mathf.Sign(dx);
                    else if(transform.position.x<=left+.1f||transform.position.x>=right-.1f)facing*=-1;
                    var v=transform.position;v.x=Mathf.Clamp(v.x+facing*1.35f*Time.deltaTime,left,right);
                    v.y=homeY+(flying?.22f*Mathf.Sin(Time.time*3):0);transform.position=v;actor.Play("Run");
                }
            }
            else if(state==1)
            {
                warning.Show(attackOrigin+new Vector2(facing*1.1f,.08f),2.5f,.25f);
                if(Time.time>=phaseUntil)
                {
                    state=2;phaseUntil=Time.time+.22f;actor.Play("Attack",true);
                    if(Mathf.Abs(p.x-(attackOrigin.x+facing))<1.6f&&Mathf.Abs(p.y-attackOrigin.y)<1.2f)game.Player.DamageQuarters(attackOrigin,2);
                }
            }
            else if(Time.time>=phaseUntil){warning.Hide();state=0;stunnedUntil=Time.time+.7f;}
            actor.Face(facing);
        }
        protected override void Hit(Vector2 origin)
        {
            if(Dead||GameRuntime.Instance?.CanPlay!=true)return;
            health--;stunnedUntil=Time.time+.3f;state=0;warning?.Hide();
            PlasmaEffects.Burst(transform.position+Vector3.up*.7f,new Color(1,.68f,.3f),.7f);
            if(Dead)
            {
                actor.Defeat();GetComponent<Collider2D>().enabled=false;
                if(persistentId=="MARA_GUARD")SierraLevel.Current.GuardDefeated=true;
            }
        }
    }
}
