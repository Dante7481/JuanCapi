using UnityEngine;
namespace JuanCapi
{
    public sealed class AttackTelegraph:MonoBehaviour
    {
        private LineRenderer line;
        private static Material material;
        public static AttackTelegraph Create(Transform parent)
        {
            var go=new GameObject("Trayectoria anunciada");go.transform.SetParent(parent,false);var warning=go.AddComponent<AttackTelegraph>();
            warning.line=go.AddComponent<LineRenderer>();if(material==null)material=new Material(Shader.Find("Sprites/Default"));
            warning.line.sharedMaterial=material;warning.line.useWorldSpace=true;warning.line.positionCount=5;warning.line.sortingOrder=34;
            warning.line.startWidth=warning.line.endWidth=.07f;warning.line.startColor=warning.line.endColor=new Color(1,.7f,.25f,.85f);warning.Hide();return warning;
        }
        public void Arrow(Vector2 origin,Vector2 direction,float length)
        {
            direction.Normalize();Vector2 end=origin+direction*length,side=new Vector2(-direction.y,direction.x)*.4f;
            if(!line.enabled)CampaignAudio.Play("warning",false,true);line.enabled=true;line.SetPositions(new[]{(Vector3)origin,(Vector3)end,(Vector3)(end-direction*.65f+side),(Vector3)end,(Vector3)(end-direction*.65f-side)});
        }
        public void Hide(){if(line!=null)line.enabled=false;}
    }
}
