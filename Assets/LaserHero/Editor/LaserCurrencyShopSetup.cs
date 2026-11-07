using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace LaserHero.Editor
{
    public static class LaserCurrencyShopSetup
    {
        static LaserHeroHUD hud;
        static LaserIAPStore store;
        static Sprite cash, stack, fire;
        [MenuItem("Laser Hero/Set up currency shop")]
        public static void Menu()
        {
            Debug.Log(Build());
        }

        public static string Build()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first");
            hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            store = UnityEngine.Object.FindFirstObjectByType<LaserIAPStore>();
            var dialog = hud.transform.Find("Store");
            var scroll = dialog.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            if (dialog.GetComponentsInChildren<LaserCurrencyOffer>(true).Length > 0)
                throw new InvalidOperationException("Currency shop already exists; edit its cards and catalogue");
            EditorSceneManager.SaveScene(hud.gameObject.scene, "Assets/Scenes/Backups/Lobby_BeforeCurrencyShop.unity", true);
            var list = store.catalog.items.Where(i => !i.IsConsumable).ToList();
            string[] money =
            {
                "839300000",
                "8300000000",
                "33500000000",
                "107400000000"
            };
            string[] energy =
            {
                "5800000",
                "58500000",
                "585000000",
                "5800000000"
            };
            double[] prices =
            {
                .99,
                2.99,
                7.99,
                19.99
            };
            foreach (var kind in new[]
            {
                LaserStoreKind.Money,
                LaserStoreKind.Energy
            }

            )
                for (int i = 0; i < 4; i++)
                    list.Add(new LaserStoreItem
                    {
                        kind = kind,
                        index = i,
                        productId = "laser_hero_" + (kind == LaserStoreKind.Money ? "money" : "energy") + "_" + (i + 1),
                        previewPrice = prices[i],
                        grantAmount = (kind == LaserStoreKind.Money ? money : energy)[i]
                    });
            store.catalog.items = list.ToArray();
            if (!store.catalog.IsValid(out var reason))
                throw new Exception(reason);
            EditorUtility.SetDirty(store.catalog);
            cash = hud.icons[1];
            stack = Import("Assets/LaserHero/UI/Green Cash 2nd Outline 256px.png");
            fire = Import("Assets/LaserHero/UI/Store Fire.png");
            for (int i = scroll.content.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(scroll.content.GetChild(i).gameObject);
            var layout = scroll.content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 14;
            foreach (var kind in new[]
            {
                LaserStoreKind.Money,
                LaserStoreKind.Energy
            }

            )
            {
                var best = Panel(kind + " best offer", scroll.content, new Vector2(718, 190), kind);
                best.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 190;
                Offer(best, kind, 3, true);
                var row = Rect(kind + " smaller offers", scroll.content, new Vector2(718, 236));
                row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 236;
                var group = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                group.spacing = 12;
                group.childControlWidth = true;
                group.childControlHeight = true;
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = true;
                for (int i = 2; i >= 0; i--)
                {
                    var card = Panel(kind + " offer " + i, row, new Vector2(226, 236), kind);
                    Offer(card, kind, i, false);
                }
            }

            // Retain the existing modal, header, close button, backdrop and all custom HUD children.
            var header = dialog.GetComponentsInChildren<LaserStudPanel>(true).First(p => p.transform.parent == dialog);
            header.topColor = new Color(1, .88f, .08f);
            header.bottomColor = new Color(1, .65f, .015f);
            header.SetVerticesDirty();
            dialog.gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            EditorUtility.SetDirty(hud);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            EditorSceneManager.SaveScene(hud.gameObject.scene);
            return "Currency shop configured: 4 money + 4 energy consumables, persistent scene cards, original Store button retained";
        }

        static Sprite Import(string path)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            Place(r, new Vector2(0, 1), Vector2.zero, size);
            return r;
        }

        static void Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        static RectTransform Panel(string name, Transform parent, Vector2 size, LaserStoreKind kind)
        {
            var r = Rect(name, parent, size);
            var p = r.gameObject.AddComponent<LaserStudPanel>();
            bool green = kind == LaserStoreKind.Money;
            p.topColor = green ? new Color(.43f, .96f, .035f) : new Color(1, .68f, .10f);
            p.bottomColor = green ? new Color(.06f, .64f, .015f) : new Color(.99f, .24f, .10f);
            p.raycastTarget = false;
            return r;
        }

        static TMP_Text Text(string name, Transform parent, Vector2 pos, Vector2 size, string value, float fontSize, Color colour)
        {
            var r = Rect(name, parent, size);
            r.anchoredPosition = pos;
            var text = r.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = hud.font;
            text.fontSharedMaterial = hud.outlinedText;
            text.text = value;
            text.color = colour;
            text.fontStyle = FontStyles.Bold;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = fontSize * .7f;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.UpdateMeshPadding();
            return text;
        }

        static void Icon(Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
        {
            var r = Rect("Offer icon", parent, size);
            r.anchoredPosition = pos;
            var image = r.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        static void Offer(RectTransform card, LaserStoreKind kind, int index, bool best)
        {
            var offer = card.gameObject.AddComponent<LaserCurrencyOffer>();
            offer.store = store;
            offer.kind = kind;
            offer.index = index;
            offer.featured = best;
            var item = store.catalog.Find(kind, index);
            string amount = "+" + LaserProgress.Format(LaserAmount.Parse(item.grantAmount)) + (best
                ? (kind == LaserStoreKind.Money ? " ДЕНЕГ" : " энергии") : "");
            offer.amountText = Text("Package amount", card, new Vector2(14, -14), new Vector2(best ? 490
                : 198, 48), amount, best ? 36 : 35, Color.white);
            if (!best)
                offer.amountText.alignment = TextAlignmentOptions.Center;
            if (best)
                Text("Best offer", card, new Vector2(14, -63), new Vector2(486, 40), "Лучшее предложение!",
                    29, new Color(.42f, 1, .03f));
            if (kind == LaserStoreKind.Energy)
                Icon(card, fire, best ? new Vector2(536, -14) : new Vector2(55, -61), best
                    ? new Vector2(163, 163) : new Vector2(116, 116));
            else if (best)
            {
                Icon(card, stack, new Vector2(528, -27), new Vector2(157, 145));
                Icon(card, cash, new Vector2(506, -103), new Vector2(77, 64));
                Icon(card, cash, new Vector2(632, -111), new Vector2(68, 58));
            }
            else
                Icon(card, index == 0 ? cash : stack, new Vector2(49, -69), new Vector2(128, 105));
            var buttonRect = Panel("Purchase", card, new Vector2(best ? 218 : 206, 61), LaserStoreKind.Money);
            Place(buttonRect, new Vector2(best ? 0 : .5f, 0), new Vector2(best ? 14 : 0, 12), new Vector2(best ? 218 : 206, 61));
            var b = buttonRect.gameObject.AddComponent<UnityEngine.UI.Button>();
            var image = buttonRect.GetComponent<LaserStudPanel>();
            image.raycastTarget = true;
            b.targetGraphic = image;
            b.navigation = new UnityEngine.UI.Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.None
            };
            offer.purchaseButton = b;
            offer.priceText = Text("Price", buttonRect, new Vector2(6, -4), new Vector2(best ? 206
                : 194, 51), item.previewPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), 36, Color.white);
            offer.priceText.alignment = TextAlignmentOptions.Center;
            UnityEventTools.AddPersistentListener(b.onClick, offer.Buy);
        }
    }
}
