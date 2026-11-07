using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace LaserHero.Editor
{
    public static class LaserDroneSetup
    {
        const string Folder = "Assets/LaserHero/Drones";
        static Material blue, white, gold, dark, cyan;
        public static string RefinePlatforms()
        {
            const string path = Folder + "/OctagonalPedestal.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                for (int i = 0; i < 8; i++)
                {
                    float a = (i * 45 + 22.5f) * Mathf.Deg2Rad, b = ((i + 1) * 45 + 22.5f) * Mathf.Deg2Rad;
                    Vector3 p = new Vector3(Mathf.Cos(a) * .5f, 0, Mathf.Sin(a) * .5f), q =
                        new Vector3(Mathf.Cos(b) * .5f, 0, Mathf.Sin(b) * .5f);
                    int s = vertices.Count;
                    vertices.AddRange(new[] { p + Vector3.down, q + Vector3.down, p + Vector3.up, q + Vector3.up });
                    triangles.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3 });
                    s = vertices.Count;
                    vertices.AddRange(new[] { Vector3.up, p + Vector3.up, q + Vector3.up, Vector3.down,
                        p + Vector3.down, q + Vector3.down });
                    triangles.AddRange(new[] { s, s + 2, s + 1, s + 3, s + 4, s + 5 });
                }

                mesh = new Mesh
                {
                    name = "Octagonal pedestal"
                };
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                AssetDatabase.CreateAsset(mesh, path);
            }

            foreach (var trigger in UnityEngine.Object.FindObjectsByType<LaserDroneShopTrigger>(FindObjectsSortMode.None))
                foreach (var f in trigger.GetComponentsInChildren<MeshFilter>())
                    if (f.name == "Octagonal pedestal" || f.name == "Inner glow")
                        f.sharedMesh = mesh;
            foreach (string file in new[]
            {
                Folder + "/Drone.prefab",
                Folder + "/MegaDrone.prefab"
            }

            )
            {
                var root = PrefabUtility.LoadPrefabContents(file);
                var visual = root.GetComponent<LaserDroneVisual>();
                var rotorMesh = visual.rotors[0].GetComponent<MeshFilter>().sharedMesh;
                foreach (var rotor in visual.rotors)
                    rotor.GetComponent<MeshFilter>().sharedMesh = rotorMesh;
                PrefabUtility.SaveAsPrefabAsset(root, file);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.Object.FindFirstObjectByType<LaserHeroPlayer>().gameObject.scene);
            EditorSceneManager.SaveScene(UnityEngine.Object.FindFirstObjectByType<LaserHeroPlayer>().gameObject.scene);
            return "Octagonal pads and shared rotor meshes saved";
        }

        static GameObject Part(Transform root, string name, PrimitiveType shape, Vector3 pos, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static Material Mat(string name, Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = name
            };
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", .3f);
            mat.enableInstancing = true;
            AssetDatabase.CreateAsset(mat, Folder + "/" + name + ".mat");
            return mat;
        }

        public static string Apply()
        {
            if (Application.isPlaying)
                throw new Exception("Stop Play Mode");
            if (GameObject.Find("Drone Shop Platforms") != null)
                return "Drone setup already exists";
            var hero = UnityEngine.Object.FindFirstObjectByType<LaserHeroPlayer>();
            var progress = UnityEngine.Object.FindFirstObjectByType<LaserProgress>();
            var hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            EditorSceneManager.SaveScene(hero.gameObject.scene, "Assets/Scenes/Backups/Lobby_BeforeDrones.unity", true);
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/LaserHero", "Drones");
            blue = Mat("Drone Blue", new Color(.035f, .45f, 1));
            white = Mat("Drone White", new Color(.84f, .95f, 1));
            gold = Mat("Drone Gold", new Color(1, .72f, .025f));
            dark = Mat("Drone Graphite", new Color(.035f, .075f, .14f));
            cyan = Mat("Drone Cyan", new Color(.02f, .85f, 1));
            var settings = ScriptableObject.CreateInstance<LaserDroneSettings>();
            settings.drones = new[]
            {
                new LaserDroneDefinition
                {
                    title = "Дрон",
                    color = Color.cyan
                },
                new LaserDroneDefinition
                {
                    title = "Мега-дрон",
                    color = new Color(1, .77f, .03f),
                    cooldown = 1.2f,
                    range = 17,
                    orbitDistance = 3.3f,
                    damageMultiplier = 1.5f,
                    minimumDamage = 45,
                    maximumDamage = 450,
                    blastRadius = .9f,
                    maximumBlocks = 20
                }
            };
            for (int i = 0; i < 2; i++)
            {
                var model = Build(i);
                string prefabPath = Folder + (i == 0 ? "/Drone.prefab" : "/MegaDrone.prefab");
                settings.drones[i].prefab = PrefabUtility.SaveAsPrefabAsset(model, prefabPath);
                settings.drones[i].icon = Icon(model, i);
                UnityEngine.Object.DestroyImmediate(model);
            }

            settings.buzz = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LaserHero/Audio/Drones/DroneBuzz.ogg");
            settings.shot = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LaserHero/Audio/Drones/DroneShot.ogg");
            settings.beam = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"))
            {
                name = "Drone Laser"
            };
            settings.beam.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(settings.beam, Folder + "/DroneLaser.mat");
            AssetDatabase.CreateAsset(settings, Folder + "/DroneSettings.asset");
            var companion = hero.GetComponent<LaserDroneCompanion>();
            if (companion == null)
                companion = hero.gameObject.AddComponent<LaserDroneCompanion>();
            companion.settings = settings;
            companion.hero = hero;
            companion.progress = progress;
            var catalog = progress.GetComponent<LaserIAPStore>().catalog;
            var list = catalog.items.ToList();
            for (int i = 0; i < 2; i++)
                if (catalog.Find(LaserStoreKind.Drone, i) == null)
                    list.Add(new LaserStoreItem
                    {
                        kind = LaserStoreKind.Drone,
                        index = i,
                        productId = i == 0 ? "laser_hero_drone" : "laser_hero_mega_drone",
                        previewPrice = i == 0 ? 2.99 : 7.99
                    });
            catalog.items = list.ToArray();
            EditorUtility.SetDirty(catalog);
            var root = new GameObject("Drone Shop Platforms");
            for (int i = 0; i < 2; i++)
                Platform(root.transform, settings, i, hud);
            foreach (string path in new[]
            {
                "Assets/LaserHero/Audio/Drones/DroneBuzz.ogg",
                "Assets/LaserHero/Audio/Drones/DroneShot.ogg"
            }

            )
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                importer.forceToMono = true;
                var sample = importer.defaultSampleSettings;
                sample.loadType = AudioClipLoadType.DecompressOnLoad;
                importer.defaultSampleSettings = sample;
                importer.SaveAndReimport();
            }

            EditorUtility.SetDirty(companion);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(hero.gameObject.scene);
            EditorSceneManager.SaveScene(hero.gameObject.scene);
            return "2 drone prefabs, 2 lobby trigger platforms, IAP-only shop, audio, 13 products saved";
        }

        static GameObject Build(int index)
        {
            bool mega = index == 1;
            var root = new GameObject(mega ? "Mega Drone" : "Drone");
            var fixedParts = new GameObject("Airframe");
            fixedParts.transform.SetParent(root.transform, false);
            Transform p = fixedParts.transform;
            float size = mega ? 1.2f : 1;
            Part(p, "White body", PrimitiveType.Cube, Vector3.zero, new Vector3(1.15f, .35f, .9f) * size, white);
            Part(p, "Blue canopy", PrimitiveType.Cube, new Vector3(0, .23f, 0), new Vector3(.85f, .16f, .65f) * size, blue);
            Part(p, "Roof panel", PrimitiveType.Cube, new Vector3(0, .33f, 0), new Vector3(.58f, .05f, .43f) * size, dark);
            for (int z = -1; z <= 1; z += 2)
                for (int x = -1; x <= 1; x += 2)
                    Part(p, "Roof stud", PrimitiveType.Cylinder, new Vector3(x * .22f, .35f, z * .16f),
                        new Vector3(.12f, .035f, .12f), blue);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(p, "Skid", PrimitiveType.Cube, new Vector3(side * .46f, -.42f, 0), new Vector3(.09f, .08f, 1.1f) * size, dark);
                for (int z = -1; z <= 1; z += 2)
                    Part(p, "Landing strut", PrimitiveType.Cube, new Vector3(side * .4f, -.25f,
                        z * .3f), new Vector3(.07f, .4f, .07f), gold);
                Part(p, "Eye", PrimitiveType.Cylinder, new Vector3(side * .3f, 0, .5f * size),
                    new Vector3(.22f, .08f, .22f), mega ? gold : cyan).transform.localRotation = Quaternion.Euler(90, 0, 0);
            }

            var rotors = new List<Transform>();
            int count = mega ? 6 : 4;
            for (int i = 0; i < count; i++)
            {
                float a = (mega ? i * 60f + 30 : i * 90f + 45) * Mathf.Deg2Rad;
                Vector3 end = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * (mega ? 1.65f : 1.25f);
                var arm = Part(p, "Gold arm", PrimitiveType.Cube, end * .5f, new Vector3(.17f, .13f, end.magnitude), gold);
                arm.transform.localRotation = Quaternion.LookRotation(end);
                Part(p, "Motor", PrimitiveType.Cylinder, end + Vector3.up * .08f, new Vector3(.28f, .16f,
                    .28f), mega ? white : cyan);
                if (mega)
                {
                    var ring = new GameObject("Rotor guard", typeof(MeshFilter), typeof(MeshRenderer));
                    ring.transform.SetParent(p, false);
                    ring.transform.localPosition = end + Vector3.up * .24f;
                    ring.GetComponent<MeshFilter>().sharedMesh = Ring(.53f, .047f);
                    ring.GetComponent<Renderer>().sharedMaterial = white;
                }

                var rotor = new GameObject("Rotor " + i);
                rotor.transform.SetParent(root.transform, false);
                rotor.transform.localPosition = end + Vector3.up * .26f;
                Part(rotor.transform, "Blade", PrimitiveType.Cube, Vector3.zero, new Vector3(mega ? .94f
                    : 1.05f, .035f, .095f), dark);
                if (mega)
                    Part(rotor.transform, "Blade cross", PrimitiveType.Cube, Vector3.zero, new Vector3(.095f, .035f, .94f), dark);
                Part(rotor.transform, "Hub", PrimitiveType.Cylinder, Vector3.up * .035f, new Vector3(.18f, .055f, .18f), gold);
                Combine(rotor.transform, (mega ? "Mega" : "Normal") + "Rotor" + i);
                rotors.Add(rotor.transform);
            }

            Combine(p, mega ? "MegaAirframe" : "DroneAirframe");
            var muzzle = new GameObject("Laser muzzle");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(0, -.03f, .65f * size);
            var visual = root.AddComponent<LaserDroneVisual>();
            visual.rotors = rotors.ToArray();
            visual.muzzle = muzzle.transform;
            return root;
        }

        static Mesh Ring(float radius, float thickness)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < 32; i++)
                for (int j = 0; j < 6; j++)
                {
                    float a = i * Mathf.PI / 16, b = j * Mathf.PI / 3;
                    vertices.Add(new Vector3(Mathf.Cos(a) * (radius + Mathf.Cos(b) * thickness),
                        Mathf.Sin(b) * thickness, Mathf.Sin(a) * (radius + Mathf.Cos(b) * thickness)));
                }

            for (int i = 0; i < 32; i++)
                for (int j = 0; j < 6; j++)
                {
                    int a = i * 6 + j, b = ((i + 1) % 32) * 6 + j, c = i * 6 + (j + 1) % 6, d = ((i + 1) % 32) * 6 + (j + 1) % 6;
                    triangles.AddRange(new[] { a, c, b, b, c, d });
                }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            return mesh;
        }

        static void Combine(Transform root, string assetName)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>();
            var materials = filters.Select(f => f.GetComponent<Renderer>().sharedMaterial).Distinct().ToArray();
            var pieces = new List<Mesh>();
            foreach (var material in materials)
            {
                var mesh = new Mesh();
                mesh.CombineMeshes(filters.Where(f =>
                    f.GetComponent<Renderer>().sharedMaterial == material).Select(f =>
                    new CombineInstance { mesh = f.sharedMesh, transform =
                    root.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray());
                pieces.Add(mesh);
            }

            var final = new Mesh()
            {
                name = assetName
            };
            final.CombineMeshes(pieces.Select(m => new CombineInstance { mesh = m, transform =
                Matrix4x4.identity }).ToArray(), false);
            AssetDatabase.CreateAsset(final, Folder + "/" + assetName + ".asset");
            foreach (var f in filters)
                UnityEngine.Object.DestroyImmediate(f.gameObject);
            foreach (var m in pieces)
                UnityEngine.Object.DestroyImmediate(m);
            root.gameObject.AddComponent<MeshFilter>().sharedMesh = final;
            root.gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }

        static Sprite Icon(GameObject model, int index)
        {
            foreach (var t in model.GetComponentsInChildren<Transform>())
                t.gameObject.layer = 31;
            var cameraGo = new GameObject("Drone icon camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.orthographic = true;
            camera.orthographicSize = index == 0 ? 1.85f : 2.25f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 30;
            cameraGo.transform.position = new Vector3(4, 4, 5);
            cameraGo.transform.LookAt(Vector3.zero);
            var lightGo = new GameObject("Icon light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2;
            light.cullingMask = 1 << 31;
            lightGo.transform.rotation = Quaternion.Euler(40, -30, 0);
            var rt = new RenderTexture(384, 384, 24);
            camera.targetTexture = rt;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(384, 384, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, 384, 384), 0, 0);
            texture.Apply();
            string path = Folder + "/" + (index == 0 ? "DroneIcon" : "MegaDroneIcon") + ".png";
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(cameraGo);
            UnityEngine.Object.DestroyImmediate(lightGo);
            foreach (var t in model.GetComponentsInChildren<Transform>())
                t.gameObject.layer = 2;
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void Platform(Transform root, LaserDroneSettings settings, int index, LaserHeroHUD hud)
        {
            var go = new GameObject(index == 0 ? "Drone cyan platform" : "Mega Drone gold platform");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(index == 0 ? 10 : 17, .1f, -1.5f);
            Part(go.transform, "Octagonal pedestal", PrimitiveType.Cylinder, Vector3.zero, new Vector3(4, .15f,
                4), index == 0 ? blue : gold);
            Part(go.transform, "Inner glow", PrimitiveType.Cylinder, new Vector3(0, .18f, 0), new Vector3(3.5f,
                .035f, 3.5f), index == 0 ? cyan : gold);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(settings.drones[index].prefab);
            model.transform.SetParent(go.transform, false);
            model.transform.localPosition = new Vector3(0, 1.7f, 0);
            model.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(0, 1, 0);
            collider.size = new Vector3(4, 3, 4);
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            go.AddComponent<LaserDroneShopTrigger>().featuredDrone = index;
            var labelGo = new GameObject("Drone sign", typeof(TextMeshPro), typeof(LaserWorldLabel));
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0, 3.3f, 0);
            var label = labelGo.GetComponent<TextMeshPro>();
            label.font = hud.font;
            label.fontSharedMaterial = hud.outlinedText;
            label.fontSize = 7;
            label.alignment = TextAlignmentOptions.Center;
            label.color = index == 0 ? Color.cyan : new Color(1, .8f, .03f);
            label.text = settings.drones[index].title + "\n<size=65%>Магазин дронов</size>";
            label.rectTransform.sizeDelta = new Vector2(6, 2);
        }
    }
}
