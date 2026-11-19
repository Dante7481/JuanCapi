using UnityEngine;

namespace JuanCapi
{
    public enum SierraPropKind{Drone,Barrier,Cage,Bench,Supplies,Exit}
    public sealed class SierraProp:SierraDamageTarget
    {
        public SierraPropKind kind;
        public string persistentId;
        public string Hint=>kind==SierraPropKind.Drone?"sword":kind==SierraPropKind.Cage?"cage":kind==SierraPropKind.Bench?"bench":kind==SierraPropKind.Exit?"dash":"crate";
        public bool Usable
        {
            get
            {
                var d=GameRuntime.Instance?.Data;if(d==null)return false;
                return kind==SierraPropKind.Drone?!d.sword:kind==SierraPropKind.Bench?d.rescues.Contains("MARA"):
                    kind==SierraPropKind.Supplies?!d.collected.Contains(persistentId):kind==SierraPropKind.Exit?d.dash:false;
            }
        }
        private void Start(){Restore();}
        public void Restore()
        {
            var d=GameRuntime.Instance?.Data;if(d==null)return;
            if(kind==SierraPropKind.Barrier&&d.collected.Contains(persistentId)||kind==SierraPropKind.Cage&&d.rescues.Contains("MARA")||kind==SierraPropKind.Supplies&&d.collected.Contains(persistentId))gameObject.SetActive(false);
        }
        public bool Use()
        {
            var game=GameRuntime.Instance;
            if(!Usable||game?.CanPlay!=true||Vector2.Distance(game.Player.transform.position,transform.position)>2.3f)return false;
            if(kind==SierraPropKind.Drone)
            {
                if(!game.Transaction(d=>{if(!d.Grant(CampaignReward.Sword))return false;d.collected.Add("CAPSULE");return true;},"Espada de plasma recuperada"))return false;
                SierraLevel.Current.ShowSwordReward();return true;
            }
            if(kind==SierraPropKind.Supplies)
            {
                if(!game.Transaction(d=>{if(!d.Collect(persistentId,"gold",40))return false;d.scrap+=10;return true;},"Provisiones recuperadas"))return false;
                gameObject.SetActive(false);return true;
            }
            if(kind==SierraPropKind.Bench){game.UI.Bank("MARA");return true;}
            if(kind==SierraPropKind.Exit)return SierraLevel.Current.FinishLevel();
            return false;
        }
        protected override void Hit(Vector2 origin)
        {
            var game=GameRuntime.Instance;if(game?.CanPlay!=true||!game.Data.sword)return;
            if(kind==SierraPropKind.Barrier)
            {
                if(!game.Transaction(d=>{if(d.collected.Contains(persistentId))return false;d.collected.Add(persistentId);return true;},"Camino abierto"))return;
            }
            else if(kind==SierraPropKind.Cage)
            {
                if(!SierraLevel.Current.GuardDefeated)return;
                if(!game.Transaction(d=>
                {
                    if(d.rescues.Contains("MARA"))return false;d.rescues.Add("MARA");d.checkpoint="CP_B03";
                    d.x=34;d.y=.08f;d.z=0;d.hasPosition=true;d.fruit=Mathf.Min(3,d.fruit+1);return true;
                },"Mara: Los cargamentos bajan al pantano. Sigue el río."))return;
                SierraLevel.Current.MaraRescued();
            }
            else return;
            PlasmaEffects.Burst(transform.position+Vector3.up,new Color(.4f,1,1),1.3f);gameObject.SetActive(false);
        }
    }
}
