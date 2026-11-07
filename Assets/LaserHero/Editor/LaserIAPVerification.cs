using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserIAPVerification
    {
        public static string Run()
        {
            var lines = new List<string>();
            void Check(bool ok, string text)
            {
                if (!ok)
                    throw new Exception(text);
                lines.Add("PASS: " + text);
            }

            var store = UnityEngine.Object.FindFirstObjectByType<LaserIAPStore>();
            var hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            var catalog = store.catalog;
            try
            {
                Check(catalog.IsValid(out _) && catalog.items.Length >= 13,
                    "Permanent products and configured training lanes have valid unique settings");
                foreach (var item in catalog.items.Where(p => !p.IsConsumable))
                {
                    var data = new LaserSave
                    {
                        cash = 123,
                        pending = 5
                    };
                    LaserIAPStore.ApplyEntitlement(data, item, true);
                    Check(item.kind == LaserStoreKind.Laser
                        ? (data.ownedLasers & (1 << item.index)) != 0 && data.equipped == item.index
                        : item.kind == LaserStoreKind.Aura ? (data.ownedAuras & (1 << item.index)) != 0
                        && data.aura == item.index : item.kind == LaserStoreKind.Drone
                        ? (data.ownedDrones & (1 << item.index)) != 0 && data.drone == item.index
                        : item.kind == LaserStoreKind.Boost ? (data.ownedBoosts & (1 << item.index)) != 0
                        : (data.ownedTargets & (1 << item.index)) != 0, "Product maps to the correct entitlement: "
                        + item.productId);
                    string once = JsonUtility.ToJson(data);
                    LaserIAPStore.ApplyEntitlement(data, item, true);
                    Check(once == JsonUtility.ToJson(data) && data.cash == 123
                        && data.pending == 5, "Repeated delivery is idempotent: " + item.productId);
                    Check(System.Text.RegularExpressions.Regex.IsMatch(store.Price(item.kind,
                        item.index), "^[0-9]+[.][0-9]{2}$"), "Numeric price has no currency icon: " + item.productId);
                }

                var saved = new LaserSave();
                saved.iapTransactions.Add("verified-transaction:product");
                Check(JsonUtility.FromJson<LaserSave>(JsonUtility.ToJson(saved))
                    .iapTransactions.Single() == "verified-transaction:product", "Transaction journal survives save round-trip");
                Check(store.IsEditorTestStore && store.Ready, "Editor Fake Store is connected without a Google public key");
                Check(catalog.items.All(p => store.CanBuy(p.kind, p.index) == !store.Owned(p.kind,
                    p.index)), "Fake Store enables purchases only for unowned products");
                hud.CloseMenus();
                hud.ToggleShop();
                Canvas.ForceUpdateCanvases();
                foreach (string window in new[]
                {
                    "Laser shop",
                    "Auras"
                }

                )
                {
                    var modal = hud.transform.Find(window);
                    var rows = modal.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true).content;
                    foreach (Transform row in rows)
                    {
                        var premium = (RectTransform)row.Find("Premium purchase");
                        var regular = row.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                            .Single(b => b.transform != premium).GetComponent<RectTransform>();
                        Check(premium.anchoredPosition.y + premium.sizeDelta.y * .5f
                            < regular.anchoredPosition.y - regular.sizeDelta.y * .5f,
                            "Donation button sits below the ordinary action: " + row.name);
                        Check(regular.anchorMin.y == .5f && premium.anchorMin.y == .5f
                            && Mathf.Abs(regular.anchoredPosition.y + premium.anchoredPosition.y) < .01f
                            && regular.sizeDelta.x == premium.sizeDelta.x, "Button pair is centred vertically with matching widths: " + row.name);
                        Check(premium.GetComponent<UnityEngine.UI.Button>() != null, "Test purchase button exists: " + row.name);
                    }
                }

                hud.CloseMenus();
                return "PASS (" + lines.Count + " checks)";
            }
            finally
            {
                Directory.CreateDirectory("LaserHeroQA");
                File.WriteAllLines("LaserHeroQA/iap-verification.txt", lines);
            }
        }
    }
}
