using UnityEngine;

namespace JuanCapi
{
    public sealed class CampaignProjectile:MonoBehaviour
    {
        public bool hostile,reflectable,gravity;
        public Vector2 Velocity { get; private set; }
        public bool Reflected { get; private set; }
        public GameObject owner;
        private int damage;
        private float age;
        private LineRenderer streak,ring;
        private static Material material;
        public static CampaignProjectile Launch(Vector2 origin,Vector2 velocity,bool hostile,bool reflectable,bool gravity,GameObject owner,int damage=1)
        {
            var go=new GameObject(hostile?"Proyectil hostil":"Plasma de Juan");go.transform.position=origin;
            var shot=go.AddComponent<CampaignProjectile>();shot.Velocity=velocity;shot.hostile=hostile;shot.reflectable=reflectable;shot.gravity=gravity;shot.owner=owner;shot.damage=damage;
            if(material==null)material=new Material(Shader.Find("Sprites/Default"));
            shot.streak=go.AddComponent<LineRenderer>();shot.streak.sharedMaterial=material;shot.streak.useWorldSpace=false;
            shot.streak.positionCount=2;shot.streak.SetPositions(new[]{Vector3.left*.34f,Vector3.right*.12f});shot.streak.startWidth=.12f;shot.streak.endWidth=.18f;shot.streak.numCapVertices=4;shot.streak.sortingOrder=35;
            shot.Tint();
            if(gravity)
            {
                shot.streak.enabled=false;var stone=go.AddComponent<SpriteRenderer>();stone.sprite=CampaignArt.Current.terrain[5];stone.sortingOrder=35;
                var b=stone.sprite.bounds.size;stone.transform.localScale=new Vector3(.45f/b.x,.4f/b.y,1);
            }
            if(reflectable)
            {
                var halo=new GameObject("Anillo reflectable");halo.transform.SetParent(go.transform,false);shot.ring=halo.AddComponent<LineRenderer>();shot.ring.sharedMaterial=material;shot.ring.useWorldSpace=false;
                shot.ring.loop=true;shot.ring.positionCount=20;shot.ring.startWidth=shot.ring.endWidth=.04f;shot.ring.startColor=shot.ring.endColor=new Color(1,1,.8f);shot.ring.sortingOrder=36;
                for(int i=0;i<20;i++){float a=i*Mathf.PI*2/20;shot.ring.SetPosition(i,new Vector3(Mathf.Cos(a),Mathf.Sin(a))*.28f);}
            }
            return shot;
        }
        private void FixedUpdate()
        {
            if(GameRuntime.Instance?.CanPlay!=true)return;
            age+=Time.fixedDeltaTime;if(age>3){Expire();return;}
            if(gravity)Velocity+=Vector2.down*(9.81f*1.3f*Time.fixedDeltaTime);
            Vector2 start=transform.position,delta=Velocity*Time.fixedDeltaTime;
            foreach(var hit in Physics2D.CircleCastAll(start,.11f,delta.normalized,delta.magnitude))
            {
                var c=hit.collider;if(c==null||owner!=null&&(c.gameObject==owner||c.transform.IsChildOf(owner.transform)))continue;
                var player=c.GetComponentInParent<PlayerController>();
                if(hostile&&player!=null)
                {
                    if(player.Combat!=null&&player.Combat.BlockProjectile(this))return;
                    player.DamageQuarters(start,damage);Impact(hit.point);return;
                }
                if(!hostile&&player!=null)continue;
                var target=c.GetComponentInParent<SierraDamageTarget>();
                if(!hostile&&target!=null){target.PlasmaHit(start,damage);Impact(hit.point);return;}
                if(c.gameObject.layer==3&&!c.isTrigger){Impact(hit.point);return;}
            }
            transform.position=start+delta;transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(Velocity.y,Velocity.x)*Mathf.Rad2Deg);
        }
        private void Tint(){var color=hostile?new Color(1,.57f,.2f):new Color(.25f,1,1);streak.startColor=streak.endColor=color;}
        private void Impact(Vector2 point){PlasmaEffects.Burst(point,hostile?new Color(1,.6f,.2f):Color.cyan,.35f);Expire();}
        public void Reflect(Vector2 direction,GameObject newOwner)
        {
            hostile=false;Reflected=true;owner=newOwner;damage=3;gravity=false;Velocity=direction.normalized*16;age=0;
            transform.position+=(Vector3)(direction.normalized*.45f);Tint();
        }
        public void Expire(){gameObject.SetActive(false);Destroy(gameObject);}
        public static void ClearHostile()
        {foreach(var shot in FindObjectsByType<CampaignProjectile>(FindObjectsSortMode.None))if(shot.hostile)shot.Expire();}
    }
}
