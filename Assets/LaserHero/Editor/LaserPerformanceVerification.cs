using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserPerformanceVerification
    {
        public static string Status = "Not run";
        public static async void Run()
        {
            if (!Application.isPlaying)
                return;
            var hero = LaserArena.Instance.hero;
            var progress = hero.GetComponent<LaserEyes>().progress;
            string saved = JsonUtility.ToJson(progress.Data);
            bool suppress = progress.SuppressSaving;
            Vector3 originalPos = hero.transform.position;
            var objects = new List<GameObject>();
            var lines = new List<string>();
            Status = "Running";
            void Check(bool ok, string text)
            {
                if (!ok)
                    throw new Exception(text);
                lines.Add("PASS: " + text);
            }

            LaserVoxelBody Make(string name, Vector3 pos, Vector3Int dims, bool fragment = false)
            {
                var go = new GameObject(name);
                objects.Add(go);
                go.transform.position = pos;
                var b = go.AddComponent<LaserVoxelBody>();
                b.displayName = name;
                b.style = 3;
                b.dimensions = dims;
                b.cellSize = .5f;
                b.hitPoints = 2;
                b.requiresGroundSupport = false;
                b.isFragment = fragment;
                b.GetComponent<MeshRenderer>().sharedMaterials = LaserVoxelBody.Active.First(v =>
                    v.terrain).GetComponent<MeshRenderer>().sharedMaterials.Take(3).ToArray();
                b.Generate();
                return b;
            }

            try
            {
                progress.SuppressSaving = true;
                LaserArena.Instance.Enter();
                var cube = Make("Greedy surface test", new Vector3(10, 2, 45), new Vector3Int(6, 6, 6));
                Array.Clear(cube.palette, 0, cube.palette.Length);
                cube.Rebuild();
                Check(cube.GetComponent<MeshFilter>().sharedMesh.triangles.Length == 36,
                    "Solid cuboid merges into six quads (12 triangles)");
                Physics.SyncTransforms();
                Vector3 center = new Vector3(11.5f, 3.5f, 46.5f);
                var collider = cube.GetComponent<MeshCollider>();
                foreach (var d in new[]
                {
                    Vector3.right,
                    Vector3.left,
                    Vector3.up,
                    Vector3.down,
                    Vector3.forward,
                    Vector3.back
                }

                )
                    Check(collider.Raycast(new Ray(center + d * 5, -d), out var hit, 10)
                        && Vector3.Dot(hit.normal, d) > .9f, "Greedy face collision and winding: " + d);
                collider.Raycast(new Ray(center - Vector3.right * 5, Vector3.right), out var first, 10);
                cube.Carve(new Vector3(10.1f, 3.5f, 46.5f), .8f, 999, Vector3.right);
                Physics.SyncTransforms();
                Check(collider.Raycast(new Ray(center - Vector3.right * 5, Vector3.right), out var after,
                    10) && after.distance > first.distance + .4f, "Carving updates the cooked collider and opens a real hole");
                var source = Make("Large falling test", new Vector3(0, 20, 60), new Vector3Int(16, 16, 16), true);
                var rb = source.gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.mass = 600;
                var impact = source.gameObject.AddComponent<LaserFallingImpact>();
                impact.voxelCount = 4096;
                var target = Make("Impact strength test", new Vector3(-36, 10, 40), new Vector3Int(24, 12, 24));
                Vector3 point = new Vector3(-30, 11, 46);
                source.dimensions = new Vector3Int(4, 4, 4);
                source.Generate();
                rb.mass = source.PhysicsMass;
                int small = impact.ApplyVoxelImpact(point, 8, Vector3.down, target);
                target.ResetBody();
                source.dimensions = new Vector3Int(16, 16, 16);
                source.Generate();
                rb.mass = source.PhysicsMass;
                await Task.Delay(40);
                int large = impact.ApplyVoxelImpact(point, 8, Vector3.down, target);
                Check(large > small && large > 16 && large <= impact.HitBudget,
                    "Large structure impact destroys more voxels than a small fragment, within its cap");
                Check(impact.HitBudget > 96 && impact.LifetimeBudget > 256,
                    "Large structure budgets scale with solid world volume");
                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(30);
                    impact.ApplyVoxelImpact(point + new Vector3(i * .5f, 0, 0), 100, Vector3.down, target);
                }

                Check(impact.DestroyedByImpact <= impact.LifetimeBudget,
                    "Repeated high-speed impacts still respect the lifetime budget");
                source.physicalGeneration = LaserCollapseSettings.Current.maximumGeneration;
                Check(impact.ApplyVoxelImpact(point, 100, Vector3.down, target) == 0,
                    "Chain reaction stops at the configured generation");
                source.physicalGeneration = 0;
                Check(impact.ApplyVoxelImpact(Vector3.zero, 100, Vector3.down, target) == 0,
                    "Stronger impact cannot damage lobby voxels");
                int[] old = source.GetComponents<BoxCollider>().Where(b => b.enabled).Select(b => b.GetInstanceID()).ToArray();
                source.Carve(source.transform.position + new Vector3(.1f, 7.8f, .1f), .6f, 999, Vector3.right);
                Check(source.GetComponents<BoxCollider>().Where(b => b.enabled).Any(b =>
                    old.Contains(b.GetInstanceID())), "Rigidbody collider rebuild reuses existing BoxCollider components");
                var city = GameObject.Find("LEGO City");
                long triangles = city.GetComponentsInChildren<MeshFilter>().Sum(f => (long)f.sharedMesh.triangles.Length / 3);
                Check(triangles < 100000, "City geometry stays below 100,000 triangles after greedy merging");
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
                foreach (var go in objects)
                    if (go != null)
                        UnityEngine.Object.Destroy(go);
                foreach (var b in LaserVoxelBody.Active.Where(b => b.isFragment
                    && (b.displayName == "Large falling test" || b.displayName == "Impact strength test"
                    || b.displayName == "Greedy surface test")).ToArray())
                    UnityEngine.Object.Destroy(b.gameObject);
                hero.Teleport(originalPos);
                progress.Data = JsonUtility.FromJson<LaserSave>(saved);
                progress.SuppressSaving = suppress;
                Directory.CreateDirectory("LaserHeroQA");
                File.WriteAllLines("LaserHeroQA/performance-verification.txt", lines);
                Debug.Log("PERFORMANCE_VERIFICATION: " + Status);
            }
        }
    }
}
