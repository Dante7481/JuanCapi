using UnityEngine;

namespace JuanCapi
{
    // All imported clips animate the Visual child, preserving their source bindings.
    [DefaultExecutionOrder(100)]
    public sealed class AnimatedActor : MonoBehaviour
    {
        public Animator animator;
        public SpriteRenderer sprite;
        private string current;
        public bool groundFeet;
        public float groundOffset;
        private static SpriteFootprints footprints;
        private bool retiring;
        private float deathStarted;
        private Vector3 deathScale;
        public void Defeat()
        {
            if(retiring)return;retiring=true;deathStarted=Time.time;deathScale=transform.localScale;
            Play("Death",true);CampaignAudio.Play("death",false,true);
        }
        private void LateUpdate()
        {
            if(sprite==null)return;
            if(groundFeet&&sprite.sprite!=null)
            {
                if(footprints==null)footprints=Resources.Load<SpriteFootprints>("Campaign/Footprints");
                float bottom=footprints!=null?footprints.Bottom(sprite.sprite):sprite.sprite.bounds.min.y;
                sprite.transform.localPosition=new Vector3(0,groundOffset-bottom,0);
            }
            if(retiring)
            {
                float t=Time.time-deathStarted;
                if(t<.35f)transform.localScale=Vector3.Scale(deathScale,new Vector3(1+.12f*Mathf.Sin(t/.35f*Mathf.PI),1-.1f*Mathf.Sin(t/.35f*Mathf.PI),1));
                var c=sprite.color;c.a=1-Mathf.Clamp01((t-.65f)/.55f);sprite.color=c;
                if(t>=1.2f)gameObject.SetActive(false);
            }
        }
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
            actor.groundFeet=controller!=null&&!controller.name.Contains("Acuatico")&&!controller.name.Contains("Dron")&&!controller.name.Contains("Pez");
            var visual=new GameObject("Visual");visual.transform.SetParent(go.transform,false);
            actor.sprite=visual.AddComponent<SpriteRenderer>();actor.sprite.sprite=first;actor.sprite.sortingOrder=20;
            if(Application.isPlaying)actor.Play("Idle");return actor;
        }
    }
}
