using UnityEngine;

namespace LaserHero
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LaserFlightArrow : UnityEngine.UI.MaskableGraphic
    {
        public bool down;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            Draw(vh, 1, new Color(.06f, .07f, .12f));
            Draw(vh, .77f, Color.white);
        }

        void Draw(UnityEngine.UI.VertexHelper vh, float scale, Color c)
        {
            var r = GetPixelAdjustedRect();
            float size = Mathf.Min(r.width, r.height) * scale;
            Vector2 center = r.center;
            float sign = down ? -1 : 1;
            Point(vh, center, size, new Vector2(-.13f, -.4f * sign), c);
            Point(vh, center, size, new Vector2(-.13f, .12f * sign), c);
            Point(vh, center, size, new Vector2(.13f, .12f * sign), c);
            Point(vh, center, size, new Vector2(.13f, -.4f * sign), c);
            int n = vh.currentVertCount - 4;
            vh.AddTriangle(n, n + 1, n + 2);
            vh.AddTriangle(n, n + 2, n + 3);
            n = vh.currentVertCount;
            Point(vh, center, size, new Vector2(-.42f, .04f * sign), c);
            Point(vh, center, size, new Vector2(0, .48f * sign), c);
            Point(vh, center, size, new Vector2(.42f, .04f * sign), c);
            vh.AddTriangle(n, n + 1, n + 2);
        }

        void Point(UnityEngine.UI.VertexHelper vh, Vector2 center, float size, Vector2 p, Color c)
        {
            vh.AddVert(center + p * size, c * color, Vector2.zero);
        }
    }
}
