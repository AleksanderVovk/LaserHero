using UnityEngine;

namespace LaserHero
{
    [CreateAssetMenu(menuName = "Laser Hero/Collapse physics settings")]
    public class LaserCollapseSettings : ScriptableObject
    {
        [Header("Weight (solid world volume, not the bounding box)")]
        [Min(1)]
        public float density = 80;
        [Min(1)]
        public float maximumMass = 100000;
        [Header("Impact strength")]
        [Min(.1f)]
        public float minimumClosingSpeed = 1.5f;
        [Min(1)]
        public float minimumEnergy = 45;
        [Min(1)]
        public float energyPerVoxel = 100;
        [Min(.1f)]
        public float radiusPerVolumeRoot = .38f;
        [Min(.3f)]
        public float maximumRadius = 6;
        [Min(1)]
        public float maximumDamage = 1500;
        [Header("Destruction proportional to volume (standard 0.5 m voxels)")]
        [Min(.01f)]
        public float voxelsPerHit = .8f;
        [Min(.01f)]
        public float voxelsPerLifetime = 4;
        [Min(1)]
        public int maximumVoxelsPerHit = 4000;
        [Min(1)]
        public int maximumVoxelsPerLifetime = 24000;
        [Min(1)]
        public int maximumVoxelsPerFrame = 4096;
        [Range(1, 8)]
        public int maximumGeneration = 4;
        [Range(.1f, 1)]
        public float generationRetention = .65f;
        [Header("Penetration and contact patches")]
        [Range(0, 1)]
        public float momentumRetention = .85f;
        [Min(.02f)]
        public float contactInterval = .12f;
        [Header("Impact damage and weak sections of the falling object")]
        public bool fractureFallingObjects = true;
        [Min(1)]
        public float fractureSpeed = 12;
        [Min(1)]
        public float minimumFractureEnergy = 400;
        [Min(1)]
        public float minimumFractureAspect = 2;
        [Min(1)]
        public int maximumFractureVoxels = 512;
        [Range(1, 4)]
        public int maximumSelfFractures = 2;
        [Min(.1f)]
        public float fractureCooldown = .45f;
        [Min(.1f)]
        public float stuckFractureDelay = .4f;
        public bool awardPhysicsRewards = true;
        static LaserCollapseSettings fallback;
        public static LaserCollapseSettings Current
        {
            get
            {
                if (LaserArena.Instance != null && LaserArena.Instance.collapseSettings != null)
                    return LaserArena.Instance.collapseSettings;
                if (fallback == null)
                {
                    fallback = CreateInstance<LaserCollapseSettings>();
                    fallback.hideFlags = HideFlags.HideAndDontSave;
                }

                return fallback;
            }
        }
    }
}
