using UnityEngine;

namespace JuanCapi
{
    // Presentation only; the combat controller owns energy, blocking and damage.
    public sealed class PlasmaShieldArtwork : MonoBehaviour
    {
        public LineRenderer rim;
        public SpriteRenderer emitter;
        private float hitUntil;
        private void OnDestroy(){if(rim!=null&&rim.sharedMaterial!=null)Destroy(rim.sharedMaterial);}
        public void Show(Vector2 direction,float energy,bool active)
        {
            gameObject.SetActive(active);if(!active)return;
            transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            if(rim==null)return;
            bool reduced=GameRuntime.Instance?.Settings.reducedFlashes==true;
            var tint=Time.time<hitUntil&&!reduced?Color.white:new Color(.25f,.9f,1,.35f+.5f*Mathf.Clamp01(energy));
            rim.startColor=rim.endColor=tint;rim.startWidth=rim.endWidth=Time.time<hitUntil?.09f:.045f;
        }
        public void Impact(){hitUntil=Time.time+.12f;PlasmaEffects.Burst(transform.position+transform.right*.8f,new Color(.3f,.95f,1),.5f);}
        public static PlasmaShieldArtwork Create(Transform parent,Sprite icon)
        {
            var go=new GameObject("Escudo · presentación");go.transform.SetParent(parent,false);
            var shield=go.AddComponent<PlasmaShieldArtwork>();shield.rim=go.AddComponent<LineRenderer>();
            shield.rim.sharedMaterial=new Material(Shader.Find("Sprites/Default"));shield.rim.useWorldSpace=false;shield.rim.positionCount=33;shield.rim.sortingOrder=28;
            for(int i=0;i<33;i++){float angle=Mathf.Lerp(-1.3f,1.3f,i/32f);shield.rim.SetPosition(i,new Vector3(Mathf.Cos(angle)*.85f,Mathf.Sin(angle)*1.05f,0));}
            var node=new GameObject("Emisor");node.transform.SetParent(go.transform,false);node.transform.localPosition=Vector3.right*.25f;
            shield.emitter=node.AddComponent<SpriteRenderer>();shield.emitter.sprite=icon;shield.emitter.sortingOrder=26;
            node.transform.localScale=Vector3.one*(.65f/icon.bounds.size.y);shield.Show(Vector2.right,1,true);return shield;
        }
    }
}
