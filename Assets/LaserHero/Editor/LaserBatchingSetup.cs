using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserBatchingSetup
    {
        public static string Apply()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first");
            var root = UnityEngine.Object.FindFirstObjectByType<LaserImpactFeedback>().transform;
            if (root.GetComponentInChildren<LaserWorldRenderBatcher>() != null)
                throw new InvalidOperationException("World already batched");
            EditorSceneManager.SaveScene(root.gameObject.scene, "Assets/Scenes/Backups/Lobby_BeforeRenderBatching.unity", true);
            const string folder = "Assets/LaserHero/BatchedWorld";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/LaserHero", "BatchedWorld");
            var bodies = root.GetComponentsInChildren<LaserVoxelBody>().Where(b => !b.isFragment).ToArray();
            var material = new Material(bodies[0].GetComponent<MeshRenderer>().sharedMaterial)
            {
                name = "Shared colourful stud plastic",
                enableInstancing = true
            };
            material.SetColor("_Color", Color.white);
            material.SetFloat("_UseVertexColor", 1);
            material.SetFloat("_IsDynamic", 0);
            AssetDatabase.CreateAsset(material, folder + "/SharedPlastic.mat");
            int before = 0;
            long triangles = 0;
            foreach (var body in bodies)
            {
                var renderer = body.GetComponent<MeshRenderer>();
                var filter = body.GetComponent<MeshFilter>();
                before += filter.sharedMesh.subMeshCount;
                triangles += filter.sharedMesh.triangles.Length / 3;
                body.singleMaterialSurface = true;
                renderer.sharedMaterials = new[]
                {
                    material
                };
                body.Generate();
                AssetDatabase.CreateAsset(filter.sharedMesh, folder + "/Source" + Array.IndexOf(bodies, body) + ".asset");
                renderer.enabled = false;
                EditorUtility.SetDirty(body);
                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(filter);
                EditorUtility.SetDirty(body.GetComponent<MeshCollider>());
            }

            var go = new GameObject("World render sectors");
            go.transform.SetParent(root, false);
            var batcher = go.AddComponent<LaserWorldRenderBatcher>();
            var groups = new List<LaserWorldRenderBatcher.Group>();
            foreach (var sector in bodies.GroupBy(b =>
                new Vector2Int(Mathf.FloorToInt(b.transform.position.x / 24), Mathf.FloorToInt(b.transform.position.z / 24))))
            {
                var chunk = new GameObject("Render sector " + sector.Key.x + "," + sector.Key.y,
                    typeof(MeshFilter), typeof(MeshRenderer));
                chunk.transform.SetParent(go.transform, false);
                var group = new LaserWorldRenderBatcher.Group
                {
                    bodies = sector.ToArray(),
                    filter = chunk.GetComponent<MeshFilter>(),
                    renderer = chunk.GetComponent<MeshRenderer>()
                };
                group.renderer.sharedMaterial = material;
                batcher.Rebuild(group);
                AssetDatabase.CreateAsset(group.filter.sharedMesh, folder + "/Sector" + groups.Count + ".asset");
                groups.Add(group);
            }

            batcher.groups = groups.ToArray();
            EditorUtility.SetDirty(batcher);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            EditorSceneManager.SaveScene(root.gameObject.scene);
            return "BATCHED: " + bodies.Length + " bodies, " + before + " source submeshes -> " + groups.Count
                + " sector renderers; original triangles=" + triangles;
        }
    }
}
