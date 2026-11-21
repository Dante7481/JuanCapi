using UnityEngine;

namespace JuanCapi
{
    [DefaultExecutionOrder(50)]
    public sealed class PlayerArtDriver : MonoBehaviour
    {
        public AnimatedActor actor;
        private PlayerController player;
        private string previous;
        private SpriteRenderer gun;
        private Transform shoulder;
        private float nextGhost;
        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public Vector3 Muzzle => shoulder!=null?shoulder.TransformPoint(new Vector3(.72f,0,0)):transform.position;
        public Vector3 Shoulder => shoulder!=null?shoulder.position:transform.position+Vector3.up*.78f;
        private void Awake(){player=GetComponent<PlayerController>();}
        public void Install(CampaignArt art)
        {
            if(actor!=null)
            {
                shoulder=actor.transform.Find("Hombro · apuntado libre");
                gun=shoulder!=null?shoulder.GetComponentInChildren<SpriteRenderer>(true):null;
                if(player==null)player=GetComponent<PlayerController>();
                actor.animator.runtimeAnimatorController=player!=null&&player.aquatic?art.playerWater:art.playerLand;
                return;
            }
            foreach(var r in GetComponentsInChildren<SpriteRenderer>())r.enabled=false;
            var old=GetComponent<Animator>();if(old!=null)old.enabled=false;
            if(player==null)player=GetComponent<PlayerController>();
            actor=AnimatedActor.Create("Arte de Juan",transform,player!=null&&player.aquatic?art.playerWater:art.playerLand,art.playerPortrait,1.35f);
            actor.transform.localPosition=new Vector3(0,1.25f,0);
            shoulder=new GameObject("Hombro · apuntado libre").transform;shoulder.SetParent(actor.transform,false);
            shoulder.localPosition=new Vector3(.16f,.03f,0);
            gun=new GameObject("Disparador y brazos").AddComponent<SpriteRenderer>();gun.transform.SetParent(shoulder,false);
            gun.sprite=art.Icon("gun");gun.sortingOrder=23;
            if(gun.sprite!=null)
            {
                float scale=.95f/gun.sprite.bounds.size.x;gun.transform.localScale=Vector3.one*scale;
                gun.transform.localPosition=new Vector3(.36f,-.04f,0);
            }
            gun.gameObject.SetActive(false);
        }
        private void LateUpdate()
        {
            if(actor==null||player==null)return;
            var game=GameRuntime.Instance;var data=game?.Data;
            string state=player.VisualState;
            actor.Play(state);
            bool armed=data!=null&&data.gun&&data.equippedWeapon=="gun"&&!player.isDead;
            gun.gameObject.SetActive(armed);
            if(armed&&CampaignArt.Current.aimBodies!=null&&CampaignArt.Current.aimBodies.Length>=4)
            {
                int pose=player.aquatic?4+(state=="Idle"||state=="Reload"?0:1+Mathf.FloorToInt(Time.time*7)%3):!player.EnSuelo?3:state=="Run"?1+Mathf.FloorToInt(Time.time*8)%2:0;
                actor.sprite.sprite=CampaignArt.Current.aimBodies[pose];
            }
            if(armed&&Camera.main!=null)
            {
                AimAt(GameInput.PointerWorld(shoulder.position));
            }
            if(state!=previous)
            {
                if(state=="Hurt")PlasmaEffects.Burst(actor.sprite.bounds.center,new Color(1,.65f,.32f),.7f);
                if(state=="Land")PlasmaEffects.Burst(transform.position,new Color(.84f,.74f,.5f,.5f),.45f);
                previous=state;
            }
            if(state=="Dash"&&Time.time>=nextGhost)
            {nextGhost=Time.time+.045f;PlasmaEffects.Ghost(actor.sprite);}
            actor.sprite.color=player.IsInvulnerable?new Color(1,.8f,.65f,game!=null&&game.Settings.reducedFlashes?.85f:.75f+.25f*Mathf.Sin(Time.time*28)):Color.white;
        }
        public void AimAt(Vector3 worldPoint)
        {
            if(float.IsNaN(worldPoint.x)||float.IsNaN(worldPoint.y)||float.IsInfinity(worldPoint.x)||float.IsInfinity(worldPoint.y))return;
            if(shoulder==null)return;
            Vector3 world=worldPoint-shoulder.position;if(world.sqrMagnitude<.0001f)return;
            AimDirection=((Vector2)world).normalized;
            // Convert through mirrored parent, keeping muzzle direction correct on both sides.
            Vector3 local=shoulder.parent.InverseTransformVector(world);
            shoulder.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(local.y,local.x)*Mathf.Rad2Deg);
            gun.flipY=local.x<0;
        }
    }
}
