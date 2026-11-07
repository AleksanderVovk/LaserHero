using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserHeroVerification
    {
        public static string Status = "Not run";
        [UnityEditor.MenuItem("Laser Hero/Verify gameplay (Play Mode)")]
        public static async void Run()
        {
            if (!Application.isPlaying)
            {
                Status = "Enter Play Mode first";
                return;
            }

            var progress = UnityEngine.Object.FindFirstObjectByType<LaserProgress>();
            var hero = UnityEngine.Object.FindFirstObjectByType<LaserHeroPlayer>();
            var eyes = hero.GetComponent<LaserEyes>();
            var arena = LaserArena.Instance;
            string saved = JsonUtility.ToJson(progress.Data);
            Vector3 position = hero.transform.position;
            Quaternion rotation = hero.transform.rotation;
            float yaw = hero.yaw, pitch = hero.pitch;
            bool auto = eyes.AutoFire, motorEnabled = hero.enabled;
            var hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            Vector3 cameraPos = hero.view.transform.position;
            Quaternion cameraRot = hero.view.transform.rotation;
            var checks = new List<string>();
            Status = "Running";
            LaserVoxelBody test = null;
            LaserVoxelBody ground = null;
            void Check(bool ok, string name)
            {
                if (!ok)
                    throw new Exception(name);
                checks.Add("PASS: " + name);
            }

            try
            {
                hud.CloseMenus();
                hero.enabled = false;
                eyes.AutoFire = false;
                progress.SuppressSaving = true;
                progress.Data = new LaserSave();
                progress.Reward(10);
                Check(progress.Data.pending == 0 && progress.Data.cash == 30, "Removed voxels credit the wallet immediately");
                progress.Collect();
                progress.Collect();
                Check(progress.Data.cash == 30 && progress.Data.pending == 0,
                    "Legacy collect callers cannot duplicate immediate earnings");
                Check(!progress.BuyLaser(1), "Cannot buy laser without money");
                progress.Data.cash = 600;
                Check(progress.BuyLaser(1) && progress.OwnsLaser(1) && progress.Data.cash == 0, "Purchase deducts exact price");
                Check(progress.BuyLaser(0) && progress.BuyLaser(1) && progress.Data.cash == 0, "Owned laser equips for free");
                float d = progress.Damage, r = progress.Radius;
                progress.GainXP(50);
                Check(progress.Data.level == 2 && progress.Damage > d && progress.Radius > r, "Level increases damage and width");
                var copy = JsonUtility.FromJson<LaserSave>(JsonUtility.ToJson(progress.Data));
                Check(copy.level == 2 && copy.ownedLasers == 3, "Save round trip retains progression");
                Check(LaserTrainingTarget.All.Any(t => t.access == LaserTrainingAccess.Free
                    && t.IsUnlocked(progress)), "Free training is available without a purchased entitlement");
                hud.ToggleShop();
                await Task.Delay(120);
                var labels = hud.GetComponentsInChildren<TMPro.TMP_Text>();
                Check(labels.Any(t => t.text == "Надеть") && labels.Any(t =>
                    t.text == "$12K"), "Shop displays equip state and prices");
                hud.CloseMenus();
                Check(hud.icons.All(i => i != null) && hud.orangePanel != null
                    && hud.greenPanel != null, "Production UI icons and stud panels assigned");
                Check(!hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text == "+"), "No center crosshair");
                progress.Data = new LaserSave();
                var go = new GameObject("Verification voxel body");
                test = go.AddComponent<LaserVoxelBody>();
                test.dimensions = new Vector3Int(6, 8, 6);
                test.cellSize = .5f;
                test.style = 3;
                test.hitPoints = 2;
                test.requiresGroundSupport = false;
                test.transform.position = new Vector3(1.5f, 0, 45);
                var material = LaserVoxelBody.Active.First(b => b != test
                    && !b.isFragment).GetComponent<MeshRenderer>().sharedMaterials;
                test.GetComponent<MeshRenderer>().sharedMaterials = material;
                test.Generate();
                hero.Teleport(Vector3.up);
                int before = test.Remaining;
                Check(test.Carve(new Vector3(3, 1.5f, 45), 2, 999, Vector3.forward) == 0
                    && test.Remaining == before, "Lobby cannot damage arena objects");
                arena.Enter();
                hero.Teleport(new Vector3(0, 1, 40));
                hero.view.transform.SetPositionAndRotation(new Vector3(0, 2, 40), Quaternion.identity);
                Vector3 aim = hero.view.WorldToScreenPoint(new Vector3(3, 1.5f, 45));
                eyes.SetAimPoint(aim);
                eyes.TestAim = null;
                int ticks = eyes.DamageTickCount;
                eyes.AutoFire = true;
                int start = Time.frameCount;
                await Task.Delay(1500);
                eyes.AutoFire = false;
                Check(Time.frameCount > start + 3, "Play Mode advances");
                Check(aim.x > Screen.width * .55f && test.Remaining < before
                    && progress.Data.cash > 0, "Off-center click coordinates guide eye lasers and reward destruction");
                Check(Mathf.Approximately(eyes.damageCooldown, .5f) && eyes.DamageTickCount - ticks >= 2
                    && eyes.DamageTickCount - ticks <= 4, "Laser damage follows the starting 0.5 second cooldown");
                foreach (var old in LaserVoxelBody.Active.Where(v => v.isFragment && v.displayName == test.displayName).ToArray())
                    UnityEngine.Object.Destroy(old.gameObject);
                await Task.Delay(50);
                test.ResetBody();
                int count = test.Remaining;
                int initialChips = LaserVoxelBody.ChipsEmitted;
                int cut = test.Carve(new Vector3(3, .2f, 46.5f), 2.25f, 999, Vector3.forward);
                await Task.Delay(80);
                Check(LaserVoxelBody.ChipsEmitted - initialChips <= 5
                    && LaserVoxelBody.ChipsEmitted - initialChips < cut, "Only a limited sample of destroyed voxels becomes flying chips");
                var fragments = LaserVoxelBody.Active.Where(b => b.isFragment
                    && b.displayName == test.displayName).OrderByDescending(b => b.Remaining).ToArray();
                int retained = fragments.Sum(b => b.Remaining);
                Check(cut < count && retained > 0 && cut + retained + test.Remaining == count,
                    "Cut attachment detaches intact voxels without rewarding the whole building");
                Check(fragments.All(b => b.GetComponent<Rigidbody>() != null
                    && b.GetComponents<BoxCollider>().Length > 0 && !b.GetComponent<MeshCollider>().enabled),
                    "Falling pieces use Rigidbody with compound colliders");
                Vector3 fallStart = fragments[0].transform.position;
                hero.ClearShake();
                int collisionCount = LaserImpactFeedback.Instance.CollapseCount;
                float fallTime = Time.time;
                await Task.Delay(1900);
                Check(fragments[0].transform.position.y < fallStart.y - .05f, "Detached structure falls under gravity");
                var impactDebug = fragments[0].GetComponent<LaserFallingImpact>();
                checks.Add("INFO: " + fragments[0].Remaining + " voxels, "
                    + fragments[0].GetComponent<Rigidbody>().mass + " kg; fall " + fallStart + " -> "
                    + fragments[0].transform.position + "; game seconds " + (Time.time - fallTime) + "; collisions "
                    + collisionCount + " -> " + LaserImpactFeedback.Instance.CollapseCount + "; shake "
                    + hero.ShakeAmplitude + "; reports " + impactDebug.ContactReports + "; max speed "
                    + impactDebug.MaximumContactSpeed + "; counter voxels " + impactDebug.voxelCount);
                checks.Add("INFO: qualified=" + impactDebug.QualifiedReports);
                Check(hero.ShakeAmplitude > 0 && LaserImpactFeedback.Instance.CollapseCount
                    > collisionCount, "Rigid fragment collision triggers camera shake");
                var physical = fragments[0].GetComponent<LaserFallingImpact>();
                Check(physical.DestroyedByImpact > 0
                    && physical.DestroyedByImpact <= physical.LifetimeBudget,
                    "Real falling fragment damages terrain within its lifetime budget");
                var piece = fragments[0];
                int pieceCount = piece.Remaining;
                int removed = piece.Carve(piece.transform.TransformPoint((Vector3)piece.dimensions
                    * piece.cellSize * .5f), 20, 999, Vector3.forward);
                Check(removed == pieceCount, "Fallen rigidbody can be destroyed further");
                ground = LaserVoxelBody.Active.First(b => b.terrain && b.transform.position.x == 0 && b.transform.position.z == 38);
                Vector3 soil = new Vector3(4, -.25f, 42);
                Check(ground.ContainsSolid(soil), "Arena has real solid terrain voxels");
                int soilCut = ground.Carve(soil, .45f, 999, Vector3.down);
                Check(soilCut > 0 && !ground.ContainsSolid(soil), "Arena ground can be carved");
                ground.ResetBody();
                var free = LaserTrainingTarget.All.First(t => t.tier == 0);
                hero.Teleport(free.transform.position + Vector3.right * 2);
                double xp = progress.Data.xp, cash = progress.Data.cash;
                await Task.Delay(600);
                Check(eyes.Training == free && progress.Data.xp > xp
                    && progress.Data.cash == cash, "Lobby target auto trains without cash rewards");
                var premium = LaserTrainingTarget.All.First(t => t.tier == 1);
                hero.Teleport(premium.transform.position + Vector3.right * 2);
                await Task.Delay(180);
                Check(eyes.Training == null, "Premium target stays locked");
                Check(LaserImpactFeedback.Instance.hitClip != null
                    && LaserImpactFeedback.Instance.collapseClip != null
                    && LaserImpactFeedback.Instance.hitClip.name.Contains("Reference"),
                    "Reference video impact sounds replace muted break sounds");
                eyes.AutoFire = true;
                hero.MenuOpen = false;
                await Task.Delay(100);
                Check(eyes.LaserLoopPlaying, "Continuous laser buzz plays between damage pulses");
                hero.MenuOpen = true;
                await Task.Delay(100);
                Check(!eyes.LaserLoopPlaying, "Laser buzz stops when firing is blocked");
                hero.MenuOpen = false;
                eyes.AutoFire = false;
                Status = "PASS (" + checks.Count + " checks)";
            }
            catch (Exception e)
            {
                Status = "FAIL: " + e.Message;
                checks.Add(Status);
                Debug.LogException(e);
            }
            finally
            {
                eyes.AutoFire = auto;
                eyes.TestAim = null;
                eyes.touchFire = false;
                hud.CloseMenus();
                progress.Data = JsonUtility.FromJson<LaserSave>(saved);
                progress.SuppressSaving = false;
                hero.Teleport(position);
                hero.transform.rotation = rotation;
                hero.yaw = yaw;
                hero.pitch = pitch;
                hero.enabled = motorEnabled;
                hero.view.transform.SetPositionAndRotation(cameraPos, cameraRot);
                if (test != null)
                {
                    string name = test.displayName;
                    UnityEngine.Object.Destroy(test.gameObject);
                    foreach (var b in LaserVoxelBody.Active.ToArray())
                        if (b.isFragment && b.displayName == name)
                            UnityEngine.Object.Destroy(b.gameObject);
                }

                if (ground != null)
                    ground.ResetBody();
                Directory.CreateDirectory("LaserHeroQA");
                File.WriteAllLines("LaserHeroQA/verification-v2.txt", checks);
                Debug.Log("LASER_HERO_VERIFICATION_V2: " + Status);
            }
        }
    }
}
