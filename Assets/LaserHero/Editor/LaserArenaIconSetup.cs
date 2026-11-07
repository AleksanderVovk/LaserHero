using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LaserHero.Editor
{
    public static class LaserArenaIconSetup
    {
        const string Folder = "Assets/LaserHero/UI/ArenaIcons";
        sealed class Shot
        {
            public string root, file;
            public Vector3 target;
            public float size;
            public Color background;
            public Shot(string root, string file, Vector3 target, float size, Color background)
            {
                this.root = root;
                this.file = file;
                this.target = target;
                this.size = size;
                this.background = background;
            }
        }

        [MenuItem("Laser Hero/Render all arena icons 512")]
        public static string RenderAll()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Render arena icons in Edit Mode");
            var arena = UnityEngine.Object.FindFirstObjectByType<LaserArena>();
            if (arena == null)
                throw new Exception("Open Lobby scene");
            Directory.CreateDirectory(Folder);
            var shots = new[]
            {
                new Shot("LEGO City", "Arena_City", new Vector3(0, 20, 106), 51, new Color(.28f, .58f, .79f)),
                new Shot("Spaceport Arena", "Arena_Spaceport", new Vector3(-7, 23, 124), 53, new Color(.15f, .42f, .64f)),
                new Shot("Ocean Arena", "Arena_Ocean", new Vector3(-8, 14, 123), 43, new Color(.1f, .48f, .6f)),
                new Shot("Amusement Park Arena", "Arena_AmusementPark", new Vector3(7, 18, 127), 48, new Color(.31f, .58f, .8f)),
                new Shot("LEGO Arena — Prototype Backup", "Arena_Prototype", new Vector3(0, 10, 66), 26,
                    new Color(.32f, .57f, .74f))
            };
            var sheet = new Texture2D(512 * shots.Length, 512, TextureFormat.RGB24, false);
            try
            {
                for (int index = 0; index < shots.Length; index++)
                {
                    var shot = shots[index];
                    var original = arena.transform.Find(shot.root);
                    if (original == null)
                        throw new Exception("Missing arena: " + shot.root);
                    var wrapper = new GameObject("Temporary arena icon stage");
                    wrapper.SetActive(false);
                    GameObject camGo = null;
                    RenderTexture rt = null;
                    Texture2D texture = null;
                    var previous = RenderTexture.active;
                    try
                    {
                        var clone = UnityEngine.Object.Instantiate(original.gameObject, wrapper.transform, false);
                        clone.name = "Icon geometry";
                        clone.transform.SetPositionAndRotation(original.position, original.rotation);
                        clone.transform.localScale = original.lossyScale;
                        // The real scene's activation, scripts, colliders and renderers stay untouched.
                        foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
                            UnityEngine.Object.DestroyImmediate(behaviour);
                        foreach (var collider in clone.GetComponentsInChildren<Collider>(true))
                            collider.enabled = false;
                        foreach (var t in clone.GetComponentsInChildren<Transform>(true))
                            t.gameObject.layer = 31;
                        foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
                        {
                            renderer.enabled = true;
                            renderer.forceRenderingOff = false;
                        }

                        clone.SetActive(true);
                        wrapper.SetActive(true);
                        camGo = new GameObject("Temporary arena icon camera");
                        var cam = camGo.AddComponent<Camera>();
                        cam.enabled = false;
                        camGo.AddComponent<UniversalAdditionalCameraData>();
                        cam.cullingMask = 1 << 31;
                        cam.orthographic = true;
                        cam.orthographicSize = shot.size;
                        cam.nearClipPlane = .1f;
                        cam.farClipPlane = 500;
                        cam.clearFlags = CameraClearFlags.SolidColor;
                        cam.backgroundColor = shot.background;
                        cam.transform.position = shot.target + new Vector3(85, 68, -100);
                        cam.transform.LookAt(shot.target);
                        rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32)
                        {
                            antiAliasing = 4
                        };
                        cam.targetTexture = rt;
                        cam.Render();
                        RenderTexture.active = rt;
                        texture = new Texture2D(512, 512, TextureFormat.RGB24, false);
                        texture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                        texture.Apply();
                        string path = Folder + "/" + shot.file + ".png";
                        File.WriteAllBytes(path, texture.EncodeToPNG());
                        sheet.SetPixels(index * 512, 0, 512, 512, texture.GetPixels());
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                        if (camGo != null)
                            UnityEngine.Object.DestroyImmediate(camGo);
                        if (rt != null)
                        {
                            rt.Release();
                            UnityEngine.Object.DestroyImmediate(rt);
                        }

                        if (texture != null)
                            UnityEngine.Object.DestroyImmediate(texture);
                        UnityEngine.Object.DestroyImmediate(wrapper);
                    }
                }

                sheet.Apply();
                Directory.CreateDirectory("LaserHeroQA");
                File.WriteAllBytes("LaserHeroQA/ArenaIcons-ContactSheet.png", sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }

            AssetDatabase.Refresh();
            foreach (var shot in shots)
            {
                string path = Folder + "/" + shot.file + ".png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null || sprite.texture.width != 512 || sprite.texture.height != 512)
                    throw new Exception("Invalid icon size: " + path);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Arena_AmusementPark.png");
            return "Five arena sprites exported and verified at 512x512; all original scene objects preserved";
        }
    }
}
