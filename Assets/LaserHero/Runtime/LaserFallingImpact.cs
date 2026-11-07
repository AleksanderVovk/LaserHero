using System.Collections.Generic;
using UnityEngine;

namespace LaserHero
{
    public class LaserFallingImpact : MonoBehaviour
    {
        public int voxelCount;
        public int DestroyedByImpact { get; private set; }
        public int ContactReports { get; private set; }
        public float MaximumContactSpeed { get; private set; }
        public int QualifiedReports { get; private set; }
        public float LastImpactRadius { get; private set; }
        public float LastImpactEnergy { get; private set; }
        public int PendingContacts => pending.Count;
        public int SelfDestroyed { get; private set; }
        public float SizeFactor => Mathf.Clamp01(EquivalentVoxels / 4000f);

        float EquivalentVoxels => source != null ? source.SolidWorldVolume / .125f : Mathf.Max(0, voxelCount);

        float GenerationRetention => Mathf.Pow(LaserCollapseSettings.Current.generationRetention, source != null
            ? source.physicalGeneration : 0);
        public int HitBudget => Mathf.Clamp(Mathf.CeilToInt(EquivalentVoxels
            * LaserCollapseSettings.Current.voxelsPerHit * GenerationRetention), 1,
            LaserCollapseSettings.Current.maximumVoxelsPerHit);
        public int LifetimeBudget => Mathf.Clamp(Mathf.CeilToInt(EquivalentVoxels
            * LaserCollapseSettings.Current.voxelsPerLifetime * GenerationRetention), 1,
            LaserCollapseSettings.Current.maximumVoxelsPerLifetime);

        struct Impact
        {
            public Collider collider;
            public Vector3 p0, p1, p2, p3, normal, velocity, angular;
            public float speed, mass, created;
            public bool carried;
            public int count;
            public Vector3 Point(int i) => i == 0 ? p0 : i == 1 ? p1 : i == 2 ? p2 : p3;
            public void Add(Vector3 point)
            {
                if (count == 0)
                    p0 = point;
                else if (count == 1)
                    p1 = point;
                else if (count == 2)
                    p2 = point;
                else
                    p3 = point;
                count++;
            }
        }

        readonly List<Impact> pending = new List<Impact>(8);
        readonly List<LaserVoxelBody> targets = new List<LaserVoxelBody>(32);
        static readonly Collider[] overlaps = new Collider[256];
        static int damageFrame = -1, frameDestroyed;
        Rigidbody body;
        LaserVoxelBody source;
        Vector3 incomingVelocity, incomingAngular, incomingCenter;
        float nextImpact;
        float nextFracture, stressProbeAt, traumaUntil, traumaSpeed;
        Vector3 supportPoint, supportNormal;
        Collider supportingCollider;
        float residualEnergy, residualUntil;
        Vector3 residualVelocity, residualAngular;
        void Awake()
        {
            body = GetComponent<Rigidbody>();
            source = GetComponent<LaserVoxelBody>();
        }

        void OnDisable()
        {
            pending.Clear();
            residualEnergy = 0;
            supportingCollider = null;
            traumaUntil = 0;
        }

