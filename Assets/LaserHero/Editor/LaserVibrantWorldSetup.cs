using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserVibrantWorldSetup
    {
        public static string Apply()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first");
            var feedback = UnityEngine.Object.FindFirstObjectByType<LaserImpactFeedback>();
            if (feedback == null)
                throw new InvalidOperationException("Open the Laser Hero lobby");
            const string folder = "Assets/LaserHero/VibrantWorld";
            if (AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException("Vibrant materials already created");
            EditorSceneManager.SaveScene(feedback.gameObject.scene, "Assets/Scenes/Backups/Lobby_BeforeVibrantWorld.unity", true);
            AssetDatabase.CreateFolder("Assets/LaserHero", "VibrantWorld");
            var shader = Shader.Find("LaserHero/Stud Plastic");
            if (shader == null)
                throw new Exception("Stud Plastic shader missing");
            var converted = new Dictionary<Material, Material>();
            Material Convert(Material old)
            {
                if (old == null || old.shader.name != "Custom/URP/DynamicTiling_URP_Mobile_Ambient_Normal")
                    return old;
                if (converted.TryGetValue(old, out var cached))
                    return cached;
                var material = new Material(old)
                {
                    shader = shader,
                    name = old.name + " Bright"
                };
                material.SetColor("_Color", Brighten(old.GetColor("_Color")));
                material.SetFloat("_BumpScale", .3f);
                material.SetFloat("_TextureStrength", .55f);
                material.SetFloat("_Smoothness", .35f);
                AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + material.name + ".mat"));
                converted.Add(old, material);
                return material;
            }

            int renderers = 0;
            foreach (var renderer in feedback.GetComponentsInChildren<Renderer>(true))
            {
                var before = renderer.sharedMaterials;
                var after = before.Select(Convert).ToArray();
                if (before.Where((m, i) => m != after[i]).Any())
                {
                    Undo.RecordObject(renderer, "Brighter Laser Hero materials");
                    renderer.sharedMaterials = after;
                    EditorUtility.SetDirty(renderer);
                    renderers++;
                }
            }

            foreach (var body in feedback.GetComponentsInChildren<LaserVoxelBody>(true))
            {
                body.bodyColor = Brighten(body.bodyColor);
                body.trimColor = Brighten(body.trimColor);
                body.accentColor = Brighten(body.accentColor);
                if (body.surfaceColors != null)
                    body.surfaceColors = body.surfaceColors.Select(Brighten).ToArray();
                EditorUtility.SetDirty(body);
            }

            feedback.debrisMaterial = Convert(feedback.debrisMaterial);
            EditorUtility.SetDirty(feedback);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(feedback.gameObject.scene);
            EditorSceneManager.SaveScene(feedback.gameObject.scene);
            return "VIBRANT_WORLD: " + converted.Count + " materials, " + renderers + " renderers; saved";
        }

        static Color Brighten(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            float newValue = value < .4f ? value * 1.05f : Mathf.Min(1, value + .08f);
            var result = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 1.18f), newValue);
            result.a = color.a;
            return result;
        }
    }
}
