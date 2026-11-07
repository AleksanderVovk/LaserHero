using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserExpandedShopSetup
    {
        static readonly string[] ids =
        {
            "white",
            "orange",
            "gold",
            "cyan",
            "indigo",
            "emerald",
            "crimson",
            "silver"
        };
        static readonly double[] prices =
        {
            12.99,
            14.99,
            17.99,
            19.99,
            24.99,
            29.99,
            39.99,
            49.99
        };
        [MenuItem("Laser Hero/Add expanded laser shop products")]
        public static void Menu()
        {
            Debug.Log(Apply());
        }

        public static string Apply()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first");
            var hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            var store = hud.progress.GetComponent<LaserIAPStore>();
            var so = new SerializedObject(hud);
            var shop = (GameObject)so.FindProperty("shop").objectReferenceValue;
            var content = shop.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true).content;
            var template = content.Find("Laser 5");
            if (template == null)
                throw new InvalidOperationException("Original laser card not found");
            EditorSceneManager.SaveScene(hud.gameObject.scene, "Assets/Scenes/Backups/Lobby_BeforeExpandedLasers.unity", true);
            System.IO.File.WriteAllText("LaserHeroQA/Recovery/Catalog.BeforeExpandedLasers.json",
                EditorJsonUtility.ToJson(store.catalog, true));
            var items = store.catalog.items.ToList();
            for (int i = 6; i < LaserProgress.Names.Length; i++)
            {
                var row = content.Find("Laser " + i);
                if (row == null)
                {
                    row = UnityEngine.Object.Instantiate(template, content, false);
                    row.name = "Laser " + i;
                }

                foreach (var label in row.GetComponentsInChildren<TMP_Text>(true).Where(t => t.transform.parent == row))
                    label.text = label.text.StartsWith("+")
                        ? "+" + LaserProgress.Format(LaserProgress.EnergyGain[i]) : LaserProgress.Names[i] + "\nОбычный";
                var icon = row.Find("Icon plate/Icon").GetComponent<UnityEngine.UI.Image>();
                icon.sprite = hud.laserTintIcon;
                icon.color = LaserProgress.Colors[i];
                icon.preserveAspect = true;
                foreach (var action in row.GetComponentsInChildren<LaserHUDButtonAction>(true))
                {
                    action.index = i;
                    action.hud = hud;
                }

                var item = items.FirstOrDefault(p => p.kind == LaserStoreKind.Laser && p.index == i);
                if (item == null)
                {
                    item = new LaserStoreItem
                    {
                        kind = LaserStoreKind.Laser,
                        index = i,
                        productId = "laser_hero_laser_tier_" + (i + 1) + "_" + ids[i - 6],
                        previewPrice = prices[i - 6]
                    };
                    items.Add(item);
                }

                var cash = row.GetComponentsInChildren<LaserHUDButtonAction>(true).Single(a => a.action == LaserHUDAction.BuyLaser);
                cash.GetComponentInChildren<TMP_Text>(true).text = "$" + LaserProgress.Format(LaserProgress.Prices[i]);
                var premium = row.GetComponentsInChildren<LaserHUDButtonAction>(true).Single(a =>
                    a.action == LaserHUDAction.PremiumLaser);
                premium.GetComponentInChildren<TMP_Text>(true).text = item.previewPrice.ToString("0.00",
                    System.Globalization.CultureInfo.InvariantCulture);
            }

            store.catalog.items = items.ToArray();
            if (!store.catalog.IsValid(out var reason))
                throw new InvalidOperationException(reason);
            var labels = so.FindProperty("shopLabels");
            var donations = so.FindProperty("laserDonationButtons");
            labels.arraySize = donations.arraySize = LaserProgress.Names.Length;
            for (int i = 0; i < LaserProgress.Names.Length; i++)
            {
                var actions = content.Find("Laser " + i).GetComponentsInChildren<LaserHUDButtonAction>(true);
                labels.GetArrayElementAtIndex(i).objectReferenceValue = actions.Single(a =>
                    a.action == LaserHUDAction.BuyLaser).GetComponentInChildren<TMP_Text>(true);
                donations.GetArrayElementAtIndex(i).objectReferenceValue = actions.Single(a =>
                    a.action == LaserHUDAction.PremiumLaser).GetComponent<UnityEngine.UI.Button>();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(store.catalog);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            EditorSceneManager.SaveScene(hud.gameObject.scene);
            return "Added 8 editable laser cards and 8 non-consumable IAP products; existing cards and save indices retained.";
        }
    }
}
