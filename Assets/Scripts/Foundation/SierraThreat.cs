using UnityEngine;

namespace JuanCapi
{
    public sealed class SierraThreat:MonoBehaviour
    {
        private LineRenderer line;
        private static Material material;
        public static SierraThreat Create(Transform owner)
        {
            var go=new GameObject("Aviso de ataque");go.transform.SetParent(owner,false);
            var result=go.AddComponent<SierraThreat>();result.line=go.AddComponent<LineRenderer>();
            if(material==null)material=new Material(Shader.Find("Sprites/Default"));
            result.line.sharedMaterial=material;result.line.useWorldSpace=true;result.line.loop=true;result.line.positionCount=4;
            result.line.startWidth=result.line.endWidth=.065f;result.line.sortingOrder=30;
            result.line.startColor=result.line.endColor=new Color(1,.63f,.18f);result.Hide();return result;
        }
        public void Show(Vector2 center,float width,float height)
        {
            line.enabled=true;float x=width*.5f,y=height*.5f;
            line.SetPositions(new[]{new Vector3(center.x-x,center.y-y),new Vector3(center.x+x,center.y-y),new Vector3(center.x+x,center.y+y),new Vector3(center.x-x,center.y+y)});
        }
        public void Hide(){if(line!=null)line.enabled=false;}
    }
}
