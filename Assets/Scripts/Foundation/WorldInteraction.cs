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
        private static Sprite marker;
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
            if(marker==null){var texture=new Texture2D(1,1);texture.SetPixel(0,0,Color.white);texture.Apply();marker=Sprite.Create(texture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);}
            var sprite=go.AddComponent<SpriteRenderer>();sprite.sprite=marker;sprite.color=color;sprite.sortingOrder=8;
            go.transform.localScale=type=="sword"?new Vector3(.15f,.9f,1):new Vector3(.5f,.5f,1);
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
            game.UI.Prompt(nearest==null?"":$"[{GameInput.Label("Interactuar")}]  {nearest.title}");
            if(nearest!=null&&GameInput.Down("Interactuar"))nearest.Interact(game);
        }
        private void Interact(GameRuntime game)
        {
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
