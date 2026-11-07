using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserSettingsSetup
    {
        static LaserHeroHUD hud;
        static readonly Color PurpleTop = new Color(.78f, .63f, 1), PurpleBottom = new Color(.49f, .3f, .9f);
        [MenuItem("Laser Hero/Add settings UI")]
        public static string Apply()
        {
            if (Application.isPlaying)
                throw new Exception("Exit Play Mode");
            hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            if (hud == null || hud.transform.childCount == 0)
                throw new Exception("Open the scene HUD");
            if (hud.settingsPanel != null)
                throw new Exception("Settings already exist; edit the scene UI");
            EditorSceneManager.SaveScene(hud.gameObject.scene, "Assets/Scenes/Backups/Lobby_BeforeSettings.unity", true);
            var settingsGO = new GameObject("Game Settings");
            settingsGO.transform.SetParent(hud.transform.parent, false);
            var settings = settingsGO.AddComponent<LaserGameSettings>();
            settings.playerCharacteristic = hud.hero.GetComponent<PlayerCharacteristic>();
            if (settings.playerCharacteristic == null)
                throw new Exception("PlayerCharacteristic missing");
            settings.defaultSensitivity = settings.playerCharacteristic.CAMERA_SENSITIVITY;
            var musicGO = new GameObject("Music AudioSource");
            musicGO.transform.SetParent(settingsGO.transform, false);
            settings.musicSource = musicGO.AddComponent<AudioSource>();
            settings.musicSource.playOnAwake = true;
            settings.musicSource.loop = true;
            settings.musicSource.spatialBlend = 0;
            settings.musicSource.ignoreListenerVolume = true;
            settings.musicSource.volume = settings.defaultMusicVolume;
            var open = Stud("Settings Button", hud.transform, Vector2.one, new Vector2(-22, -18),
                new Vector2(88, 88), new Color(.38f, .88f, 1), new Color(.06f, .52f, .94f));
            Bind(open, LaserHUDAction.ToggleSettings);
            var gear = new GameObject("Gear", typeof(RectTransform), typeof(LaserSettingsGear));
            gear.transform.SetParent(open, false);
            Place((RectTransform)gear.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(60, 60));
            gear.GetComponent<LaserSettingsGear>().raycastTarget = false;
            var panel = Image("Settings", hud.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(780,
                610), new Color(.11f, .12f, .17f, .98f));
            panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            hud.settingsPanel = panel.gameObject;
            var header = Stud("Header", panel, new Vector2(0, 1), Vector2.zero, new Vector2(780, 100), PurpleTop, PurpleBottom);
            var headerGear = new GameObject("Gear", typeof(RectTransform), typeof(LaserSettingsGear));
            headerGear.transform.SetParent(header, false);
            Place((RectTransform)headerGear.transform, new Vector2(0, 1), new Vector2(20, -18), new Vector2(64, 64));
            headerGear.GetComponent<LaserSettingsGear>().raycastTarget = false;
            Label(header, "Настройки", new Vector2(105, -9), new Vector2(550, 80), 48);
            var close = Stud("X", header, Vector2.one, new Vector2(-12, -10), new Vector2(78, 78), new Color(1,
                .55f, .25f), new Color(1, .08f, .04f));
            Bind(close, LaserHUDAction.CloseMenus);
            var cross = Label(close, "X", new Vector2(0, 0), new Vector2(78, 78), 44);
            cross.alignment = TextAlignmentOptions.Center;
            settings.sensitivitySlider = Row(panel, "Camera sensitivity", "Чувствительность камеры", 125,
                settings.minSensitivity, settings.maxSensitivity, settings.defaultSensitivity, out settings.sensitivityValue);
            settings.musicSlider = Row(panel, "Music volume", "Музыка", 278, 0, 1, settings.defaultMusicVolume,
                out settings.musicValue);
            settings.soundSlider = Row(panel, "Sound volume", "Звуки", 431, 0, 1, settings.defaultSoundVolume,
                out settings.soundValue);
            settings.sensitivityValue.text = Mathf.RoundToInt(settings.defaultSensitivity).ToString();
            settings.musicValue.text = "65%";
            settings.soundValue.text = "100%";
            var note = Label(panel, "Изменения сохраняются автоматически", new Vector2(24, -576), new Vector2(732, 25), 19);
            note.alignment = TextAlignmentOptions.Center;
            note.color = new Color(.86f, .8f, 1);
            panel.gameObject.SetActive(false);
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(settings.musicSource);
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            EditorSceneManager.SaveScene(hud.gameObject.scene);
            Selection.activeGameObject = settingsGO;
            return "Settings UI and music source saved in scene; sensitivity edits PlayerCharacteristic only";
        }

        static UnityEngine.UI.Slider Row(Transform parent, string name, string title, float y, float min, float max,
            float value, out TMP_Text valueText)
        {
            var row = Stud(name, parent, new Vector2(0, 1), new Vector2(18, -y), new Vector2(744, 130),
                new Color(.57f, .42f, .88f), new Color(.28f, .17f, .54f));
            Label(row, title, new Vector2(22, -10), new Vector2(600, 44), 29);
            valueText = Label(row, "", new Vector2(610, -10), new Vector2(112, 44), 30);
            valueText.alignment = TextAlignmentOptions.Right;
            var sliderRoot = Image("Slider", row, new Vector2(0, 1), new Vector2(24, -72), new Vector2(696, 36),
                new Color(.07f, .04f, .15f));
            sliderRoot.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var fillArea = Rect("Fill Area", sliderRoot);
            Stretch(fillArea, new Vector2(12, 6), new Vector2(-12, -6));
            var fill = Image("Fill", fillArea, Vector2.zero, Vector2.zero, Vector2.zero, new Color(.42f, .96f, .12f));
            Stretch(fill, Vector2.zero, Vector2.zero);
            var handleArea = Rect("Handle Area", sliderRoot);
            Stretch(handleArea, new Vector2(12, 0), new Vector2(-12, 0));
            var handle = Stud("Handle", handleArea, new Vector2(0, .5f), Vector2.zero, new Vector2(28, 48),
                Color.white, new Color(.52f, .83f, 1));
            handle.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var slider = sliderRoot.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = max > 1;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.SetValueWithoutNotify(value);
            return slider;
        }

        static void Bind(RectTransform rect, LaserHUDAction action)
        {
            var graphic = rect.GetComponent<UnityEngine.UI.Image>();
            graphic.raycastTarget = true;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = graphic;
            var binding = rect.gameObject.AddComponent<LaserHUDButtonAction>();
            binding.hud = hud;
            binding.action = action;
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.gameObject.layer = 5;
            return r;
        }

        static void Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        static void Stretch(RectTransform r, Vector2 min, Vector2 max)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = min;
            r.offsetMax = max;
        }

        static RectTransform Image(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var r = Rect(name, parent);
            Place(r, anchor, pos, size);
            var g = r.gameObject.AddComponent<UnityEngine.UI.Image>();
            g.color = color;
            g.raycastTarget = false;
            return r;
        }

        static RectTransform Stud(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color top, Color bottom)
        {
            var r = Rect(name, parent);
            Place(r, anchor, pos, size);
            var g = r.gameObject.AddComponent<LaserStudPanel>();
            g.topColor = top;
            g.bottomColor = bottom;
            g.raycastTarget = false;
            return r;
        }

        static TMP_Text Label(Transform parent, string text, Vector2 pos, Vector2 size, float fontSize)
        {
            var r = Rect("Label", parent);
            Place(r, new Vector2(0, 1), pos, size);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = hud.font;
            t.fontSharedMaterial = hud.outlinedText;
            t.fontStyle = FontStyles.Bold;
            t.fontSize = fontSize;
            t.color = Color.white;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.text = text;
            t.UpdateMeshPadding();
            return t;
        }
    }
}
