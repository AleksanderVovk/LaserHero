using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LaserHero
{
    public class LaserEyes : MonoBehaviour
    {
        public LaserHeroPlayer hero;
        public LaserProgress progress;
        public Material beamMaterial;
        public Material impactParticleMaterial, dustMaterial;
        public LaserHitMarkerEffect hitMarker;
        [Min(.05f)]
        public float damageCooldown = .5f;
        public float EffectiveCooldown => damageCooldown * progress.CooldownMultiplier;
        public int DamageTickCount { get; private set; }
        public float LastDamageAt { get; private set; } = -100;
        public Vector3 LastImpactPoint { get; private set; }

        public bool touchFire;
        public bool AutoFire;
        public LaserTrainingTarget Training { get; private set; }
        public LaserTrainingTarget Nearby { get; private set; }
        public LaserVoxelBody LastBody { get; private set; }
        public int Destroyed { get; private set; }
        public bool IsFiring { get; private set; }

        LineRenderer[] beams = new LineRenderer[4];
        ParticleSystem sparks;
        ParticleSystem dust;
        AudioSource hum;
        public bool LaserLoopPlaying => hum != null && hum.isPlaying;

        float nextDamageTick, nextTrainingTick;
        bool trainingBlockedUntilExit;
        LaserTrainingTarget enteredTarget;
        bool trainingPurchaseOffered;
        public Vector3? TestAim;
        public Vector2 AimScreen { get; private set; }

        int firingFinger = -1;
        float tapUntil;
        LaserArenaAbilities abilities;
        readonly List<RaycastResult> pointerHits = new List<RaycastResult>(16);
        PointerEventData aimPointer;
        EventSystem aimEventSystem;
        bool BlocksAim(Vector2 position, int pointerId = -1)
        {
            var system = EventSystem.current;
            if (system == null)
                return false;
            if (aimPointer == null || aimEventSystem != system)
            {
                aimEventSystem = system;
                aimPointer = new PointerEventData(system);
            }

            aimPointer.Reset();
            aimPointer.position = position;
            aimPointer.pointerId = pointerId;
            pointerHits.Clear();
            system.RaycastAll(aimPointer, pointerHits);
            foreach (var hit in pointerHits)
            {
                if (!(hit.module is UnityEngine.UI.GraphicRaycaster))
                    continue;
                var surface = hit.gameObject.GetComponentInParent<LaserAimSurface>();
                if (surface != null && surface.isActiveAndEnabled)
                    continue;
                return true;
            }

            return false;
        }

        GameObject impactGlow;
        Material impactMaterial;
        public void SetAimPoint(Vector2 point)
        {
            AimScreen = point;
        }

        void Start()
        {
            abilities = GetComponent<LaserArenaAbilities>();
            if (beamMaterial == null)
                beamMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject(i < 2 ? "Eye laser glow" : "Eye laser core");
                go.transform.SetParent(transform);
                var l = go.AddComponent<LineRenderer>();
                l.sharedMaterial = beamMaterial;
                l.positionCount = 2;
                l.numCapVertices = 5;
                l.useWorldSpace = true;
                l.enabled = false;
                beams[i] = l;
            }

            var impact = new GameObject("Laser impact sparks");
            sparks = impact.AddComponent<ParticleSystem>();
            var main = sparks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.07f, .22f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6);
            main.startSize = new ParticleSystem.MinMaxCurve(.04f, .13f);
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = .6f;
            var emission = sparks.emission;
            emission.rateOverTime = 0;
            var shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .12f;
            var r = sparks.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = impactParticleMaterial != null ? impactParticleMaterial : beamMaterial;
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = .35f;
            r.velocityScale = .05f;
            var cloud = new GameObject("Short impact dust");
            dust = cloud.AddComponent<ParticleSystem>();
            var dm = dust.main;
            dm.startLifetime = new ParticleSystem.MinMaxCurve(.16f, .32f);
            dm.startSpeed = .6f;
            dm.startSize = new ParticleSystem.MinMaxCurve(.15f, .3f);
            dm.startColor = new Color(.75f, .78f, .82f, .3f);
            dm.simulationSpace = ParticleSystemSimulationSpace.World;
            dm.maxParticles = 30;
            var de = dust.emission;
            de.rateOverTime = 0;
            dust.GetComponent<ParticleSystemRenderer>().sharedMaterial = dustMaterial != null ? dustMaterial : beamMaterial;
            hum = gameObject.AddComponent<AudioSource>();
            hum.playOnAwake = false;
            hum.loop = true;
            hum.volume = .24f;
            hum.spatialBlend = 0;
            if (LaserImpactFeedback.Instance != null)
                hum.clip = LaserImpactFeedback.Instance.laserLoop;
            AimScreen = new Vector2(Screen.width * .5f, Screen.height * .5f);
            impactGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impactGlow.name = "Laser impact core";
            impactGlow.layer = 2;
            Destroy(impactGlow.GetComponent<Collider>());
            impactMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            impactGlow.GetComponent<Renderer>().sharedMaterial = impactMaterial;
            impactGlow.SetActive(false);
        }

        void Update()
        {
            Nearby = null;
            float best = 100;
            foreach (var target in LaserTrainingTarget.All)
            {
                Vector3 d = target.transform.position - hero.transform.position;
                d.y = 0;
                if (d.sqrMagnitude < target.radius * target.radius && d.sqrMagnitude < best)
                {
                    Nearby = target;
                    best = d.sqrMagnitude;
                }
            }

            if (Nearby == null)
                trainingBlockedUntilExit = false;
            if (Nearby != enteredTarget)
            {
                enteredTarget = Nearby;
                trainingPurchaseOffered = false;
            }

            if (Nearby != null && !hero.MenuOpen && !trainingPurchaseOffered)
                trainingPurchaseOffered = Nearby.TryOfferPurchaseOnEntry();
            Training = Nearby != null && Nearby.IsUnlocked(progress) && !trainingBlockedUntilExit ? Nearby : null;
            bool pointerFire = false;
            if (!Application.isMobilePlatform)
            {
                if (Input.GetMouseButton(0) && !BlocksAim(Input.mousePosition))
                {
                    AimScreen = Input.mousePosition;
                    pointerFire = true;
                }
            }
            else
            {
                foreach (var touch in Input.touches)
                {
                    bool blocked = touch.phase == TouchPhase.Began && BlocksAim(touch.position, touch.fingerId);
                    if (touch.phase == TouchPhase.Began && !blocked && firingFinger < 0)
                    {
                        firingFinger = touch.fingerId;
                        tapUntil = Time.time + .2f;
                    }

                    if (touch.fingerId != firingFinger)
                        continue;
                    AimScreen = touch.position;
                    pointerFire = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
                    if (!pointerFire)
                        firingFinger = -1;
                }
            }

            bool fire = !hero.MenuOpen && (Training != null || touchFire || AutoFire || pointerFire || Time.time < tapUntil);
            IsFiring = fire;
            if (!fire)
            {
                foreach (var l in beams)
                    l.enabled = false;
                impactGlow.SetActive(false);
                if (hitMarker != null)
                    hitMarker.Hide();
                if (hum.isPlaying)
                    hum.Stop();
                return;
            }

            if (hum.clip != null && !hum.isPlaying)
                hum.Play();
            Vector3 end;
            bool impactHit = false;
            RaycastHit hit = default;
            if (Training != null)
            {
                end = Training.aimPoint.position;
                impactHit = true;
            }
            else
            {
                Ray ray = hero.view.ScreenPointToRay(AimScreen);
                if (TestAim.HasValue)
                    ray = new Ray(hero.view.transform.position, (TestAim.Value - hero.view.transform.position).normalized);
                impactHit = Physics.Raycast(ray, out hit, 100, 1, QueryTriggerInteraction.Ignore);
                end = impactHit ? hit.point : ray.GetPoint(100);
            }

            bool superBurst = abilities != null && abilities.BurstActive;
            Color color = superBurst ? Color.HSVToRGB(Mathf.Repeat(Time.time * 2, 1), .8f, 1) : progress.BeamColor;
            for (int i = 0; i < 4; i++)
            {
                float pulse = Mathf.Clamp01(1 - (Time.time - LastDamageAt) / .12f);
                var l = beams[i];
                l.enabled = true;
                l.startWidth = l.endWidth = (i < 2 ? 1
                    : .3f) * (.045f + progress.BaseRadius * .17f) * progress.LaserSizeMultiplier * (1 + pulse * .45f)
                    * (superBurst ? 2 : 1);
                l.startColor = l.endColor = i < 2 ? color : Color.Lerp(color, Color.white, .8f);
                Vector3 origin = hero.EyeCenter + hero.transform.right * (i % 2 == 0 ? -.10f : .10f);
                // Eye rays cannot burn through a wall that blocks the character even if the camera sees past it.
                if (Training == null && Physics.Raycast(origin, (end - origin).normalized,
                    out var obstruction, Vector3.Distance(origin, end) - .05f, 1, QueryTriggerInteraction.Ignore))
                {
                    hit = obstruction;
                    end = hit.point;
                    impactHit = true;
                }

                l.SetPosition(0, origin);
                l.SetPosition(1, end);
            }

            sparks.transform.position = end;
            dust.transform.position = end;
            if (hitMarker != null)
            {
                if (impactHit)
                    hitMarker.ShowHit(end, Training != null ? (hero.EyeCenter - end).normalized : hit.normal, color);
                else
                    hitMarker.Hide();
            }

            float flash = Mathf.Clamp01(1 - (Time.time - LastDamageAt) / .13f);
            impactGlow.SetActive(impactHit && hitMarker == null);
            impactGlow.transform.position = end;
            impactGlow.transform.localScale = Vector3.one * (.12f + progress.Radius * .22f + flash * .7f);
            impactMaterial.SetColor("_BaseColor", Color.Lerp(color, Color.white, .95f));
            if (Training != null)
            {
                if (Time.time >= nextTrainingTick)
                {
                    nextTrainingTick = Time.time + EffectiveCooldown;
                    LastDamageAt = Time.time;
                    progress.GainXP(Training.Multiplier * progress.TrainingGain);
                }

                return;
            }

            LastBody = impactHit && hit.collider != null ? hit.collider.GetComponent<LaserVoxelBody>() : null;
            bool validTarget = LastBody != null && LastBody.allowDestruction && LaserArena.CanDamage(LastBody, end);
            if (validTarget && abilities != null)
                abilities.ChargeLaser(Time.deltaTime);
            if (Time.time < nextDamageTick)
                return;
            nextDamageTick = Time.time + EffectiveCooldown;
            if (validTarget)
            {
                DamageTickCount++;
                LastDamageAt = Time.time;
                LastImpactPoint = end;
                Vector3 direction = (end - hero.EyeCenter).normalized;
                bool superShot = abilities != null && abilities.TrySuperStrike(end, direction, hit.normal);
                int removed = superShot ? abilities.LastDestroyed
                    : LastBody.Carve(end + direction * .12f, progress.Radius, progress.Damage, direction);
                LaserImpactFeedback.Hit(end, removed, direction);
                LaserImpactFeedback.LaserFire(end, hit.normal, removed);
                if (hitMarker == null)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        var p = new ParticleSystem.EmitParams
                        {
                            startColor = Color.Lerp(color, Color.white, .75f),
                            velocity = hit.normal * Random.Range(1.5f, 4) + Random.insideUnitSphere * 2
                        };
                        sparks.Emit(p, 1);
                    }

                    dust.Emit(3);
                }

                progress.GainXP(progress.TrainingGain);
                if (removed > 0)
                {
                    Destroyed += removed;
                    progress.Reward(removed);
                }
            }
        }

        public void InteractTarget()
        {
            if (Nearby == null)
                return;
            if (!Nearby.IsUnlocked(progress))
                Nearby.TryUnlock();
            else
                trainingBlockedUntilExit = !trainingBlockedUntilExit;
        }

        void OnDisable()
        {
            if (hum != null)
                hum.Stop();
            if (hitMarker != null)
                hitMarker.Hide();
            foreach (var l in beams)
                if (l != null)
                    l.enabled = false;
        }

        void OnDestroy()
        {
            if (sparks != null)
                Destroy(sparks.gameObject);
            if (dust != null)
                Destroy(dust.gameObject);
            if (impactGlow != null)
                Destroy(impactGlow);
            if (impactMaterial != null)
                Destroy(impactMaterial);
        }
    }
}
