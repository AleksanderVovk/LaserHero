using UnityEngine;

namespace LaserHero
{
    public class LaserLobbyKiosk : MonoBehaviour
    {
        public int menu;
        LaserHeroHUD hud;
        void Update()
        {
            if (hud == null)
                hud = FindFirstObjectByType<LaserHeroHUD>();
            if (hud == null || hud.hero.MenuOpen || !Input.GetKeyDown(KeyCode.E))
                return;
            if (Vector3.Distance(hud.hero.transform.position, transform.position) > 4)
                return;
            if (menu == 0)
                hud.ToggleShop();
            else if (menu == 1)
                hud.ToggleUpgrade();
            else if (menu == 2)
                hud.ToggleAuras();
            else
                hud.ToggleRebirth();
        }
    }
}
