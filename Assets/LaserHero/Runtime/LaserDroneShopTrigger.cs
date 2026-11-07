using UnityEngine;

namespace LaserHero
{
    [RequireComponent(typeof(BoxCollider))]
    public class LaserDroneShopTrigger : MonoBehaviour
    {
        public int featuredDrone;
        LaserHeroHUD hud;
        bool entered;
        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<LaserHeroPlayer>() == null || entered)
                return;
            entered = true;
            if (hud == null)
                hud = FindFirstObjectByType<LaserHeroHUD>();
            if (hud != null)
                hud.OpenDrones(featuredDrone);
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<LaserHeroPlayer>() != null)
                entered = false;
        }
    }
}
