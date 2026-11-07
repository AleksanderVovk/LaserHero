using UnityEngine;

namespace LaserHero
{
    // Draw the frame once, then fit a centred grid of complete, equally sized studs.
    // Repeating a whole framed/gradient sprite introduces seams and cropped studs.
    public class LaserStudPanel : UnityEngine.UI.Image
    {
        public Color topColor = Color.white, bottomColor = Color.gray;
        public float studLeftInset;
        public float studSize = 18, studGap = 12;
        public override Texture mainTexture => Texture2D.whiteTexture;

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0)
                return;
            float edge = Mathf.Min(3, Mathf.Min(rect.width, rect.height) / 6);
            Quad(mesh, rect, Color.black, Color.black);
            rect = Inset(rect, edge);
            Quad(mesh, rect, Color.Lerp(bottomColor, Color.white, .7f), Color.Lerp(topColor, Color.white, .7f));
            rect = Inset(rect, edge);
            Quad(mesh, rect, Color.Lerp(bottomColor, Color.black, .35f), Color.Lerp(topColor, Color.black, .35f));
            rect = Inset(rect, edge);
            if (rect.width <= 0 || rect.height <= 0)
                return;
            Quad(mesh, rect, bottomColor, topColor);
            rect.xMin += Mathf.Min(studLeftInset, rect.width);
            float size = Mathf.Max(1, studSize), gap = Mathf.Max(0, studGap), padding = 2, pitch = size + gap;
            int columns = Mathf.Max(0, Mathf.FloorToInt((rect.width - 2 * padding + gap) / pitch)), rows =
                Mathf.Max(0, Mathf.FloorToInt((rect.height - 2 * padding + gap) / pitch));
            float left = rect.center.x - ((columns - 1) * pitch + size) * .5f;
            float bottom = rect.center.y - ((rows - 1) * pitch + size) * .5f;
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    Rect stud = new Rect(left + x * pitch, bottom + y * pitch, size, size);
                    Color low = Color.Lerp(bottomColor, topColor,
                        (stud.yMin - rect.yMin) / rect.height), high = Color.Lerp(bottomColor, topColor,
                        (stud.yMax - rect.yMin) / rect.height);
                    bool highlight = (x + y * 2) % 4 == 0;
                    RoundedQuad(mesh, stud, 3, Color.Lerp(low, Color.black, .16f), Color.Lerp(high,
                        Color.white, highlight ? .3f : .17f));
                    RoundedQuad(mesh, Inset(stud, 1.5f), 2, Color.Lerp(low, Color.black, highlight
                        ? .01f : .11f), Color.Lerp(high, highlight ? Color.white : Color.black, highlight ? .08f : .04f));
                }
        }

        static Rect Inset(Rect r, float n) => new Rect(r.x + n, r.y + n, Mathf.Max(0, r.width - 2 * n), Mathf.Max(0,
            r.height - 2 * n));
        void RoundedQuad(UnityEngine.UI.VertexHelper mesh, Rect r, float radius, Color low, Color high)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(r.center, Color.Lerp(low, high, .5f) * color, Vector2.zero);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? r.xMax - radius
                    : r.xMin + radius, corner < 2 ? r.yMax - radius : r.yMin + radius);
                for (int step = 0; step <= 2; step++)
                {
                    float angle = (corner * 90 + step * 45) * Mathf.Deg2Rad;
                    Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    mesh.AddVert(point, Color.Lerp(low, high, (point.y - r.yMin) / r.height) * color, Vector2.zero);
                }
            }

            for (int i = 0; i < 12; i++)
                mesh.AddTriangle(first, first + 1 + (i + 1) % 12, first + 1 + i);
        }

        void Quad(UnityEngine.UI.VertexHelper mesh, Rect r, Color low, Color high)
        {
            int index = mesh.currentVertCount;
            Color32 bottom = low * color, top = high * color;
            mesh.AddVert(new Vector3(r.xMin, r.yMin), bottom, Vector2.zero);
            mesh.AddVert(new Vector3(r.xMin, r.yMax), top, Vector2.zero);
            mesh.AddVert(new Vector3(r.xMax, r.yMax), top, Vector2.zero);
            mesh.AddVert(new Vector3(r.xMax, r.yMin), bottom, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index + 2, index + 3, index);
        }
    }
}
