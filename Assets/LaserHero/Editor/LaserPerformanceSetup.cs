using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserPerformanceSetup
    {
        [MenuItem("Laser Hero/Refresh optimized city geometry")]
        public static string Apply()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first");
            var city = GameObject.Find("LEGO City");
            if (city == null)
                throw new InvalidOperationException("Open city Lobby scene");
            EditorSceneManager.SaveScene(city.scene, "Assets/Scenes/Backups/Lobby_BeforePerformance.unity", true);
            int count = 0;
            long before = 0, after = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var body in city.GetComponentsInChildren<LaserVoxelBody>())
                {
                    var filter = body.GetComponent<MeshFilter>();
                    var old = filter.sharedMesh;
                    if (old != null)
                        before += old.triangles.Length / 3;
                    body.Generate();
                    var optimized = filter.sharedMesh;
                    after += optimized.triangles.Length / 3;
                    if (old != null && AssetDatabase.Contains(old) && old != optimized)
                    {
                        EditorUtility.CopySerialized(optimized, old);
                        filter.sharedMesh = old;
                        var collider = body.GetComponent<MeshCollider>();
                        collider.sharedMesh = null;
                        collider.sharedMesh = old;
                        EditorUtility.SetDirty(old);
                        UnityEngine.Object.DestroyImmediate(optimized);
                    }

                    EditorUtility.SetDirty(body);
                    EditorUtility.SetDirty(filter);
                    EditorUtility.SetDirty(body.GetComponent<MeshCollider>());
                    count++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(city.scene);
            EditorSceneManager.SaveScene(city.scene);
            return "OPTIMIZED " + count + " bodies; triangles " + before + " -> " + after;
        }
    }
}
