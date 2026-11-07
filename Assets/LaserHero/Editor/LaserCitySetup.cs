using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserCitySetup
    {
        const string Folder = "Assets/LaserHero/GeneratedCity";
        static Transform root;
        static Material studs;
        static Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        static int meshId;
        static readonly Color Grass = new Color(.13f, .76f, .22f), Dirt = new Color(.44f, .3f, .18f), Road =
            new Color(.18f, .23f, .3f), Paving = new Color(.7f, .76f, .84f), White = new Color(.95f, .94f, .76f), Glass =
            new Color(.16f, .76f, .95f), Steel = new Color(.26f, .34f, .43f), Wood = new Color(.6f, .32f, .12f), Green =
            new Color(.04f, .58f, .2f), Gold = new Color(1, .65f, .06f);
        [MenuItem("Laser Hero/Expand arena to LEGO city")]
        public static string Build()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first");
            if (GameObject.Find("LEGO City") != null)
                throw new InvalidOperationException("City already exists; use the saved scene");
            var arena = UnityEngine.Object.FindFirstObjectByType<LaserArena>();
            if (arena == null || arena.gameObject.scene.path != "Assets/Scenes/Lobby.unity")
                throw new InvalidOperationException("Open Lobby scene");
            EditorSceneManager.SaveScene(arena.gameObject.scene, "Assets/Scenes/Backups/Lobby_BeforeCity.unity", true);
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            studs = AssetDatabase.LoadAssetAtPath<Material>("Assets/Graphics/Material/Global/Studs.mat");
            materials.Clear();
            meshId = 0;
            var previous = GameObject.Find("LEGO Arena");
            if (previous != null)
            {
                previous.name = "LEGO Arena — Prototype Backup";
                previous.SetActive(false);
            }

            root = new GameObject("LEGO City").transform;
            root.SetParent(arena.transform, false);
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int z = 0; z < 16; z++)
                    for (int x = 0; x < 16; x++)
                        Body("City ground " + x + "," + z, new Vector3(-64 + x * 8, -2, 38 + z * 8),
                            new Vector3Int(16, 4, 16), 9, .5f, 2, new[] { Dirt, Grass, Road, Paving, White }, true);
                float[] xs =
                {
                    -52,
                    -20,
                    20,
                    52
                };
                float[] zs =
                {
                    80,
                    122,
                    156
                };
                Color[] facades =
                {
                    new Color(.93f, .63f, .29f),
                    new Color(.45f, .67f, .9f),
                    new Color(.82f, .43f, .35f),
                    new Color(.6f, .52f, .82f)
                };
                for (int row = 0; row < 3; row++)
                    for (int col = 0; col < 4; col++)
                    {
                        bool site = row == 1 && col == 3 || row == 2 && col == 0;
                        int floors = 5 + (row * 3 + col * 2) % 8;
                        int width = col == 0 || col == 3 ? 24 : 28, depth = row == 2 ? 22 : 28;
                        Vector3 pos = new Vector3(xs[col] - width * .25f, 0, zs[row] - depth * .25f);
                        Body(site ? "Construction frame " + row
                            : "High-rise " + row + "-" + col, pos, new Vector3Int(width, site ? 64
                            : floors * 6 + 8, depth), site ? 15 : 10, .5f, site ? 7 : 6, new[] { site ? Paving
                            : facades[(row + col) % facades.Length], site ? Gold : White, Glass });
                    }

                var crane = Body("Tower crane East", new Vector3(62, 0, 110), new Vector3Int(70, 100, 12),
                    11, .5f, 6, new[] { Gold, Steel, Glass });
                crane.transform.rotation = Quaternion.Euler(0, 180, 0);
                Body("Tower crane West", new Vector3(-63, 0, 133), new Vector3Int(70, 92, 12), 11, .5f, 6,
                    new[] { new Color(1, .35f, .08f), Steel, Glass });
                // Street lamps alternate sides of each avenue; every post is one coherent body.
                for (int z = 46; z <= 158; z += 16)
                    foreach (int side in new[]
                    {
                        -1,
                        1
                    }

                    )
                    {
                        Body("Street lamp", new Vector3(side * 5.8f, 0, z), new Vector3Int(8, 30,
                            4), 6, .3f, 3, new[] { Steel, Steel, new Color(1, .94f, .5f) });
                        if (z >= 78)
                            Body("Outer street lamp", new Vector3(side * 45.8f, 0, z),
                                new Vector3Int(8, 30, 4), 6, .3f, 3, new[] { Steel, Steel, new Color(1, .94f, .5f) });
                    }

                foreach (float z in new[]
                {
                    51f,
                    70,
                    94,
                    111,
                    135,
                    148
                }

                )
                    foreach (int side in new[]
                    {
                        -1,
                        1
                    }

                    )
                    {
                        Body("Park bench", new Vector3(side * 9, 0, z), new Vector3Int(12, 7, 5), 5,
                            .25f, 3, new[] { Wood, Steel, Gold });
                        Body("Litter bin", new Vector3(side * 12.8f, 0, z), new Vector3Int(4, 6, 4),
                            12, .25f, 3, new[] { Steel, new Color(.12f, .18f, .22f), Green });
                        Body("Planter bush", new Vector3(side * 16, 0, z), new Vector3Int(8, 7, 8),
                            13, .35f, 2, new[] { new Color(.76f, .4f, .22f), Green, Grass });
                    }

                foreach (float x in new[]
                {
                    -58f,
                    -32,
                    -8,
                    8,
                    32,
                    56
                }

                )
                    foreach (float z in new[]
                    {
                        55f,
                        108,
                        145
                    }

                    )
                    {
                        Body("City tree", new Vector3(x, 0, z), new Vector3Int(10, 14, 10), 4, .5f,
                            3, new[] { Wood, Green, Grass });
                        Body("Fire hydrant", new Vector3(x + 3, 0, z - 1), new Vector3Int(6, 8, 6),
                            19, .2f, 3, new[] { new Color(.95f, .15f, .06f), Gold, Steel });
                    }

                foreach (float z in new[]
                {
                    74f,
                    90,
                    116,
                    132,
                    151
                }

                )
                    foreach (int side in new[]
                    {
                        -1,
                        1
                    }

                    )
                        Body("Parked car", new Vector3(side < 0 ? -3.7f
                            : 1.3f, 0, z), new Vector3Int(8, 6, 18), 16, .3f, 4, new[] { side < 0
                            ? new Color(.98f, .24f, .12f) : new Color(.1f, .58f, .92f), new Color(.1f, .12f, .15f), Glass });
                for (int i = 0; i < 4; i++)
                {
                    Body("Construction safety fence", new Vector3(43 + i * 5, 0, 108),
                        new Vector3Int(18, 8, 2), 17, .3f, 3, new[] { Steel, Gold, White });
                    Body("Construction crate", new Vector3(46 + i * 3.3f, 0, 131), new Vector3Int(6, 6,
                        6), 18, .35f, 4, new[] { Wood, Gold, Steel });
                    Body("West construction fence", new Vector3(-63 + i * 5, 0, 145), new Vector3Int(18,
                        8, 2), 17, .3f, 3, new[] { Steel, Gold, White });
                }

                Body("Entrance plaza fountain", new Vector3(17, 0, 45), new Vector3Int(16, 7, 16), 20, .4f,
                    4, new[] { Paving, White, new Color(.07f, .66f, 1) });
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            arena.arenaBounds = new Bounds(new Vector3(0, 25, 102), new Vector3(128, 80, 128));
            arena.entry = new Vector3(0, 1, 42);
            arena.hero.view.farClipPlane = 350;
            arena.hero.flightCeiling = 65;
            EditorUtility.SetDirty(arena);
            EditorUtility.SetDirty(arena.hero);
            EditorUtility.SetDirty(arena.hero.view);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(arena.gameObject.scene);
            EditorSceneManager.SaveScene(arena.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            return "CITY_READY: " + root.GetComponentsInChildren<LaserVoxelBody>().Length + " voxel bodies, 128 x 128 m";
        }

        static Material Mat(Color colour)
        {
            if (materials.TryGetValue(colour, out var m))
                return m;
            string path = Folder + "/Studs" + materials.Count + ".mat";
            m = new Material(studs);
            m.SetColor("_Color", colour);
            m.SetColor("_BaseColor", colour);
            m.SetFloat("_Scale", .5f);
            m.SetFloat("_IsDynamic", 1);
            m.SetFloat("_AmbientIntensity", 1.3f);
            m.SetVector("_ObjectScale", Vector4.one);
            AssetDatabase.CreateAsset(m, AssetDatabase.GenerateUniqueAssetPath(path));
            materials[colour] = m;
            return m;
        }

        static LaserVoxelBody Body(string name, Vector3 pos, Vector3Int dimensions, int style, float cell, float hp,
            Color[] colours, bool ground = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var body = go.AddComponent<LaserVoxelBody>();
            body.displayName = name;
            body.dimensions = dimensions;
            body.style = style;
            body.cellSize = cell;
            body.hitPoints = hp;
            body.terrain = ground;
            body.requiresGroundSupport = !ground;
            body.collapseUnsupported = true;
            body.allowDestruction = true;
            body.autoRespawn = false;
            body.surfaceColors = colours;
            body.bodyColor = colours[0];
            body.trimColor = colours[1];
            body.accentColor = colours[2];
            go.GetComponent<MeshRenderer>().sharedMaterials = colours.Select(Mat).ToArray();
            body.Generate();
            AssetDatabase.CreateAsset(go.GetComponent<MeshFilter>().sharedMesh,
                AssetDatabase.GenerateUniqueAssetPath(Folder + "/CityMesh" + meshId++ + ".asset"));
            return body;
        }
    }
}
