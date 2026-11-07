using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

namespace LaserHero
{
    [DisallowMultipleComponent]
    public class LaserIAPStore : MonoBehaviour
    {
        public LaserStoreCatalog catalog;
        public LaserProgress progress;
        public bool Ready { get; private set; }
        public string Status { get; private set; } = "Магазин не подключён";

        StoreController store;
        CrossPlatformValidator validator;
        readonly Dictionary<string, Product> products = new Dictionary<string, Product>();
        readonly HashSet<string> deferred = new HashSet<string>();
        string busyProduct;
        bool connecting;
        bool ownsRuntimeCatalog;
        public static void ConfigureTrainingProducts(LaserStoreCatalog target, IEnumerable<LaserTrainingTarget> lanes)
        {
            var active = lanes.ToArray();
            if (active.GroupBy(l => l.tier).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Training lanes must have unique save IDs (tier)");
            var items = target.items.Where(p => p.kind != LaserStoreKind.Training).ToList();
            foreach (var lane in active)
                if (lane.access == LaserTrainingAccess.IAP)
                    items.Add(new LaserStoreItem
                    {
                        kind = LaserStoreKind.Training,
                        index = lane.tier,
                        productId = lane.iapProductId,
                        previewPrice = lane.iapPrice
                    });
            target.items = items.ToArray();
        }

        // Editor and Windows use the explicit Fake Store; Android still validates Google Play receipts.
        public bool IsTestStore
        {
            get
            {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                return true;
#else
                return false;
#endif
            }
        }

        public bool IsEditorTestStore => IsTestStore; // Compatibility with existing editor tools.

        async void Awake()
        {
            if (progress == null)
                progress = GetComponent<LaserProgress>();
            await Connect();
        }

        void OnEnable()
        {
            // Unity can keep the Ready flag across a Play Mode script reload while dropping services.
            if (Application.isPlaying && store == null && !connecting)
            {
                Ready = false;
                _ = Connect();
            }
        }

        async System.Threading.Tasks.Task Connect()
        {
            if (connecting || Ready)
                return;
            if (!IsTestStore && Application.platform != RuntimePlatform.Android)
            {
                Status = "Оплата доступна в Google Play на Android";
                return;
            }

            if (catalog != null)
            {
                if (!ownsRuntimeCatalog)
                {
                    catalog = Instantiate(catalog);
                    ownsRuntimeCatalog = true;
                }

                try
                {
                    ConfigureTrainingProducts(catalog, FindObjectsByType<LaserTrainingTarget>(FindObjectsSortMode.None));
                }
                catch (Exception e)
                {
                    Status = "Проверьте ID тренировочных дорожек";
                    Debug.LogWarning(e.Message);
                    return;
                }
            }

            if (catalog == null || !catalog.IsValid(out var reason))
            {
                Status = "Проверьте каталог Google Play";
                return;
            }

            if (!IsTestStore && string.IsNullOrWhiteSpace(catalog.googlePlayPublicKey))
            {
                Status = "Добавьте публичный ключ Google Play в каталог";
                return;
            }

            connecting = true;
            try
            {
                if (!IsTestStore)
                    validator = new CrossPlatformValidator(Convert.FromBase64String(catalog.googlePlayPublicKey.Trim()), Application.identifier);
                if (store == null)
                {
                    store = UnityIAPServices.StoreController(IsTestStore ? "fake" : null);
                    store.OnStoreConnected += Connected;
                    store.OnStoreDisconnected += Disconnected;
                    store.OnProductsFetched += ProductsFetched;
                    store.OnProductsFetchFailed += ProductsFailed;
                    store.OnPurchasePending += Pending;
                    store.OnPurchaseConfirmed += Confirmed;
                    store.OnPurchaseFailed += Failed;
                    store.OnPurchaseDeferred += Deferred;
                    store.OnPurchasesFetched += PurchasesFetched;
                    store.OnPurchasesFetchFailed += PurchasesFailed;
                    store.OnAuthAccountChanged += AccountChanged;
                    store.ProcessPendingOrdersOnPurchasesFetched(false);
                }

                Status = IsTestStore ? "Подключение к тестовому магазину Unity" : "Подключение к Google Play";
                await store.Connect();
            }
            catch (Exception e)
            {
                Status = "Не удалось подключить Google Play";
                Debug.LogWarning("Laser Hero IAP: " + e.Message);
            }
            finally
            {
                connecting = false;
            }
        }

        void Connected()
        {
            store.FetchProducts(catalog.items.Select(p => new ProductDefinition(p.productId, p.IsConsumable
                ? ProductType.Consumable : ProductType.NonConsumable)).ToList());
        }

        void Disconnected(StoreConnectionFailureDescription failure)
        {
            if (busyProduct != null && progress != null)
                progress.ReportPurchaseRejected();
            Ready = false;
            busyProduct = null;
            Status = "Google Play недоступен. Попробуйте позже";
        }

        void ProductsFetched(List<Product> fetched)
        {
            products.Clear();
            foreach (var p in fetched)
                if (p.availableToPurchase)
                    products[p.definition.id] = p;
            Ready = true;
            Status = IsTestStore ? "Тестовый магазин Unity — без списания денег" : "Google Play подключён";
            store.FetchPurchases();
        }

        void ProductsFailed(ProductFetchFailed failure)
        {
            Status = "Товары недоступны: проверьте Google Play Console";
        }

        void PurchasesFailed(PurchasesFetchFailureDescription failure)
        {
            busyProduct = null;
            Status = "Не удалось восстановить покупки. Попробуйте позже";
        }

        void AccountChanged()
        {
            Ready = false;
            products.Clear();
            deferred.Clear();
            busyProduct = null;
            Connected();
        }

        public bool Owned(LaserStoreKind kind, int index) => kind != LaserStoreKind.Money
            && kind != LaserStoreKind.Energy && progress != null && (kind == LaserStoreKind.Laser
            ? progress.OwnsLaser(index) : kind == LaserStoreKind.Aura ? progress.OwnsAura(index)
            : kind == LaserStoreKind.Drone ? progress.OwnsDrone(index) : kind == LaserStoreKind.Boost
            ? progress.OwnsBoost(index) : progress.OwnsTarget(index));
        public bool CanBuy(LaserStoreKind kind, int index)
        {
            var item = catalog != null ? catalog.Find(kind, index) : null;
            return Ready && item != null && products.ContainsKey(item.productId) && busyProduct == null
                && !deferred.Contains(item.productId) && !Owned(kind, index);
        }

        public string Price(LaserStoreKind kind, int index)
        {
            var item = catalog != null ? catalog.Find(kind, index) : null;
            if (item == null)
                return "—";
            return !IsTestStore && products.TryGetValue(item.productId, out var product)
                ? product.metadata.localizedPrice.ToString("0.00", CultureInfo.InvariantCulture)
                : item.previewPrice.ToString("0.00", CultureInfo.InvariantCulture);
        }

        public void Buy(LaserStoreKind kind, int index)
        {
            if (!CanBuy(kind, index))
            {
                progress.ReportPurchaseRejected();
                progress.Notify(Owned(kind, index) ? "Этот предмет уже открыт" : Status);
                return;
            }

            var item = catalog.Find(kind, index);
            busyProduct = item.productId;
            try
            {
                store.PurchaseProduct(products[item.productId]);
            }
            catch (Exception)
            {
                busyProduct = null;
                progress.ReportPurchaseRejected();
                progress.Notify("Не удалось начать покупку. Попробуйте позже");
            }
        }

        public async void RestorePurchases()
        {
            if (!Ready)
            {
                await Connect();
                if (!Ready)
                {
                    progress.Notify(Status);
                    return;
                }
            }

            store.FetchPurchases();
            progress.Notify(IsTestStore ? "Восстанавливаем тестовые покупки…" : "Восстанавливаем покупки Google Play…");
        }

        void Pending(PendingOrder order)
        {
            if (ValidateAndGrant(order, true))
            {
                store.ConfirmPurchase(order);
                progress.Notify("Покупка получена!", NotificationType.Success);
            }

            busyProduct = null;
        }

        void Confirmed(Order order)
        {
            busyProduct = null;
            if (order is FailedOrder)
                progress.Notify("Покупка сохранена. Подтверждение повторится при подключении");
        }

        void Failed(FailedOrder order)
        {
            busyProduct = null;
            foreach (var item in order.CartOrdered.Items())
                deferred.Remove(item.Product.definition.id);
            progress.ReportPurchaseRejected();
            progress.Notify("Покупка не завершена");
        }

        void Deferred(DeferredOrder order)
        {
            foreach (var item in order.CartOrdered.Items())
                deferred.Add(item.Product.definition.id);
            busyProduct = null;
            progress.Notify("Покупка ожидает подтверждения");
        }

        void PurchasesFetched(Orders orders)
        {
            foreach (var order in orders.ConfirmedOrders)
                ValidateAndGrant(order, false);
            foreach (var order in orders.PendingOrders)
                Pending(order);
        }

        bool ValidateAndGrant(Order order, bool equip)
        {
            if ((validator == null && !IsTestStore) || progress == null || progress.SuppressSaving)
                return false;
            string previous = JsonUtility.ToJson(progress.Data);
            var previousLevel = progress.Data.Level;
            bool newPurchase = false;
            try
            {
                var receipts = IsTestStore ? Array.Empty<GooglePlayReceipt>()
                    : validator.Validate(order.Info.Receipt).OfType<GooglePlayReceipt>().Where(r =>
                    r.purchaseState == GooglePurchaseState.Purchased).ToArray();
                var grants = new List<(LaserStoreItem item, string transaction, int quantity)>();
                foreach (var cart in order.CartOrdered.Items())
                {
                    var item = catalog.Find(cart.Product.definition.id);
                    if (item == null)
                        throw new InvalidOperationException("Unknown product");
                    if (item.IsConsumable && !equip)
                        continue; // Historical confirmed purchases never re-grant currency.
                    if (cart.Quantity <= 0)
                        throw new InvalidOperationException("Invalid purchase quantity");
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                    if (IsTestStore)
                    {
                        if (string.IsNullOrEmpty(order.Info.TransactionID))
                            throw new InvalidOperationException("Missing fake transaction ID");
                        grants.Add((item, (Application.isEditor ? "editor-test:"
                            : "windows-test:") + order.Info.TransactionID + ":" + item.productId, cart.Quantity));
                        continue;
                    }

#endif
                    var receipt = receipts.FirstOrDefault(r => r.productID == item.productId
                        && (string.IsNullOrEmpty(order.Info.TransactionID) || r.transactionID == order.Info.TransactionID));
                    if (receipt == null || string.IsNullOrEmpty(receipt.transactionID))
                        throw new InvalidOperationException("Receipt does not match purchase");
                    grants.Add((item, receipt.transactionID + ":" + item.productId, cart.Quantity));
                }

                if (grants.Count == 0)
                    return false;
                if (progress.Data.iapTransactions == null)
                    progress.Data.iapTransactions = new List<string>();
                foreach (var grant in grants)
                {
                    bool unseen = !progress.Data.iapTransactions.Contains(grant.transaction);
                    GrantTransaction(progress.Data, grant.item, grant.transaction, grant.quantity, equip);
                    newPurchase |= unseen;
                    deferred.Remove(grant.item.productId);
                }

                progress.Save(); // PlayerPrefs.Save completes before acknowledgement.
                if (equip && newPurchase)
                {
                    progress.ReportPurchaseSucceeded();
                    if (progress.Data.Level > previousLevel)
                        progress.ReportLevelRaised();
                }

                return true;
            }
            catch (Exception e)
            {
                progress.Data = JsonUtility.FromJson<LaserSave>(previous);
                progress.ReportPurchaseRejected();
                progress.Notify("Покупка не подтверждена. Попробуйте восстановить её позже");
                Debug.LogWarning("Laser Hero IAP: " + e.Message);
                return false;
            }
        }

        public static bool GrantTransaction(LaserSave data, LaserStoreItem item, string transaction, int quantity, bool equip)
        {
            if (string.IsNullOrEmpty(transaction))
                throw new InvalidOperationException("Missing transaction ID");
            if (data.iapTransactions == null)
                data.iapTransactions = new List<string>();
            bool previous = data.iapTransactions.Contains(transaction);
            if (item.IsConsumable && previous)
                return false;
            ApplyEntitlement(data, item, equip && !previous, quantity);
            if (!previous)
                data.iapTransactions.Add(transaction);
            return true;
        }

        public static void ApplyEntitlement(LaserSave data, LaserStoreItem item, bool equip, int quantity = 1)
        {
            if (item.IsConsumable)
            {
                if (quantity <= 0)
                    throw new ArgumentOutOfRangeException(nameof(quantity));
                var amount = LaserAmount.Parse(item.grantAmount) * quantity;
                if (amount <= 0)
                    throw new InvalidOperationException("Empty currency reward");
                if (item.kind == LaserStoreKind.Money)
                    data.Cash += amount;
                else
                    LaserLevelCurve.AddXP(data, amount);
                return;
            }

            int count = item.kind == LaserStoreKind.Laser ? LaserProgress.Names.Length
                : item.kind == LaserStoreKind.Aura ? LaserProgress.AuraNames.Length : (item.kind == LaserStoreKind.Drone
                || item.kind == LaserStoreKind.Boost) ? 2 : 32;
            if (item.index < 0 || item.index >= count)
                throw new ArgumentOutOfRangeException(nameof(item.index));
            if (item.kind == LaserStoreKind.Laser)
            {
                data.ownedLasers |= 1 << item.index;
                if (equip)
                    data.equipped = item.index;
            }
            else if (item.kind == LaserStoreKind.Aura)
            {
                data.ownedAuras |= 1 << item.index;
                if (equip)
                    data.aura = item.index;
            }
            else if (item.kind == LaserStoreKind.Drone)
            {
                data.ownedDrones |= 1 << item.index;
                if (equip)
                    data.drone = item.index;
            }
            else if (item.kind == LaserStoreKind.Boost)
            {
                data.ownedBoosts |= 1 << item.index;
            }
            else
            {
                data.ownedTargets |= 1 << item.index;
            }
        }

        void OnDestroy()
        {
            if (ownsRuntimeCatalog && catalog != null)
                Destroy(catalog);
            if (store == null)
                return;
            store.OnStoreConnected -= Connected;
            store.OnStoreDisconnected -= Disconnected;
            store.OnProductsFetched -= ProductsFetched;
            store.OnProductsFetchFailed -= ProductsFailed;
            store.OnPurchasePending -= Pending;
            store.OnPurchaseConfirmed -= Confirmed;
            store.OnPurchaseFailed -= Failed;
            store.OnPurchaseDeferred -= Deferred;
            store.OnPurchasesFetched -= PurchasesFetched;
            store.OnPurchasesFetchFailed -= PurchasesFailed;
            store.OnAuthAccountChanged -= AccountChanged;
        }
    }
}
