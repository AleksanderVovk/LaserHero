using UnityEngine;

namespace LaserHero
{
    // A pooled physical carrier for the artist's FallenFire effect.
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class LaserFallenFire : MonoBehaviour
    {
        public AudioClip fireLoop;
        public float lifetime = 8;
        public bool Active => gameObject.activeSelf;
        public int LandingCount { get; private set; }
        public Rigidbody Body { get; private set; }
        public AudioSource Sound { get; private set; }

        ParticleSystem[] particles;
        float started;
        bool stopping;
        Vector3 originalScale;
        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            particles = GetComponentsInChildren<ParticleSystem>(true);
            originalScale = transform.localScale;
            Sound = GetComponent<AudioSource>();
            if (Sound == null)
                Sound = gameObject.AddComponent<AudioSource>();
            Sound.playOnAwake = false;
            Sound.loop = true;
            Sound.spatialBlend = 1;
            Sound.minDistance = 2;
            Sound.maxDistance = 24;
            Sound.rolloffMode = AudioRolloffMode.Linear;
            Sound.dopplerLevel = .25f;
            Sound.priority = 180;
        }

        public void Launch(Vector3 point, Vector3 velocity)
        {
            gameObject.SetActive(true);
            started = Time.time;
            stopping = false;
            LandingCount = 0;
            transform.SetPositionAndRotation(point, Quaternion.identity);
            transform.localScale = originalScale * Random.Range(.55f, .8f);
            Body.position = point;
            Body.rotation = Quaternion.identity;
            Body.linearVelocity = velocity;
            Body.angularVelocity = Vector3.zero;
            Body.WakeUp();
            foreach (var ps in particles)
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(false);
            }

            Sound.clip = fireLoop;
            Sound.volume = .32f;
            Sound.pitch = Random.Range(.94f, 1.06f);
            if (fireLoop != null)
            {
                Sound.time = Random.Range(0, Mathf.Max(0, fireLoop.length - .1f));
                Sound.Play();
            }
        }

        void Update()
        {
            float left = lifetime - (Time.time - started);
            if (left <= 1.2f && !stopping)
            {
                stopping = true;
                foreach (var ps in particles)
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }

            Sound.volume = .32f * Mathf.Clamp01(left / 1.2f);
            if (left <= 0 || transform.position.y < -15 || !LaserArena.CanPhysicsDamage(transform.position))
                Release();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.gameObject.layer != 2)
                LandingCount++;
        }

        public void Release()
        {
            if (Sound != null)
                Sound.Stop();
            if (particles != null)
                foreach (var ps in particles)
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (Body != null)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
                Body.Sleep();
            }

            gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (Sound != null)
                Sound.Stop();
        }
    }
}
