using UnityEngine;
using UnityEngine.UI;

namespace JuanCapi
{
    public sealed class SymbolGraphic : MaskableGraphic
    {
        public enum Shape { Clock,Lock,Check,Arrow,Mouse,Save,Pack,Mountain,Water,Factory,Pause,Reticle,Medal }
        public Shape shape;
        public int mouseButton;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            System.Func<Vector2,Vector2> p=v=>new Vector2(r.x+v.x*r.width,r.y+v.y*r.height);
            System.Action<Vector2[],Color> poly=(points,tint)=>
            {int start=vh.currentVertCount;foreach(var v in points)vh.AddVert(p(v),tint,Vector2.zero);for(int i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);};
            System.Action<Vector2,Vector2,float> line=(a,b,w)=>
            {var n=new Vector2(-(b-a).y,(b-a).x).normalized*w*.5f;poly(new[]{a+n,b+n,b-n,a-n},color);};
            System.Action<float,float,float,float> box=(x,y,w,h)=>poly(new[]{new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h)},color);
            System.Action<Vector2,float,float,float> arc=(center,radius,start,end)=>
            {for(int i=0;i<24;i++){float a=Mathf.Lerp(start,end,i/24f),b=Mathf.Lerp(start,end,(i+1)/24f);line(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,.07f);}};
            switch(shape)
            {
                case Shape.Medal:
                    poly(new[]{new Vector2(.22f,.9f),new Vector2(.4f,.9f),new Vector2(.57f,.53f),new Vector2(.39f,.44f)},color);
                    poly(new[]{new Vector2(.6f,.9f),new Vector2(.78f,.9f),new Vector2(.61f,.44f),new Vector2(.43f,.53f)},color);
                    arc(new Vector2(.5f,.33f),.25f,0,Mathf.PI*2);
                    line(new Vector2(.37f,.34f),new Vector2(.46f,.24f),.05f);line(new Vector2(.46f,.24f),new Vector2(.65f,.44f),.05f);break;
                case Shape.Clock:arc(Vector2.one*.5f,.37f,0,Mathf.PI*2);line(new Vector2(.5f,.5f),new Vector2(.5f,.76f),.07f);line(new Vector2(.5f,.5f),new Vector2(.7f,.4f),.07f);break;
                case Shape.Lock:box(.18f,.12f,.64f,.45f);arc(new Vector2(.5f,.58f),.22f,0,Mathf.PI);box(.25f,.5f,.07f,.1f);box(.68f,.5f,.07f,.1f);break;
                case Shape.Check:line(new Vector2(.12f,.47f),new Vector2(.38f,.2f),.13f);line(new Vector2(.38f,.2f),new Vector2(.88f,.82f),.13f);break;
                case Shape.Arrow:line(new Vector2(.1f,.5f),new Vector2(.86f,.5f),.1f);line(new Vector2(.55f,.8f),new Vector2(.86f,.5f),.1f);line(new Vector2(.55f,.2f),new Vector2(.86f,.5f),.1f);break;
                case Shape.Mouse:arc(new Vector2(.5f,.5f),.35f,0,Mathf.PI*2);line(new Vector2(.15f,.52f),new Vector2(.85f,.52f),.04f);line(new Vector2(.5f,.52f),new Vector2(.5f,.84f),.04f);box(mouseButton==0?.25f:.54f,.6f,.2f,.17f);break;
                case Shape.Save:box(.16f,.14f,.68f,.7f);poly(new[]{new Vector2(.3f,.55f),new Vector2(.68f,.55f),new Vector2(.68f,.84f),new Vector2(.3f,.84f)},new Color(.05f,.13f,.14f));break;
                case Shape.Pack:box(.2f,.13f,.6f,.62f);arc(new Vector2(.5f,.75f),.15f,0,Mathf.PI);line(new Vector2(.22f,.42f),new Vector2(.78f,.42f),.06f);break;
                case Shape.Mountain:line(new Vector2(.06f,.15f),new Vector2(.45f,.85f),.1f);line(new Vector2(.45f,.85f),new Vector2(.85f,.15f),.1f);line(new Vector2(.58f,.6f),new Vector2(.73f,.8f),.08f);line(new Vector2(.73f,.8f),new Vector2(.96f,.15f),.08f);break;
                case Shape.Water:for(int j=0;j<3;j++)for(int i=0;i<10;i++){float a=i*.08f+.1f,b=a+.08f;line(new Vector2(a,.2f+j*.28f+Mathf.Sin(i*1.25f)*.06f),new Vector2(b,.2f+j*.28f+Mathf.Sin((i+1)*1.25f)*.06f),.07f);}break;
                case Shape.Factory:box(.15f,.15f,.72f,.38f);box(.2f,.5f,.16f,.4f);box(.56f,.5f,.16f,.23f);break;
                case Shape.Pause:box(.22f,.15f,.18f,.7f);box(.6f,.15f,.18f,.7f);break;
                case Shape.Reticle:box(.46f,.1f,.08f,.22f);box(.46f,.68f,.08f,.22f);box(.1f,.46f,.22f,.08f);box(.68f,.46f,.22f,.08f);box(.47f,.47f,.06f,.06f);break;
            }
        }
    }
}
