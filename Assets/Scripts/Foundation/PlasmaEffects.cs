using UnityEngine;

namespace JuanCapi
{
    public sealed class PlasmaEffects : MonoBehaviour
    {
        private LineRenderer ring;
        private SpriteRenderer ghost;
        private float age,duration=.35f,size=1;
        private Color tint;
        private static Material lineMaterial;
        public static void Burst(Vector3 position,Color color,float radius=1)
        {
            var go=new GameObject("Pulso de plasma");go.transform.position=position;
            var effect=go.AddComponent<PlasmaEffects>();effect.tint=color;effect.size=radius;
            effect.ring=go.AddComponent<LineRenderer>();effect.ring.useWorldSpace=false;effect.ring.loop=true;effect.ring.positionCount=24;
            if(lineMaterial==null)lineMaterial=new Material(Shader.Find("Sprites/Default")){name="Plasma · líneas"};
            effect.ring.sharedMaterial=lineMaterial;effect.ring.sortingOrder=32;
            effect.ring.startWidth=effect.ring.endWidth=.045f;
        }
        public static void Ghost(SpriteRenderer source)
        {
            var go=new GameObject("Estela de impulso");go.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);go.transform.localScale=source.transform.lossyScale;
            var effect=go.AddComponent<PlasmaEffects>();effect.duration=.22f;effect.tint=new Color(.16f,.95f,1,.42f);
            effect.ghost=go.AddComponent<SpriteRenderer>();effect.ghost.sprite=source.sprite;effect.ghost.flipX=source.flipX;effect.ghost.sortingOrder=source.sortingOrder-1;
        }
        private void Update()
        {
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/duration);var color=tint;color.a*=1-t;
            if(GameRuntime.Instance?.Settings.reducedFlashes==true)color.a*=.45f;
            if(ghost!=null)ghost.color=color;
            if(ring!=null)
            {
                ring.startColor=ring.endColor=color;
                for(int i=0;i<24;i++){float a=i*Mathf.PI*2/24;ring.SetPosition(i,new Vector3(Mathf.Cos(a),Mathf.Sin(a))*(.08f+t*size));}
            }
            if(t>=1)Destroy(gameObject);
        }
    }
}
