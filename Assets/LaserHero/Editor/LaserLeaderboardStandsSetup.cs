using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserLeaderboardStandsSetup
    {
        const string Folder = "Assets/LaserHero/LeaderboardStands";
        static LaserHeroHUD hud;
        static Material wood, trim, dark;
        public static string Create()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first");
            hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            var lobby = UnityEngine.Object.FindFirstObjectByType<LaserArena>().transform.Find("Safe LEGO Lobby");
            if (lobby.Find("Leaderboard Stands") != null)
                throw new InvalidOperationException("Leaderboard stands already exist");
            EditorSceneManager.SaveScene(lobby.gameObject.scene, "Assets/Scenes/Backups/Lobby_BeforeLeaderboardStands.unity", true);
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/LaserHero", "LeaderboardStands");
            var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/LaserHero/BatchedWorld/SharedPlastic.mat");
            wood = Plastic(source, "Board brown", new Color(.32f, .17f, .065f));
            trim = Plastic(source, "Frame warm brown", new Color(.52f, .29f, .11f));
            dark = Plastic(source, "Backing dark brown", new Color(.12f, .09f, .045f));
            var group = new GameObject("Leaderboard Stands");
            group.transform.SetParent(lobby, false);
            Undo.RegisterCreatedObjectUndo(group, "Create leaderboard stands");
            string[] titles =
            {
                "Максимальная\nмощность",
                "Наибольшее количество\nденег",
                "Наибольшее время игры"
            };
            string[] columns =
            {
                "Мощность",
                "Деньги",
                "Время игры"
            };
            string[] ids =
            {
                "Power",
                "Money",
                "Playtime"
            };
            string[][] values =
            {
                new[]
                {
                    "775.7B",
                    "388.6B",
                    "365.9B",
                    "205.9B",
                    "126.8B",
                    "120B",
                    "107.8B"
                },
                new[]
                {
                    "28.5T",
                    "21.2T",
                    "11.2T",
                    "10.7T",
                    "9.8T",
                    "7.5T",
                    "6.6T"
                },
                new[]
                {
                    "4д 18ч 47м",
                    "4д 11ч 13м",
                    "3д 18ч 12м",
                    "3д 12ч 58м",
                    "3д 7ч 35м",
                    "3д 4ч 55м",
                    "2д 14ч 38м"
                }
            };
            Color[] tops =
            {
                new Color(1, .82f, .02f),
                new Color(.52f, 1, .01f),
                new Color(.08f, .86f, 1)
            };
            Color[] bottoms =
            {
                new Color(1, .25f, .015f),
                new Color(.12f, .78f, .005f),
                new Color(.01f, .47f, .98f)
            };
            string[] players =
            {
                "BrickMaster",
                "ЛегоГерой",
                "SpaceRanger",
                "MegaLaser",
                "Кубик_007",
                "AuraKnight",
                "Игрок123"
            };
            for (int i = 0; i < 3; i++)
            {
                var root = new GameObject("Leaderboard " + ids[i]);
                root.transform.SetParent(group.transform, false);
                root.transform.localPosition = new Vector3(-24 + i * 10, 0, -18);
                root.transform.localRotation = Quaternion.Euler(0, 180, 0);
                var model = new GameObject("LEGO Frame");
                model.transform.SetParent(root.transform, false);
                Block(model.transform, "Studded backboard", new Vector3(0, 5.8f, 0), new Vector3(8.6f, 10.4f, .6f), wood);
                Block(model.transform, "Dark backing", new Vector3(0, 5.8f, .28f), new Vector3(8.9f, 10.65f, .28f), dark);
                Block(model.transform, "Top frame", new Vector3(0, 10.85f, -.15f), new Vector3(8.75f, .48f, .7f), trim);
                Block(model.transform, "Bottom frame", new Vector3(0, .75f, -.15f), new Vector3(8.75f, .48f, .7f), trim);
                foreach (int side in new[]
                {
                    -1,
                    1
                }

                )
                {
                    Block(model.transform, side < 0 ? "Left frame"
                        : "Right frame", new Vector3(side * 4.13f, 5.8f, -.15f), new Vector3(.48f, 9.65f, .7f), trim);
                    Block(model.transform, side < 0 ? "Left foot"
                        : "Right foot", new Vector3(side * 2.7f, .25f, .05f), new Vector3(1.55f, .5f, 2), trim);
                    Block(model.transform, "Foot support " + side, new Vector3(side * 2.7f, .65f, .15f),
                        new Vector3(.55f, .8f, .6f), wood);
                }

                var canvasGO = new GameObject("WorldCanvas", typeof(RectTransform), typeof(Canvas));
                canvasGO.layer = 5;
                canvasGO.transform.SetParent(root.transform, false);
                var rect = (RectTransform)canvasGO.transform;
                rect.sizeDelta = new Vector2(780, 960);
                rect.localPosition = new Vector3(0, 5.8f, -.52f);
                rect.localScale = Vector3.one * .01f;
                var canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = hud.hero.view;
                canvas.sortingOrder = 0;
                // Display-only Canvas: no raycaster, buttons, network calls or gameplay dependencies.
                Frame("Black outer line", rect, Vector2.zero, new Vector2(780, 960), 5, Color.black);
                Frame("Category border", rect, new Vector2(5, -5), new Vector2(770, 950), 3, tops[i]);
                Frame("Inner black line", rect, new Vector2(8, -8), new Vector2(764, 944), 4, Color.black);
                Image("Transparent brown face", rect, new Vector2(12, -12), new Vector2(756, 936),
                    new Color(.25f, .12f, .035f, .12f));
                var header = Rect("Header", rect, new Vector2(16, -16), new Vector2(748, 138));
                var panel = header.gameObject.AddComponent<LaserStudPanel>();
                panel.topColor = tops[i];
                panel.bottomColor = bottoms[i];
                panel.studSize = 10000;
                panel.raycastTarget = false;
                var title = Text("Title", header, new Vector2(82, -10), new Vector2(584, 118), titles[i], i == 2 ? 40 : 43);
                title.alignment = TextAlignmentOptions.Center;
                title.textWrappingMode = TextWrappingModes.Normal;
                var icon = i == 0 ? hud.icons[0] : i == 1 ? hud.icons[1] : hud.extraIcons[4];
                Icon(header, "Left title icon", icon, new Vector2(15, -42), new Vector2(58, 58));
                Icon(header, "Right title icon", icon, new Vector2(675, -42), new Vector2(58, 58));
                var headings = Rect("Columns", rect, new Vector2(18, -168), new Vector2(744, 56));
                Text("Place heading", headings, new Vector2(4, 0), new Vector2(123, 56), "Место", 34);
                var playerHeading = Text("Player heading", headings, new Vector2(137, 0), new Vector2(328, 56), "Игрок", 34);
                playerHeading.alignment = TextAlignmentOptions.Center;
                var recordHeading = Text("Record heading", headings, new Vector2(471, 0), new Vector2(265, 56), columns[i], 34);
                recordHeading.alignment = TextAlignmentOptions.Right;
                var rows = Rect("Rows", rect, new Vector2(18, -230), new Vector2(744, 707));
                for (int rowIndex = 0; rowIndex < 7; rowIndex++)
                {
                    var row = Rect("Row" + (rowIndex + 1).ToString("00"), rows, new Vector2(0,
                        -rowIndex * 100), new Vector2(744, 96));
                    Image("Top separator", row, Vector2.zero, new Vector2(744, 4), Color.black);
                    Image("Bottom separator", row, new Vector2(0, -92), new Vector2(744, 4), Color.black);
                    Image("Row tint", row, new Vector2(0, -4), new Vector2(744, 88), new Color(.1f,
                        .04f, .01f, rowIndex % 2 == 0 ? .16f : .08f));
                    var rank = Text("Rank", row, new Vector2(8, -17), new Vector2(85, 62), (rowIndex + 1) + ")", 38);
                    rank.alignment = TextAlignmentOptions.MidlineLeft;
                    var player = Text("PlayerName", row, new Vector2(103, -17), new Vector2(356, 62),
                        players[(rowIndex + i * 2) % players.Length], 34);
                    player.alignment = TextAlignmentOptions.Center;
                    var record = Text("Record", row, new Vector2(471, -17), new Vector2(265, 62),
                        values[i][rowIndex], i == 2 ? 29 : 37);
                    record.alignment = TextAlignmentOptions.MidlineRight;
                }

                PrefabUtility.SaveAsPrefabAssetAndConnect(root,
                    Folder + "/Leaderboard" + ids[i] + ".prefab", InteractionMode.AutomatedAction);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(lobby.gameObject.scene);
            EditorSceneManager.SaveScene(lobby.gameObject.scene);
            Selection.activeGameObject = group;
            return "Three separate LEGO leaderboard prefabs in lobby; World Space Canvas, 7 placeholder rows per board; no backend or runtime data scripts";
        }

        static Material Plastic(Material source, string name, Color color)
        {
            var mat = new Material(source)
            {
                name = name,
                enableInstancing = true
            };
            mat.SetFloat("_UseVertexColor", 0);
            mat.SetFloat("_IsDynamic", 0);
            mat.SetFloat("_UseStudAnchor", 0);
            mat.SetColor("_Color", color);
            AssetDatabase.CreateAsset(mat, Folder + "/" + name + ".mat");
            return mat;
        }

        static void Block(Transform parent, string name, Vector3 pos, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static void Image(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var rect = Rect(name, parent, position, size);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        static void Frame(string name, Transform parent, Vector2 position, Vector2 size, float width, Color color)
        {
            var frame = Rect(name, parent, position, size);
            Image("Top", frame, Vector2.zero, new Vector2(size.x, width), color);
            Image("Bottom", frame, new Vector2(0, -size.y + width), new Vector2(size.x, width), color);
            Image("Left", frame, Vector2.zero, new Vector2(width, size.y), color);
            Image("Right", frame, new Vector2(size.x - width, 0), new Vector2(width, size.y), color);
        }

        static void Icon(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var rect = Rect(name, parent, position, size);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        static TMP_Text Text(string name, Transform parent, Vector2 position, Vector2 size, string value, float fontSize)
        {
            var rect = Rect(name, parent, position, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = hud.font;
            text.fontSharedMaterial = hud.outlinedText;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * .7f;
            text.fontSizeMax = fontSize;
            text.raycastTarget = false;
            text.UpdateMeshPadding();
            return text;
        }
    }
}
