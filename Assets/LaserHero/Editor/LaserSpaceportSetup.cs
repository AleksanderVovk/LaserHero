using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserSpaceportSetup
    {
        const string Folder = "Assets/LaserHero/Spaceport";
        static Transform root, section;
        static Material plastic;
        static int meshId;
        static readonly Color White = new Color(.94f, .98f, 1), Blue = new Color(.04f, .48f, .97f), Glass =
            new Color(.02f, .8f, 1), Steel = new Color(.12f, .25f, .34f), Red = new Color(1, .13f, .08f), Yellow =
            new Color(1, .79f, .05f), Grass = new Color(.16f, .91f, .24f), Gray = new Color(.63f, .74f, .81f), Black =
            new Color(.055f, .09f, .13f);
        [MenuItem("Laser Hero/Create editable spaceport arena")]
        public static string Build()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode");
            var arena = UnityEngine.Object.FindFirstObjectByType<LaserArena>();
            if (arena == null || arena.gameObject.scene.path != "Assets/Scenes/Lobby.unity")
                throw new Exception("Open Lobby scene");
            if (arena.transform.Find("Spaceport Arena") != null)
                throw new Exception("Spaceport already exists; edit its objects directly");
            EditorSceneManager.SaveScene(arena.gameObject.scene, "Assets/Scenes/Backups/Lobby_BeforeSpaceport.unity", true);
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            meshId = 0;
            var old = arena.transform.Find("LEGO City");
            if (old != null)
                old.gameObject.SetActive(false);
            // Existing sector renderer can contain the old arena; leave it saved but disabled.
            var oldSectors = arena.transform.Find("World render sectors");
            if (oldSectors != null)
                oldSectors.gameObject.SetActive(false);
            plastic = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/LaserHero/BatchedWorld/SharedPlastic.mat"))
            {
                name = "Spaceport editable stud plastic",
                enableInstancing = true
            };
            plastic.SetColor("_Color", Color.white);
            plastic.SetFloat("_UseVertexColor", 1);
            plastic.SetFloat("_IsDynamic", 0);
            AssetDatabase.CreateAsset(plastic, Folder + "/SpaceportPlastic.mat");
            root = new GameObject("Spaceport Arena").transform;
            root.SetParent(arena.transform, false);
            Group("01 Deep terrain and roads");
            for (int z = 0; z < 16; z++)
                for (int x = 0; x < 16; x++)
                    Body("Ground 12m [" + x + "," + z + "]", new Vector3(-64 + x * 8, -12, 38 + z * 8),
                        new Vector3Int(16, 24, 16), 30, .5f, 5, new[] { new Color(.25f, .3f, .4f), Grass, new Color(.075f,
                        .13f, .19f), Gray, White, new Color(.43f, .34f, .26f) }, true);
            Group("02 Assembly district");
            Body("Vehicle assembly hall", new Vector3(-33, 0, 70), new Vector3Int(48, 58, 48), 33, .5f, 6,
                new[] { White, Blue, Glass });
            Body("Curved service hangar", new Vector3(-62, 0, 72), new Vector3Int(28, 26, 42), 39, .5f, 5,
                new[] { White, Blue, Glass });
            Body("Assembly radio antenna", new Vector3(-8, 0, 95), new Vector3Int(6, 74, 6), 32, .3f, 4,
                new[] { Red, White, Yellow });
            // Ground-mounted workstations and vehicles remain independent, easy to rearrange.
            for (int i = 0; i < 3; i++)
            {
                Body("Assembly cargo trolley " + i, new Vector3(-29 + i * 6, 0, 68), new Vector3Int(8, 6,
                    16), 16, .28f, 3, new[] { i == 0 ? Yellow : Blue, Black, Glass });
                Body("Assembly storage crate " + i, new Vector3(-61 + i * 4, 0, 94), new Vector3Int(8, 7,
                    8), 18, .35f, 3, new[] { Yellow, Steel, White });
            }

            Group("03 Mission control");
            Body("Mission control and observation tower", new Vector3(7, 0, 72), new Vector3Int(48, 58, 40), 34,
                .5f, 6, new[] { White, Blue, Glass });
            Body("Communications dish", new Vector3(27, 0, 95), new Vector3Int(18, 24, 18), 36, .35f, 4,
                new[] { White, Steel, Yellow });
            Body("Relay mast", new Vector3(9, 0, 95), new Vector3Int(7, 108, 7), 32, .4f, 4, new[] { Red, White, Yellow });
            Group("04 Launch complexes");
            Launch("A", -20, 122, 100, Red);
            Launch("B", 20, 122, 86, Blue);
            Launch("C", -53, 155, 92, Red);
            Launch("D", 20, 155, 110, Steel);
            Launch("E", 53, 155, 82, Blue);
            Group("05 Fuel and power district");
            foreach (float z in new[]
            {
                77f,
                87f,
                115f,
                128f
            }

            )
            {
                for (int i = 0; i < 2; i++)
                    Body("Fuel silo " + z + "-" + i, new Vector3(47 + i * 7, 0, z), new Vector3Int(14,
                        44 + (i % 2) * 10, 14), 35, .4f, 5, new[] { White, Red, Glass, Steel });
                Body("Fuel transfer pipe " + z, new Vector3(46, 0, z + 6), new Vector3Int(40, 12, 5), 38,
                    .35f, 4, new[] { Steel, Red, Yellow });
            }

            for (int i = 0; i < 4; i++)
            {
                Body("Power transformer " + i, new Vector3(-61, 0, 110 + i * 6), new Vector3Int(15, 14, 10),
                    12, .4f, 4, new[] { Gray, Steel, Glass });
                Body("Power pylon " + i, new Vector3(-49, 0, 111 + i * 6), new Vector3Int(6, 72, 6), 32,
                    .4f, 4, new[] { Steel, White, Yellow });
            }

            Group("06 Logistics yard");
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 4; col++)
                {
                    var color = new[]
                    {
                        Red,
                        Blue,
                        Yellow,
                        new Color(.15f, .78f, .5f)
                    }[col];
                    Body("Freight container " + row + "-" + col, new Vector3(-32 + col * 6, 0,
                        149 + row * 8), new Vector3Int(12, 10, 18), 37, .4f, 4, new[] { color, Color.Lerp(color, White,
                        .25f), Steel });
                }

            Group("07 Visitor parking and gardens");
            for (int i = 0; i < 7; i++)
            {
                Body("Visitor car " + i, new Vector3(-60 + i * 7, 0, 46), new Vector3Int(8, 6, 18), 16,
                    .35f, 3, new[] { new[] { Red, Blue, White, Yellow }[i % 4], Black, Glass });
                Solid("Parking divider " + i, new Vector3(-61 + i * 7, -.03f, 44), new Vector3Int(1, 1, 16), .3f, White);
            }

            Body("Visitor fountain", new Vector3(13, 0, 46), new Vector3Int(22, 7, 22), 20, .35f, 4, new[] { Gray, White, Glass });
            foreach (float x in new[]
            {
                9f,
                26f,
                48f,
                58f
            }

            )
                foreach (float z in new[]
                {
                    43f,
                    55f
                }

                )
                {
                    Body("Garden tree", new Vector3(x, 0, z), new Vector3Int(10, 14, 10), 4, .4f, 3,
                        new[] { new Color(.57f, .31f, .12f), new Color(.04f, .65f, .22f), Grass });
                }

            foreach (float x in new[]
            {
                28f,
                48f
            }

            )
            {
                Body("Visitor bench", new Vector3(x, 0, 49), new Vector3Int(12, 7, 5), 5, .3f, 3,
                    new[] { new Color(.81f, .49f, .17f), Steel, Yellow });
                Body("Visitor litter bin", new Vector3(x + 4, 0, 49), new Vector3Int(4, 6, 4), 12, .3f, 3,
                    new[] { Blue, Black, White });
            }

            Group("08 Street furniture and safety");
            foreach (float x in new[]
            {
                -45.7f,
                -5.7f,
                5.7f,
                44.5f
            }

            )
                for (int z = 49; z < 166; z += 19)
                    Body("Road light", new Vector3(x, 0, z), new Vector3Int(8, 32, 4), 6, .3f, 3, new[] { Steel, Steel, Yellow });
            foreach (float z in new[]
            {
                68f,
                97f,
                108f,
                138f,
                149f,
                164f
            }

            )
                foreach (float x in new[]
                {
                    -34f,
                    -7f,
                    7f,
                    33f,
                    46f,
                    60f
                }

                )
                {
                    Body("Safety bollard", new Vector3(x, 0, z), new Vector3Int(2, 7, 2), 8, .25f, 3, new[] { Yellow, Red, White });
                }

            // Green borders and roadside shrubs are placed outside the carriageways.
            foreach (float z in new[]
            {
                71f,
                92f,
                111f,
                133f,
                152f
            }

            )
                foreach (float x in new[]
                {
                    -63f,
                    61f
                }

                )
                    Body("Perimeter planter", new Vector3(x, 0, z), new Vector3Int(7, 7, 7), 13, .35f,
                        3, new[] { Gray, new Color(.04f, .65f, .22f), Grass });
            for (int i = 0; i < 5; i++)
                foreach (float z in new[]
                {
                    69f,
                    135f
                }

                )
                    Body("Launch safety barrier", new Vector3(-33 + i * 5, 0, z), new Vector3Int(14, 7,
                        2), 17, .3f, 3, new[] { Steel, Yellow, White });
            arena.arenaBounds = new Bounds(new Vector3(0, 24, 102), new Vector3(128, 100, 128));
            arena.entry = new Vector3(0, 1, 42);
            arena.hero.flightCeiling = 80;
            arena.hero.view.farClipPlane = 400;
            EditorUtility.SetDirty(arena);
            EditorUtility.SetDirty(arena.hero);
            EditorUtility.SetDirty(arena.hero.view);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(arena.gameObject.scene);
            EditorSceneManager.SaveScene(arena.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(0, 18, 102), new Vector3(135, 80, 135)), false);
            return "SPACEPORT_READY: " + root.GetComponentsInChildren<LaserVoxelBody>()
                .Length + " separate editable bodies; 128x128m; terrain 12m; no batching";
        }

        static void Group(string name)
        {
            section = new GameObject(name).transform;
            section.SetParent(root, false);
        }

        static void Launch(string id, float x, float z, int height, Color accent)
        {
            Body("Launch pad " + id, new Vector3(x - 6, -.25f, z - 6), new Vector3Int(24, 1, 24), 40, .5f, 5,
                new[] { Black, Yellow, White });
            Body("Rocket " + id, new Vector3(x - 3.6f, 0, z - 3.6f), new Vector3Int(18, height, 18), 31, .4f, 5,
                new[] { White, accent, Glass, Steel });
            Body("Service tower " + id, new Vector3(x + 5, 0, z - 2), new Vector3Int(10, height + 15, 10), 32,
                .45f, 4, new[] { Steel, White, Yellow });
            // Entire access arm belongs to its own editable support bridge, not merged with the rocket.
            Body("Access gantry " + id, new Vector3(x + 1, 0, z - 5), new Vector3Int(12, 55, 4), 38, .4f, 4,
                new[] { Steel, Blue, Yellow });
            Body("Launch cabinet " + id, new Vector3(x - 6, 0, z + 6), new Vector3Int(6, 8, 5), 12, .4f, 3,
                new[] { White, Blue, Glass });
        }

        static LaserVoxelBody Solid(string name, Vector3 pos, Vector3Int d, float cell, Color color) =>
            Body(name, pos, d, 8, cell, 3, new[] { color, color, color }, false, false);
        static LaserVoxelBody Body(string name, Vector3 pos, Vector3Int d, int style, float cell, float hp,
            Color[] colors, bool ground = false, bool groundSupport = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(section, false);
            go.transform.position = pos;
            var b = go.AddComponent<LaserVoxelBody>();
            b.displayName = name;
            b.dimensions = d;
            b.cellSize = cell;
            b.style = style;
            b.hitPoints = hp;
            b.terrain = ground;
            b.requiresGroundSupport = !ground && groundSupport;
            b.collapseUnsupported = !ground;
            b.allowDestruction = true;
            b.autoRespawn = false;
            b.singleMaterialSurface = true;
            b.surfaceColors = colors;
            b.bodyColor = colors[0];
            b.trimColor = colors[1];
            b.accentColor = colors[2];
            go.GetComponent<MeshRenderer>().sharedMaterial = plastic;
            b.Generate();
            AssetDatabase.CreateAsset(go.GetComponent<MeshFilter>().sharedMesh,
                Folder + "/Mesh" + (meshId++).ToString("D4") + ".asset");
            return b;
        }
    }
}
