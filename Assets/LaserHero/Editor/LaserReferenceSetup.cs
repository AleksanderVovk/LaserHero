using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserReferenceSetup
    {
        [MenuItem("Laser Hero/Apply Observed Simulator Features")]
        public static void Apply()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first");
            var hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            if (hud == null)
                throw new InvalidOperationException("Open Lobby scene");
            const string platePath = "Assets/LaserHero/GeneratedV2/IconPlate.png";
            if (!File.Exists(platePath))
            {
                var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                        tex.SetPixel(x, y, x < 4 || x >= 60 || y < 4 || y >= 60 ? Color.black : Color.white);
                tex.Apply();
                File.WriteAllBytes(platePath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(platePath);
                var plateImporter = (TextureImporter)AssetImporter.GetAtPath(platePath);
                plateImporter.textureType = TextureImporterType.Sprite;
                plateImporter.spriteBorder = Vector4.one * 8;
                plateImporter.mipmapEnabled = false;
                plateImporter.SaveAndReimport();
            }

            hud.iconPlate = AssetDatabase.LoadAssetAtPath<Sprite>(platePath);
            string path = "Assets/LaserHero/UI/SimulatorExtras.png";
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.GetSourceTextureWidthAndHeight(out int w, out int h);
            string[] names =
            {
                "Crate",
                "Aura",
                "Rebirth",
                "Speed",
                "Clock",
                "Gem"
            };
            var metadata = new SpriteMetaData[6];
            for (int i = 0; i < 6; i++)
            {
                float x = i % 3 * 1280f / 3;
                var rect = i < 3 ? new Rect(x, 650, 1280f / 3, 450) : new Rect(x, 175, 1280f / 3, 470);
                rect = new Rect(rect.x * w / 1280, rect.y * h / 1280, rect.width * w / 1280, rect.height * h / 1280);
                metadata[i] = new SpriteMetaData
                {
                    name = names[i],
                    rect = rect,
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(.5f, .5f)
                };
            }

#pragma warning disable 618
            importer.spritesheet = metadata;
#pragma warning restore 618
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            hud.extraIcons = names.Select(n => sprites.First(s => s.name == n)).ToArray();
            var hero = hud.hero;
            if (hero.GetComponent<LaserArenaAbilities>() == null)
                hero.gameObject.AddComponent<LaserArenaAbilities>();
            var aura = hero.GetComponent<LaserAuraVisual>();
            if (aura == null)
                aura = hero.gameObject.AddComponent<LaserAuraVisual>();
            aura.progress = hud.progress;
            aura.material = hud.eyes.beamMaterial;
            const string blurPath = "Assets/LaserHero/GeneratedV2/MenuBlur.mat";
            var blur = AssetDatabase.LoadAssetAtPath<Material>(blurPath);
            if (blur == null)
            {
                blur = new Material(Shader.Find("LaserHero/BackdropBlur"));
                AssetDatabase.CreateAsset(blur, blurPath);
            }

            hud.menuBlurMaterial = blur;
            foreach (var label in UnityEngine.Object.FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None))
            {
                if (label.text == "×1 Мощность")
                {
                    label.text = "×1.5 Мощность";
                    EditorUtility.SetDirty(label);
                }
                else if (label.text == "×8 Мощность")
                {
                    label.text = "×6 Мощность";
                    EditorUtility.SetDirty(label);
                }
            }

            var arena = UnityEngine.Object.FindFirstObjectByType<LaserArena>();
            arena.roundDuration = 300;
            AddKiosk(hud, "АУРЫ", new Vector3(12, 0, 6), 2, new Color(.65f, .15f, 1));
            AddKiosk(hud, "ПЕРЕРОЖДЕНИЕ", new Vector3(22, 0, 6), 3, new Color(.08f, .75f, 1));
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(aura);
            EditorUtility.SetDirty(arena);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            EditorSceneManager.SaveScene(hud.gameObject.scene);
        }

        static void AddKiosk(LaserHeroHUD hud, string title, Vector3 position, int menu, Color color)
        {
            string name = title + " Reference Kiosk";
            if (GameObject.Find(name) != null)
                return;
            var go = new GameObject(name);
            go.transform.SetParent(GameObject.Find("Safe LEGO Lobby").transform);
            go.transform.position = position;
            go.AddComponent<LaserLobbyKiosk>().menu = menu;
            Block("Platform", go.transform, new Vector3(0, .2f, 0), new Vector3(7, .4f, 5), color);
            Block("Counter", go.transform, new Vector3(0, 1, 0), new Vector3(6, 1.4f, 2), color * .8f);
            foreach (int side in new[]
            {
                -1,
                1
            }

            )
                Block("Pillar", go.transform, new Vector3(side * 2.8f, 2.3f, 0), new Vector3(.4f, 4, .4f), color);
            Block("Roof", go.transform, new Vector3(0, 4.4f, 0), new Vector3(7, .7f, 4), color);
            var label = new GameObject("Shop title").AddComponent<TextMeshPro>();
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0, 5.8f, 0);
            label.font = hud.font;
            label.fontSharedMaterial = hud.outlinedText;
            label.text = title + "\n<size=45%>E — ОТКРЫТЬ</size>";
            label.fontSize = 9;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(10, 4);
            label.gameObject.AddComponent<LaserWorldLabel>();
        }

        static void Block(string name, Transform parent, Vector3 pos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var mat = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Graphics/Material/Global/Studs.mat"));
            mat.SetColor("_Color", color);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Scale", .5f);
            mat.SetFloat("_IsDynamic", 1);
            mat.SetFloat("_AmbientIntensity", 1.3f);
            mat.SetVector("_ObjectScale", new Vector4(size.x, size.y, size.z, 1));
            AssetDatabase.CreateAsset(mat, AssetDatabase.GenerateUniqueAssetPath("Assets/LaserHero/GeneratedV2/ReferenceStuds.mat"));
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
