using System.Collections.Generic;
using UnityEngine;

namespace LaserHero
{
    public enum LaserTriggerMenu
    {
        [InspectorName("Магазин")]
        Shop,
        [InspectorName("Магазин лазеров")]
        Lasers,
        [InspectorName("Магазин аур")]
        Auras,
        [InspectorName("Перерождение")]
        Rebirth,
        [InspectorName("Улучшения")]
        Upgrades
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public sealed class LaserMenuTrigger : MonoBehaviour
    {
        public LaserTriggerMenu menu = LaserTriggerMenu.Lasers;
        public LaserHeroHUD hud;
        readonly HashSet<Collider> occupants = new HashSet<Collider>();
        void Reset()
        {
            ConfigurePhysics();
        }

        void Awake()
        {
            ConfigurePhysics();
        }

        void ConfigurePhysics()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<LaserHeroPlayer>();
            if (player == null)
                return;
            bool first = occupants.Count == 0;
            if (!occupants.Add(other) || !first)
                return;
            if (hud == null)
                foreach (var candidate in FindObjectsByType<LaserHeroHUD>(FindObjectsSortMode.None))
                    if (candidate.hero == player && candidate.transform.childCount > 0)
                    {
                        hud = candidate;
                        break;
                    }

            OpenMenu();
        }

        void OnTriggerExit(Collider other)
        {
            occupants.Remove(other);
        }

        void OnDisable()
        {
            occupants.Clear();
        }

        // Opening is explicit rather than toggling: re-entry never closes an already open menu.
        public void OpenMenu()
        {
            if (hud == null)
                return;
            hud.CloseMenus();
            switch (menu)
            {
                case LaserTriggerMenu.Shop:
                    hud.ToggleStore();
                    break;
                case LaserTriggerMenu.Lasers:
                    hud.ToggleShop();
                    break;
                case LaserTriggerMenu.Auras:
                    hud.ToggleAuras();
                    break;
                case LaserTriggerMenu.Rebirth:
                    hud.ToggleRebirth();
                    break;
                case LaserTriggerMenu.Upgrades:
                    hud.ToggleUpgrade();
                    break;
            }
        }
    }
}
