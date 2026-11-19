using UnityEngine;

namespace JuanCapi
{
    // All imported clips animate the Visual child, preserving their source bindings.
    public sealed class AnimatedActor : MonoBehaviour
    {
        public Animator animator;
        public SpriteRenderer sprite;
        private string current;
        public string CurrentState => current;
        public void Play(string state, bool restart=false)
        {
            if(animator==null||(!restart&&current==state))return;
            int hash=Animator.StringToHash(state);
            if(!animator.HasState(0,hash))return;
            current=state;animator.Play(hash,0,0);
        }
        public void Face(float direction) { if(sprite!=null&&Mathf.Abs(direction)>.01f)sprite.flipX=direction<0; }
        public static AnimatedActor Create(string name,Transform parent,RuntimeAnimatorController controller,Sprite first,float scale=1)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localScale=Vector3.one*scale;
            var actor=go.AddComponent<AnimatedActor>();actor.animator=go.AddComponent<Animator>();actor.animator.runtimeAnimatorController=controller;
            var visual=new GameObject("Visual");visual.transform.SetParent(go.transform,false);
            actor.sprite=visual.AddComponent<SpriteRenderer>();actor.sprite.sprite=first;actor.sprite.sortingOrder=20;
            if(Application.isPlaying)actor.Play("Idle");return actor;
        }
    }
}
