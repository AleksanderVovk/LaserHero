using UnityEngine;

namespace LaserHero
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LaserRainbowCharge : UnityEngine.UI.MaskableGraphic
    {
        [Range(0, 1)]
        public float amount;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            float width = rect.width * Mathf.Clamp01(amount);
            if (width <= 0)
                return;
            for (int i = 0; i < 24; i++)
            {
                float left = rect.width * i / 24f, right = Mathf.Min(width, rect.width * (i + 1) / 24f);
                if (left >= width)
                    break;
                Color a = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * .65f + i / 24f, 1), .8f, 1) * color;
                Color b = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * .65f + (i + 1) / 24f, 1), .8f, 1) * color;
                int n = vh.currentVertCount;
                vh.AddVert(new Vector3(rect.xMin + left, rect.yMin), a, Vector2.zero);
                vh.AddVert(new Vector3(rect.xMin + left, rect.yMax), Color.Lerp(a, Color.white, .28f), Vector2.zero);
                vh.AddVert(new Vector3(rect.xMin + right, rect.yMax), Color.Lerp(b, Color.white, .28f), Vector2.zero);
                vh.AddVert(new Vector3(rect.xMin + right, rect.yMin), b, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2);
                vh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
