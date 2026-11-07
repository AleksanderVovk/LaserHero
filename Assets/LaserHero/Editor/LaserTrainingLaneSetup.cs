using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace LaserHero.Editor
{
    public static class LaserTrainingLaneSetup
    {
        const string Folder = "Assets/LaserHero/Training";
        static LaserHeroHUD hud;
        public static string Apply()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Stop Play Mode");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!System.IO.File.Exists("Assets/Scenes/Backups/Lobby_BeforeTrainingPrices.unity"))
                EditorSceneManager.SaveScene(scene, "Assets/Scenes/Backups/Lobby_BeforeTrainingPrices.unity", true);
            hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            var targets = UnityEngine.Object.FindObjectsByType<LaserTrainingTarget>(FindObjectsSortMode.None);
            if (targets.Length == 0)
                throw new Exception("No active training target");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/LaserHero", "Training");
            foreach (var target in targets)
            {
                if (target.trainingMultiplier == 0)
                    target.trainingMultiplier = target.Multiplier;
                BuildSign(target);
                RefinePlate(target);
                target.RefreshPlate();
                EditorUtility.SetDirty(target);
            }

            var source = targets.OrderBy(t => t.tier).First();
            for (int i = 0; i < 4; i++)
            {
                var clone = UnityEngine.Object.Instantiate(source.gameObject);
                clone.transform.position = Vector3.zero;
                clone.name = new[]
                {
                    "TrainingLane_Free",
                    "TrainingLane_Rebirths2",
                    "TrainingLane_Rebirths5",
                    "TrainingLane_IAP"
                }[i];
                var t = clone.GetComponent<LaserTrainingTarget>();
                t.tier = i;
                t.trainingMultiplier = new[]
                {
                    1.5f,
                    2,
                    4,
                    6
                }[i];
                t.access = i == 0 ? LaserTrainingAccess.Free : i == 3 ? LaserTrainingAccess.IAP : LaserTrainingAccess.Rebirths;
                t.requiredRebirths = i == 2 ? 5 : 2;
                t.iapProductId = "laser_hero_training_6x";
                t.iapPrice = 4.99;
                t.RefreshPlate();
                PrefabUtility.SaveAsPrefabAsset(clone, Folder + "/" + clone.name + ".prefab");
                UnityEngine.Object.DestroyImmediate(clone);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = source.gameObject;
            EditorGUIUtility.PingObject(source.gameObject);
            return "Training signs saved; four editable templates: Free, 2 rebirths, 5 rebirths, IAP";
        }

        static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static TMP_Text Label(Transform parent, string name, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var rect = Rect(parent, name, position, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = hud.font;
            text.fontSharedMaterial = hud.outlinedText;
            text.color = color;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * .7f;
            text.fontSizeMax = fontSize;
            text.raycastTarget = false;
            text.UpdateMeshPadding();
            return text;
        }

        static void BuildSign(LaserTrainingTarget target)
        {
            if (target.priceCanvas != null)
                return;
            foreach (var old in target.GetComponentsInChildren<TMP_Text>().Where(t => t.text.Contains("Мощность")).ToArray())
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            var sign = new GameObject("Training price sign", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.GraphicRaycaster), typeof(LaserWorldLabel));
            sign.transform.SetParent(target.transform, false);
            sign.transform.localPosition = new Vector3(0, 4.4f, 0);
            sign.transform.localScale = Vector3.one * .009f;
            if (Camera.main != null)
                sign.transform.rotation = Camera.main.transform.rotation;
            var canvas = sign.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 4;
            ((RectTransform)sign.transform).sizeDelta = new Vector2(700, 210);
            target.priceCanvas = canvas;
            var plate = Rect(sign.transform, "Black price plate", new Vector2(0, 55), new Vector2(285, 86));
            var image = plate.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = new Color(.015f, .025f, .06f, .82f);
            image.raycastTarget = true;
            var outline = plate.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(4, -4);
            target.purchaseButton = plate.gameObject.AddComponent<UnityEngine.UI.Button>();
            target.purchaseButton.targetGraphic = image;
            var colors = UnityEngine.UI.ColorBlock.defaultColorBlock;
            colors.disabledColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            target.purchaseButton.colors = colors;
            var icon = Rect(plate, "Rebirth icon", new Vector2(44, 0), new Vector2(66, 66));
            icon.anchorMin = icon.anchorMax = new Vector2(0, .5f);
            target.rebirthIcon = icon.gameObject.AddComponent<UnityEngine.UI.Image>();
            target.rebirthIcon.sprite = hud.extraIcons[2];
            target.rebirthIcon.preserveAspect = true;
            target.rebirthIcon.raycastTarget = false;
            target.priceLabel = Label(plate, "Access requirement", Vector2.zero, Vector2.zero, 36, Color.white);
            var textRect = target.priceLabel.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            target.multiplierLabel = Label(sign.transform, "Training multiplier", new Vector2(0, -40),
                new Vector2(700, 98), 60, new Color(1, .39f, .025f));
            target.multiplierLabel.enableVertexGradient = true;
            target.multiplierLabel.colorGradient = new VertexGradient(new Color(1, .7f, .05f), new Color(1, .7f,
                .05f), new Color(1, .2f, .015f), new Color(1, .2f, .015f));
        }

        static void RefinePlate(LaserTrainingTarget target)
        {
            var plate = target.purchaseButton.gameObject;
            var image = plate.GetComponent<UnityEngine.UI.Image>();
            if (!(image is LaserTrainingPricePlate))
            {
                Color fill = image.color;
                UnityEngine.Object.DestroyImmediate(image);
                var outline = plate.GetComponent<UnityEngine.UI.Outline>();
                if (outline != null)
                    UnityEngine.Object.DestroyImmediate(outline);
                image = plate.AddComponent<LaserTrainingPricePlate>();
                image.color = fill;
                image.raycastTarget = true;
                target.purchaseButton.targetGraphic = image;
            }
        }
    }
}
