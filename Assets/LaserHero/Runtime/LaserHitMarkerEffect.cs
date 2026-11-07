using UnityEngine;

namespace LaserHero
{
    public sealed class LaserHitMarkerEffect : MonoBehaviour
    {
        public GameObject effect;
        public Vector3 parkingPosition = new Vector3(0, -100, 0);
        ParticleSystem[] systems;
        ParticleSystem.MinMaxGradient[] originalColors;
        ParticleSystem.Particle[][] particles;
        Quaternion originalRotation;
        Color lastColor;
        bool initialized, hasColor;
        public bool Showing => effect != null && effect.activeSelf;

        void Awake()
        {
            Initialize();
            Hide();
        }

        void Initialize()
        {
            if (initialized || effect == null)
                return;
            initialized = true;
            originalRotation = effect.transform.rotation;
            systems = effect.GetComponentsInChildren<ParticleSystem>(true);
            originalColors = new ParticleSystem.MinMaxGradient[systems.Length];
            particles = new ParticleSystem.Particle[systems.Length][];
            for (int i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;
                originalColors[i] = main.startColor;
                main.playOnAwake = false;
                particles[i] = new ParticleSystem.Particle[main.maxParticles];
                var lifetime = systems[i].colorOverLifetime;
                if (lifetime.enabled)
                    lifetime.color = Tint(lifetime.color, Color.white);
                var speed = systems[i].colorBySpeed;
                if (speed.enabled)
                    speed.color = Tint(speed.color, Color.white);
                var trails = systems[i].trails;
                if (trails.enabled)
                {
                    trails.inheritParticleColor = true;
                    trails.colorOverLifetime = Tint(trails.colorOverLifetime, Color.white);
                    trails.colorOverTrail = Tint(trails.colorOverTrail, Color.white);
                }
            }
        }

        public void ShowHit(Vector3 point, Vector3 normal, Color color)
        {
            Initialize();
            if (!initialized)
                return;
            bool restart = !Showing || (effect.transform.position - point).sqrMagnitude > 4;
            if (restart)
                foreach (var ps in systems)
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (!hasColor || lastColor != color)
            {
                for (int i = 0; i < systems.Length; i++)
                {
                    var main = systems[i].main;
                    main.startColor = Tint(originalColors[i], color);
                    int count = systems[i].GetParticles(particles[i]);
                    for (int j = 0; j < count; j++)
                    {
                        Color old = particles[i][j].startColor;
                        particles[i][j].startColor = new Color(color.r, color.g, color.b, old.a);
                    }

                    if (count > 0)
                        systems[i].SetParticles(particles[i], count);
                }

                lastColor = color;
                hasColor = true;
            }

            if (normal.sqrMagnitude < .001f)
                normal = Vector3.up;
            normal.Normalize();
            effect.transform.SetPositionAndRotation(point + normal * .025f, Quaternion.LookRotation(normal,
                Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > .95f ? Vector3.forward : Vector3.up) * originalRotation);
            effect.SetActive(true);
            if (restart)
                foreach (var ps in systems)
                    ps.Play(false);
        }

        public void Hide()
        {
            if (effect == null)
                return;
            if (effect.activeSelf)
            {
                if (systems != null)
                    foreach (var ps in systems)
                        ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                effect.SetActive(false);
            }

            if (effect.transform.position != parkingPosition)
                effect.transform.position = parkingPosition;
        }

        static Color TintColor(Color source, Color tint) => new Color(tint.r, tint.g, tint.b, source.a * tint.a);
        static Gradient TintGradient(Gradient original, Color tint)
        {
            var keys = original.colorKeys;
            for (int i = 0; i < keys.Length; i++)
                keys[i].color = tint;
            var result = new Gradient
            {
                mode = original.mode
            };
            result.SetKeys(keys, original.alphaKeys);
            return result;
        }

        static ParticleSystem.MinMaxGradient Tint(ParticleSystem.MinMaxGradient original, Color color)
        {
            switch (original.mode)
            {
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(TintColor(original.colorMin, color),
                        TintColor(original.colorMax, color));
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(TintGradient(original.gradient, color));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(TintGradient(original.gradientMin, color),
                        TintGradient(original.gradientMax, color));
                case ParticleSystemGradientMode.RandomColor:
                    var g = new ParticleSystem.MinMaxGradient(TintGradient(original.gradient, color));
                    g.mode = ParticleSystemGradientMode.RandomColor;
                    return g;
                default:
                    return new ParticleSystem.MinMaxGradient(TintColor(original.color, color));
            }
        }

        void OnDisable()
        {
            Hide();
        }
    }
}
