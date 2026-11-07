using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserParkSetup
    {
        const string Folder = "Assets/LaserHero/AmusementPark";
        static Transform root, section;
        static Material plastic;
        static int meshId;
        static readonly Color Pink = new Color(.98f, .22f, .58f), Yellow = new Color(1, .8f, .16f), Cyan =
            new Color(.12f, .75f, .95f), Lime = new Color(.43f, .82f, .18f), Purple = new Color(.58f, .31f, .9f), Orange =
            new Color(.98f, .46f, .12f), Blue = new Color(.14f, .33f, .76f), White = new Color(.96f, .97f, .99f), Dark =
            new Color(.12f, .18f, .29f);
        static readonly Color[] Rainbow =
        {
            Pink,
            Yellow,
            Cyan,
            Lime,
            Purple,
            Orange
        };
        [MenuItem("Laser Hero/Create editable amusement park arena")]
        public static string Build()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode");
            var arena = UnityEngine.Object.FindFirstObjectByType<LaserArena>();
            if (arena == null || arena.gameObject.scene.path != "Assets/Scenes/Lobby.unity")
                throw new Exception("Open Lobby scene");
            if (arena.transform.Find("Amusement Park Arena") != null)
                throw new Exception("Park already exists; edit its objects directly");
            EditorSceneManager.SaveScene(arena.gameObject.scene, "Assets/Scenes/Backups/Lobby_BeforeAmusementPark.unity", true);
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            meshId = 0;
            plastic = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/LaserHero/BatchedWorld/SharedPlastic.mat"))
            {
                name = "Amusement park editable stud plastic",
                enableInstancing = true
            };
            plastic.SetColor("_Color", Color.white);
            plastic.SetFloat("_UseVertexColor", 1);
            plastic.SetFloat("_IsDynamic", 0);
            AssetDatabase.CreateAsset(plastic, Folder + "/ParkPlastic.mat");
            root = new GameObject("Amusement Park Arena").transform;
            root.SetParent(arena.transform, false);
            root.gameObject.SetActive(false);
            try
            {
                AssetDatabase.StartAssetEditing();
                Terrain();
                Coaster();
                MainRides();
                Entrance();
                Stalls();
                Furniture();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Sign();
            foreach (string name in new[]
            {
                "Ocean Arena",
                "Spaceport Arena",
                "LEGO City",
                "LEGO Arena — Prototype Backup",
                "World render sectors"
            }

            )
            {
                var old = arena.transform.Find(name);
                if (old != null)
                    old.gameObject.SetActive(false);
            }

            root.gameObject.SetActive(true);
            arena.arenaBounds = new Bounds(new Vector3(0, 24, 102), new Vector3(128, 100, 128));
            arena.entry = new Vector3(0, 1, 42);
            LaserArena.InvalidateGroundIndex();
            EditorUtility.SetDirty(arena);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(arena.gameObject.scene);
            EditorSceneManager.SaveScene(arena.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(0, 17, 102), new Vector3(135, 85, 135)), false);
            string result = "PARK_READY: " + root.GetComponentsInChildren<LaserVoxelBody>()
                .Length + " separate editable destructible objects; 128x128m; ground 16m; former arenas preserved inactive";
            Directory.CreateDirectory("LaserHeroQA");
            File.WriteAllText("LaserHeroQA/park-setup.txt", result);
            return result;
        }

        static void Terrain()
        {
            Group("01 Deep ground and colourful park paths");
            for (int z = 0; z < 16; z++)
                for (int x = 0; x < 16; x++)
                    Body("Park ground 16m [" + x + "," + z + "]", new Vector3(-64 + x * 8, -16,
                        38 + z * 8), new Vector3Int(16, 32, 16), 70, .5f, new[]
                    {
                        Dark,
                        new Color(.48f, .3f, .17f),
                        new Color(.23f, .73f, .3f),
                        new Color(.23f, .47f, .57f),
                        new Color(.37f, .68f, .8f),
                        White,
                        new Color(.74f, .45f, .84f),
                        new Color(.43f, .37f, .68f)
                    }, true);
        }

        static void Coaster()
        {
            Group("02 Roller coaster - six editable hill and track sections");
            for (int sector = 0; sector < 6; sector++)
            {
                Vector3 min = new Vector3(float.MaxValue, 0, float.MaxValue), max = new Vector3(float.MinValue, 34, float.MinValue);
                for (int sample = 0; sample <= 120; sample++)
                {
                    float a = (sector + sample / 120f) * Mathf.PI / 3;
                    var p = new Vector3(-32 + 25 * Mathf.Cos(a), 0, 130 + 29 * Mathf.Sin(a));
                    min = Vector3.Min(min, p - Vector3.one * 2);
                    max = Vector3.Max(max, p + Vector3.one * 2);
                }

                min.x = Mathf.Floor(min.x * 2) / 2;
                min.z = Mathf.Floor(min.z * 2) / 2;
                min.y = 0;
                var d = new Vector3Int(Mathf.CeilToInt((max.x - min.x) * 2), 70, Mathf.CeilToInt((max.z - min.z) * 2));
                Body("Roller coaster section " + (sector + 1), min, d, 71 + sector, .5f, new[] { Pink,
                    Yellow, Blue, Cyan, White, Dark });
            }

            Center("Roller coaster boarding station", -32, 100, new Vector3Int(40, 16, 20), 83, .4f,
                new[] { Cyan, Yellow, Purple });
            Center("Pirate ship swing", -32, 130, new Vector3Int(48, 50, 24), 89, .4f, new[] { Blue, Yellow, Orange });
        }

        static void MainRides()
        {
            Group("03 Ferris wheel and drop tower");
            Center("Giant rainbow Ferris wheel", 33, 145, new Vector3Int(74, 90, 12), 77, .45f, new[] { Yellow,
                White, Blue, Pink, Cyan, Orange, Lime });
            Center("Sky drop tower", 11, 116, new Vector3Int(20, 100, 20), 80, .44f, new[] { Purple, Yellow, Pink });
            Group("04 Carousels and playground swings");
            Center("Grand striped carousel", -34, 65, new Vector3Int(36, 36, 36), 78, .4f, new[] { Purple,
                Yellow, White, Pink, Cyan, Orange, Lime });
            Center("Children carousel", 45, 59, new Vector3Int(28, 28, 28), 78, .4f, new[] { Cyan, Yellow,
                White, Purple, Pink, Orange, Lime });
            Center("Pink double swings", -13, 65, new Vector3Int(36, 31, 18), 79, .38f, new[] { Pink, Yellow, Cyan });
            Center("Blue double swings", 18, 62, new Vector3Int(34, 28, 18), 79, .38f, new[] { Blue, Yellow, Orange });
            Group("05 Bumper cars and open pavilion");
            Center("Bumper cars pavilion", 36, 83, new Vector3Int(57, 24, 38), 83, .4f, new[] { Cyan, Yellow, Purple });
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 4; i++)
                {
                    var b = Center("Bumper car " + row + "-" + i, 28 + i * 5.2f, 79 + row * 7,
                        new Vector3Int(12, 10, 18), 82, .3f, new[] { Rainbow[(i + row) % 6], White, Yellow, Dark });
                    b.transform.Rotate(0, (i % 2 == 0 ? -12 : 12) + row * 180, 0);
                }
        }

        static void Entrance()
        {
            Group("06 Rainbow entrance, tickets and giant sweets");
            Center("Rainbow park entrance arch", 0, 49, new Vector3Int(46, 27, 6), 87, .45f, Rainbow);
            foreach (float x in new[]
            {
                -13f,
                13f
            }

            )
                Center("Entrance pennant " + x, x, 49, new Vector3Int(15, 44, 5), 86, .35f, new[] { Blue, Yellow, White });
            Center("Left ticket booth", -10, 56, new Vector3Int(14, 20, 14), 81, .36f, new[] { Pink, White, Cyan });
            Center("Right ticket booth", 10, 56, new Vector3Int(14, 20, 14), 81, .36f, new[] { Cyan, White, Pink });
            foreach (float x in new[]
            {
                -22f,
                29f
            }

            )
                Center("Giant ice cream " + x, x, 49, new Vector3Int(18, 42, 18), 85, .35f, new[] { Orange, Pink, White });
        }

        static void Stalls()
        {
            Group("07 Food court, games and balloon bouquets");
            int id = 0;
            foreach (float z in new[]
            {
                105f,
                120f
            }

            )
                foreach (float x in new[]
                {
                    26f,
                    38f,
                    51f
                }

                )
                {
                    Color c = Rainbow[id % 6];
                    Center((z < 110 ? "Food kiosk " : "Carnival game kiosk ") + id, x, z,
                        new Vector3Int(21, 24, 17), 81, .35f, new[] { c, White, Color.Lerp(c, Blue, .35f) });
                    Center("Balloon bouquet " + id, x + 4.4f, z, new Vector3Int(18, 38, 18), 84, .28f,
                        new[] { c, Yellow, White, Pink, Cyan, Lime });
                    id++;
                }

            foreach (float x in new[]
            {
                -47f,
                -18f
            }

            )
            {
                Center("Souvenir kiosk " + x, x, 88, new Vector3Int(22, 23, 17), 81, .36f, new[] { Orange, White, Purple });
                Center("Souvenir balloons " + x, x + 5, 88, new Vector3Int(18, 38, 18), 84, .28f,
                    new[] { Pink, Yellow, White, Purple, Orange, Cyan });
            }
        }

        static void Furniture()
        {
            Group("08 Fountain plaza and benches");
            Center("Central park fountain", 0, 84, new Vector3Int(20, 8, 20), 20, .35f, new[] { White, Yellow, Cyan });
            int id = 0;
            foreach (float x in new[]
            {
                -8f,
                8f
            }

            )
                foreach (float z in new[]
                {
                    79f,
                    89f,
                    105f,
                    145f
                }

                )
                {
                    Center("Park bench " + id, x, z, new Vector3Int(12, 7, 5), 5, .32f, new[] { Rainbow[id % 6], Dark, Yellow });
                    Center("Litter bin " + id, x + 2.7f, z, new Vector3Int(4, 6, 4), 12, .32f, new[] { Cyan, Blue, Yellow });
                    id++;
                }

            Group("09 Bright fences, flags and street lamps");
            id = 0;
            foreach (float x in new[]
            {
                -63f,
                62f
            }

            )
                for (int z = 44; z < 164; z += 6)
                {
                    var b = Center("Boundary fence " + id, x, z, new Vector3Int(18, 7, 3), 17, .3f,
                        new[] { Rainbow[id % 6], Yellow, White });
                    b.transform.Rotate(0, 90, 0);
                    id++;
                }

            for (int x = -58; x <= 58; x += 6)
            {
                Center("Northern fence " + x, x, 164, new Vector3Int(18, 7, 3), 17, .3f,
                    new[] { Rainbow[Mathf.Abs(x) % 6], Yellow, White });
            }

            foreach (float x in new[]
            {
                -8f,
                8f
            }

            )
                foreach (float z in new[]
                {
                    72f,
                    96f,
                    136f,
                    158f
                }

                )
                    Center("Park street lamp " + x + " " + z, x, z, new Vector3Int(8, 27, 4), 6, .3f,
                        new[] { Purple, Blue, Yellow });
            foreach (float x in new[]
            {
                -58f,
                57f
            }

            )
                foreach (float z in new[]
                {
                    63f,
                    91f,
                    118f,
                    144f
                }

                )
                    Center("Tall park flag " + x + " " + z, x, z, new Vector3Int(15, 43, 5), 86, .35f,
                        new[] { Blue, Pink, Yellow });
            Group("10 Landscaping");
            foreach (float x in new[]
            {
                -57f,
                -48f,
                -4f,
                7f,
                56f
            }

            )
                foreach (float z in new[]
                {
                    58f,
                    72f,
                    92f
                }

                )
                    if (!(Mathf.Abs(x) < 8 && z < 65))
                        Center("Decorative tree " + x + " " + z, x, z, new Vector3Int(10, 14, 10),
                            4, .38f, new[] { Orange, new Color(.13f, .57f, .24f), Lime });
        }

        static void Sign()
        {
            var hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            var go = new GameObject("Park entrance title", typeof(TMPro.TextMeshPro), typeof(LaserWorldLabel));
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(0, 13.4f, 49);
            var text = go.GetComponent<TMPro.TextMeshPro>();
            text.font = hud.font;
            text.fontSharedMaterial = hud.outlinedText;
            text.fontSize = 9;
            text.color = Yellow;
            text.fontStyle = TMPro.FontStyles.Bold;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            text.text = "ПАРК АТТРАКЦИОНОВ";
            text.rectTransform.sizeDelta = new Vector2(28, 2);
            text.UpdateMeshPadding();
        }

        static void Group(string name)
        {
            section = new GameObject(name).transform;
            section.SetParent(root, false);
        }

        static LaserVoxelBody Center(string name, float x, float z, Vector3Int d, int style, float cell,
            Color[] colors) => Body(name, new Vector3(x - d.x * cell * .5f, 0, z - d.z * cell * .5f), d, style, cell, colors);
        static LaserVoxelBody Body(string name, Vector3 pos, Vector3Int d, int style, float cell, Color[] colors,
            bool ground = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(section, false);
            go.transform.position = pos;
            var body = go.AddComponent<LaserVoxelBody>();
            body.displayName = name;
            body.dimensions = d;
            body.cellSize = cell;
            body.style = style;
            body.hitPoints = ground ? 5 : 4;
            body.terrain = ground;
            body.requiresGroundSupport = !ground;
            body.collapseUnsupported = !ground;
            body.allowDestruction = true;
            body.autoRespawn = false;
            body.singleMaterialSurface = true;
            body.surfaceColors = colors;
            body.bodyColor = colors[0];
            body.trimColor = colors[1];
            body.accentColor = colors[2];
            go.GetComponent<MeshRenderer>().sharedMaterial = plastic;
            body.Generate();
            AssetDatabase.CreateAsset(go.GetComponent<MeshFilter>().sharedMesh,
                Folder + "/ParkMesh" + (meshId++).ToString("D4") + ".asset");
            return body;
        }
    }
}
