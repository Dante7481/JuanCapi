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
            game.NotifyChanged();game.UI.Toast(item=="piece"?$"Pieza de corazón · {game.Data.heartPieces}/4":item=="container"?"¡Un corazón más de capacidad!":"Objeto obtenido");
            gameObject.SetActive(false);
        }
    }
}
