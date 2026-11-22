using UnityEngine;
namespace JuanCapi
{
    public sealed class MinePress:MonoBehaviour
    {
        public Transform head;public SpriteRenderer lamp;public Vector3 safePosition;
        public string StateName=>cycle<1.2f?"Warning":cycle<1.55f?"Strike":"Recovery";
        private float cycle;private bool hit;private AttackTelegraph warning;
        private void Start(){warning=AttackTelegraph.Create(transform);}
        private void Update()
        {
            var g=GameRuntime.Instance;if(g?.CanPlay!=true)return;
            if(g.Data.completed){warning.Hide();head.localPosition=Vector3.up*4;lamp.color=Color.gray;return;}
            cycle+=Time.deltaTime;if(cycle>=4.3f){cycle=0;hit=false;}
            lamp.color=cycle<1.2f?new Color(1,.65f,.12f):cycle<1.55f?new Color(1,.2f,.1f):new Color(.3f,.8f,.75f);
            if(cycle<1.2f){head.localPosition=Vector3.up*4;warning.Arrow(transform.position+Vector3.up*.12f,Vector2.right,2.4f);}
            else if(cycle<1.55f)
            {
                head.localPosition=Vector3.up*Mathf.Lerp(4,.45f,(cycle-1.2f)/.18f);warning.Hide();
                if(!hit&&cycle>1.35f&&Mathf.Abs(g.Player.transform.position.x-transform.position.x)<1.2f&&g.Player.transform.position.y<2)
                {hit=true;g.Player.DamageQuarters(transform.position,4);if(!g.Player.isDead){g.Player.transform.position=safePosition;g.Player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;}}
            }
            else{head.localPosition=Vector3.MoveTowards(head.localPosition,Vector3.up*4,4*Time.deltaTime);warning.Hide();}
        }
    }
}
