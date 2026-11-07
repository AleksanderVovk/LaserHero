using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LaserHero.Editor
{
    public static class LaserArenaBatchingSetup
    {
        const string Folder = "Assets/LaserHero/BatchedArenas";
        [MenuItem("Laser Hero/Bake render sectors for all arenas")]
        public static void BakeMenu()
        {
            Debug.Log(Apply());
        }

        public static string Apply()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first");
            var rotation = UnityEngine.Object.FindFirstObjectByType<LaserArenaRotation>();
            if (rotation == null)
                throw new InvalidOperationException("Open the Lobby scene first");
            var maps = rotation.maps.Where(m => m != null && m.root != null).GroupBy(m => m.root).Select(g => g.First()).ToArray();
            // Validate before changing anything. Current arenas already store colours in vertices.
            foreach (var map in maps)
                foreach (var body in map.root.GetComponentsInChildren<LaserVoxelBody>(true))
                {
                    var mesh = body.GetComponent<MeshFilter>().sharedMesh;
                    var renderer = body.GetComponent<MeshRenderer>();
                    if (!body.singleMaterialSurface || mesh == null || mesh.subMeshCount != 1 || renderer.sharedMaterial == null)
                        throw new InvalidOperationException("Expected a prepared single-material source: " + body.name);
                }

            EditorSceneManager.SaveScene(rotation.gameObject.scene,
                "Assets/Scenes/Backups/Lobby_BeforeArenaBatching_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity", true);
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/LaserHero", "BatchedArenas");
            var report = new List<string>();
            foreach (var map in maps)
            {
                string folder = Folder + "/" + map.id;
                if (!AssetDatabase.IsValidFolder(folder))
                    AssetDatabase.CreateFolder(Folder, map.id);
                var sources = map.root.GetComponentsInChildren<LaserVoxelBody>(true).Where(b => !b.isFragment).ToArray();
                var batcher = map.root.GetComponentInChildren<LaserWorldRenderBatcher>(true);
                if (batcher == null)
                {
                    var go = new GameObject("Arena render sectors");
                    go.transform.SetParent(map.root.transform, false);
                    batcher = go.AddComponent<LaserWorldRenderBatcher>();
                }

                batcher.enabled = false;
                if (batcher.groups != null)
                    foreach (var old in batcher.groups)
                        if (old.filter != null)
                            UnityEngine.Object.DestroyImmediate(old.filter.gameObject);
                var groups = new List<LaserWorldRenderBatcher.Group>();
                foreach (var sector in sources.GroupBy(b =>
                    new { x = Mathf.FloorToInt(b.transform.position.x / 24), z = Mathf.FloorToInt(b.transform.position.z
                    / 24), material = b.GetComponent<MeshRenderer>().sharedMaterial }))
                {
                    var go = new GameObject("Sector " + sector.Key.x + "," + sector.Key.z,
                        typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(batcher.transform, false);
                    var group = new LaserWorldRenderBatcher.Group
                    {
                        bodies = sector.ToArray(),
                        filter = go.GetComponent<MeshFilter>(),
                        renderer = go.GetComponent<MeshRenderer>()
                    };
                    var source = group.bodies[0].GetComponent<MeshRenderer>();
                    go.layer = source.gameObject.layer;
                    group.renderer.sharedMaterial = sector.Key.material;
                    group.renderer.shadowCastingMode = source.shadowCastingMode;
                    group.renderer.receiveShadows = source.receiveShadows;
                    group.renderer.lightProbeUsage = source.lightProbeUsage;
                    group.renderer.reflectionProbeUsage = source.reflectionProbeUsage;
                    var instances = group.bodies.Where(b => Included(b, map.root.transform)).Select(b => new CombineInstance
                    {
                        mesh = b.GetComponent<MeshFilter>().sharedMesh,
                        subMeshIndex = 0,
                        transform = go.transform.worldToLocalMatrix * b.transform.localToWorldMatrix
                    }).ToArray();
                    var baked = new Mesh
                    {
                        name = map.title + " sector " + groups.Count,
                        indexFormat = IndexFormat.UInt32
                    };
                    if (instances.Length > 0)
                        baked.CombineMeshes(instances, true, true, false);
                    string path = folder + "/Sector" + groups.Count + ".asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (existing != null)
                    {
                        EditorUtility.CopySerialized(baked, existing);
                        EditorUtility.SetDirty(existing);
                        UnityEngine.Object.DestroyImmediate(baked);
                        baked = existing;
                    }
                    else
                        AssetDatabase.CreateAsset(baked, path);
                    group.filter.sharedMesh = baked;
                    group.renderer.enabled = instances.Length > 0;
                    groups.Add(group);
                }

                batcher.groups = groups.ToArray();
                batcher.enabled = true;
                foreach (var body in sources)
                {
                    body.GetComponent<MeshRenderer>().enabled = false;
                    EditorUtility.SetDirty(body.GetComponent<MeshRenderer>());
                }

                EditorUtility.SetDirty(batcher);
                report.Add(map.title + ": " + sources.Length + " object renderers -> " + groups.Count
                    + " sector renderers; triangles=" + groups.Sum(g => g.filter.sharedMesh.triangles.Length / 3));
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(rotation.gameObject.scene);
            EditorSceneManager.SaveScene(rotation.gameObject.scene);
            Directory.CreateDirectory("LaserHeroQA");
            File.WriteAllLines("LaserHeroQA/arena-batching.txt", report);
            return string.Join("\n", report);
        }

        static bool Included(LaserVoxelBody body, Transform root)
        {
            if (!body.enabled)
                return false;
            for (var t = body.transform; t != root && t != null; t = t.parent)
                if (!t.gameObject.activeSelf)
                    return false;
            return true;
        }
    }
}
