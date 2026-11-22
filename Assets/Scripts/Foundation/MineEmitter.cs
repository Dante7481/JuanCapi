using UnityEngine;
namespace JuanCapi
{
    public sealed class MineEmitter:MonoBehaviour
    {
        public string stopFlag;public bool reflectable=true;public float period=3;private float nextShot;private AttackTelegraph warning;
        private void Start(){warning=AttackTelegraph.Create(transform);nextShot=Time.time+period;}
        private void Update()
        {
            var game=GameRuntime.Instance;if(game?.CanPlay!=true)return;
            if(game.Data.completed||game.Data.collected.Contains(stopFlag)||Vector2.Distance(game.Player.Center,transform.position)>9)
            {warning.Hide();nextShot=Time.time+period;return;}
            Vector2 direction=(game.Player.Center-(Vector2)transform.position).normalized;
            if(nextShot-Time.time<.9f)warning.Arrow(transform.position,direction,2);
            if(Time.time<nextShot)return;
            warning.Hide();CampaignProjectile.Launch((Vector2)transform.position+direction*.8f,direction*4,true,reflectable,false,gameObject,2);nextShot=Time.time+period;
        }
    }
}
