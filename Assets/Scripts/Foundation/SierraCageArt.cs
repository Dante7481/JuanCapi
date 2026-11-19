using UnityEngine;
namespace JuanCapi
{
    public sealed class SierraCageArt:MonoBehaviour
    {
        private Material material;
        private void Start()
        {
            material=new Material(Shader.Find("Sprites/Default"));
            Stroke("Marco",new[]{new Vector3(-1,0),new Vector3(-1,2.25f),new Vector3(1,2.25f),new Vector3(1,0),new Vector3(-1,0)},.14f);
            for(int i=-1;i<=1;i++)Stroke("Barrote",new[]{new Vector3(i*.5f,.04f),new Vector3(i*.5f,2.24f)},.08f);
            Stroke("Cerrojo",new[]{new Vector3(-.15f,1),new Vector3(.18f,1)},.21f);
        }
        private void Stroke(string name,Vector3[] points,float width)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;
            line.useWorldSpace=false;line.positionCount=points.Length;line.SetPositions(points);line.startWidth=line.endWidth=width;
            line.startColor=line.endColor=new Color(.36f,.33f,.27f);line.numCornerVertices=3;line.numCapVertices=3;line.sortingOrder=22;
        }
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
