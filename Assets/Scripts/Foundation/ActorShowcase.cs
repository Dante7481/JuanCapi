using UnityEngine;

namespace JuanCapi
{
    public sealed class ActorShowcase : MonoBehaviour
    {
        public AnimatedActor actor;
        public string[] states={"Idle","Run","Attack","Hurt"};
        private int previous=-1;
        private void Update()
        {
            if(actor==null||states==null||states.Length==0)return;
            int index=Mathf.FloorToInt(Time.time/2.5f)%states.Length;
            if(index!=previous){previous=index;actor.Play(states[index],true);}
        }
    }
}
