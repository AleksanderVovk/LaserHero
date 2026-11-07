using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserBatchingVerification
    {
        public static string Status = "Not run";
        public static string Run()
        {
            var batcher = UnityEngine.Object.FindFirstObjectByType<LaserWorldRenderBatcher>();
            var hero = LaserArena.Instance.hero;
            Vector3 position = hero.transform.position;
            float yaw = hero.yaw, pitch = hero.pitch;
            var lines = new List<string>();
            LaserVoxelBody ground = null;
            GameObject test = null;
            void Check(bool ok, string text)
            {
                if (!ok)
                    throw new Exception(text);
                lines.Add("PASS: " + text);
            }

            try
            {
                batcher.Flush();
                LaserArena.Instance.Enter();
                var sources = batcher.groups.SelectMany(g => g.bodies).ToArray();
                Check(batcher.groups.Length == 36, "World uses 36 spatial render sectors");
                Check(sources.All(b => b.singleMaterialSurface
                    && b.GetComponent<MeshFilter>().sharedMesh.subMeshCount == 1), "Every batched source has one submesh");
                Check(sources.All(b => !b.GetComponent<MeshRenderer>().enabled),
                    "Source renderers are hidden to avoid duplicate rendering");
                Check(batcher.groups.Sum(g => g.filter.sharedMesh.triangles.Length) == sources.Sum(b =>
                    b.GetComponent<MeshFilter>().sharedMesh.triangles.Length),
                    "Sector meshes retain every triangle of the original surfaces");
                Check(batcher.groups.Select(g => g.renderer.sharedMaterial).Distinct().Count() == 1,
                    "All sectors share one material");
                Check(sources.Where(b => b.terrain).Any(b =>
                    b.GetComponent<MeshFilter>().sharedMesh.colors.Distinct().Count() >= 4),
                    "Grass, soil, roads and pavement retain distinct vertex colours");
                ground = sources.First(b => b.terrain && b.transform.position.x == 0 && b.transform.position.z == 38);
                var group = batcher.groups.First(g => g.bodies.Contains(ground));
                int before = batcher.RebuildCount;
                int removed = ground.Carve(new Vector3(4, -.25f, 42), .5f, 999, Vector3.down);
                batcher.Flush();
                Check(removed > 0 && batcher.RebuildCount == before + 1, "A laser-sized terrain hit rebuilds exactly one sector");
                Check(group.filter.sharedMesh.vertexCount == group.bodies.Where(b =>
                    b.isActiveAndEnabled).Sum(b => b.GetComponent<MeshFilter>().sharedMesh.vertexCount),
                    "Rendered sector geometry matches the carved body meshes");
                ground.ResetBody();
                batcher.Flush();
                Check(group.filter.sharedMesh.triangles.Length == group.bodies.Sum(b =>
                    b.GetComponent<MeshFilter>().sharedMesh.triangles.Length), "Arena reset restores the batched visual surface");
                test = new GameObject("Batch falling verification");
                test.transform.position = new Vector3(-30, 1, 44);
                var body = test.AddComponent<LaserVoxelBody>();
                body.displayName = "Batch falling verification";
                body.style = 3;
                body.dimensions = new Vector3Int(6, 8, 6);
                body.cellSize = .5f;
                body.requiresGroundSupport = false;
                body.singleMaterialSurface = true;
                body.GetComponent<MeshRenderer>().sharedMaterial = group.renderer.sharedMaterial;
                body.Generate();
                body.Carve(new Vector3(-28.5f, 1, 45.5f), 2.5f, 999, Vector3.down);
                var fragments = LaserVoxelBody.Active.Where(b => b.isFragment && b.displayName == body.displayName).ToArray();
                Check(fragments.Length > 0 && fragments.All(b => b.GetComponent<Rigidbody>() != null
                    && b.GetComponent<MeshRenderer>().enabled && b.GetComponent<MeshFilter>().sharedMesh.subMeshCount == 1),
                    "Detached structures remain visible dynamic rigidbodies with one submesh");
                Check(fragments.All(b => b.GetComponent<MeshFilter>()
                    .sharedMesh.colors.Length == b.GetComponent<MeshFilter>().sharedMesh.vertexCount),
                    "Falling pieces retain their vertex palette");
                Status = "PASS (" + lines.Count + " checks)";
            }
            catch (Exception e)
            {
                Status = "FAIL: " + e.Message;
                lines.Add(Status);
                Debug.LogException(e);
            }
            finally
            {
                if (ground != null)
                {
                    ground.ResetBody();
                    batcher.Flush();
                }

                if (test != null)
                    UnityEngine.Object.Destroy(test);
                foreach (var b in LaserVoxelBody.Active.Where(b =>
                    b.displayName == "Batch falling verification" && b.isFragment).ToArray())
                    UnityEngine.Object.Destroy(b.gameObject);
                hero.Teleport(position);
                hero.yaw = yaw;
                hero.pitch = pitch;
                Directory.CreateDirectory("LaserHeroQA");
                File.WriteAllLines("LaserHeroQA/batching-verification.txt", lines);
            }

            return Status;
        }
    }
}
