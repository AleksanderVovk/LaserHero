using System;
using System.Linq;
using UnityEngine;

namespace LaserHero
{
    public enum LaserStoreKind
    {
        Laser,
        Aura,
        Drone,
        Training,
        Boost,
        Money,
        Energy
    }

    [Serializable]
    public class LaserStoreItem
    {
        public LaserStoreKind kind;
        public int index;
        public string productId;
        [Min(.01f)]
        public double previewPrice = .99;
        [Tooltip("Exact currency amount per consumable purchase. Decimal string, no separators.")]
        public string grantAmount = "";
        public bool IsConsumable => kind == LaserStoreKind.Money || kind == LaserStoreKind.Energy;
    }

    [CreateAssetMenu(menuName = "Laser Hero/Google Play product catalogue")]
    public class LaserStoreCatalog : ScriptableObject
    {
        [Tooltip("Google Play Console > Monetization setup > licensing public key. Required for receipt validation.")]
        [TextArea(3, 6)]
        public string googlePlayPublicKey = "";
        public LaserStoreItem[] items = Array.Empty<LaserStoreItem>();
        public LaserStoreItem Find(LaserStoreKind kind, int index) => items.FirstOrDefault(p => p.kind == kind && p.index == index);
        public LaserStoreItem Find(string id) => items.FirstOrDefault(p => p.productId == id);
        public bool IsValid(out string reason)
        {
            if (items == null || items.Length == 0)
            {
                reason = "Каталог покупок пуст";
                return false;
            }

            if (items.Any(p => p == null || !Enum.IsDefined(typeof(LaserStoreKind), p.kind)
                || string.IsNullOrWhiteSpace(p.productId) || p.previewPrice <= 0 || double.IsNaN(p.previewPrice)
                || double.IsInfinity(p.previewPrice) || p.index < 0 || p.index >= (p.kind == LaserStoreKind.Laser
                ? LaserProgress.Names.Length : p.kind == LaserStoreKind.Aura ? LaserProgress.AuraNames.Length
                : (p.kind == LaserStoreKind.Drone || p.kind == LaserStoreKind.Boost) ? 2 : 32)))
            {
                reason = "Проверьте ID, цену и индекс товаров";
                return false;
            }

            if (items.Select(p => p.productId).Distinct().Count() != items.Length || items.Select(p =>
                (p.kind, p.index)).Distinct().Count() != items.Length)
            {
                reason = "В каталоге есть повторяющиеся товары";
                return false;
            }

            try
            {
                if (items.Any(p => p.IsConsumable && LaserAmount.Parse(p.grantAmount) <= 0))
                {
                    reason = "Укажите положительную сумму пакета";
                    return false;
                }
            }
            catch (Exception)
            {
                reason = "Неверная сумма пакета";
                return false;
            }

            reason = "";
            return true;
        }
    }
}
