using UnityEngine;

namespace LaserHero
{
    public class LaserImpactFeedback : MonoBehaviour
    {
        public static LaserImpactFeedback Instance;
        public LaserHeroPlayer hero;
        public AudioClip laserLoop, hitClip, collapseClip, collectClip;
        public AudioClip alternateHitClip;
        public int CollapseCount { get; private set; }

        public Material debrisMaterial;
        [HideInInspector]
        public Material fireMaterial;
        public LaserFallenFire fallenFirePrefab;
        [Range(0, 1)]
        public float laserFireChance = .06f;
        public int firePoolSize = 12;
        AudioSource[] sources;
        int voice;
        float nextHit;
        LaserFallenFire[] fires;
        int recycledFire;
        public int ActiveFireCount
        {
            get
            {
                int n = 0;
                if (fires != null)
                    foreach (var fire in fires)
                        if (fire.Active)
                            n++;
                return n;
            }
        }

        ParticleSystem collapseDust;
        void Awake()
        {
            Instance = this;
            sources = new AudioSource[10];
            for (int i = 0; i < sources.Length; i++)
            {
                var g = new GameObject("Impact voice");
                g.transform.SetParent(transform);
                sources[i] = g.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = .3f;
                sources[i].minDistance = 8;
                sources[i].maxDistance = 85;
                sources[i].rolloffMode = AudioRolloffMode.Linear;
            }
        }

        void Start()
        {
            var eyes = hero.GetComponent<LaserEyes>();
            if (fallenFirePrefab != null)
            {
                fires = new LaserFallenFire[Mathf.Clamp(firePoolSize, 1, 16)];
                for (int i = 0; i < fires.Length; i++)
                {
                    fires[i] = Instantiate(fallenFirePrefab, transform);
                    fires[i].name = "FallenFire pooled " + i;
                    fires[i].Release();
                }
            }

            var dust = new GameObject("Collapse dust puff");
            dust.transform.SetParent(transform);
            collapseDust = dust.AddComponent<ParticleSystem>();
            collapseDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var dm = collapseDust.main;
            dm.startLifetime = new ParticleSystem.MinMaxCurve(.3f, .65f);
            dm.startSize = new ParticleSystem.MinMaxCurve(.4f, .9f);
            dm.startSpeed = 1.5f;
            dm.startColor = new Color(.8f, .8f, .8f, .19f);
            dm.maxParticles = 60;
            dm.simulationSpace = ParticleSystemSimulationSpace.World;
            var de = collapseDust.emission;
            de.rateOverTime = 0;
            collapseDust.GetComponent<ParticleSystemRenderer>().sharedMaterial = eyes.dustMaterial;
            collapseDust.Play();
        }

        public static void LaserFire(Vector3 point, Vector3 normal, int destroyed)
        {
            if (Instance != null && destroyed > 0 && Random.value < Instance.laserFireChance)
                Instance.SpawnFire(point, normal);
        }

        public static void ExplosionFire(Vector3 point, int destroyed)
        {
            if (Instance != null && destroyed > 0)
                for (int i = 0; i < 3; i++)
                    Instance.SpawnFire(point, Vector3.up);
        }

        public static void SuperFire(Vector3 point, Vector3 normal, float radius, int count)
        {
            if (Instance == null)
                return;
            for (int i = 0; i < count; i++)
                Instance.SpawnFire(point + Random.insideUnitSphere * Mathf.Min(radius * .4f, .7f), normal, true);
        }

        public bool SpawnFire(Vector3 point, Vector3 normal, bool recycle = false)
        {
            if (fires == null || !LaserArena.CanPhysicsDamage(point))
                return false;
            LaserFallenFire chosen = null;
            foreach (var fire in fires)
                if (!fire.Active)
                {
                    chosen = fire;
                    break;
                }

            if (chosen == null && recycle)
            {
                chosen = fires[recycledFire++ % fires.Length];
                chosen.Release();
            }

            if (chosen != null)
            {
                Vector2 side = Random.insideUnitCircle;
                if (side.sqrMagnitude < .01f)
                    side = Vector2.right;
                side.Normalize();
                Vector3 velocity = new Vector3(side.x, 0, side.y) * Random.Range(2.2f, 5) + Vector3.up * Random.Range(4, 7);
                chosen.Launch(point + normal.normalized * .4f + Vector3.up * .15f, velocity);
                return true;
            }

            return false;
        }

        public void ClearFires()
        {
            if (fires != null)
                foreach (var fire in fires)
                    fire.Release();
        }

        void OnEnable()
        {
            Instance = this;
        }

        void OnDisable()
        {
            ClearFires();
            if (Instance == this)
                Instance = null;
        }

        public void Play(AudioClip clip, Vector3 position, float volume, float pitch = 1)
        {
            if (clip == null)
                return;
            var s = sources[voice++ % sources.Length];
            s.transform.position = position;
            s.pitch = pitch;
            s.clip = clip;
            s.volume = volume;
            s.Play();
        }

        public static void Hit(Vector3 position, int count, Vector3 direction)
        {
            if (Instance == null)
                return;
            float distance = Vector3.Distance(Instance.hero.transform.position, position);
            Instance.hero.AddShake(Mathf.Min(.14f, .055f + count * .003f) * Mathf.Clamp01(1 - distance / 70));
            if (Time.time >= Instance.nextHit)
            {
                Instance.Play(Instance.alternateHitClip != null && Random.value > .55f
                    ? Instance.alternateHitClip : Instance.hitClip, position, .8f, Random.Range(.96f, 1.05f));
                Instance.nextHit = Time.time + .08f;
            }
        }

        public static void Collapse(Vector3 position, int blocks, float velocity)
        {
            if (Instance == null)
                return;
            float distance = Vector3.Distance(Instance.hero.transform.position, position);
            float amplitude = Mathf.Clamp(blocks / 900f, .03f,
                .55f) * Mathf.Clamp01(1 - distance / 75) * Mathf.Clamp(velocity / 4, .2f, 1.5f);
            Instance.CollapseCount++;
            Instance.hero.AddShake(amplitude);
            Instance.Play(Instance.collapseClip, position, Mathf.Clamp(blocks / 500f, .2f, .9f), Random.Range(.95f, 1.05f));
            if (Instance.collapseDust != null)
            {
                Instance.collapseDust.transform.position = position;
                Instance.collapseDust.Emit(Mathf.Clamp(blocks / 40, 6, 22));
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
