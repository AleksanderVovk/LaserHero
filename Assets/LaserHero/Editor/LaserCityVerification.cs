using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserCityVerification
    {
        public static string Status = "Not run";
        public static async void Run()
        {
            if (!Application.isPlaying)
                return;
            var arena = LaserArena.Instance;
            var hero = arena.hero;
            var hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            var progress = hud.progress;
            string saved = JsonUtility.ToJson(progress.Data);
            bool suppress = progress.SuppressSaving;
            Vector3 position = hero.transform.position;
            float yaw = hero.yaw, pitch = hero.pitch;
            var lines = new List<string>();
            Status = "Running";
            LaserVoxelBody clone = null, ground = null, crane = null;
            void Check(bool ok, string text)
            {
                if (!ok)
                    throw new Exception(text);
                lines.Add("PASS: " + text);
            }

            try
            {
                progress.SuppressSaving = true;
                hud.CloseMenus();
                var city = GameObject.Find("LEGO City");
                var bodies = city.GetComponentsInChildren<LaserVoxelBody>();
                Check(arena.arenaBounds.size.x == 128 && arena.arenaBounds.size.z == 128, "City arena covers 128 x 128 metres");
                Check(bodies.Count(b => b.terrain) == 256 && bodies.All(b =>
                    b.allowDestruction), "All 256 terrain chunks and all props are destructible");
                Check(bodies.Count(b => b.style == 10) == 10 && bodies.Count(b => b.style == 15) == 2
                    && bodies.Count(b => b.style == 11) == 2, "Ten finished high-rises, two construction frames and two tower cranes");
                Check(bodies.Where(b => b.style == 10).All(b => b.palette.Count(c => c == 2) > 100)
                    && bodies.Where(b => b.style == 10).Max(b => b.dimensions.y * b.cellSize) >= 37,
                    "High-rises contain coloured glass windows and reach over 37 metres");
                Check(bodies.Any(b => b.style == 12) && bodies.Any(b => b.style == 13) && bodies.Any(b =>
                    b.style == 5) && bodies.Count(b => b.style == 6) >= 20, "Street lighting, bins, bushes and benches exist");
                Check(bodies.Count(b => b.style == 16) == 10 && bodies.Any(b => b.style == 20)
                    && bodies.Any(b => b.style == 17), "Parked cars, fountain and construction fencing exist");
                Check(bodies.Where(b => b.terrain).Any(b => b.palette.Contains((byte)4))
                    && bodies.Where(b => b.terrain).All(b => b.GetComponent<MeshRenderer>().sharedMaterials.Length == 5),
                    "Road markings, crossings and sidewalks use distinct voxel surfaces");
                arena.Enter();
                Vector3 point = new Vector3(2, -.1f, 42);
                ground = bodies.First(b => b.terrain && b.ContainsSolid(point));
                Check(LaserArena.GroundSupports(point), "Spatial terrain lookup finds a real support cell");
                int cut = ground.Carve(point, .55f, 999, Vector3.down);
                Check(cut > 0 && !LaserArena.GroundSupports(point), "Terrain lookup immediately sees a carved hole");
                ground.ResetBody();
                Check(LaserArena.GroundSupports(point), "Reset restores terrain support");
                var source = bodies.First(b => b.style == 10 && b.dimensions.y > 60);
                var go = UnityEngine.Object.Instantiate(source.gameObject, city.transform);
                clone = go.GetComponent<LaserVoxelBody>();
                clone.displayName = "City verification tower";
                clone.transform.position = new Vector3(-28, 0, 40);
                clone.Generate();
                int total = clone.Remaining;
                Vector3 center = clone.transform.TransformPoint(new Vector3(clone.dimensions.x * .25f, 0,
                    clone.dimensions.z * .25f));
                int removed = clone.Carve(center, 11, 999, Vector3.forward);
                var fragments = LaserVoxelBody.Active.Where(b => b.isFragment && b.displayName == clone.displayName).ToArray();
                Check(removed > 0 && fragments.Length > 0 && fragments.Sum(b =>
                    b.Remaining) + clone.Remaining + removed == total, "Removing tower foundations detaches intact upper floors with no lost cells");
                Check(fragments.All(b => b.GetComponent<Rigidbody>() != null && b.surfaceColors.Length == 3
                    && b.GetComponents<BoxCollider>().Length > 0), "Falling high-rise sections retain their window colours and compound physics");
                foreach (var b in fragments)
                    UnityEngine.Object.Destroy(b.gameObject);
                UnityEngine.Object.Destroy(clone.gameObject);
                clone = null;
                crane = bodies.First(b => b.style == 11);
                int craneTotal = crane.Remaining;
                Vector3 foot = crane.transform.TransformPoint(new Vector3(22.5f, .5f, 5.5f) * crane.cellSize);
                int severed = crane.Carve(foot, 1.9f, 999, Vector3.right);
                var boom = LaserVoxelBody.Active.Where(b => b.isFragment && b.displayName == crane.displayName).ToArray();
                Check(severed > 0 && boom.Any(b => b.dimensions.x > 40
                    && b.Remaining > 1000), "Severing crane mast releases a coherent boom and upper tower");
                Check(severed + crane.Remaining + boom.Sum(b =>
                    b.Remaining) == craneTotal, "Crane collapse preserves intact voxels instead of exploding every cell");
                foreach (var b in boom)
                    UnityEngine.Object.Destroy(b.gameObject);
                crane.ResetBody();
                crane = null;
                Check(hero.flightCeiling >= 65 && hero.view.farClipPlane >= 300,
                    "Flight ceiling and camera range cover the enlarged skyline");
                arena.Lobby();
                Check(!LaserArena.CanPhysicsDamage(Vector3.zero), "City expansion preserves safe lobby boundaries");
                int frame = Time.frameCount;
                await Task.Delay(600);
                Check(Time.frameCount > frame + 5, "Expanded city runs through advancing Play Mode frames");
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
                if (clone != null)
                    UnityEngine.Object.Destroy(clone.gameObject);
                foreach (var body in LaserVoxelBody.Active.Where(b => b.isFragment
                    && b.displayName == "City verification tower").ToArray())
                    UnityEngine.Object.Destroy(body.gameObject);
                if (ground != null)
                    ground.ResetBody();
                if (crane != null)
                {
                    foreach (var body in LaserVoxelBody.Active.Where(b => b.isFragment
                        && b.displayName == crane.displayName).ToArray())
                        UnityEngine.Object.Destroy(body.gameObject);
                    crane.ResetBody();
                }

                hero.Teleport(position);
                hero.yaw = yaw;
                hero.pitch = pitch;
                progress.Data = JsonUtility.FromJson<LaserSave>(saved);
                progress.SuppressSaving = suppress;
                Directory.CreateDirectory("LaserHeroQA");
                File.WriteAllLines("LaserHeroQA/city-verification.txt", lines);
                Debug.Log("CITY_VERIFICATION: " + Status);
            }
        }
    }
}
