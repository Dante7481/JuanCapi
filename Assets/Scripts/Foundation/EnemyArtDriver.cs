using UnityEngine;

namespace JuanCapi
{
    public sealed class EnemyArtDriver : MonoBehaviour
    {
        public AnimatedActor actor;
        private EnemyController enemy;
        private void Awake(){enemy=GetComponent<EnemyController>();}
        public void Install(CampaignArt art)
        {
            if(actor!=null)return;
            foreach(var r in GetComponentsInChildren<SpriteRenderer>())r.enabled=false;
            var old=GetComponent<Animator>();if(old!=null)old.enabled=false;
            actor=AnimatedActor.Create("Arte del vigilante",transform,art.enemyControllers[0],art.enemies[0]);
            actor.sprite.sharedMaterial=art.enemyMaterial;
            float scale=1.7f/Mathf.Abs(transform.lossyScale.y)/actor.sprite.sprite.bounds.size.y;
            actor.transform.localScale=Vector3.one*scale;
            actor.transform.localPosition=new Vector3(0,.15f,0);
        }
        private void LateUpdate()
        {
            if(actor==null||enemy==null)return;
            actor.Play(enemy.VisualState);
            actor.sprite.color=enemy.VisualState=="Hurt"?new Color(1,.6f,.4f):Color.white;
        }
    }
}
