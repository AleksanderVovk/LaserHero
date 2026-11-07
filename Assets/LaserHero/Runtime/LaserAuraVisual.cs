using UnityEngine;

namespace LaserHero
{
    // Cosmetic rings stay attached to the player; bonuses are owned by LaserProgress.
    public class LaserAuraVisual : MonoBehaviour
    {
        public LaserProgress progress;
        public Material material;
        LineRenderer[] rings;
        void Start()
        {
            rings = new LineRenderer[2];
            for (int i = 0; i < rings.Length; i++)
            {
                var go = new GameObject("Aura orbit");
                go.transform.SetParent(transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = 48;
                line.widthMultiplier = .045f;
                line.numCornerVertices = 2;
                rings[i] = line;
            }
        }

        void LateUpdate()
        {
            if (rings == null)
                return;
            foreach (var line in rings)
                line.enabled = progress != null && progress.Data.aura >= 0;
            if (progress == null || progress.Data.aura < 0)
                return;
            int id = progress.Data.aura;
            Color color = LaserProgress.Colors[id == 0 ? 0 : id == 1 ? 2 : id == 2 ? 4 : id == 3 ? 5 : 3];
            for (int i = 0; i < rings.Length; i++)
            {
                var line = rings[i];
                line.startColor = line.endColor = color;
                for (int n = 0; n < line.positionCount; n++)
                {
                    float a = n * Mathf.PI * 2 / line.positionCount + Time.time * (i == 0 ? 1 : -.7f);
                    line.SetPosition(n, new Vector3(Mathf.Cos(a) * .8f,
                        .45f + i * .3f + Mathf.Sin(a * 2 + Time.time) * .15f, Mathf.Sin(a) * .8f));
                }
            }
        }
    }
}
