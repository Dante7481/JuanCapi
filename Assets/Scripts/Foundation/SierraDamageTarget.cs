using UnityEngine;

namespace JuanCapi
{
    public abstract class SierraDamageTarget:MonoBehaviour
    {
        private int lastSwing=-1;
        public void SwordHit(int sequence,Vector2 origin)
        {if(lastSwing==sequence)return;lastSwing=sequence;Hit(origin);}
        protected abstract void Hit(Vector2 origin);
    }
}