        void FixedUpdate()
        {
            if (body == null)
                return;
            // Work is queued by collision callbacks. The next simulation sees updated geometry
            // and the remaining incoming momentum, instead of treating a broken wall as solid.
            for (int n = 0; n < 2 && pending.Count > 0; n++)
            {
                if (FrameBudgetRemaining <= 0)
                    break;
                var impact = pending[0];
                pending.RemoveAt(0);
                if (Time.time - impact.created > .6f || !LaserArena.CanPhysicsDamage(impact.p0))
                    continue;
                if (impact.carried && Time.time >= residualUntil)
                    continue;
                int removed = 0, budget = HitBudget;
                float energy = .5f * impact.mass * impact.speed * impact.speed;
                if (Time.time < residualUntil)
                    energy = Mathf.Min(energy, residualEnergy);
                else
                    residualUntil = Time.time + .9f;
                residualEnergy = energy;
                if (energy < LaserCollapseSettings.Current.minimumEnergy)
                    continue;
                float effectiveMass = 2 * energy / (impact.speed * impact.speed);
                for (int i = 0; i < impact.count && removed < budget; i++)
                {
                    var target = impact.collider != null ? impact.collider.GetComponentInParent<LaserVoxelBody>() : null;
                    removed += Apply(impact.Point(i), impact.speed, -impact.normal, target,
                        effectiveMass / impact.count, budget - removed);
                }

                traumaSpeed = Mathf.Max(traumaSpeed, impact.speed);
                traumaUntil = Time.time + 2;
                stressProbeAt = Time.time + LaserCollapseSettings.Current.stuckFractureDelay;
                if (removed > 0)
                {
                    float retained = Mathf.Sqrt(Mathf.Clamp01(1 - removed
                        * LaserCollapseSettings.Current.energyPerVoxel / Mathf.Max(energy,
                        1))) * LaserCollapseSettings.Current.momentumRetention;
                    residualEnergy = energy * retained * retained;
                    residualVelocity = impact.velocity * retained;
                    residualAngular = impact.angular * retained;
                    // Restore only inward motion lost by the previous solver; never add speed.
                    float inward = Vector3.Dot(impact.velocity, -impact.normal) * retained;
                    float current = Vector3.Dot(body.linearVelocity, -impact.normal);
                    if (inward > current && inward > 0)
                        body.linearVelocity += -impact.normal * (inward - current);
                    if (body.angularVelocity.sqrMagnitude < impact.angular.sqrMagnitude * retained * retained)
                        body.angularVelocity = impact.angular * retained;
                    body.WakeUp();
                    LaserImpactFeedback.Collapse(impact.p0, voxelCount, impact.speed);
                    nextImpact = Time.time + LaserCollapseSettings.Current.contactInterval;
                }
                else
                {
                    residualEnergy *= .5f;
                    nextImpact = Time.time + LaserCollapseSettings.Current.contactInterval;
                }

                ApplySelfDamage(impact.p0, impact.normal, impact.speed, false);
                if (source.Remaining == 0)
                {
                    pending.Clear();
                    return;
                }
            }

            if (Time.time >= stressProbeAt && Time.time < traumaUntil && supportingCollider != null
                && supportingCollider.enabled && supportingCollider.gameObject.activeInHierarchy
                && body.linearVelocity.sqrMagnitude < 6.25f)
            {
                ApplySelfDamage(supportPoint, supportNormal, traumaSpeed * .8f, true);
                stressProbeAt = Time.time + LaserCollapseSettings.Current.stuckFractureDelay;
            }

            incomingVelocity = body.linearVelocity;
            incomingAngular = body.angularVelocity;
            incomingCenter = body.worldCenterOfMass;
        }

        void ApplySelfDamage(Vector3 point, Vector3 normal, float speed, bool delayed)
        {
            var settings = LaserCollapseSettings.Current;
            if (!settings.fractureFallingObjects || source == null || source.Remaining == 0
                || Time.time < nextFracture || FrameBudgetRemaining <= 0 || !LaserArena.CanPhysicsDamage(point))
                return;
            int crushed = 0;
            if (!delayed)
            {
                float cell = Mathf.Pow(Mathf.Max(.0001f, source.SolidWorldVolume / Mathf.Max(1, source.Remaining)), 1f / 3);
                crushed = source.Carve(point + normal * cell * .35f, cell * .9f,
                    source.hitPoints * Mathf.Clamp(speed / 6, .2f, 3), normal, Mathf.Min(FrameBudgetRemaining,
                    Mathf.Clamp(source.Remaining / 30, 1, 32)), true, source.physicalGeneration);
                source.FlushSurface();
                frameDestroyed += crushed;
            }

            int broken = source.FractureWeakSection(point, speed, FrameBudgetRemaining);
            frameDestroyed += broken;
            SelfDestroyed += crushed + broken;
            if (broken > 0)
                LaserImpactFeedback.Collapse(source.LastFracturePoint, voxelCount, speed);
            if (crushed + broken > 0)
                nextFracture = Time.time + settings.fractureCooldown;
        }

