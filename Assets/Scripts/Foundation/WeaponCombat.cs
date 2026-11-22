using UnityEngine;

namespace JuanCapi
{
    [DefaultExecutionOrder(60)]
    public sealed class WeaponCombat:MonoBehaviour
    {
        public bool Blocking { get; private set; }
        public bool Firing=>Time.time<fireUntil;
        public Vector2 Aim { get; private set; }=Vector2.right;
        public float ReloadDuration=>Game.Data.fastReload?4.5f:6;
        public float ShieldRecovery=>Mathf.Max(0,brokenUntil-Time.time);
        private PlayerController player;
        private PlayerArtDriver art;
        private PlasmaShieldArtwork shield;
        private float nextFire,fireUntil,raisedAt,lastShieldUse,brokenUntil;
        private GameRuntime Game=>GameRuntime.Instance;
        private void Start()
        {
            player=GetComponent<PlayerController>();art=GetComponent<PlayerArtDriver>();
            shield=PlasmaShieldArtwork.Create(transform,CampaignArt.Current.Icon("shield"));shield.Show(Vector2.right,1,false);
        }
        private void Update()
        {
            if(Game?.Data==null||player==null||art==null)return;
            var d=Game.Data;
            if(!Game.CanPlay){shield.Show(Aim,d.shieldEnergy/d.MaximumShield,false);return;}
            Vector3 pointer=GameInput.PointerWorld(art.Shoulder);art.AimAt(pointer);
            Vector2 direction=(Vector2)(pointer-art.Muzzle);if(direction.sqrMagnitude>.025f)Aim=direction.normalized;
            if(d.gun)
            {
                if(d.ammunition==0&&d.equippedWeapon=="gun"&&Game.AttackInputAllowed&&GameInput.Down("Atacar"))CampaignAudio.Play("empty");
                if(d.ammunition==0&&d.reloadRemaining<=0)d.reloadRemaining=ReloadDuration;
                if(d.reloadRemaining>0)
                {
                    d.reloadRemaining=Mathf.Max(0,d.reloadRemaining-Time.deltaTime);
                    if(d.reloadRemaining==0){d.ammunition=3;CampaignAudio.Play("reload");PlasmaEffects.Burst(art.Muzzle,new Color(.3f,.9f,1),.3f);}
                }
            }
            bool previous=Blocking;
            bool wants=Game.InputAllowed&&d.shield&&GameInput.Held("Escudo")&&!player.IsDashing&&!player.IsInvulnerable&&!player.SwingActive;
            Blocking=wants&&Time.time>=brokenUntil&&(previous?d.shieldEnergy>0:d.shieldEnergy>=25);
            if(Blocking)
            {
                if(!previous){raisedAt=Time.time;CampaignAudio.Play("shield");player.StopAttack();}
                d.shieldEnergy=Mathf.Max(0,d.shieldEnergy-20*Time.deltaTime);lastShieldUse=Time.time;
                if(d.shieldEnergy==0)BreakShield();
            }
            else if(Time.time-lastShieldUse>1)d.shieldEnergy=Mathf.Min(d.MaximumShield,d.shieldEnergy+25*Time.deltaTime);
            shield.transform.position=player.Center;
            var scale=transform.lossyScale;shield.transform.localScale=new Vector3(1/scale.x,1/scale.y,1);
            shield.Show(Aim,d.shieldEnergy/d.MaximumShield,Blocking);
            if(Game.AttackInputAllowed&&!Blocking&&!player.IsInvulnerable&&!player.IsDashing&&d.gun&&d.equippedWeapon=="gun"&&(GameInput.Down("Atacar")||GameInput.Held("Atacar")))TryFire(pointer);
        }
        public bool TryFire(Vector3 pointer)
        {
            if(float.IsNaN(pointer.x)||float.IsNaN(pointer.y)||float.IsInfinity(pointer.x)||float.IsInfinity(pointer.y))return false;
            if(Game?.AttackInputAllowed!=true||player==null||Blocking||player.IsDashing||player.IsInvulnerable)return false;
            var d=Game.Data;if(!d.gun||d.equippedWeapon!="gun"||d.ammunition<=0||d.reloadRemaining>0||Time.time<nextFire)return false;
            art.AimAt(pointer);Vector2 direction=(Vector2)(pointer-art.Muzzle);if(direction.sqrMagnitude>.025f)Aim=direction.normalized;
            Vector2 origin=art.Muzzle;var wall=Physics2D.Linecast(player.Center,origin,1<<3);
            CampaignAudio.Play("shot");d.ammunition--;if(d.ammunition==0)d.reloadRemaining=ReloadDuration;
            nextFire=Time.time+.42f;fireUntil=Time.time+.24f;
            if(wall.collider!=null){PlasmaEffects.Burst(wall.point,Color.cyan,.25f);return true;}
            CampaignProjectile.Launch(origin,Aim*16,false,false,false,gameObject,1);
            PlasmaEffects.Burst(origin,new Color(.35f,1,1),.22f);return true;
        }
        public bool BlockMelee(Vector2 source)=>ConsumeBlock(source,false,out _);
        public bool BlockProjectile(CampaignProjectile shot)
        {
            if(!ConsumeBlock(shot.transform.position,shot.reflectable,out bool precise))return false;
            if(precise){shot.Reflect(Aim,gameObject);SwampLevel.Current?.ReflectedShot();MineLevel.Current?.ReflectedShot();}
            else shot.Expire();return true;
        }
        private bool ConsumeBlock(Vector2 source,bool reflectable,out bool precise)
        {
            precise=false;if(!Blocking||Game?.CanPlay!=true)return false;
            Vector2 toward=source-player.Center;if(toward.sqrMagnitude>.001f&&Vector2.Dot(Aim,toward.normalized)<.5f)return false;
            var d=Game.Data;if(d.shieldEnergy<25){BreakShield();return false;}
            d.shieldEnergy-=25;lastShieldUse=Time.time;precise=reflectable&&Time.time-raisedAt<=.15f;shield.Impact();
            CampaignAudio.Play(precise?"parry":"block",false,true);
            if(d.shieldEnergy<=0)BreakShield();return true;
        }
        private void BreakShield()
        {CampaignAudio.Play("break",false,true);Blocking=false;Game.Data.shieldEnergy=0;brokenUntil=Time.time+1.5f;lastShieldUse=Time.time;PlasmaEffects.Burst(player.Center,new Color(.25f,.7f,1),.9f);}
    }
}
