using UnityEngine;

namespace JuanCapi
{
    public sealed class CampaignCheckpoint : MonoBehaviour
    {
        public string checkpointId="CP_B01";
        public string region="Bosque herido";
        public Transform safeSpawn;
        private bool activated;
        private void OnTriggerEnter2D(Collider2D other)
        {
            var game=GameRuntime.Instance;
            if(activated||!other.CompareTag("Player")||game?.CanPlay!=true||safeSpawn==null)return;
            var position=safeSpawn.position;
            activated=game.Transaction(d=>{d.checkpoint=checkpointId;d.region=region;d.x=position.x;d.y=position.y;d.z=position.z;d.hasPosition=true;return true;},"Llegada a "+region);
        }
    }
}
