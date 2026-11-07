using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserOceanSetup
    {
        const string Folder = "Assets/LaserHero/Ocean";
        static Transform root, section;
        static Material plastic;
        static int meshId;
        static readonly Color Sand = new Color(.91f, .78f, .48f), Pale = new Color(1, .92f, .67f), Stone =
            new Color(.13f, .4f, .48f), LightStone = new Color(.32f, .7f, .7f), Gold = new Color(1, .72f, .12f), Pink =
            new Color(1, .18f, .53f), Purple = new Color(.57f, .2f, .9f), Cyan = new Color(.08f, .85f, .94f), Orange =
            new Color(1, .4f, .12f), Green = new Color(.08f, .57f, .31f), Lime = new Color(.38f, .91f, .25f), Wood =
            new Color(.35f, .18f, .09f);
        [MenuItem("Laser Hero/Create editable ocean arena")]
        public static string Build()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode before creating an arena");
            var arena = UnityEngine.Object.FindFirstObjectByType<LaserArena>();
            if (arena == null || arena.gameObject.scene.path != "Assets/Scenes/Lobby.unity")
                throw new Exception("Open Lobby scene");
            if (arena.transform.Find("Ocean Arena") != null)
                throw new Exception("Ocean Arena already exists; edit its separate objects directly");
            EditorSceneManager.SaveScene(arena.gameObject.scene, "Assets/Scenes/Backups/Lobby_BeforeOcean.unity", true);
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            meshId = 0;
            plastic = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/LaserHero/BatchedWorld/SharedPlastic.mat"))
            {
                name = "Ocean editable stud plastic",
                enableInstancing = true
            };
            plastic.SetColor("_Color", Color.white);
            plastic.SetFloat("_UseVertexColor", 1);
            plastic.SetFloat("_IsDynamic", 0);
            AssetDatabase.CreateAsset(plastic, Folder + "/OceanPlastic.mat");
            root = new GameObject("Ocean Arena").transform;
            root.SetParent(arena.transform, false);
            // Keep the completed previous arenas intact. No cross-object mesh combining.
            root.gameObject.SetActive(false);
            try
            {
                AssetDatabase.StartAssetEditing();
                Terrain();
                Reefs();
                Ruins();
                Shipwreck();
                ResearchSite();
                Details();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Bubbles();
            foreach (string name in new[]
            {
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
            SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(0, 15, 102), new Vector3(135, 75, 135)), false);
            string report = "OCEAN_READY: " + root.GetComponentsInChildren<LaserVoxelBody>()
                .Length + " separate editable destructible bodies; 128x128m; 16m deep ground; existing arenas preserved inactive";
            File.WriteAllText("LaserHeroQA/ocean-setup.txt", report);
            return report;
        }

        static void Terrain()
        {
            Group("01 Seabed - 16m deep sand and stone");
            for (int z = 0; z < 16; z++)
                for (int x = 0; x < 16; x++)
                    Body("Seabed 16m [" + x + "," + z + "]", new Vector3(-64 + x * 8, -16, 38 + z * 8),
                        new Vector3Int(16, 32, 16), 50, .5f, new[] { Sand, new Color(.53f, .42f, .29f), new Color(.12f,
                        .25f, .34f), new Color(.53f, .75f, .63f), Pale }, true);
        }

        static void Reefs()
        {
            Group("02 Coral gardens - branching coral and tube sponges");
            var colors = new[]
            {
                Pink,
                Purple,
                Orange,
                Cyan
            };
            int id = 0;
            foreach (float z in new[]
            {
                58f,
                78f
            }

            )
                foreach (float x in new[]
                {
                    -49f,
                    -32f,
                    -16f,
                    16f,
                    32f,
                    49f
                }

                )
                {
                    Color c = colors[id % 4];
                    int height = 34 + id % 4 * 9;
                    Center("Branching coral " + id, x, z, new Vector3Int(18, height, 18), 51, .45f,
                        new[] { c, Color.Lerp(c, Color.white, .4f), Stone });
                    Center("Tube sponge cluster " + id, x + 4.6f, z + 5, new Vector3Int(14,
                        23 + id % 3 * 9, 14), 52, .35f, new[] { colors[(id + 1) % 4], Pale, Stone });
                    id++;
                }

            foreach (float x in new[]
            {
                -55f,
                55f
            }

            )
                foreach (float z in new[]
                {
                    97f,
                    116f,
                    138f,
                    155f
                }

                )
                {
                    Color c = colors[id % 4];
                    Center("Giant reef coral " + id, x, z, new Vector3Int(22, 58 + id % 3 * 12, 22), 51,
                        .45f, new[] { c, Color.Lerp(c, Color.white, .4f), Stone });
                    id++;
                }

            Group("03 Reef cliffs and tall kelp forest");
            id = 0;
            foreach (float x in new[]
            {
                -61f,
                60f
            }

            )
                foreach (float z in new[]
                {
                    53f,
                    74f,
                    95f,
                    117f,
                    139f,
                    159f
                }

                )
                {
                    Center("Weathered sea stack " + id, x, z, new Vector3Int(17, 48 + id % 4 * 12, 19),
                        54, .45f, new[] { Stone, LightStone, new Color(.44f, .77f, .72f) });
                    for (int k = 0; k < 3; k++)
                        Center("Kelp grove " + id + " frond " + k, x + (x < 0 ? 4
                            : -4) + k * 1.4f, z - 4 + k * 2, new Vector3Int(12, 35 + (id + k) % 4 * 10, 8), 53, .38f,
                            new[] { Green, Lime, Stone });
                    id++;
                }

            foreach (float x in new[]
            {
                -40f,
                -25f,
                26f,
                41f
            }

            )
                Center("Northern reef spire " + x, x, 160, new Vector3Int(20, 70, 20), 54, .45f, new[] { Stone, LightStone, Cyan });
        }

        static void Ruins()
        {
            Group("04 Sunken temple - arches, columns and rubble");
            // The central approach stays clear and the arches have real openings.
            for (int row = 0; row < 3; row++)
                foreach (float x in new[]
                {
                    -17f,
                    17f
                }

                )
                    Center("Temple colonnade " + row + " " + x, x, 103 + row * 15, new Vector3Int(12,
                        45 + row * 11, 12), 55, .5f, new[] { LightStone, Sand, Gold });
            Center("Temple entrance arch", 0, 100, new Vector3Int(42, 38, 6), 56, .5f, new[] { Stone, Gold, LightStone });
            Center("Great sunken arch", 0, 132, new Vector3Int(44, 58, 8), 56, .5f, new[] { LightStone, Gold, Stone });
            foreach (float x in new[]
            {
                -11f,
                11f
            }

            )
                Center("Sanctuary tower " + x, x, 155, new Vector3Int(15, 84, 15), 55, .5f, new[] { Stone, Gold, LightStone });
            Center("Sanctuary crown arch", 0, 156, new Vector3Int(38, 64, 8), 56, .5f, new[] { LightStone, Gold, Stone });
            for (int i = 0; i < 12; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * (9 + i % 3 * 3), z = 108 + i / 2 * 7;
                Center("Temple fallen masonry " + i, x, z, new Vector3Int(8 + i % 4 * 2, 4 + i % 3 * 2, 9),
                    54, .45f, new[] { LightStone, Stone, Sand });
            }

            Center("Sanctuary treasure", 0, 149, new Vector3Int(12, 9, 9), 59, .4f, new[] { Wood, Gold, Cyan });
        }

        static void Shipwreck()
        {
            Group("05 Shipwreck cove - hull, masts and lost cargo");
            Center("Sunken galleon hull", -37, 116, new Vector3Int(28, 20, 58), 57, .45f, new[] { Wood,
                new Color(.54f, .31f, .12f), Gold });
            Center("Galleon main mast and torn sail", -37, 113, new Vector3Int(32, 76, 5), 58, .45f,
                new[] { Wood, Gold, new Color(.88f, .87f, .68f) });
            Center("Galleon foremast and torn sail", -37, 123, new Vector3Int(23, 54, 5), 58, .45f,
                new[] { Wood, Gold, new Color(.88f, .87f, .68f) });
            for (int i = 0; i < 9; i++)
            {
                Center("Lost cargo barrel " + i, -47 + i % 3 * 3.8f, 134 + i / 3 * 3.4f, new Vector3Int(8,
                    11, 8), 62, .3f, new[] { Wood, Gold, Stone });
                if (i % 3 == 0)
                    Center("Shipwreck treasure chest " + i, -28, 107 + i * 3.8f, new Vector3Int(10, 8,
                        8), 59, .35f, new[] { Wood, Gold, Pale });
            }

            Center("Shipwreck encrusted coral", -26, 126, new Vector3Int(18, 39, 18), 51, .4f, new[] { Orange, Pale, Stone });
        }

        static void ResearchSite()
        {
            Group("06 Submarine exploration site");
            Center("Yellow exploration submarine", 37, 115, new Vector3Int(24, 25, 56), 63, .45f, new[] { Gold,
                Orange, new Color(.035f, .24f, .36f) });
            for (int i = 0; i < 6; i++)
            {
                Center("Research supply crate " + i, 28 + i % 2 * 4, 134 + i / 2 * 4, new Vector3Int(8, 7,
                    8), 18, .4f, new[] { new Color(.2f, .52f, .6f), LightStone, Gold });
            }

            foreach (float z in new[]
            {
                100f,
                130f
            }

            )
                Center("Survey beacon " + z, 45, z, new Vector3Int(9, 28, 9), 55, .35f, new[] { LightStone, Stone, Cyan });
            for (int i = 0; i < 4; i++)
                Center("Research sponge bed " + i, 27 + i * 5, 93, new Vector3Int(13, 25 + i % 2 * 10, 13),
                    52, .35f, new[] { Purple, Pink, Stone });
        }

        static bool Reserved(float x, float z) => Mathf.Abs(x) < 7 || Mathf.Abs(x) < 23 && z > 95 || x > -52
            && x < -22 && z > 98 && z < 146 || x > 24 && x < 49 && z > 98 && z < 147;
        static void Details()
        {
            Group("07 Shells, starfish and small reef life");
            var random = new System.Random(8431);
            int count = 0;
            for (int attempt = 0; attempt < 500 && count < 62; attempt++)
            {
                float x = (float)random.NextDouble() * 112 - 56, z = 46 + (float)random.NextDouble() * 109;
                if (Reserved(x, z))
                    continue;
                int style = count % 3 == 0 ? 61 : 60;
                Color c = count % 4 == 0 ? Pink : count % 4 == 1 ? Orange : count % 4 == 2 ? Pale : Cyan;
                Center((style == 61 ? "Starfish " : "Scallop shell ") + count, x, z, new Vector3Int(8,
                    style == 61 ? 3 : 5, 8), style, .28f, new[] { c, Color.Lerp(c, Color.white, .5f), Gold });
                count++;
            }

            Group("08 Smaller coral outcrops");
            for (int i = 0; i < 18; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * (24 + i % 3 * 9), z = 49 + i / 2 * 12;
                if (Reserved(x, z))
                    continue;
                Center("Reef outcrop " + i, x, z, new Vector3Int(13, 19 + i % 4 * 5, 13), i % 2 == 0 ? 51
                    : 52, .32f, new[] { i % 3 == 0 ? Cyan : Pink, Orange, Stone });
            }
        }

        static void Bubbles()
        {
            Group("09 Ambient bubble plumes");
            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"))
            {
                name = "Ocean bubble material"
            };
            mat.SetColor("_BaseColor", new Color(.65f, 1, 1, .32f));
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_SrcBlend", 5);
            mat.SetFloat("_DstBlend", 10);
            mat.SetFloat("_ZWrite", 0);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            AssetDatabase.CreateAsset(mat, Folder + "/OceanBubbles.mat");
            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var sphere = temp.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(temp);
            foreach (var point in new[]
            {
                new Vector3(-27, .3f, 83),
                new Vector3(25, .3f, 79),
                new Vector3(-46, .3f, 131),
                new Vector3(47, .3f, 135),
                new Vector3(0, .3f, 143)
            }

            )
            {
                var go = new GameObject("Rising bubbles " + point, typeof(ParticleSystem));
                go.transform.SetParent(section, false);
                go.transform.position = point;
                var ps = go.GetComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = true;
                main.startLifetime = 9;
                main.startSpeed = .8f;
                main.startSize = new ParticleSystem.MinMaxCurve(.05f, .15f);
                main.startColor = new Color(.7f, 1, 1, .4f);
                main.maxParticles = 24;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = ps.emission;
                emission.rateOverTime = 2;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(3, .1f, 3);
                go.transform.rotation = Quaternion.Euler(-90, 0, 0);
                var render = ps.GetComponent<ParticleSystemRenderer>();
                render.renderMode = ParticleSystemRenderMode.Mesh;
                render.mesh = sphere;
                render.sharedMaterial = mat;
                render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
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
                Folder + "/OceanMesh" + (meshId++).ToString("D4") + ".asset");
            return body;
        }
    }
}
