using UnityEngine;
namespace JuanCapi
{
    public enum MinePropKind{Bench,Supply,Cable,Receiver,Target,Luma,FinalValve}
    public sealed class MineProp:SierraDamageTarget
    {
        public MinePropKind kind;public string persistentId,icon;public int scrap=7;
        private GameRuntime Game=>GameRuntime.Instance;
        public bool Usable
        {
            get
            {
                var d=Game?.Data;if(d==null||!gameObject.activeInHierarchy)return false;
                switch(kind)
                {
                    case MinePropKind.Bench:return d.rescues.Contains("MARA");
                    case MinePropKind.Supply:return !d.collected.Contains(persistentId);
                    case MinePropKind.Luma:return !d.rescues.Contains("LUMA")&&d.collected.Contains("M04_CABLE")&&d.collected.Contains("M04_REFLECT");
                    case MinePropKind.FinalValve:return d.defeated.Contains("DIRECTOR")&&!d.completed;
                    default:return false;
                }
            }
        }
        public void Refresh()
        {
            if(Game?.Data==null)return;var d=Game.Data;
            if(kind==MinePropKind.Cable||kind==MinePropKind.Supply||kind==MinePropKind.Target)gameObject.SetActive(!d.collected.Contains(persistentId));
        }
        public bool Use()
        {
            if(!Usable||!Game.CanPlay||Vector2.Distance(Game.Player.Center,transform.position)>2.4f)return false;
            if(kind==MinePropKind.Bench){Game.UI.Bank(persistentId);return true;}
            if(kind==MinePropKind.FinalValve)return MineLevel.Current.CloseDischarge();
            bool ok=Game.Transaction(d=>
            {
                if(kind==MinePropKind.Supply){if(!d.Collect(persistentId,"gold",30))return false;d.scrap+=scrap;}
                else if(kind==MinePropKind.Luma)
                {if(d.rescues.Contains("LUMA"))return false;d.rescues.Add("LUMA");d.maintenanceKey=true;d.checkpoint="CP_M04";d.x=90;d.y=.08f;d.z=0;d.hasPosition=true;}
                else return false;return true;
            },kind==MinePropKind.Luma?"Luma: Con esta llave podremos cerrar las descargas desde arriba.":"Suministros recuperados");
            if(ok){PlasmaEffects.Burst(transform.position,Color.cyan,.7f);Refresh();}return ok;
        }
        protected override void Hit(Vector2 origin){if(kind==MinePropKind.Cable)Complete("Cable de alimentación cortado");}
        public override void PlasmaHit(Vector2 origin,int damage)
        {
            if(kind==MinePropKind.Receiver&&damage>=3)Complete("Receptor liberado");
            if(kind==MinePropKind.Target)Complete("Cierre del conducto abierto");
        }
        private void Complete(string label)
        {
            if(Game?.CanPlay!=true||Game.Data.collected.Contains(persistentId))return;
            if(Game.Transaction(d=>{if(d.collected.Contains(persistentId))return false;d.collected.Add(persistentId);return true;},label))
            {PlasmaEffects.Burst(transform.position,Color.cyan,.6f);Refresh();}
        }
    }
}
