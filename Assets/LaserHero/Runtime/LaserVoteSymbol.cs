using System.Collections.Generic;
using UnityEngine;

namespace LaserHero
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class LaserVoteSymbol : UnityEngine.UI.MaskableGraphic
    {
        public enum Kind
        {
            Pin,
            Check,
            Cross
        }

        public Kind symbol;
        public Sprite spriteOverride;
        public override Texture mainTexture => spriteOverride != null ? spriteOverride.texture : Texture2D.whiteTexture;

        static readonly Color Ink = new Color(.025f, .02f, .13f);
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = GetPixelAdjustedRect();
            float size = Mathf.Min(r.width, r.height);
            Vector2 center = r.center;
            if (spriteOverride != null)
            {
                float aspect = spriteOverride.rect.width / spriteOverride.rect.height;
                Vector2 extent = aspect > r.width / r.height ? new Vector2(r.width, r.width / aspect)
                    : new Vector2(r.height * aspect, r.height);
                Vector2 min = center - extent * .5f, max = center + extent * .5f;
                Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(spriteOverride);
                mesh.AddVert(new Vector3(min.x, min.y), color, new Vector2(uv.x, uv.y));
                mesh.AddVert(new Vector3(min.x, max.y), color, new Vector2(uv.x, uv.w));
                mesh.AddVert(new Vector3(max.x, max.y), color, new Vector2(uv.z, uv.w));
                mesh.AddVert(new Vector3(max.x, min.y), color, new Vector2(uv.z, uv.y));
                mesh.AddTriangle(0, 1, 2);
                mesh.AddTriangle(2, 3, 0);
                return;
            }

            if (symbol == Kind.Check)
            {
                Circle(mesh, center, size * .46f, Ink);
                Circle(mesh, center, size * .42f, new Color(.03f, .58f, .16f));
                Circle(mesh, center + Vector2.up * size * .025f, size * .38f, new Color(.12f, .78f, .26f));
                Line(mesh, center + new Vector2(-.24f, -.01f) * size, center + new Vector2(-.06f,
                    -.2f) * size, size * .12f, Color.white);
                Line(mesh, center + new Vector2(-.06f, -.2f) * size, center + new Vector2(.27f, .2f) * size,
                    size * .12f, Color.white);
                Circle(mesh, center + new Vector2(.24f, .27f) * size, size * .09f, new Color(.9f, 1, .9f, .8f));
            }
            else if (symbol == Kind.Cross)
            {
                Vector2[] shape =
                {
                    new Vector2(-.21f, .47f),
                    new Vector2(0, .26f),
                    new Vector2(.21f, .47f),
                    new Vector2(.47f, .21f),
                    new Vector2(.26f, 0),
                    new Vector2(.47f, -.21f),
                    new Vector2(.21f, -.47f),
                    new Vector2(0, -.26f),
                    new Vector2(-.21f, -.47f),
                    new Vector2(-.47f, -.21f),
                    new Vector2(-.26f, 0),
                    new Vector2(-.47f, .21f)
                };
                Polygon(mesh, shape, center, size, Ink);
                Polygon(mesh, shape, center, size * .88f, new Color(.78f, .02f, .23f));
                Polygon(mesh, shape, center + new Vector2(0, .015f) * size, size * .75f, new Color(1, .12f, .36f));
                Line(mesh, center + new Vector2(-.31f, .13f) * size, center + new Vector2(-.2f,
                    .02f) * size, size * .025f, Color.white);
                Line(mesh, center + new Vector2(.15f, -.28f) * size, center + new Vector2(.27f,
                    -.35f) * size, size * .025f, Color.white);
            }
            else
            {
                var pin = new List<Vector2>
                {
                    new Vector2(0, -.48f)
                };
                for (int i = 0; i <= 24; i++)
                {
                    float a = (210 - i * 10) * Mathf.Deg2Rad;
                    pin.Add(new Vector2(Mathf.Cos(a) * .36f, Mathf.Sin(a) * .36f + .14f));
                }

                Polygon(mesh, pin.ToArray(), center, size, Ink);
                Polygon(mesh, pin.ToArray(), center + Vector2.up * size * .02f, size * .83f, new Color(1, .08f, .35f));
                Circle(mesh, center + Vector2.up * size * .15f, size * .17f, Ink);
                Circle(mesh, center + new Vector2(-.045f, .2f) * size, size * .095f, new Color(.45f, .24f, .75f));
            }
        }

        static void Polygon(UnityEngine.UI.VertexHelper mesh, Vector2[] points, Vector2 center, float scale, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(center, tint, Vector2.zero);
            foreach (var p in points)
                mesh.AddVert(center + p * scale, tint, Vector2.zero);
            for (int i = 0; i < points.Length; i++)
                mesh.AddTriangle(start, start + i + 1, start + (i + 1) % points.Length + 1);
        }

        static void Circle(UnityEngine.UI.VertexHelper mesh, Vector2 center, float radius, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(center, tint, Vector2.zero);
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI / 24;
                mesh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, tint, Vector2.zero);
            }

            for (int i = 0; i < 48; i++)
                mesh.AddTriangle(start, start + i + 1, start + (i + 1) % 48 + 1);
        }

        static void Line(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, float width, Color tint)
        {
            Vector2 d = (b - a).normalized, n = new Vector2(-d.y, d.x) * width * .5f;
            int start = mesh.currentVertCount;
            mesh.AddVert(a - n, tint, Vector2.zero);
            mesh.AddVert(a + n, tint, Vector2.zero);
            mesh.AddVert(b + n, tint, Vector2.zero);
            mesh.AddVert(b - n, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
