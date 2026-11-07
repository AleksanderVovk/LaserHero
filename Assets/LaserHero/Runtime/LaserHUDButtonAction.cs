using UnityEngine;

namespace LaserHero
{
    public enum LaserHUDAction
    {
        None,
        ToggleShop,
        ToggleAuras,
        ToggleUpgrade,
        ToggleRebirth,
        ToggleStore,
        CloseMenus,
        BuyLaser,
        BuyAura,
        BuyUpgrade,
        Drone,
        PremiumLaser,
        PremiumAura,
        Restore,
        Rebirth,
        Category,
        Explosion,
        Flight,
        Jump,
        Reset,
        ToggleSettings
    }

    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class LaserHUDButtonAction : MonoBehaviour
    {
        public LaserHeroHUD hud;
        public LaserHUDAction action;
        public int index;
        void OnEnable()
        {
            var b = GetComponent<UnityEngine.UI.Button>();
            b.onClick.RemoveListener(Execute);
            b.onClick.AddListener(Execute);
        }

        void OnDisable()
        {
            GetComponent<UnityEngine.UI.Button>().onClick.RemoveListener(Execute);
        }

        public void Execute()
        {
            if (hud == null)
                return;
            var progress = hud.progress;
            var store = progress.GetComponent<LaserIAPStore>();
            switch (action)
            {
                case LaserHUDAction.ToggleShop:
                    hud.ToggleShop();
                    break;
                case LaserHUDAction.ToggleAuras:
                    hud.ToggleAuras();
                    break;
                case LaserHUDAction.ToggleUpgrade:
                    hud.ToggleUpgrade();
                    break;
                case LaserHUDAction.ToggleRebirth:
                    hud.ToggleRebirth();
                    break;
                case LaserHUDAction.ToggleStore:
                    hud.ToggleStore();
                    break;
                case LaserHUDAction.ToggleSettings:
                    hud.ToggleSettings();
                    break;
                case LaserHUDAction.CloseMenus:
                    hud.CloseMenus();
                    break;
                case LaserHUDAction.BuyLaser:
                    progress.BuyLaser(index);
                    break;
                case LaserHUDAction.BuyAura:
                    progress.BuyAura(index);
                    break;
                case LaserHUDAction.BuyUpgrade:
                    progress.BuyUpgrade(index);
                    break;
                case LaserHUDAction.Drone:
                    if (progress.OwnsDrone(index))
                        progress.EquipDrone(index);
                    else if (store != null)
                        store.Buy(LaserStoreKind.Drone, index);
                    break;
                case LaserHUDAction.PremiumLaser:
                    if (store != null)
                        store.Buy(LaserStoreKind.Laser, index);
                    break;
                case LaserHUDAction.PremiumAura:
                    if (store != null)
                        store.Buy(LaserStoreKind.Aura, index);
                    break;
                case LaserHUDAction.Restore:
                    if (store != null)
                        store.RestorePurchases();
                    break;
                case LaserHUDAction.Rebirth:
                    if (progress.Rebirth())
                        hud.CloseMenus();
                    break;
                case LaserHUDAction.Category:
                    if (index == 0)
                        hud.ToggleShop();
                    else if (index == 1)
                        hud.ToggleAuras();
                    else
                    {
                        hud.CloseMenus();
                        LaserArena.Instance.Lobby();
                        var free = LaserTrainingTarget.All.Find(t => t != null && t.access == LaserTrainingAccess.Free);
                        if (free != null)
                        {
                            hud.hero.Teleport(free.transform.position + free.transform.forward * 2 + Vector3.up);
                            hud.hero.yaw = free.transform.eulerAngles.y + 180;
                            hud.hero.pitch = 6;
                        }
                    }

                    break;
                case LaserHUDAction.Explosion:
                    break; // Former manual explosion is now an automatic charged laser shot.
                case LaserHUDAction.Flight:
                    hud.hero.ToggleFlight();
                    break;
                case LaserHUDAction.Jump:
                    hud.hero.touchJump = true;
                    break;
                case LaserHUDAction.Reset:
                    hud.CloseMenus();
                    hud.eyes.AutoFire = false;
                    hud.eyes.touchFire = false;
                    if (LaserImpactFeedback.Instance != null)
                        LaserImpactFeedback.Instance.ClearFires();
                    progress.ResetProgress();
                    if (LaserArena.Instance != null)
                    {
                        LaserArena.Instance.Lobby();
                        LaserArena.Instance.ResetArena();
                    }

                    break;
            }
        }
    }
}
