using System.Collections.Generic;
using UnityEngine;

namespace LaserHero
{
    public class LaserDroneCompanion : MonoBehaviour
    {
        public LaserDroneSettings settings;
        public LaserHeroPlayer hero;
        public LaserProgress progress;
        public const float ShotDuration = .25f;
        public int ShotsFired { get; private set; }
        public int BlocksDestroyed { get; private set; }
        public int Equipped => active;
        public bool BeamVisible => beam != null && beam.enabled;
        public Transform Visual => visual != null ? visual.transform : null;

        int active = -1;
        GameObject visual;
        LaserDroneVisual model;
        LineRenderer beam, core;
        AudioSource buzz, shot;
        Vector3 beamEnd;
        float nextShot, beamUntil, angle;
        readonly List<LaserVoxelBody> candidates = new List<LaserVoxelBody>(128);
        void Start()
        {
            if (hero == null)
                hero = GetComponent<LaserHeroPlayer>();
            if (progress == null)
                progress = GetComponent<LaserProgress>();
            beam = MakeBeam("Drone beam", .12f);
            core = MakeBeam("Drone beam core", .035f);
        }

        LineRenderer MakeBeam(string title, float width)
        {
            var go = new GameObject(title);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = settings.beam;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = line.endWidth = width;
            line.numCapVertices = 3;
            line.enabled = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return line;
        }

        void Update()
        {
            if (settings == null || hero == null || progress == null || beam == null)
                return;
            int wanted = progress.Data.drone;
            if (!progress.OwnsDrone(wanted))
                wanted = -1;
            if (wanted != active)
                Switch(wanted);
            if (visual == null)
                return;
            var def = settings.drones[active];
            angle += Time.deltaTime * (active == 0 ? 28 : 22);
            Vector3 offset = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad)) * def.orbitDistance;
            Vector3 destination = hero.transform.position + offset + Vector3.up * (2.8f + Mathf.Sin(Time.time * 2.8f) * .15f);
            if ((visual.transform.position - destination).sqrMagnitude > 100)
                visual.transform.position = destination;
            else
                visual.transform.position = Vector3.Lerp(visual.transform.position, destination,
                    1 - Mathf.Exp(-7 * Time.deltaTime));
            bool visible = Time.time < beamUntil && !hero.MenuOpen && LaserArena.Instance != null && LaserArena.Instance.Inside;
            beam.enabled = core.enabled = visible;
            if (visible)
            {
                Vector3 origin = model.muzzle.position;
                beam.SetPosition(0, origin);
                beam.SetPosition(1, beamEnd);
                core.SetPosition(0, origin);
                core.SetPosition(1, beamEnd);
            }
            else
                visual.transform.rotation = Quaternion.Slerp(visual.transform.rotation, Quaternion.Euler(0,
                    angle + 90, 0), Time.deltaTime * 3);
            if (hero.MenuOpen || LaserArena.Instance == null || !LaserArena.Instance.Inside)
            {
                beamUntil = 0;
                return;
            }

            if (Time.time >= nextShot)
            {
                nextShot = Time.time + def.cooldown;
                if (TryTarget(def, out var hit))
                    Fire(def, hit);
            }
        }

        void Switch(int index)
        {
            if (visual != null)
                Destroy(visual);
            active = index;
            beam.enabled = core.enabled = false;
            beamUntil = 0;
            if (index < 0)
                return;
            var def = settings.drones[index];
            visual = Instantiate(def.prefab);
            visual.name = def.title + " — companion";
            model = visual.GetComponent<LaserDroneVisual>();
            visual.transform.position = hero.transform.position + Vector3.up * 3;
            buzz = visual.AddComponent<AudioSource>();
            buzz.clip = settings.buzz;
            buzz.loop = true;
            buzz.spatialBlend = 1;
            buzz.minDistance = 3;
            buzz.maxDistance = 18;
            buzz.dopplerLevel = 0;
            buzz.volume = .12f;
            buzz.pitch = index == 0 ? 1.25f : .95f;
            buzz.Play();
            shot = visual.AddComponent<AudioSource>();
            shot.clip = settings.shot;
            shot.spatialBlend = 1;
            shot.minDistance = 4;
            shot.maxDistance = 25;
            shot.volume = .4f;
            shot.dopplerLevel = 0;
            beam.startColor = beam.endColor = def.color;
            core.startColor = core.endColor = Color.white;
            nextShot = Time.time + .6f;
        }

        bool TryTarget(LaserDroneDefinition def, out RaycastHit hit)
        {
            hit = default;
            candidates.Clear();
            bool buildings = Random.value < .8f;
            // Prefer scenery, but fall back to ground when no building is nearby.
            for (int pass = 0; pass < 2 && candidates.Count == 0; pass++)
                foreach (var body in LaserVoxelBody.Active)
                {
                    if (body == null || !body.allowDestruction || body.Remaining <= 0 || (pass == 0 && buildings && body.terrain))
                        continue;
                    var collider = body.GetComponent<MeshCollider>();
                    if (collider == null || !collider.enabled)
                        continue;
                    Vector3 closest = collider.bounds.ClosestPoint(hero.transform.position);
                    if ((closest - hero.transform.position).sqrMagnitude <= def.range * def.range)
                        candidates.Add(body);
                }

            for (int attempt = 0; attempt < 18 && candidates.Count > 0; attempt++)
            {
                var body = candidates[Random.Range(0, candidates.Count)];
                Vector3 size = (Vector3)body.dimensions * body.cellSize;
                Vector3 point = body.transform.TransformPoint(new Vector3(Random.Range(0, size.x),
                    Random.Range(0, size.y), Random.Range(0, size.z)));
                Vector3 origin = model.muzzle.position, direction = point - origin;
                if (Physics.Raycast(origin, direction.normalized, out var found,
                    Mathf.Min(direction.magnitude + 1, def.range + def.orbitDistance), 1, QueryTriggerInteraction.Ignore))
                {
                    var actual = found.collider.GetComponent<LaserVoxelBody>();
                    if (actual != null && actual.allowDestruction
                        && LaserArena.CanDamage(actual, found.point) && (found.point
                        - hero.transform.position).sqrMagnitude <= def.range * def.range)
                    {
                        hit = found;
                        return true;
                    }
                }
            }

            return false;
        }

        void Fire(LaserDroneDefinition def, RaycastHit hit)
        {
            Vector3 direction = (hit.point - model.muzzle.position).normalized;
            beamEnd = hit.point;
            beamUntil = Time.time + ShotDuration;
            visual.transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            var body = hit.collider.GetComponent<LaserVoxelBody>();
            float damage = Mathf.Clamp(progress.Damage * def.damageMultiplier, def.minimumDamage, def.maximumDamage);
            int removed = body.Carve(hit.point + direction * .1f, def.blastRadius, damage, direction, def.maximumBlocks);
            ShotsFired++;
            BlocksDestroyed += removed;
            if (removed > 0)
                progress.Reward(removed);
            LaserImpactFeedback.Hit(hit.point, removed, direction);
            shot.pitch = Random.Range(.95f, 1.1f);
            shot.PlayOneShot(settings.shot);
            beam.enabled = core.enabled = true;
            beam.SetPosition(0, model.muzzle.position);
            beam.SetPosition(1, beamEnd);
            core.SetPosition(0, model.muzzle.position);
            core.SetPosition(1, beamEnd);
        }

        void OnDisable()
        {
            if (visual != null)
                Destroy(visual);
            active = -1;
            if (beam != null)
                beam.enabled = false;
            if (core != null)
                core.enabled = false;
        }
    }
}
