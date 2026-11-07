using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Globalization;

namespace LaserHero
{
    public enum LaserTrainingAccess
    {
        Free,
        Rebirths,
        IAP
    }

    [ExecuteAlways]
    public class LaserTrainingTarget : MonoBehaviour
    {
        [Tooltip("Permanent save ID. Give each lane a unique number from 0 to 31.")]
        [Range(0, 31)]
        public int tier;
        public Transform aimPoint;
        public float radius = 3.6f;
        [Header("Training")]
        [Tooltip("0 keeps the old multiplier determined by the lane ID.")]
        [Min(0)]
        public float trainingMultiplier;
        [Header("Lane access")]
        public LaserTrainingAccess access = LaserTrainingAccess.Free;
        [Tooltip("Required total rebirths. Nothing is spent; the lane unlocks automatically.")]
        [Min(0)]
        public int requiredRebirths = 2;
        public string iapProductId = "laser_hero_training_";
        [Min(.01f)]
        public double iapPrice = 2.99;
        [Header("World price plate")]
        public Canvas priceCanvas;
        public TMP_Text priceLabel, multiplierLabel;
        public UnityEngine.UI.Image rebirthIcon;
        public UnityEngine.UI.Button purchaseButton;
        public float Multiplier => trainingMultiplier > 0 ? trainingMultiplier : tier == 0 ? 1.5f : tier == 1 ? 2
            : tier == 2 ? 4 : 6;

        LaserProgress progress;
        LaserIAPStore store;
        float nextRefresh;
        public static readonly List<LaserTrainingTarget> All = new List<LaserTrainingTarget>();
        void OnEnable()
        {
            if (!All.Contains(this))
                All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        void Start()
        {
            progress = FindFirstObjectByType<LaserProgress>();
            if (progress != null)
                store = progress.GetComponent<LaserIAPStore>();
            if (purchaseButton != null)
                purchaseButton.onClick.AddListener(TryUnlock);
            RefreshPlate();
        }

        public bool IsUnlocked(LaserProgress player) => access == LaserTrainingAccess.Free || (player != null
            && (access == LaserTrainingAccess.Rebirths ? player.Data.rebirths >= requiredRebirths : player.OwnsTarget(tier)));
        public void TryUnlock()
        {
            if (progress == null)
                progress = FindFirstObjectByType<LaserProgress>();
            if (progress == null || IsUnlocked(progress))
                return;
            if (access == LaserTrainingAccess.Rebirths)
                progress.Notify("Нужно перерождений: " + requiredRebirths);
            else if (access == LaserTrainingAccess.IAP)
            {
                if (store == null)
                    store = progress.GetComponent<LaserIAPStore>();
                if (store != null)
                    store.Buy(LaserStoreKind.Training, tier);
            }

            RefreshPlate();
        }

        public bool TryOfferPurchaseOnEntry()
        {
            if (access != LaserTrainingAccess.IAP)
                return false;
            if (progress == null)
                progress = FindFirstObjectByType<LaserProgress>();
            if (progress == null || IsUnlocked(progress))
                return false;
            if (store == null)
                store = progress.GetComponent<LaserIAPStore>();
            // Wait silently for the store connection or another checkout to finish.
            if (store == null || !store.CanBuy(LaserStoreKind.Training, tier))
                return false;
            TryUnlock();
            return true;
        }

        void Update()
        {
            if (!Application.isPlaying)
            {
                RefreshPlate();
                return;
            }

            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + .2f;
                RefreshPlate();
            }
        }

        public void RefreshPlate()
        {
            if (priceLabel == null)
                return;
            if (priceCanvas != null && priceCanvas.worldCamera == null)
                priceCanvas.worldCamera = Camera.main;
            bool icon = access != LaserTrainingAccess.IAP;
            if (rebirthIcon != null)
                rebirthIcon.gameObject.SetActive(icon);
            priceLabel.rectTransform.offsetMin = new Vector2(icon ? 84 : 12, 8);
            priceLabel.rectTransform.offsetMax = new Vector2(-12, -8);
            priceLabel.text = access == LaserTrainingAccess.Free ? "Бесплатно" : icon
                ? requiredRebirths.ToString(CultureInfo.InvariantCulture) : Application.isPlaying && store != null
                ? store.Price(LaserStoreKind.Training, tier) : iapPrice.ToString("0.00", CultureInfo.InvariantCulture);
            bool owned = IsUnlocked(progress);
            priceLabel.color = owned && access != LaserTrainingAccess.Free ? new Color(.5f, 1, .6f) : Color.white;
            if (multiplierLabel != null)
                multiplierLabel.text = "x" + Multiplier.ToString("0.##", CultureInfo.InvariantCulture) + " Мощность";
            if (purchaseButton != null)
                purchaseButton.interactable = access == LaserTrainingAccess.IAP && !owned && store != null
                    && store.CanBuy(LaserStoreKind.Training, tier);
        }
    }
}
