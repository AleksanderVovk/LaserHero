using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace LaserHero.Editor
{
    public static class LaserMapVotingSetup
    {
        static LaserHeroHUD hud;
        [MenuItem("Laser Hero/Set up arena voting")]
        public static string Build()
        {
            if (Application.isPlaying)
                throw new Exception("Exit Play mode");
            var arena = UnityEngine.Object.FindFirstObjectByType<LaserArena>();
            hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            if (arena == null || hud == null)
                throw new Exception("Open Lobby scene");
            if (arena.transform.Find("Arena Rotation") != null || hud.transform.Find("Map Voting") != null)
                throw new Exception("Voting already configured; edit existing objects");
            EditorSceneManager.SaveScene(arena.gameObject.scene, "Assets/Scenes/Backups/Lobby_BeforeMapVoting.unity", true);
            var owner = new GameObject("Arena Rotation");
            owner.transform.SetParent(arena.transform, false);
            var rotation = owner.AddComponent<LaserArenaRotation>();
            rotation.arena = arena;
            rotation.hud = hud;
            rotation.roundIntervalSeconds = 180;
            rotation.votingSeconds = 10;
            rotation.startingArenaId = "city";
            rotation.maps = new List<LaserArenaOption>();
            string[] roots =
            {
                "LEGO City",
                "Spaceport Arena",
                "Ocean Arena",
                "Amusement Park Arena",
                "LEGO Arena — Prototype Backup"
            };
            string[] ids =
            {
                "city",
                "spaceport",
                "ocean",
                "amusement_park",
                "prototype"
            };
            string[] names =
            {
                "Город",
                "Космодром",
                "Морская арена",
                "Парк аттракционов",
                "Первая арена"
            };
            string[] icons =
            {
                "City",
                "Spaceport",
                "Ocean",
                "AmusementPark",
                "Prototype"
            };
            for (int i = 0; i < roots.Length; i++)
            {
                var map = arena.transform.Find(roots[i]);
                if (map == null)
                    throw new Exception("Missing " + roots[i]);
                rotation.maps.Add(new LaserArenaOption
                {
                    id = ids[i],
                    title = names[i],
                    root = map.gameObject,
                    enabledInRotation = i < 4,
                    icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/LaserHero/UI/ArenaIcons/Arena_" + icons[i] + ".png"),
                    bounds = i < 4 ? new Bounds(new Vector3(0, 24, 102), new Vector3(128, 100, 128))
                        : new Bounds(new Vector3(0, 8, 66), new Vector3(50, 35, 58)),
                    entry = new Vector3(0, 1, i < 4 ? 42 : 41)
                });
                map.gameObject.SetActive(i == 0);
            }

            arena.rotation = rotation;
            arena.arenaBounds = rotation.maps[0].bounds;
            arena.entry = rotation.maps[0].entry;
            var ui = hud.gameObject.AddComponent<LaserMapVoteUI>();
            ui.rotation = rotation;
            rotation.votingUI = ui;
            hud.mapVotingUI = ui;
            var dialog = Box("Map Voting", hud.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1800,
                885), new Color(.03f, .03f, .08f, .12f));
            ui.dialog = dialog.gameObject;
            dialog.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var body = Box("Body background", dialog, new Vector2(.5f, 0), new Vector2(0, 9), new Vector2(1782,
                680), new Color(.035f, .055f, .15f, .42f));
            Frame(dialog, new Vector2(1800, 885), 9);
            var header = Stud("Red header", dialog, new Vector2(.5f, 1), new Vector2(0, -9), new Vector2(1782, 190));
            Box("Header shadow", dialog, new Vector2(.5f, 1), new Vector2(0, -196), new Vector2(1800, 12), Color.black);
            var pin = Rect("Map pin", header, new Vector2(0, .5f), new Vector2(36, 0), new Vector2(118, 146));
            pin.gameObject.AddComponent<LaserVoteSymbol>().symbol = LaserVoteSymbol.Kind.Pin;
            pin.GetComponent<LaserVoteSymbol>().raycastTarget = false;
            ui.heading = Text("Voting countdown", header, new Vector2(0, .5f), new Vector2(178, 0),
                new Vector2(1400, 110), "Голосование за карту (10 секунд)", 56);
            ui.heading.alignment = TextAlignmentOptions.MidlineLeft;
            ui.heading.enableAutoSizing = true;
            ui.heading.fontSizeMin = 40;
            ui.heading.fontSizeMax = 56;
            var close = Stud("Close voting", header, new Vector2(1, .5f), new Vector2(-24, 0), new Vector2(154, 154));
            var closeButton = close.gameObject.AddComponent<UnityEngine.UI.Button>();
            closeButton.targetGraphic = close.GetComponent<LaserStudPanel>();
            closeButton.navigation = new UnityEngine.UI.Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.None
            };
            UnityEventTools.AddPersistentListener(closeButton.onClick, hud.CloseMenus);
            Text("Close X", close, new Vector2(.5f, .5f), Vector2.zero, new Vector2(150, 150), "X", 144);
            var row = Rect("Candidate cards", dialog, new Vector2(.5f, 1), new Vector2(0, -276), new Vector2(1740, 540));
            var layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 55;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            for (int i = 0; i < 3; i++)
            {
                var card = Stud("Candidate " + (i + 1), row, new Vector2(.5f, .5f), Vector2.zero, new Vector2(535, 535));
                ui.cards[i] = card.gameObject;
                var button = card.gameObject.AddComponent<UnityEngine.UI.Button>();
                button.targetGraphic = card.GetComponent<LaserStudPanel>();
                button.navigation = new UnityEngine.UI.Navigation
                {
                    mode = UnityEngine.UI.Navigation.Mode.None
                };
                if (i == 0)
                    UnityEventTools.AddPersistentListener(button.onClick, ui.VoteFirst);
                else if (i == 1)
                    UnityEventTools.AddPersistentListener(button.onClick, ui.VoteSecond);
                else
                    UnityEventTools.AddPersistentListener(button.onClick, ui.VoteThird);
                ui.titles[i] = Text("Arena title", card, new Vector2(.5f, 1), new Vector2(0, -21),
                    new Vector2(503, 100), names[i + 1], 47);
                ui.titles[i].textWrappingMode = TextWrappingModes.Normal;
                ui.titles[i].enableAutoSizing = true;
                ui.titles[i].fontSizeMin = 33;
                ui.titles[i].fontSizeMax = 47;
                var border = Box("Rounded thumbnail border", card, new Vector2(.5f, 0), new Vector2(0, 40),
                    new Vector2(365, 365), new Color(1, .6f, .63f));
                var borderImage = border.GetComponent<UnityEngine.UI.Image>();
                borderImage.sprite = rounded;
                borderImage.type = UnityEngine.UI.Image.Type.Sliced;
                borderImage.pixelsPerUnitMultiplier = .25f;
                var mask = Box("Rounded thumbnail mask", border, new Vector2(.5f, .5f), Vector2.zero,
                    new Vector2(350, 350), Color.white);
                var maskImage = mask.GetComponent<UnityEngine.UI.Image>();
                maskImage.sprite = rounded;
                maskImage.type = UnityEngine.UI.Image.Type.Sliced;
                maskImage.pixelsPerUnitMultiplier = .25f;
                mask.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
                var photo = Box("Arena icon 512", mask, new Vector2(.5f, .5f), Vector2.zero, new Vector2(350, 350), Color.white);
                ui.thumbnails[i] = photo.GetComponent<UnityEngine.UI.Image>();
                ui.thumbnails[i].sprite = rotation.maps[i + 1].icon;
                ui.thumbnails[i].preserveAspect = true;
                var symbol = Rect("Vote mark", card, new Vector2(.5f, 0), new Vector2(0, 50), new Vector2(340, 340));
                ui.symbols[i] = symbol.gameObject.AddComponent<LaserVoteSymbol>();
                ui.symbols[i].symbol = i == 0 ? LaserVoteSymbol.Kind.Check : LaserVoteSymbol.Kind.Cross;
                ui.symbols[i].raycastTarget = false;
            }

            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(row);
            ui.preferredWidth = 900;
            ui.ApplySize();
            dialog.gameObject.SetActive(false);
            EditorUtility.SetDirty(arena);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(rotation);
            EditorUtility.SetDirty(ui);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(arena.gameObject.scene);
            EditorSceneManager.SaveScene(arena.gameObject.scene);
            Selection.activeGameObject = owner;
            return "Voting configured: city first; 180 seconds play + 10 seconds voting; 4 active maps and optional prototype; persistent reference-style Canvas UI";
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        static RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color colour)
        {
            var rect = Rect(name, parent, anchor, pos, size);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = colour;
            image.raycastTarget = false;
            return rect;
        }

        static RectTransform Stud(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rect = Rect(name, parent, anchor, pos, size);
            var panel = rect.gameObject.AddComponent<LaserStudPanel>();
            panel.topColor = new Color(.97f, .26f, .27f);
            panel.bottomColor = new Color(.65f, .055f, .065f);
            panel.studSize = 30;
            panel.studGap = 30;
            panel.raycastTarget = true;
            return rect;
        }

        static TMP_Text Text(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string value, float fontSize)
        {
            var rect = Rect(name, parent, anchor, pos, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = hud.font;
            text.fontSharedMaterial = hud.outlinedText;
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.UpdateMeshPadding();
            return text;
        }

        static void Frame(RectTransform rect, Vector2 size, float width)
        {
            Box("Top black frame", rect, new Vector2(.5f, 1), Vector2.zero, new Vector2(size.x, width), Color.black);
            Box("Bottom black frame", rect, new Vector2(.5f, 0), Vector2.zero, new Vector2(size.x, width), Color.black);
            Box("Left black frame", rect, new Vector2(0, .5f), Vector2.zero, new Vector2(width, size.y), Color.black);
            Box("Right black frame", rect, new Vector2(1, .5f), Vector2.zero, new Vector2(width, size.y), Color.black);
        }
    }
}
