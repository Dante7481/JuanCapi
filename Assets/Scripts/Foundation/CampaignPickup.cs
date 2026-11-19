using UnityEngine;

namespace JuanCapi
{
    public sealed class CampaignPickup : MonoBehaviour
    {
        public string persistentId;
        public string item = "piece";
        public int amount = 1;
        private void Start()
        {
            if(string.IsNullOrWhiteSpace(persistentId))persistentId=WorldInteraction.StableId(gameObject);
            if(GameRuntime.Instance?.Data?.collected.Contains(persistentId)==true)gameObject.SetActive(false);
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            var game=GameRuntime.Instance;
            if(!other.CompareTag("Player")||game?.CanPlay!=true)return;
            if(!game.Data.Collect(persistentId,item,amount)){game.UI.Toast("No necesitas este objeto o ya tienes el máximo.");return;}
            game.NotifyChanged();PlasmaEffects.Burst(transform.position,item=="piece"?new Color(1,.3f,.5f):new Color(.3f,.95f,.85f),.6f);
            gameObject.SetActive(false);
        }
    }
}
