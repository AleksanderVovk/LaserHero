using TMPro;
using UnityEngine;

namespace LaserHero
{
    public enum LaserPermanentBoost
    {
        DoubleSize,
        DoubleHitSpeed
    }

    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
    public class LaserBoostTrigger : MonoBehaviour
    {
        public LaserPermanentBoost boost;
        public LaserIAPStore store;
        public TMP_Text titleLabel, priceLabel;
        public Transform floatingIcon;
        public Color accent = Color.cyan;
        [Min(0)]
        public float bobHeight = .12f;
        [Min(0)]
        public float bobSpeed = 1.5f;
        bool offered;
        Vector3 iconOrigin;
        float refreshAt;
        string lastPrice;
        BoxCollider triggerVolume;
        LaserHeroPlayer player;
        Collider playerCollider;
        public bool Occupied { get; private set; }
        public bool PurchaseOffered => offered;

        void Awake()
        {
            triggerVolume = GetComponent<BoxCollider>();
            if (store == null)
                store = FindFirstObjectByType<LaserIAPStore>();
            if (floatingIcon != null)
                iconOrigin = floatingIcon.localPosition;
            RefreshLabels();
        }

        void Update()
        {
            if (store != null && store.Owned(LaserStoreKind.Boost, (int)boost))
            {
                gameObject.SetActive(false);
                return;
            }

            // Read the current position instead of relying on trigger callbacks, which
            // CharacterController teleports and collider toggles can omit.
            if (player == null)
                player = FindFirstObjectByType<LaserHeroPlayer>();
            if ((playerCollider == null || !playerCollider.enabled) && player != null)
            {
                playerCollider = player.GetComponent<CharacterController>();
                if (playerCollider == null || !playerCollider.enabled)
                    foreach (var collider in player.GetComponents<Collider>())
                        if (collider.enabled)
                        {
                            playerCollider = collider;
                            break;
                        }
            }

            Occupied = triggerVolume.enabled && playerCollider != null && playerCollider.enabled
                && playerCollider.gameObject.activeInHierarchy && triggerVolume.bounds.Intersects(playerCollider.bounds);
            if (!Occupied)
                offered = false;
            if (floatingIcon != null)
                floatingIcon.localPosition = iconOrigin + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
            if (Time.unscaledTime >= refreshAt)
            {
                RefreshLabels();
                refreshAt = Time.unscaledTime + .25f;
            }

            if (!Occupied || offered || store == null || store.Owned(LaserStoreKind.Boost, (int)boost))
                return;
            if (player != null && player.MenuOpen)
                return;
            if (!store.CanBuy(LaserStoreKind.Boost, (int)boost))
                return;
            offered = true;
            store.Buy(LaserStoreKind.Boost, (int)boost);
        }

        void RefreshLabels()
        {
            string price = store != null && store.Owned(LaserStoreKind.Boost, (int)boost) ? "Активно"
                : store != null ? store.Price(LaserStoreKind.Boost, (int)boost) : "—";
            if (price == lastPrice)
                return;
            lastPrice = price;
            if (priceLabel != null)
            {
                priceLabel.text = price;
                priceLabel.color = price == "Активно" ? new Color(.4f, 1f, .2f) : Color.white;
            }

            if (titleLabel != null)
            {
                titleLabel.text = boost == LaserPermanentBoost.DoubleSize ? "2x Размер лазера" : "2x Скорость лазера";
                titleLabel.color = accent;
            }
        }

        void OnDisable()
        {
            Occupied = false;
            offered = false;
        }
    }
}
