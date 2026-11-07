using System;
using UnityEngine;

namespace LaserHero
{
    [Serializable]
    public class LaserDroneDefinition
    {
        public string title;
        public GameObject prefab;
        public Sprite icon;
        public Color color = Color.cyan;
        [Min(.25f)]
        public float cooldown = 2;
        [Min(1)]
        public float range = 12;
        [Min(.5f)]
        public float orbitDistance = 2.5f;
        public float damageMultiplier = .75f, minimumDamage = 20, maximumDamage = 240, blastRadius = .55f;
        [Min(1)]
        public int maximumBlocks = 8;
    }

    [CreateAssetMenu(menuName = "Laser Hero/Drones")]
    public class LaserDroneSettings : ScriptableObject
    {
        public LaserDroneDefinition[] drones = new LaserDroneDefinition[2];
        public AudioClip buzz, shot;
        public Material beam;
    }
}
