using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace LaserHero.Editor
{
    public static class LaserHeroArenaSetup
    {
        const string Folder = "Assets/LaserHero/GeneratedV2";
        static Transform world, lobby, arena;
        static Material studs, outline;
        static TMP_FontAsset font;
        static int matId, meshId;
        static readonly System.Collections.Generic.Dictionary<Color, Material> Mats =
            new System.Collections.Generic.Dictionary<Color, Material>();
        [MenuItem("Laser Hero/Apply Reference Impact")]
        public static void ApplyImpact()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first");
            var feedback = UnityEngine.Object.FindFirstObjectByType<LaserImpactFeedback>();
            feedback.hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LaserHero/Audio/ReferencePulse.wav");
            feedback.alternateHitClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LaserHero/Audio/ReferencePulse2.wav");
            feedback.collapseClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LaserHero/Audio/ReferenceCollapse.wav");
            feedback.laserLoop = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LaserHero/Audio/LaserBuzzLoop.wav");
            var eyes = UnityEngine.Object.FindFirstObjectByType<LaserEyes>();
            eyes.damageCooldown = .5f;
            var particle = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/ImpactParticle.mat");
            if (particle == null)
            {
                particle = new Material(Shader.Find("LaserHero/ImpactParticle"));
                AssetDatabase.CreateAsset(particle, Folder + "/ImpactParticle.mat");
            }

            var dust = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/ImpactDust.mat");
            if (dust == null)
            {
                dust = new Material(Shader.Find("LaserHero/ImpactParticle"));
                dust.SetFloat("_DstBlend", 10);
                AssetDatabase.CreateAsset(dust, Folder + "/ImpactDust.mat");
            }

            eyes.impactParticleMaterial = particle;
            eyes.dustMaterial = dust;
            foreach (string path in new[]
            {
                "Assets/LaserHero/Audio/ReferencePulse.wav",
                "Assets/LaserHero/Audio/ReferencePulse2.wav",
                "Assets/LaserHero/Audio/ReferenceCollapse.wav"
            }

            )
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }

            EditorUtility.SetDirty(feedback);
            EditorUtility.SetDirty(eyes);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(eyes.gameObject.scene);
            EditorSceneManager.SaveScene(eyes.gameObject.scene);
        }

        [MenuItem("Laser Hero/Refresh Arena Art")]
        public static void RefreshArt()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first");
            var hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            ConfigureSprites(hud);
            var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Graphics/Material/Global/Studs.mat");
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Folder }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m.shader != source.shader)
                    continue;
                m.SetTexture("_MainTex", source.GetTexture("_MainTex"));
                m.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
                m.SetFloat("_Scale", .5f);
                m.SetFloat("_AmbientIntensity", 1.3f);
                EditorUtility.SetDirty(m);
            }

            var feedback = UnityEngine.Object.FindFirstObjectByType<LaserImpactFeedback>();
            feedback.debrisMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Studs0.mat");
            EditorUtility.SetDirty(feedback);
            EditorUtility.SetDirty(hud);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            EditorSceneManager.SaveScene(hud.gameObject.scene);
        }

        [MenuItem("Laser Hero/Build LEGO Lobby and Arena")]
        public static void Install()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Lobby.unity")
                throw new InvalidOperationException("Open Lobby");
            if (GameObject.Find("LEGO Arena") != null)
                throw new InvalidOperationException("Arena already installed");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Backups/Lobby_BeforeArenaV2.unity", true);
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            studs = AssetDatabase.LoadAssetAtPath<Material>("Assets/Graphics/Material/Global/Studs.mat");
            MakeFont();
            var previous = GameObject.Find("LaserHeroWorld");
            if (previous != null)
            {
                previous.name = "LaserHeroWorld_V1_Backup";
                previous.SetActive(false);
            }

            world = new GameObject("LaserHeroWorld").transform;
            lobby = new GameObject("Safe LEGO Lobby").transform;
            lobby.SetParent(world);
            arena = new GameObject("LEGO Arena").transform;
            arena.SetParent(world);
            var hero = UnityEngine.Object.FindFirstObjectByType<LaserHeroPlayer>();
            hero.pitch = 8;
            hero.yaw = 0;
            var controller = hero.GetComponent<CharacterController>();
            controller.enabled = false;
            hero.transform.position = new Vector3(0, 1, 0);
            controller.enabled = true;
            var progress = world.gameObject.AddComponent<LaserProgress>();
            var laser = hero.GetComponent<LaserEyes>();
            laser.hero = hero;
            laser.progress = progress;
            var arenaSystem = world.gameObject.AddComponent<LaserArena>();
            arenaSystem.hero = hero;
            arenaSystem.arenaBounds = new Bounds(new Vector3(0, 8, 62), new Vector3(48, 38, 48));
            arenaSystem.entry = new Vector3(0, 1, 41);
            var feedback = world.gameObject.AddComponent<LaserImpactFeedback>();
            feedback.hero = hero;
            feedback.hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LaserHero/Audio/BrickBreak.ogg");
            feedback.collapseClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LaserHero/Audio/BuildingFall.ogg");
            feedback.laserLoop = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LaserHero/Audio/LaserLoop.ogg");
            var hud = new GameObject("SimulatorHUD", typeof(RectTransform)).AddComponent<LaserHeroHUD>();
            hud.transform.SetParent(world);
            hud.hero = hero;
            hud.eyes = laser;
            hud.progress = progress;
            hud.font = font;
            hud.outlinedText = outline;
            ConfigureSprites(hud);
            BuildLobby();
            BuildArena();
            hero.view.backgroundColor = new Color(.43f, .96f, 1);
            hero.view.fieldOfView = 65;
            hero.view.farClipPlane = 200;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.85f, .88f, .95f);
            RenderSettings.fog = false;
            var sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l =>
                l.type == LightType.Directional);
            if (sun != null)
            {
                sun.intensity = 1.1f;
                sun.transform.rotation = Quaternion.Euler(50, -25, 0);
            }

            var oldVolume = GameObject.Find("Global Volume");
            if (oldVolume != null)
                oldVolume.SetActive(false);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = world.gameObject;
            Debug.Log("LEGO_ARENA_READY: safe lobby, 36 destructible terrain chunks, houses, trees, benches and lamps.");
        }

        static void MakeFont()
        {
            const string path = Folder + "/SimulatorBlack.ttf";
            if (!File.Exists(path))
                File.Copy("C:/Windows/Fonts/ariblk.ttf", path);
            AssetDatabase.ImportAsset(path);
            font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(path), 90, 12,
                GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            font.name = "Simulator Black SDF";
            AssetDatabase.CreateAsset(font, Folder + "/SimulatorFont.asset");
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var tex in font.atlasTextures)
                AssetDatabase.AddObjectToAsset(tex, font);
            font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя$+×•:/.-[] !?↑");
            outline = new Material(font.material);
            outline.name = "Simulator Bold Outline";
            outline.EnableKeyword("OUTLINE_ON");
            outline.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            outline.SetFloat(ShaderUtilities.ID_OutlineWidth, .28f);
            outline.SetFloat(ShaderUtilities.ID_FaceDilate, .07f);
            AssetDatabase.CreateAsset(outline, Folder + "/Outline.mat");
            EditorUtility.SetDirty(font);
        }

        static void ConfigureSprites(LaserHeroHUD hud)
        {
            string path = "Assets/LaserHero/UI/SimulatorIcons.png";
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            float cw = sourceWidth / 3f, ch = sourceHeight / 2f;
            var metadata = new SpriteMetaData[6];
            string[] names =
            {
                "Flame",
                "Cash",
                "Upgrade",
                "LaserHero",
                "Portal",
                "Shield"
            };
            Rect[] regions =
            {
                new Rect(15, 690, 410, 500),
                new Rect(425, 690, 445, 470),
                new Rect(875, 690, 365, 450),
                new Rect(0, 140, 435, 510),
                new Rect(425, 130, 450, 520),
                new Rect(870, 130, 395, 480)
            };
            for (int i = 0; i < 6; i++)
            {
                var region = regions[i];
                region = new Rect(region.x * sourceWidth / 1280f, region.y * sourceHeight / 1280f,
                    region.width * sourceWidth / 1280f, region.height * sourceHeight / 1280f);
                metadata[i] = new SpriteMetaData
                {
                    name = names[i],
                    rect = region,
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(.5f, .5f)
                };
            }

#pragma warning disable 618
            importer.spritesheet = metadata;
#pragma warning restore 618
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            hud.icons = names.Select(n => sprites.First(s => s.name == n)).ToArray();
            hud.orangePanel = Panel("Orange", new Color(1, .77f, .22f), new Color(1, .27f, .035f));
            hud.greenPanel = Panel("Green", new Color(.62f, 1, .13f), new Color(.05f, .72f, .02f));
            hud.purplePanel = Panel("Purple", new Color(.78f, .63f, 1), new Color(.49f, .3f, .9f));
            hud.bluePanel = Panel("Blue", new Color(.38f, .88f, 1), new Color(.06f, .52f, .94f));
        }

        static Sprite Panel(string name, Color top, Color bottom)
        {
            var tex = new Texture2D(256, 128, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 256; x++)
                {
                    Color c = Color.Lerp(bottom, top, y / 127f);
                    int edge = Mathf.Min(Mathf.Min(x, 255 - x), Mathf.Min(y, 127 - y));
                    if (edge < 4)
                        c = Color.black;
                    else if (edge < 8)
                        c = Color.Lerp(c, Color.white, .7f);
                    else if (edge < 12)
                        c = Color.Lerp(c, Color.black, .35f);
                    else
                    {
                        int sx = x % 27, sy = y % 27;
                        if (sx >= 6 && sx <= 21 && sy >= 6 && sy <= 21)
                        {
                            bool border = sx <= 7 || sx >= 20 || sy <= 7 || sy >= 20;
                            c = Color.Lerp(c, sy > 18 ? Color.white : Color.black, border ? .13f : .05f);
                        }
                    }

                    tex.SetPixel(x, y, c);
                }

            tex.Apply();
            string path = Folder + "/Panel" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(14, 14, 14, 14);
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Material Mat(Color color)
        {
            if (Mats.TryGetValue(color, out var result))
                return result;
            result = new Material(studs);
            result.name = "Studs " + matId;
            result.SetColor("_Color", color);
            result.SetColor("_BaseColor", color);
            result.SetFloat("_Scale", .5f);
            result.SetFloat("_IsDynamic", 1);
            result.SetFloat("_AmbientIntensity", 1.3f);
            result.SetFloat("_BumpScale", .35f);
            result.SetFloat("_NormalStrength", .4f);
            result.SetVector("_ObjectScale", Vector4.one);
            AssetDatabase.CreateAsset(result, Folder + "/Studs" + (matId++) + ".mat");
            Mats[color] = result;
            return result;
        }

        static GameObject Block(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            var mat = Mat(color);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var props = new MaterialPropertyBlock();
            props.SetVector("_ObjectScale", new Vector4(scale.x, scale.y, scale.z, 1));
            go.GetComponent<Renderer>().SetPropertyBlock(props);
            // Persist scaled triplanar coordinates as a per-object material for the authored static prop.
            var local = new Material(mat);
            local.SetVector("_ObjectScale", new Vector4(scale.x, scale.y, scale.z, 1));
            AssetDatabase.CreateAsset(local, Folder + "/StaticStuds" + (matId++) + ".mat");
            go.GetComponent<Renderer>().sharedMaterial = local;
            return go;
        }

        static void Label(string text, Vector3 position, Color color, float size, Transform parent)
        {
            var go = new GameObject(text);
            go.transform.SetParent(parent);
            go.transform.position = position;
            var t = go.AddComponent<TextMeshPro>();
            t.font = font;
            t.fontSharedMaterial = outline;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.rectTransform.sizeDelta = new Vector2(40, 5);
            t.UpdateMeshPadding();
            go.AddComponent<LaserWorldLabel>();
        }

        static void BuildLobby()
        {
            Color grass = new Color(.13f, .87f, .17f), blue = new Color(.03f, .6f, 1), path = new Color(.6f, .62f, 1);
            Block("Lobby island", lobby, new Vector3(0, -1, -3), new Vector3(62, 2, 40), grass);
            Block("Lobby main path", lobby, new Vector3(0, .08f, -2), new Vector3(9, .16f, 36), path);
            Block("Lobby cross path", lobby, new Vector3(0, .1f, -5), new Vector3(51, .2f, 6), path);
            Block("Spawn podium", lobby, new Vector3(0, .15f, 0), new Vector3(5, .3f, 5), new Color(.67f, .73f, 1));
            for (int s = -1; s <= 1; s += 2)
            {
                Block("Lobby terraced wall", lobby, new Vector3(s * 30, 1.5f, -3), new Vector3(2, 5, 40),
                    new Color(.64f, .48f, .22f));
                Block("Grass wall cap", lobby, new Vector3(s * 30, 4.1f, -3), new Vector3(3, .4f, 40), grass);
                for (int z = -18; z <= 12; z += 10)
                    StaticTree(new Vector3(s * 29, 4.2f, z));
            }

            Block("Lobby back wall", lobby, new Vector3(0, 1.3f, -23), new Vector3(62, 4.6f, 2), new Color(.64f, .48f, .22f));
            Block("Lobby rear grass", lobby, new Vector3(0, 3.8f, -23), new Vector3(62, .5f, 3), grass);
            Block("Training blue back", lobby, new Vector3(-27, 3, -3), new Vector3(1, 6, 34), blue);
            Label("ЗОНА\nТРЕНИРОВОК", new Vector3(-26, 6, -3), Color.white, 20, lobby);
            for (int i = 0; i < 4; i++)
                Training(i, new Vector3(-23, 0, -14 + i * 8));
            Kiosk("ЛАЗЕРЫ", new Vector3(12, 0, -11), new Color(.1f, .6f, 1), 0);
            Kiosk("УЛУЧШЕНИЯ", new Vector3(22, 0, -11), new Color(.4f, 1, .1f), 1);
            Block("Safe exit bridge", lobby, new Vector3(0, -.25f, 27), new Vector3(8, .5f, 22), path);
            for (int s = -1; s <= 1; s += 2)
            {
                Block("Arena gate pillar", lobby, new Vector3(s * 5, 4, 14), new Vector3(1.6f, 8, 2), new Color(.55f, .24f, 1));
                Block("Bridge rail", lobby, new Vector3(s * 4.3f, .7f, 27), new Vector3(.4f, 1.5f, 22), new Color(.48f, .3f, 1));
            }

            Block("Arena gate arch", lobby, new Vector3(0, 8.2f, 14), new Vector3(12, 1.8f, 2), new Color(.75f, .4f, 1));
            Label("АРЕНА", new Vector3(0, 7.3f, 14), Color.white, 10, lobby);
            Label("БЕЗОПАСНОЕ ЛОББИ", new Vector3(0, 4, -18), new Color(.4f, 1, .1f), 18, lobby);
            Label("ВПЕРЁД — ЛОМАЙ ВСЁ!", new Vector3(0, 2.6f, 32), new Color(1, .85f, .15f), 9, lobby);
        }

        static void Training(int tier, Vector3 position)
        {
            var station = new GameObject("Training target x" + (1 << tier));
            station.transform.SetParent(lobby);
            station.transform.SetPositionAndRotation(position, Quaternion.Euler(0, 90, 0));
            var target = station.AddComponent<LaserTrainingTarget>();
            target.tier = tier;
            target.radius = 3.3f;
            Color color = tier == 0 ? new Color(.7f, .9f, 1) : LaserProgress.Colors[tier];
            var pad = Block("Training pad", lobby, position + new Vector3(2, .15f, 0), new Vector3(5, .3f, 6), color);
            Block("Target leg", lobby, position + new Vector3(0, 1, 0), new Vector3(.5f, 2, .5f), new Color(.8f, .85f, .9f));
            for (int ring = 0; ring < 4; ring++)
            {
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Target ring";
                disc.transform.SetParent(station.transform, false);
                disc.transform.localPosition = new Vector3(0, 2.4f, .06f + ring * .09f);
                disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
                float diameter = 2.6f - ring * .56f;
                disc.transform.localScale = new Vector3(diameter, .055f, diameter);
                UnityEngine.Object.DestroyImmediate(disc.GetComponent<Collider>());
                disc.GetComponent<Renderer>().sharedMaterial = Mat(ring % 2 == 0 ? color : Color.white);
            }

            target.aimPoint = new GameObject("Aim point").transform;
            target.aimPoint.SetParent(station.transform, false);
            target.aimPoint.localPosition = new Vector3(0, 2.4f, .43f);
            Label("×" + (1 << tier) + " Мощность", position + new Vector3(0, 4.5f, 0), color, 8, lobby);
            Label(tier == 0 ? "БЕСПЛАТНО" : "ПРЕМИУМ", position + new Vector3(3, .6f, 0), Color.white, 5, lobby);
        }

        static void Kiosk(string title, Vector3 position, Color color, int menu)
        {
            var k = new GameObject(title + " Kiosk");
            k.transform.SetParent(lobby);
            k.transform.position = position;
            k.AddComponent<LaserLobbyKiosk>().menu = menu;
            Block("Shop base", lobby, position + new Vector3(0, .25f, 0), new Vector3(7, .5f, 5), color);
            Block("Shop counter", lobby, position + new Vector3(0, 1, 0), new Vector3(6, 1.5f, 2), new Color(.75f, .65f, 1));
            foreach (int s in new[]
            {
                -1,
                1
            }

            )
                Block("Shop pillar", lobby, position + new Vector3(s * 2.8f, 2.3f, 0), new Vector3(.3f, 4, .3f), color);
            Block("Shop roof", lobby, position + new Vector3(0, 4.4f, 0), new Vector3(7, 1, 4), color);
            Label(title, position + new Vector3(0, 5.7f, 0), Color.white, 11, lobby);
            Label("E — ОТКРЫТЬ", position + new Vector3(0, 2, -1.4f), new Color(1, 1, .3f), 5, lobby);
        }

        static void StaticTree(Vector3 pos)
        {
            Block("Safe tree trunk", lobby, pos + Vector3.up * 1.5f, new Vector3(.8f, 3, .8f), new Color(.4f, .3f, .16f));
            Block("Safe tree canopy", lobby, pos + Vector3.up * 4, new Vector3(4, 3, 4), new Color(.05f, .65f, .28f));
            Block("Safe tree top", lobby, pos + Vector3.up * 5.8f, new Vector3(2.8f, .7f, 2.8f), new Color(.12f, .8f, .35f));
        }

        static LaserVoxelBody Body(string name, Vector3 position, Vector3Int dimensions, int style, Color body,
            Color trim, float hp, float cell = .5f, bool ground = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(arena);
            go.transform.position = position;
            var b = go.AddComponent<LaserVoxelBody>();
            b.displayName = name;
            b.dimensions = dimensions;
            b.style = style;
            b.bodyColor = body;
            b.trimColor = trim;
            b.hitPoints = hp;
            b.cellSize = cell;
            b.terrain = ground;
            b.requiresGroundSupport = !ground;
            b.collapseUnsupported = true;
            b.allowDestruction = true;
            b.autoRespawn = false;
            b.GetComponent<MeshRenderer>().sharedMaterials = new[]
            {
                Mat(body),
                Mat(trim),
                Mat(b.accentColor)
            };
            b.Generate();
            AssetDatabase.CreateAsset(b.GetComponent<MeshFilter>().sharedMesh, Folder + "/VoxelMesh" + (meshId++) + ".asset");
            return b;
        }

        static void BuildArena()
        {
            for (int z = 0; z < 6; z++)
                for (int x = 0; x < 6; x++)
                    Body("Земля " + x + "," + z, new Vector3(-24 + x * 8, -2, 38 + z * 8),
                        new Vector3Int(16, 4, 16), 8, new Color(.52f, .34f, .15f), new Color(.12f, .85f, .15f), 2, .5f, true);
            Body("Красный дом", new Vector3(-17, 0, 50), new Vector3Int(18, 14, 16), 2, new Color(1, .25f,
                .13f), new Color(1, .85f, .2f), 5);
            Body("Синий дом", new Vector3(7, 0, 52), new Vector3Int(18, 18, 16), 2, new Color(.08f, .52f, 1),
                new Color(.75f, .9f, 1), 5);
            Body("Жёлтый дом", new Vector3(-14, 0, 70), new Vector3Int(22, 18, 18), 2, new Color(1, .74f, .08f),
                new Color(1, .25f, .1f), 6);
            int tree = 0;
            foreach (var p in new[]
            {
                new Vector3(-20, 0, 43),
                new Vector3(18, 0, 43),
                new Vector3(-20, 0, 64),
                new Vector3(18, 0, 67),
                new Vector3(12, 0, 79),
                new Vector3(-20, 0, 80)
            }

            )
                Body("Дерево " + tree++, p - new Vector3(2.5f, 0, 2.5f), new Vector3Int(10, 14, 10), 4,
                    new Color(.45f, .28f, .12f), new Color(.05f, .7f, .27f), 3);
            Body("Лавочка у дома", new Vector3(-4, 0, 59), new Vector3Int(12, 7, 5), 5, new Color(.9f, .46f,
                .14f), new Color(.16f, .2f, .28f), 3, .4f);
            Body("Лавочка в парке", new Vector3(6, 0, 72), new Vector3Int(12, 7, 5), 5, new Color(.9f, .46f,
                .14f), new Color(.16f, .2f, .28f), 3, .4f);
            Body("Фонарь у входа", new Vector3(-5, 0, 46), new Vector3Int(8, 18, 4), 6, new Color(.2f, .25f,
                .32f), new Color(.2f, .25f, .32f), 3, .4f);
            Body("Фонарь в парке", new Vector3(15, 0, 62), new Vector3Int(8, 18, 4), 6, new Color(.2f, .25f,
                .32f), new Color(.2f, .25f, .32f), 3, .4f);
            Label("МОЖНО ЛОМАТЬ ВСЁ", new Vector3(0, 2.7f, 38), new Color(1, .9f, .2f), 6, arena);
        }
    }
}
