using UnityEngine;

namespace LaserHero
{
    public class LaserTrainingPricePlate : UnityEngine.UI.Image
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper helper)
        {
            helper.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0)
                return;
            float radius = Mathf.Min(22, rect.height * .5f);
            Fill(helper, rect, radius, Color.black);
            rect.xMin += 4;
            rect.xMax -= 4;
            rect.yMin += 4;
            rect.yMax -= 4;
            Fill(helper, rect, Mathf.Max(0, radius - 4), color);
        }

        static void Fill(UnityEngine.UI.VertexHelper helper, Rect rect, float radius, Color tint)
        {
            int start = helper.currentVertCount;
            helper.AddVert(rect.center, tint, Vector2.zero);
            const int steps = 8, count = 4 * (steps + 1);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - radius
                    : rect.xMin + radius, corner < 2 ? rect.yMax - radius : rect.yMin + radius);
                for (int i = 0; i <= steps; i++)
                {
                    float angle = (corner * 90f + i * 90f / steps) * Mathf.Deg2Rad;
                    helper.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
                }
            }

            for (int i = 0; i < count; i++)
                helper.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % count);
        }
    }
}
