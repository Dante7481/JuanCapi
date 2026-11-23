using UnityEngine;
using UnityEngine.UI;
namespace JuanCapi
{
    // Resolution-independent brass inlay and cut corners, shared by cards and buttons.
    public sealed class AdventureFrame:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float x=r.xMin,y=r.yMin,w=r.width,h=r.height;
            void Quad(float a,float b,float c,float d,Color tint){int n=vh.currentVertCount;vh.AddVert(new Vector3(a,b),tint,Vector2.zero);vh.AddVert(new Vector3(a+c,b),tint,Vector2.zero);vh.AddVert(new Vector3(a+c,b+d),tint,Vector2.zero);vh.AddVert(new Vector3(a,b+d),tint,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
            var line=new Color(.74f,.57f,.3f,.55f);var shine=new Color(.95f,.8f,.47f,.9f);
            Quad(x+8,y+3,w-16,1,line);Quad(x+8,y+h-4,w-16,1,line);
            Quad(x+3,y+8,1,h-16,line);Quad(x+w-4,y+8,1,h-16,line);
            foreach(float a in new[]{x+3,x+w-16})foreach(float b in new[]{y+3,y+h-5})Quad(a,b,13,2,shine);
            foreach(float a in new[]{x+3,x+w-5})foreach(float b in new[]{y+3,y+h-16})Quad(a,b,2,13,shine);
        }
        public static void Add(RectTransform parent)
        {
            var go=new GameObject("Filete de latón",typeof(RectTransform));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;go.AddComponent<AdventureFrame>().raycastTarget=false;
        }
    }
}
