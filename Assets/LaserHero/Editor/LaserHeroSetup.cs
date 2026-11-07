using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserHeroSetup
    {
        const string Folder = "Assets/LaserHero/Generated";
        static Material voxel, beam;
        static TMP_FontAsset font;
        static Transform root;
        [MenuItem("Laser Hero/Install into Lobby")]
        public static void Install()
        {
            if (Application.isPlaying)
                throw new System.InvalidOperationException("Exit Play Mode first.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Lobby.unity")
                throw new System.InvalidOperationException("Open Lobby first.");
            if (GameObject.Find("LaserHeroWorld") != null)
                throw new System.InvalidOperationException("Laser Hero is already installed.");
            Directory.CreateDirectory("Assets/Scenes/Backups");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Backups/Lobby_BeforeLaserHero.unity", true);
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            voxel = AssetMaterial("Voxel", Shader.Find("LaserHero/VoxelColor"), Color.white);
            beam = AssetMaterial("Beam", Shader.Find("LaserHero/BeamColor"), Color.white);
            const string fontPath = Folder + "/LaserHeroArial.ttf";
            if (!File.Exists(fontPath))
                File.Copy("C:/Windows/Fonts/arialbd.ttf", fontPath);
            AssetDatabase.ImportAsset(fontPath);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + "/LaserHeroFont.asset");
            if (font == null)
            {
                font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(fontPath));
                font.name = "Laser Hero Cyrillic";
                font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                AssetDatabase.CreateAsset(font, Folder + "/LaserHeroFont.asset");
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var texture in font.atlasTextures)
                    AssetDatabase.AddObjectToAsset(texture, font);
                font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя$+×•:/.-[] ");
                EditorUtility.SetDirty(font);
            }

            var legacyMap = GameObject.Find("Map");
            if (legacyMap != null)
                legacyMap.SetActive(false);
            var oldCanvas = GameObject.Find("Canvas");
            if (oldCanvas != null)
                oldCanvas.SetActive(false);
            var settings = GameObject.Find("GameSettingsSystem");
            if (settings != null)
                settings.SetActive(false);
            var scripts = GameObject.Find("@Scripts");
            if (scripts != null)
                scripts.SetActive(false);
            var cm = GameObject.Find("CinemachineCamera");
            if (cm != null)
                cm.SetActive(false);
            var camera = Camera.main;
            foreach (var component in camera.GetComponents<Behaviour>())
                if (component.GetType().Name == "CinemachineBrain")
                    component.enabled = false;
            camera.fieldOfView = 65;
            camera.farClipPlane = 220;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.34f, .69f, .92f);
            var p = GameObject.Find("Player");
            foreach (var behaviour in p.GetComponents<MonoBehaviour>())
                behaviour.enabled = false;
            foreach (var t in p.GetComponentsInChildren<Transform>(true))
                if (t.name == "CarryCountLabel")
                    t.gameObject.SetActive(false);
            var capsule = p.GetComponent<CapsuleCollider>();
            if (capsule != null)
                capsule.enabled = false;
            var controller = p.GetComponent<CharacterController>();
            controller.enabled = false;
            p.transform.SetPositionAndRotation(new Vector3(0, 1, 0), Quaternion.identity);
            controller.enabled = true;
            var motor = p.AddComponent<LaserHeroPlayer>();
            motor.view = camera;
            motor.animator = p.GetComponentInChildren<Animator>(true);
            motor.head = p.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "mixamorig:Head");
            if (motor.animator != null)
                motor.animator.applyRootMotion = false;
            p.layer = 2;
            foreach (var t in p.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = 2;
            root = new GameObject("LaserHeroWorld").transform;
            var progress = root.gameObject.AddComponent<LaserProgress>();
            var eyes = p.AddComponent<LaserEyes>();
            eyes.hero = motor;
            eyes.progress = progress;
            eyes.beamMaterial = beam;
            var hud = new GameObject("LaserHeroHUD", typeof(RectTransform)).AddComponent<LaserHeroHUD>();
            hud.transform.SetParent(root);
            hud.hero = motor;
            hud.eyes = eyes;
            hud.progress = progress;
            hud.font = font;
            BuildArena();
            RenderSettings.fog = true;
            RenderSettings.fogColor = camera.backgroundColor;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 85;
            RenderSettings.fogEndDistance = 190;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.8f, .85f, 1);
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.intensity = 1.35f;
                sun.transform.rotation = Quaternion.Euler(45, -30, 0);
            }

            var builds = EditorBuildSettings.scenes.ToList();
            if (!builds.Any(s => s.path == scene.path))
            {
                builds.Add(new EditorBuildSettingsScene(scene.path, true));
                EditorBuildSettings.scenes = builds.ToArray();
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("LASER_HERO_INSTALLED: Lobby saved; original scene preserved in Assets/Scenes/Backups.");
        }

        static void BuildArena()
        {
            Cube("Arena foundation", new Vector3(0, -.6f, 32), new Vector3(74, 1, 100), new Color(.11f, .29f, .36f));
            Cube("Plaza", new Vector3(0, -.04f, -3), new Vector3(70, .12f, 28), new Color(.25f, .36f, .46f));
            Cube("City road", new Vector3(0, .02f, 42), new Vector3(12, .12f, 65), new Color(.09f, .14f, .21f));
            for (int z = 13; z < 75; z += 5)
                Cube("Road dash", new Vector3(0, .1f, z), new Vector3(.25f, .04f, 2), new Color(1, .8f, .2f));
            for (int x = -32; x <= 32; x += 4)
                for (int z = -14; z < 10; z += 4)
                    Cube("Plaza inset", new Vector3(x, .04f, z), new Vector3(3.8f, .04f, 3.8f), new Color(.29f, .41f, .5f));
            Cube("City left sidewalk", new Vector3(-8, .1f, 44), new Vector3(3, .3f, 67), new Color(.56f, .66f, .72f));
            Cube("City right sidewalk", new Vector3(8, .1f, 44), new Vector3(3, .3f, 67), new Color(.56f, .66f, .72f));
            for (int side = -1; side <= 1; side += 2)
            {
                Cube("Boundary", new Vector3(side * 36, 1.2f, 32), new Vector3(.8f, 3, 100), new Color(.12f, .35f, .46f));
                for (int z = 16; z < 78; z += 13)
                {
                    Tree(new Vector3(side * 31, 0, z));
                    Lamp(new Vector3(side * 7, .2f, z));
                }
            }

            Cube("Back wall", new Vector3(0, 1, 81), new Vector3(74, 3, 1), new Color(.12f, .35f, .46f));
            Cube("Front wall", new Vector3(0, 1, -18), new Vector3(74, 3, 1), new Color(.12f, .35f, .46f));
            Sign("LASER CITY", new Vector3(0, 7, 12), new Color(.1f, .86f, 1), 3f);
            Sign("РАЗРУШАЙ  •  СОБИРАЙ  •  УСИЛИВАЙ", new Vector3(0, 5.5f, 12), Color.white, .65f);
            Color[] colors =
            {
                new Color(.96f, .33f, .17f),
                new Color(.15f, .6f, .95f),
                new Color(.97f, .7f, .08f),
                new Color(.6f, .28f, .85f),
                new Color(.15f, .8f, .55f),
                new Color(.95f, .35f, .55f)
            };
            for (int row = 0; row < 3; row++)
                for (int side = 0; side < 2; side++)
                {
                    int index = row * 2 + side;
                    Vector3 origin = new Vector3(side == 0 ? -24 : 12, 0, 20 + row * 19);
                    var go = new GameObject("Destructible " + index);
                    go.transform.SetParent(root);
                    go.transform.position = origin;
                    var body = go.AddComponent<LaserVoxelBody>();
                    body.dimensions = new Vector3Int(20, 12 + row * 6, 20);
                    body.bodyColor = colors[index];
                    body.trimColor = Color.Lerp(colors[index], Color.white, .75f);
                    body.style = index % 3;
                    body.hitPoints = 5 + row * 5;
                    body.cellSize = .5f;
                    body.displayName = new[]
                    {
                        "Кафе",
                        "Башня",
                        "Галерея",
                        "Отель",
                        "Аркада",
                        "Небоскрёб"
                    }[index];
                    body.GetComponent<MeshRenderer>().sharedMaterial = voxel;
                    body.Generate();
                    var mesh = body.GetComponent<MeshFilter>().sharedMesh;
                    AssetDatabase.CreateAsset(mesh, Folder + "/Building" + index + ".asset");
                    Cube("Building base", origin + new Vector3(5, -.1f, 5), new Vector3(10.8f, .2f,
                        10.8f), new Color(.2f, .25f, .33f));
                    Sign(body.displayName.ToUpper(), origin + new Vector3(5, body.dimensions.y * .5f + 1, -.1f), Color.white, 1.3f);
                }

            Sign("ТРЕНИРОВКА ГЛАЗ", new Vector3(0, 4.8f, -8), new Color(1, .7f, .16f), 1.6f, true);
            for (int i = 0; i < 4; i++)
            {
                var station = new GameObject("Training target x" + (1 << i));
                station.transform.SetParent(root);
                station.transform.position = new Vector3(-12 + i * 8, 0, -9);
                var target = station.AddComponent<LaserTrainingTarget>();
                target.tier = i;
                target.radius = 3.6f;
                Color color = i == 0 ? new Color(.25f, .85f, 1) : LaserProgress.Colors[i];
                Cube("Training pad", station.transform.position + new Vector3(0, .08f, 2.3f), new Vector3(5,
                    .16f, 4.5f), Color.Lerp(color, new Color(.05f, .15f, .25f), .6f));
                Cube("Target stand", station.transform.position + new Vector3(0, 1, 0), new Vector3(.25f, 2,
                    .3f), new Color(.6f, .7f, .8f));
                for (int ring = 0; ring < 4; ring++)
                {
                    var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    disc.name = "Target ring";
                    disc.transform.SetParent(station.transform);
                    disc.transform.localPosition = new Vector3(0, 2, .05f + ring * .08f);
                    disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    float diameter = 2.3f - ring * .5f;
                    disc.transform.localScale = new Vector3(diameter, .05f, diameter);
                    Object.DestroyImmediate(disc.GetComponent<Collider>());
                    disc.GetComponent<Renderer>().sharedMaterial = Mat(ring % 2 == 0 ? color : new Color(.03f, .08f, .15f));
                }

                target.aimPoint = new GameObject("Aim point").transform;
                target.aimPoint.SetParent(station.transform);
                target.aimPoint.localPosition = new Vector3(0, 2, .4f);
                Sign("×" + (1 << i) + " СИЛА", station.transform.position + new Vector3(0, 3.6f, .3f), color, 1.05f, true);
                Sign(i == 0 ? "БЕСПЛАТНО" : "ПРЕМИУМ", station.transform.position + new Vector3(0, .3f,
                    4.1f), Color.white, .65f, true);
            }

            // A low, easy first target directly ahead teaches carving before the buildings.
            var wall = new GameObject("Starter voxel wall");
            wall.transform.SetParent(root);
            wall.transform.position = new Vector3(-3, 0, 10);
            var w = wall.AddComponent<LaserVoxelBody>();
            w.dimensions = new Vector3Int(12, 6, 2);
            w.cellSize = .5f;
            w.hitPoints = 2;
            w.bodyColor = new Color(1, .45f, .08f);
            w.trimColor = new Color(1, .8f, .25f);
            w.displayName = "Тренировочная стена";
            w.style = 3;
            w.GetComponent<MeshRenderer>().sharedMaterial = voxel;
            w.Generate();
            AssetDatabase.CreateAsset(w.GetComponent<MeshFilter>().sharedMesh, Folder + "/StarterWall.asset");
        }

        static Material AssetMaterial(string name, Shader shader, Color color)
        {
            var m = new Material(shader)
            {
                name = name
            };
            m.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(m, Folder + "/" + name + ".mat");
            return m;
        }

        static int matIndex;
        static readonly System.Collections.Generic.Dictionary<Color, Material> Materials =
            new System.Collections.Generic.Dictionary<Color, Material>();
        static Material Mat(Color c)
        {
            if (Materials.TryGetValue(c, out var value))
                return value;
            value = AssetMaterial("World" + (matIndex++), Shader.Find("Universal Render Pipeline/Simple Lit"), c);
            Materials[c] = value;
            return value;
        }

        static GameObject Cube(string name, Vector3 pos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return go;
        }

        static void Sign(string text, Vector3 pos, Color color, float size, bool reverse = false)
        {
            var go = new GameObject(text);
            go.transform.SetParent(root);
            go.transform.position = pos;
            if (reverse)
                go.transform.rotation = Quaternion.Euler(0, 180, 0);
            var t = go.AddComponent<TextMeshPro>();
            t.font = font;
            t.text = text;
            t.fontSize = size * 8;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = FontStyles.Bold;
            t.rectTransform.sizeDelta = new Vector2(50, 4);
        }

        static void Tree(Vector3 pos)
        {
            Cube("Tree trunk", pos + Vector3.up * 1.4f, new Vector3(.7f, 2.8f, .7f), new Color(.38f, .22f, .14f));
            Cube("Tree canopy", pos + Vector3.up * 3.5f, new Vector3(3.5f, 3.5f, 3.5f), new Color(.14f, .65f, .35f));
            Cube("Tree crown", pos + Vector3.up * 5, new Vector3(2.5f, 1, 2.5f), new Color(.27f, .8f, .41f));
        }

        static void Lamp(Vector3 pos)
        {
            Cube("Streetlight", pos + Vector3.up * 2, new Vector3(.16f, 4, .16f), new Color(.12f, .21f, .29f));
            Cube("Lamp", pos + Vector3.up * 4, new Vector3(.7f, .2f, .7f), new Color(1, .85f, .4f));
        }
    }
}