        static int FrameBudgetRemaining
        {
            get
            {
                if (damageFrame != Time.frameCount)
                {
                    damageFrame = Time.frameCount;
                    frameDestroyed = 0;
                }

                return Mathf.Max(0, LaserCollapseSettings.Current.maximumVoxelsPerFrame - frameDestroyed);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            Report(collision);
        }

        void OnCollisionStay(Collision collision)
        {
            Report(collision);
        }

        void OnCollisionExit(Collision collision)
        {
            if (collision.collider == supportingCollider)
                supportingCollider = null;
        }

        void Report(Collision collision)
        {
            if (body == null || source == null || collision.collider == null)
                return;
            var target = collision.collider.GetComponentInParent<LaserVoxelBody>();
            if (target == source || target != null && (!target.isActiveAndEnabled || !target.allowDestruction))
                return;
            if (target == null && (collision.collider.gameObject.layer == 2
                || collision.collider.GetComponentInParent<LaserHeroPlayer>() != null))
                return;
            if (collision.contactCount > 0)
            {
                var support = collision.GetContact(0);
                supportPoint = support.point;
                supportNormal = support.normal;
                supportingCollider = collision.collider;
            }

            if (Time.time < nextImpact || pending.Count >= 8)
                return;
            ContactReports++;
            foreach (var queued in pending)
                if (queued.collider == collision.collider)
                    return;
            var hit = new Impact
            {
                collider = collision.collider,
                velocity = incomingVelocity,
                angular = incomingAngular,
                created = Time.time
            };
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                var velocity = incomingVelocity + Vector3.Cross(incomingAngular, contact.point - incomingCenter);
                if (collision.rigidbody != null)
                    velocity -= collision.rigidbody.GetPointVelocity(contact.point);
                float speed = Mathf.Max(0, -Vector3.Dot(velocity, contact.normal));
                if (i == 0 || speed > hit.speed)
                {
                    hit.speed = speed;
                    hit.normal = contact.normal;
                }

                if (hit.count == 0)
                    hit.Add(contact.point);
            }

            // Spread samples across the whole contact manifold, not four neighboring triangles.
            while (hit.count > 0 && hit.count < 4)
            {
                float farthest = .25f;
                Vector3 chosen = Vector3.zero;
                for (int i = 0; i < collision.contactCount; i++)
                {
                    var point = collision.GetContact(i).point;
                    float nearest = float.MaxValue;
                    for (int j = 0; j < hit.count; j++)
                        nearest = Mathf.Min(nearest, (point - hit.Point(j)).sqrMagnitude);
                    if (nearest > farthest)
                    {
                        farthest = nearest;
                        chosen = point;
                    }
                }

                if (farthest <= .25f)
                    break;
                hit.Add(chosen);
            }

            // Speculative CCD can report a collision before a contact manifold is available.
            if (hit.count == 0)
            {
                var point = collision.collider.ClosestPoint(incomingCenter);
                hit.normal = (incomingCenter - point).normalized;
                if (hit.normal.sqrMagnitude < .001f)
                    hit.normal = -incomingVelocity.normalized;
                var velocity = incomingVelocity + Vector3.Cross(incomingAngular, point - incomingCenter);
                if (collision.rigidbody != null)
                    velocity -= collision.rigidbody.GetPointVelocity(point);
                hit.speed = Mathf.Max(0, -Vector3.Dot(velocity, hit.normal));
                hit.Add(point);
            }

            hit.mass = collision.rigidbody != null ? 1 / (1 / Mathf.Max(body.mass,
                .1f) + 1 / Mathf.Max(collision.rigidbody.mass, .1f)) : body.mass;
            // The solver may stop the body against a surface that is being crushed. Spend the
            // remaining energy of that same impact; resting weight never creates fresh energy.
            if (hit.speed <= LaserCollapseSettings.Current.minimumClosingSpeed && Time.time < residualUntil
                && residualEnergy >= LaserCollapseSettings.Current.minimumEnergy)
            {
                hit.speed = Mathf.Sqrt(2 * residualEnergy / hit.mass);
                hit.velocity = residualVelocity;
                hit.angular = residualAngular;
                hit.carried = true;
            }

            MaximumContactSpeed = Mathf.Max(MaximumContactSpeed, hit.speed);
            if (hit.count == 0 || hit.speed <= LaserCollapseSettings.Current.minimumClosingSpeed
                || !LaserArena.CanPhysicsDamage(hit.p0))
                return;
            if (.5f * hit.mass * hit.speed * hit.speed < LaserCollapseSettings.Current.minimumEnergy)
                return;
            QualifiedReports++;
            pending.Add(hit);
        }

