using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace JuanCapi
{
    public sealed class WorldInteraction : MonoBehaviour
    {
        private static readonly List<WorldInteraction> Active = new List<WorldInteraction>();
        public string persistentId, title, kind;
        public float radius=1.8f;
        private void OnEnable(){if(!Active.Contains(this))Active.Add(this);}
        private void OnDisable(){Active.Remove(this);}
        public static string StableId(GameObject target)
        {
            string hierarchy=target.name;var parent=target.transform.parent;
            while(parent!=null){hierarchy=parent.name+"/"+hierarchy;parent=parent.parent;}
            return target.scene.name+":"+hierarchy+":"+target.transform.position.x.ToString("F3",CultureInfo.InvariantCulture)+":"+target.transform.position.y.ToString("F3",CultureInfo.InvariantCulture);
        }
        public static void Create(string id,string label,string type,Vector3 position,Color color)
        {
            var go=new GameObject(label);go.transform.position=position;
            var interaction=go.AddComponent<WorldInteraction>();interaction.persistentId=id;interaction.title=label;interaction.kind=type;
            var art=CampaignArt.Current;if(art==null)return;
            string key=type=="sword"?"drone":type=="shelter"?"bench":"crate";
            var visual=new GameObject("Objeto");visual.transform.SetParent(go.transform,false);
            var sprite=visual.AddComponent<SpriteRenderer>();sprite.sprite=art.Icon(key);sprite.sortingOrder=12;
            float height=type=="shelter"?1.1f:.75f;
            visual.transform.localScale=Vector3.one*(height/sprite.sprite.bounds.size.y);visual.transform.localPosition=Vector3.up*(height*.5f-.12f);
            if(type=="shelter")
            {
                var mara=AnimatedActor.Create("Mara",go.transform,art.allyControllers[0],art.allies[0],.85f);
                mara.transform.localPosition=new Vector3(1.1f,-.12f,0);
            }
            var beacon=new GameObject("Señal de interacción");beacon.transform.SetParent(go.transform,false);beacon.transform.localPosition=Vector3.up*(height+.4f);
            var sign=beacon.AddComponent<SpriteRenderer>();sign.sprite=art.Icon(type=="sword"?"sword":type=="shelter"?"scrap":"fruit");sign.sortingOrder=18;
            beacon.transform.localScale=Vector3.one*(.42f/sign.sprite.bounds.size.y);beacon.AddComponent<FloatingArtwork>();
        }
        public static void Tick(GameRuntime game)
        {
            WorldInteraction nearest=null;float best=float.MaxValue;
            foreach(var target in Active)
            {
                if(target==null||!target.gameObject.activeInHierarchy)continue;
                if(target.kind=="sword"&&game.Data.sword||target.kind=="supplies"&&game.Data.collected.Contains(target.persistentId)) {target.gameObject.SetActive(false);break;}
                float distance=Vector2.Distance(target.transform.position,game.Player.transform.position);
                if(distance<target.radius&&distance<best){best=distance;nearest=target;}
            }
            game.UI.PromptIcon(nearest==null?null:nearest.kind=="sword"?"sword":nearest.kind=="shelter"?"bench":"crate");
            if(nearest!=null&&GameInput.Down("Interactuar"))nearest.Interact(game);
        }
        private void Interact(GameRuntime game)
        {
            PlasmaEffects.Burst(transform.position+Vector3.up*.5f,new Color(.25f,.95f,1),.8f);
            if(kind=="sword")game.Transaction(d=>{if(!d.Grant(CampaignReward.Sword))return false;d.collected.Add(persistentId);return true;},"Espada de plasma obtenida");
            if(kind=="supplies")game.Transaction(d=>
            {if(d.collected.Contains(persistentId))return false;d.Collect(persistentId,"gold",40);d.scrap+=10;d.fruit=Mathf.Min(3,d.fruit+1);return true;},"Provisiones recuperadas");
            if(kind=="shelter")
            {
                if(!game.Data.sword){game.UI.Toast("Recupera primero la espada del dron.");return;}
                if(!game.Data.rescues.Contains("MARA")&&!game.Transaction(d=>{d.rescues.Add("MARA");d.checkpoint="CP_B03";return true;},"Refugio de Mara abierto"))return;
                game.UI.Bank(persistentId);
            }
        }
    }
}
