using UnityEngine;
namespace JuanCapi
{
    public sealed class ShieldPracticeEmitter:MonoBehaviour
    {
        private float nextShot;
        private AttackTelegraph warning;
        private void Start(){warning=AttackTelegraph.Create(transform);nextShot=Time.time+2.8f;}
        private void Update()
        {
            var game=GameRuntime.Instance;if(game?.CanPlay!=true)return;
            if(!game.Data.shield||game.Data.collected.Contains("A06_REFLECTED")||Vector2.Distance(game.Player.Center,transform.position)>10){warning.Hide();nextShot=Time.time+2.8f;return;}
            Vector2 direction=(game.Player.Center-(Vector2)transform.position).normalized;
            if(nextShot-Time.time<.8f)warning.Arrow(transform.position,direction,2);
            if(Time.time>=nextShot)
            {warning.Hide();CampaignProjectile.Launch((Vector2)transform.position+direction*.8f,direction*4,true,true,false,gameObject,2);nextShot=Time.time+2.8f;}
        }
    }
}
