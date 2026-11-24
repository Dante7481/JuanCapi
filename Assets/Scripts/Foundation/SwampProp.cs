using UnityEngine;
namespace JuanCapi
{
    public enum SwampPropKind{Bench,Supply,Cable,Gun,Target,Latch,Tico,Dive,Shield,Receiver,Exit}
    public sealed class SwampProp:SierraDamageTarget
    {
        public SwampPropKind kind;
        public string persistentId;
        public string icon;
        private GameRuntime Game=>GameRuntime.Instance;
        public bool Usable
        {
            get
            {
                var d=Game?.Data;if(d==null||!gameObject.activeInHierarchy)return false;
                switch(kind)
                {
                    case SwampPropKind.Bench:return d.rescues.Contains("MARA");
                    case SwampPropKind.Supply:return !d.collected.Contains(persistentId);
                    case SwampPropKind.Gun:return !d.gun&&d.defeated.Contains("CAIMAN_LAND");
                    case SwampPropKind.Tico:return !d.rescues.Contains("TICO")&&d.collected.Contains("A03_LATCH_1")&&d.collected.Contains("A03_LATCH_2");
                    case SwampPropKind.Dive:return d.gun&&(SwampLevel.Current.boss.Resolved||SwampLevel.Current.boss.StateName=="Retreat");
                    case SwampPropKind.Shield:return !d.shield&&d.defeated.Contains("CAIMAN_WATER");
                    case SwampPropKind.Exit:return d.shield&&d.collected.Contains("A06_REFLECTED");
                    default:return false;
                }
            }
        }
        public void Refresh()
        {
            var d=Game.Data;bool visible=true;
            if(kind==SwampPropKind.Cable||kind==SwampPropKind.Supply||kind==SwampPropKind.Target||kind==SwampPropKind.Latch)visible=!d.collected.Contains(persistentId);
            if(kind==SwampPropKind.Gun)visible=d.defeated.Contains("CAIMAN_LAND")&&!d.gun;
            if(kind==SwampPropKind.Shield)visible=d.defeated.Contains("CAIMAN_WATER")&&!d.shield;
            gameObject.SetActive(visible);
        }
        public bool Use()
        {
            if(!Usable||Game.CanPlay!=true||Vector2.Distance(Game.Player.Center,transform.position)>2.4f)return false;
            if(kind==SwampPropKind.Bench){Game.UI.Bank(persistentId);return true;}
            if(kind==SwampPropKind.Dive)return SwampLevel.Current.Dive();
            if(kind==SwampPropKind.Exit)return SwampLevel.Current.FinishLevel();
            bool ok=Game.Transaction(d=>
            {
                if(kind==SwampPropKind.Gun){if(!d.Grant(CampaignReward.PlasmaGun))return false;d.reloadRemaining=0;d.equippedWeapon="gun";}
                else if(kind==SwampPropKind.Shield){if(!d.Grant(CampaignReward.PlasmaShield))return false;d.cells=Mathf.Min(3,d.cells+1);d.x=93;d.y=8;d.z=0;}
                else if(kind==SwampPropKind.Supply){if(!d.Collect(persistentId,"gold",25))return false;d.scrap+=5;}
                else if(kind==SwampPropKind.Tico)
                {if(d.rescues.Contains("TICO"))return false;d.rescues.Add("TICO");d.checkpoint="CP_A03";d.x=40;d.y=10;d.z=0;d.fruit=Mathf.Min(3,d.fruit+1);}
                else return false;return true;
            },kind==SwampPropKind.Gun?"Mara: El condensador estabiliza el disparador bajo el agua.":kind==SwampPropKind.Shield?"Emisor de escudo recuperado":kind==SwampPropKind.Tico?"Tico: La salida de mantenimiento lleva a MinerTown.":"Suministros recuperados");
            if(ok){PlasmaEffects.Burst(transform.position,Color.cyan,1);Refresh();}return ok;
        }
        protected override void Hit(Vector2 origin)
        {if(kind==SwampPropKind.Cable)Complete("Cable cortado");}
        public override void PlasmaHit(Vector2 origin,int damage)
        {
            if(kind==SwampPropKind.Target)Complete("Blanco de práctica alcanzado");
            if(kind==SwampPropKind.Latch)Complete("Cierre de la campana liberado");
            if(kind==SwampPropKind.Receiver&&damage>=3)Complete("Bloqueo preciso · acceso abierto");
        }
        private void Complete(string label)
        {
            if(Game?.CanPlay!=true||Game.Data.collected.Contains(persistentId))return;
            if(Game.Transaction(d=>{if(d.collected.Contains(persistentId))return false;d.collected.Add(persistentId);return true;},label))
            {PlasmaEffects.Burst(transform.position,Color.cyan,.7f);Refresh();}
        }
    }
}