        public int ApplyVoxelImpact(Vector3 point, float speed, Vector3 direction, LaserVoxelBody directTarget =
            null, float effectiveMass = -1) => Apply(point, speed, direction, directTarget, effectiveMass > 0
            ? effectiveMass : body != null ? body.mass : 0, HitBudget);
        int Apply(Vector3 point, float speed, Vector3 direction, LaserVoxelBody directTarget, float mass, int hitBudget)
        {
            var settings = LaserCollapseSettings.Current;
            if (body == null || source == null || !isActiveAndEnabled
                || source.physicalGeneration >= settings.maximumGeneration || speed <= settings.minimumClosingSpeed
                || DestroyedByImpact >= LifetimeBudget || !LaserArena.CanPhysicsDamage(point))
                return 0;
            float energy = .5f * mass * speed * speed;
            if (energy < settings.minimumEnergy || FrameBudgetRemaining <= 0)
                return 0;
            LastImpactEnergy = energy;
            float damage = Mathf.Min(settings.maximumDamage, (Mathf.Sqrt(energy) * .7f + speed * 2) * GenerationRetention);
            float cell = Mathf.Pow(Mathf.Max(.0001f, source.SolidWorldVolume / Mathf.Max(1, source.Remaining)), 1f / 3);
            float radius = Mathf.Clamp(cell * .75f + Mathf.Pow(source.SolidWorldVolume,
                1f / 3) * settings.radiusPerVolumeRoot * Mathf.Clamp(speed / 8, .5f, 1.5f), .3f, settings.maximumRadius);
            LastImpactRadius = radius;
            int energyBudget = energy / settings.energyPerVoxel >= settings.maximumVoxelsPerHit
                ? settings.maximumVoxelsPerHit : Mathf.FloorToInt(energy / settings.energyPerVoxel);
            int budget = Mathf.Min(Mathf.Min(hitBudget, LifetimeBudget - DestroyedByImpact),
                Mathf.Min(FrameBudgetRemaining, energyBudget));
            if (budget <= 0)
                return 0;
            var forceDirection = direction.sqrMagnitude > .001f ? direction.normalized : Vector3.down;
            Vector3 center = point + forceDirection * Mathf.Min(cell * .8f, radius * .35f);
            targets.Clear();
            if (directTarget != null && directTarget != source && directTarget.isActiveAndEnabled)
                targets.Add(directTarget);
            int count = Physics.OverlapSphereNonAlloc(center, radius, overlaps, 1, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var target = overlaps[i].GetComponentInParent<LaserVoxelBody>();
                overlaps[i] = null;
                if (target != null && target != source && target.isActiveAndEnabled
                    && target.allowDestruction && !targets.Contains(target))
                    targets.Add(target);
            }

            int removed = 0;
            foreach (var target in targets)
            {
                if (target == null || removed >= budget)
                    break;
                removed += target.Carve(center, radius, damage, forceDirection, budget - removed, true, source.physicalGeneration);
                target.FlushSurface();
            }

            DestroyedByImpact += removed;
            frameDestroyed += removed;
            if (removed > 0 && settings.awardPhysicsRewards && LaserImpactFeedback.Instance != null
                && LaserImpactFeedback.Instance.hero != null)
            {
                var laser = LaserImpactFeedback.Instance.hero.GetComponent<LaserEyes>();
                if (laser != null && laser.progress != null)
                {
                    laser.progress.Reward(removed);
                    laser.progress.GainXP(removed * .15f);
                }
            }

            return removed;
        }
    }
}
