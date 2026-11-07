using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LaserHero.Editor
{
    public static class LaserTrainingLaneVerification
    {
        const string FakeBackup = "LaserHeroQA/training-iap-save-backup.json";
        public static async void RunFakeAutomatically()
        {
            bool started = false;
            try
            {
                BeginFakePurchase();
                started = true;
                var store = UnityEngine.Object.FindFirstObjectByType<LaserIAPStore>();
                for (int i = 0; i < 100 && !store.CanBuy(LaserStoreKind.Training, 31); i++)
                    await System.Threading.Tasks.Task.Delay(20);
                BuyFake();
                await System.Threading.Tasks.Task.Delay(100);
                var window = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .Single(w => w.GetType().Name == "UIFakeStoreWindow");
                window.SendMessage("OnOkClicked");
                await System.Threading.Tasks.Task.Delay(100);
                File.WriteAllText("LaserHeroQA/training-fake-store-result.txt", EndFakePurchase());
            }
            catch (Exception e)
            {
                if (started)
                {
                    var player = UnityEngine.Object.FindFirstObjectByType<LaserProgress>();
                    if (player != null)
                    {
                        player.Data = JsonUtility.FromJson<LaserSave>(File.ReadAllText(FakeBackup));
                        player.Save();
                    }

                    var temporary = GameObject.Find("Training IAP verification temporary");
                    if (temporary != null)
                        UnityEngine.Object.DestroyImmediate(temporary);
                }

                Directory.CreateDirectory("LaserHeroQA");
                File.WriteAllText("LaserHeroQA/training-fake-store-result.txt", "FAILED: " + e.Message);
            }
        }

        public static string BeginFakePurchase()
        {
            var player = UnityEngine.Object.FindFirstObjectByType<LaserProgress>();
            var store = player.GetComponent<LaserIAPStore>();
            if (!Application.isPlaying || !store.Ready || !store.IsEditorTestStore)
                throw new Exception("Start Play Mode and wait for Fake Store");
            Directory.CreateDirectory("LaserHeroQA");
            File.WriteAllText(FakeBackup, JsonUtility.ToJson(player.Data));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LaserHero/Training/TrainingLane_IAP.prefab");
            var clone = UnityEngine.Object.Instantiate(prefab, Vector3.up * 1000, Quaternion.identity);
            clone.name = "Training IAP verification temporary";
            clone.GetComponent<LaserTrainingTarget>().tier = 31;
            LaserIAPStore.ConfigureTrainingProducts(store.catalog,
                UnityEngine.Object.FindObjectsByType<LaserTrainingTarget>(FindObjectsSortMode.None));
            store.SendMessage("Connected");
            return "Temporary IAP lane added to Fake Store";
        }

        public static string BuyFake()
        {
            var store = UnityEngine.Object.FindFirstObjectByType<LaserIAPStore>();
            if (!store.CanBuy(LaserStoreKind.Training, 31))
                throw new Exception("Test IAP lane is not purchasable");
            store.Buy(LaserStoreKind.Training, 31);
            return "Fake Store confirmation opened";
        }

        public static string EndFakePurchase()
        {
            var player = UnityEngine.Object.FindFirstObjectByType<LaserProgress>();
            var original = JsonUtility.FromJson<LaserSave>(File.ReadAllText(FakeBackup));
            var temporary = GameObject.Find("Training IAP verification temporary");
            bool passed = temporary != null && temporary.GetComponent<LaserTrainingTarget>().IsUnlocked(player)
                && player.OwnsTarget(31) && player.Data.rebirths == original.rebirths && player.Data.cash == original.cash;
            player.Data = original;
            player.Save();
            if (temporary != null)
                UnityEngine.Object.Destroy(temporary);
            if (!passed)
                throw new Exception("Fake Store failed to unlock the lane correctly");
            File.AppendAllText("LaserHeroQA/training-lanes-verification.txt",
                "PASS: Fake Store purchase unlocks training lane and preserves rebirth count and cash\nPASS: Original player save restored after purchase test\n");
            return "PASS: Fake Store grants training access; original save restored";
        }

        public static string Run()
        {
            var lines = new List<string>();
            var clones = new List<GameObject>();
            var player = UnityEngine.Object.FindFirstObjectByType<LaserProgress>();
            var original = player.Data;
            LaserStoreCatalog catalog = null;
            void Check(bool condition, string message)
            {
                if (!condition)
                    throw new Exception(message);
                lines.Add("PASS: " + message);
            }

            try
            {
                string[] names =
                {
                    "Free",
                    "Rebirths2",
                    "Rebirths5",
                    "IAP"
                };
                foreach (string name in names)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LaserHero/Training/TrainingLane_" + name + ".prefab");
                    Check(prefab != null, "Template exists: " + name);
                    var clone = UnityEngine.Object.Instantiate(prefab);
                    clone.transform.position = Vector3.up * 1000;
                    clones.Add(clone);
                }

                var lanes = clones.Select(g => g.GetComponent<LaserTrainingTarget>()).ToArray();
                player.Data = new LaserSave();
                Check(lanes[0].IsUnlocked(player), "Free lane works from a fresh save without a purchase bit");
                foreach (var lane in lanes.Where(t => t.access == LaserTrainingAccess.Rebirths))
                {
                    player.Data.rebirths = lane.requiredRebirths - 1;
                    player.Data.ownedTargets = -1;
                    Check(!lane.IsUnlocked(player), "Below threshold remains locked even with old purchase bits: " + lane.requiredRebirths);
                    player.Data.rebirths = lane.requiredRebirths;
                    float power = player.PowerMultiplier;
                    string before = JsonUtility.ToJson(player.Data);
                    Check(lane.IsUnlocked(player), "Lane opens automatically at threshold: " + lane.requiredRebirths);
                    Check(before == JsonUtility.ToJson(player.Data)
                        && power == player.PowerMultiplier, "Access does not spend rebirths or alter bonuses: "
                        + lane.requiredRebirths);
                    player.Data.rebirths++;
                    Check(lane.IsUnlocked(player), "Access remains open above threshold: " + lane.requiredRebirths);
                }

                player.Data = new LaserSave
                {
                    rebirths = 20
                };
                Check(!lanes[3].IsUnlocked(player), "Rebirth count cannot bypass an IAP-only lane");
                var grant = new LaserStoreItem
                {
                    kind = LaserStoreKind.Training,
                    index = 3,
                    productId = lanes[3].iapProductId,
                    previewPrice = lanes[3].iapPrice
                };
                LaserIAPStore.ApplyEntitlement(player.Data, grant, true);
                Check(lanes[3].IsUnlocked(player) && player.Data.rebirths == 20,
                    "IAP grants permanent training access without changing rebirths");
                string saved = JsonUtility.ToJson(player.Data);
                LaserIAPStore.ApplyEntitlement(player.Data, grant, false);
                Check(saved == JsonUtility.ToJson(player.Data), "Restored training entitlement is idempotent");
                player.Data = JsonUtility.FromJson<LaserSave>(saved);
                Check(lanes[3].IsUnlocked(player), "Training purchase survives save round trip");
                player.Data = new LaserSave();
                Check(!lanes[1].IsUnlocked(player) && !lanes[3].IsUnlocked(player), "Reset locks gated and purchased lanes again");
                foreach (var lane in lanes)
                {
                    lane.RefreshPlate();
                    Check(lane.rebirthIcon.gameObject.activeSelf == (lane.access
                        != LaserTrainingAccess.IAP), "Icon is hidden only for IAP: " + lane.name);
                    Check(lane.priceLabel.text == (lane.access == LaserTrainingAccess.Free
                        ? "Бесплатно" : lane.access == LaserTrainingAccess.Rebirths ? lane.requiredRebirths.ToString()
                        : "4.99"), "Correct free/threshold/numeric IAP label: " + lane.name);
                }

                catalog = UnityEngine.Object.Instantiate(player.GetComponent<LaserIAPStore>().catalog);
                LaserIAPStore.ConfigureTrainingProducts(catalog, lanes);
                Check(catalog.IsValid(out _) && catalog.items.Count(p =>
                    p.kind == LaserStoreKind.Training) == 1, "Only IAP lane is registered as a store product");
                Check(catalog.Find(LaserStoreKind.Training, 3).previewPrice == 4.99,
                    "Per-lane IAP price flows into the store catalogue");
                Check(lanes.All(t => t.priceCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() != null
                    && t.purchaseButton != null), "World price plates have clickable IAP controls");
                return "PASS (" + lines.Count + " training lane checks)";
            }
            finally
            {
                player.Data = original;
                foreach (var clone in clones)
                    UnityEngine.Object.DestroyImmediate(clone);
                if (catalog != null)
                    UnityEngine.Object.DestroyImmediate(catalog);
                Directory.CreateDirectory("LaserHeroQA");
                File.WriteAllLines("LaserHeroQA/training-lanes-verification.txt", lines);
            }
        }
    }
}
