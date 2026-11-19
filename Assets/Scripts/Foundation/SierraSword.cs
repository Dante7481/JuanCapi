using UnityEngine;

namespace JuanCapi
{
    public sealed class SierraSword:MonoBehaviour
    {
        private PlayerController player;
        private readonly Collider2D[] hits=new Collider2D[20];
        private void Awake(){player=GetComponent<PlayerController>();}
        private void FixedUpdate()
        {
            if(!player.SwingActive)return;
            Vector2 center=(Vector2)transform.position+new Vector2(player.Facing*1.1f,.8f);
            var filter=new ContactFilter2D{useTriggers=true};
            int count=Physics2D.OverlapBox(center,new Vector2(2.15f,1.8f),0,filter,hits);
            for(int i=0;i<count;i++)hits[i].GetComponentInParent<SierraDamageTarget>()?.SwordHit(player.AttackSequence,transform.position);
        }
    }
}
