using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserCollisionVerification
    {
        public static string Run()
        {
            if (!Application.isPlaying)
                throw new InvalidOperationException("Play Mode required");
            var progress = UnityEngine.Object.FindFirstObjectByType<LaserProgress>();
            string save = JsonUtility.ToJson(progress.Data);
            bool suppress = progress.SuppressSaving;
            progress.SuppressSaving = true;
            var checks = new List<string>();
            GameObject a = null, b = null;
            var hero = UnityEngine.Object.FindFirstObjectByType<LaserHeroPlayer>();
            bool enabled = hero.enabled;
            Vector3 cameraPos = hero.view.transform.position;
            Quaternion cameraRot = hero.view.transform.rotation;
            void Check(bool ok, string message)
            {
                if (!ok)
                    throw new Exception(message);
                checks.Add("PASS: " + message);
            }

            try
            {
                var materials = LaserVoxelBody.Active.First(v => v.terrain).GetComponent<MeshRenderer>().sharedMaterials;
                a = new GameObject("Collision verification target");
                var target = a.AddComponent<LaserVoxelBody>();
                target.dimensions = new Vector3Int(14, 8, 14);
                target.style = 3;
                target.hitPoints = 2;
                target.cellSize = .5f;
                target.requiresGroundSupport = false;
                a.transform.position = new Vector3(9, 0, 39);
                target.GetComponent<MeshRenderer>().sharedMaterials = materials;
                target.Generate();
                b = new GameObject("Collision verification source");
                var source = b.AddComponent<LaserVoxelBody>();
                source.isFragment = true;
                source.dimensions = new Vector3Int(3, 3, 3);
                source.style = 3;
                source.cellSize = .5f;
                b.transform.position = new Vector3(5, 10, 42);
                source.GetComponent<MeshRenderer>().sharedMaterials = materials;
                source.Generate();
                var rb = b.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.mass = 80;
                var impact = b.AddComponent<LaserFallingImpact>();
                impact.voxelCount = 27;
                Vector3 point = new Vector3(12.5f, 1.5f, 42.5f);
                int count = target.Remaining;
                Check(impact.ApplyVoxelImpact(point, 1.5f, Vector3.down, target) == 0
                    && target.Remaining == count, "Slow contacts do not damage voxels");
                rb.mass = .2f;
                Check(impact.ApplyVoxelImpact(point, 3, Vector3.down, target) == 0,
                    "Light debris cannot create heavy impact damage");
                rb.mass = 80;
                int first = impact.ApplyVoxelImpact(point, 6, Vector3.down, target);
                Check(first > 0 && first <= impact.HitBudget
                    && count - target.Remaining == first, "Mass and speed create bounded local damage");
                int second = impact.ApplyVoxelImpact(point + Vector3.right, 100, Vector3.down, target);
                Check(second <= impact.HitBudget && impact.DestroyedByImpact <= impact.LifetimeBudget,
                    "Extreme speed still respects per-hit and lifetime limits");
                int before = target.Remaining;
                impact.ApplyVoxelImpact(point, 100, Vector3.down, target);
                Check(impact.DestroyedByImpact <= impact.LifetimeBudget
                    && target.Remaining <= before, "Repeated contacts respect the volume-based lifetime budget");
                source.physicalGeneration = LaserCollapseSettings.Current.maximumGeneration;
                Check(impact.ApplyVoxelImpact(point, 100, Vector3.down, target) == 0,
                    "Configured maximum generation stops the destruction cascade");
                source.physicalGeneration = 0;
                Check(impact.ApplyVoxelImpact(Vector3.zero, 100, Vector3.down, target) == 0,
                    "Physical damage cannot reach the lobby");
                hero.enabled = false;
                hero.ClearShake();
                hero.SendMessage("LateUpdate");
                Vector3 pivot = hero.transform.position + Vector3.up * .75f;
                Vector3 right = Quaternion.Euler(0, hero.yaw, 0) * Vector3.right;
                Check(Mathf.Abs(Vector3.Dot(hero.view.transform.position - pivot, right)) < .001f,
                    "Camera sits centered behind the player view");
                Check(LaserImpactFeedback.Instance.laserLoop != null
                    && LaserImpactFeedback.Instance.laserLoop.length > 1, "Seamless sustained laser buzz is assigned");
                return "PASS (" + checks.Count + " collision/audio/camera checks)";
            }
            finally
            {
                if (a != null)
                    UnityEngine.Object.Destroy(a);
                if (b != null)
                    UnityEngine.Object.Destroy(b);
                progress.Data = JsonUtility.FromJson<LaserSave>(save);
                progress.SuppressSaving = suppress;
                hero.enabled = enabled;
                hero.view.transform.SetPositionAndRotation(cameraPos, cameraRot);
                Directory.CreateDirectory("LaserHeroQA");
                File.WriteAllLines("LaserHeroQA/collision-verification.txt", checks);
            }
        }
    }
}
