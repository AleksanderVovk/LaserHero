using UnityEngine;

namespace LaserHero
{
    // Resolution-independent gear for the HUD settings button.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LaserSettingsGear : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            Ring(vh, rect.center, radius, .33f, .75f, new Color(.035f, .025f, .09f));
            Ring(vh, rect.center, radius * .84f, .46f, .75f, Color.white);
        }

        void Ring(UnityEngine.UI.VertexHelper vh, Vector2 center, float radius, float inner, float valley, Color tint)
        {
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2 / 48, b = (i + 1) * Mathf.PI * 2 / 48;
                float ra = (i % 6 < 3 ? 1 : valley) * radius, rb = ((i + 1) % 6 < 3 ? 1 : valley) * radius;
                var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var v = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                int n = vh.currentVertCount;
                vh.AddVert(center + u * radius * inner, tint * color, Vector2.zero);
                vh.AddVert(center + u * ra, tint * color, Vector2.zero);
                vh.AddVert(center + v * rb, tint * color, Vector2.zero);
                vh.AddVert(center + v * radius * inner, tint * color, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2);
                vh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
