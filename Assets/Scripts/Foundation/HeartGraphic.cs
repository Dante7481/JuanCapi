using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JuanCapi
{
    // A vector silhouette, clipped to quarter-heart increments. Empty capacity remains visible.
    public sealed class HeartGraphic : MaskableGraphic
    {
        [Range(0, 4)] public int quarters = 4;
        private static readonly Vector2[] Shape =
        {
            new Vector2(.5f,0), new Vector2(.08f,.42f), new Vector2(0,.68f), new Vector2(.07f,.9f),
            new Vector2(.25f,1), new Vector2(.4f,.96f), new Vector2(.5f,.82f),
            new Vector2(.6f,.96f), new Vector2(.75f,1), new Vector2(.93f,.9f), new Vector2(1,.68f), new Vector2(.92f,.42f)
        };
        public void SetQuarters(int value) { if (quarters == value) return; quarters = value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Draw(vh, new List<Vector2>(Shape), new Color(.22f,.31f,.32f), 1);
            if (quarters <= 0) return;
            var polygon = new List<Vector2>(); float edge = Mathf.Clamp01(quarters / 4f);
            for (int i = 0; i < Shape.Length; i++)
            {
                Vector2 a = Shape[i], b = Shape[(i+1)%Shape.Length];
                if (a.x <= edge) polygon.Add(a);
                if ((a.x <= edge) != (b.x <= edge)) polygon.Add(Vector2.Lerp(a,b,(edge-a.x)/(b.x-a.x)));
            }
            Draw(vh, polygon, color, .86f);
        }
        private void Draw(VertexHelper vh, List<Vector2> points, Color tint, float scale)
        {
            if (points.Count < 3) return;
            Rect rect = GetPixelAdjustedRect(); int start = vh.currentVertCount;
            Vector2 center = Vector2.zero; foreach (var p in points) center += p; center /= points.Count;
            Add(vh, center, rect, tint, scale);
            foreach (var p in points) Add(vh,p,rect,tint,scale);
            for(int i=0;i<points.Count;i++) vh.AddTriangle(start,start+1+i,start+1+(i+1)%points.Count);
        }
        private static void Add(VertexHelper vh, Vector2 p, Rect rect, Color tint, float scale)
        {
            p = (p - Vector2.one * .5f) * scale + Vector2.one * .5f;
            vh.AddVert(new Vector3(rect.x + p.x * rect.width, rect.y + p.y * rect.height), tint, Vector2.zero);
        }
    }
}
