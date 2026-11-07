using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace LaserHero
{
    [DisallowMultipleComponent]
    public class LaserArenaAbilities : MonoBehaviour
    {
        [FormerlySerializedAs("cooldown"), Min(.1f)]
        public float chargeSeconds = 8;
        [Min(1)]
        public float radiusMultiplier = 2, damageMultiplier = 5;
        [Range(1, 24)]
        public int fireCount = 12;
        [Min(1)]
        public int maxSuperVoxels = 320;
        [Range(0, .3f)]
        public float superShotShake = .12f;
        public AudioClip superStrikeClip;
        public AudioSource superStrikeSource;
        [Range(0, 1)]
        public float superStrikeVolume = .85f;
        public float Charge01 => Mathf.Clamp01(charge / Mathf.Max(.1f, chargeSeconds));
        public bool BurstActive => Time.time < burstUntil;
        public int LastDestroyed { get; private set; }
        public int SuperShotCount { get; private set; }
        public float LastRadius { get; private set; }
        public float LastDamage { get; private set; }
        public Vector3 LastPoint { get; private set; }
        // Kept only so historical editor verification scripts still compile.
        public float RemainingCooldown => Mathf.Max(0, chargeSeconds - charge);

        public bool TryExplode(Vector3? aim = null) => false;
        LaserHeroPlayer hero;
        LaserEyes eyes;
        float charge, burstUntil, ringStarted = -100;
        LineRenderer ring;
        readonly HashSet<LaserVoxelBody> targets = new HashSet<LaserVoxelBody>();
        readonly List<LaserVoxelBody> ordered = new List<LaserVoxelBody>();
        readonly Collider[] overlaps = new Collider[128];
        void Awake()
        {
            hero = GetComponent<LaserHeroPlayer>();
            eyes = GetComponent<LaserEyes>();
            if (superStrikeSource == null)
            {
                var go = new GameObject("Super Strike Audio");
                go.transform.SetParent(transform, false);
                superStrikeSource = go.AddComponent<AudioSource>();
                superStrikeSource.playOnAwake = false;
                superStrikeSource.spatialBlend = 0;
                superStrikeSource.priority = 40;
            }
        }

        void Update()
        {
            if (LaserArena.Instance == null || !LaserArena.Instance.Inside)
                charge = 0;
            if (ring == null)
                return;
            float t = (Time.time - ringStarted) / .4f;
            ring.enabled = t < 1;
            if (t >= 1)
                return;
            ring.widthMultiplier = .14f * (1 - t);
            var c = Color.HSVToRGB(Mathf.Repeat(Time.time * 1.8f, 1), .85f, 1);
            c.a = 1 - t;
            ring.startColor = ring.endColor = c;
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2 / 48;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * LastRadius * t, .08f, Mathf.Sin(a) * LastRadius * t));
            }
        }

        public void ChargeLaser(float seconds)
        {
            if (hero.MenuOpen || LaserArena.Instance == null || !LaserArena.Instance.Inside)
                return;
            charge = Mathf.Min(Mathf.Max(.1f, chargeSeconds), charge + Mathf.Max(0, seconds));
        }

        public bool TrySuperStrike(Vector3 point, Vector3 direction, Vector3 normal)
        {
            if (Charge01 < 1 || hero.MenuOpen || LaserArena.Instance == null || !LaserArena.Instance.Inside
                || !LaserArena.CanPhysicsDamage(point))
                return false;
            charge = 0;
            burstUntil = Time.time + .35f;
            SuperShotCount++;
            LastPoint = point;
            if (superStrikeClip != null && superStrikeSource != null)
                superStrikeSource.PlayOneShot(superStrikeClip, superStrikeVolume);
            LastRadius = eyes.progress.Radius * radiusMultiplier;
            LastDamage = eyes.progress.Damage * damageMultiplier;
            LastDestroyed = 0;
            targets.Clear();
            ordered.Clear();
            int count = Physics.OverlapSphereNonAlloc(point, LastRadius, overlaps, 1, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var body = overlaps[i].GetComponent<LaserVoxelBody>();
                if (body != null && body.allowDestruction && targets.Add(body))
                    ordered.Add(body);
            }

            ordered.Sort((a, b) => a.GetComponent<MeshRenderer>().bounds.SqrDistance(point)
                .CompareTo(b.GetComponent<MeshRenderer>().bounds.SqrDistance(point)));
            foreach (var body in ordered)
            {
                if (LastDestroyed >= maxSuperVoxels)
                    break;
                LastDestroyed += body.Carve(point + direction * .12f, LastRadius, LastDamage, direction,
                    maxSuperVoxels - LastDestroyed);
            }

            LaserImpactFeedback.SuperFire(point, normal, LastRadius, fireCount);
            hero.AddShake(superShotShake);
            LaserImpactFeedback.Collapse(point, Mathf.Max(200, LastDestroyed * 5), 5);
            if (ring == null)
            {
                var go = new GameObject("Super strike shockwave");
                ring = go.AddComponent<LineRenderer>();
                ring.sharedMaterial = eyes.beamMaterial;
                ring.loop = true;
                ring.positionCount = 48;
                ring.useWorldSpace = false;
            }

            ring.transform.position = point;
            ringStarted = Time.time;
            return true;
        }

        void OnDestroy()
        {
            if (ring != null)
                Destroy(ring.gameObject);
        }
    }
}
