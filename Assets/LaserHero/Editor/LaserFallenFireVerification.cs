using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserFallenFireVerification
    {
        public static string Status = "Not run";
        public static async void Run()
        {
            if (!Application.isPlaying)
                return;
            var feedback = LaserImpactFeedback.Instance;
            var hero = feedback.hero;
            var eyes = hero.GetComponent<LaserEyes>();
            var progress = eyes.progress;
            var hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            string saved = JsonUtility.ToJson(progress.Data), disk = PlayerPrefs.GetString("LaserHero.Save.v1", "");
            bool suppress = progress.SuppressSaving, motor = hero.enabled, eyesEnabled = eyes.enabled;
            Vector3 position = hero.transform.position;
            float yaw = hero.yaw, pitch = hero.pitch, chance = feedback.laserFireChance;
            var random = UnityEngine.Random.state;
            var checks = new List<string>();
            GameObject target = null;
            Status = "Running";
            void Check(bool ok, string text)
            {
                if (!ok)
                    throw new Exception(text);
                checks.Add("PASS: " + text);
            }

            try
            {
                progress.SuppressSaving = true;
                hero.enabled = false;
                eyes.enabled = false;
                hud.CloseMenus();
                LaserArena.Instance.Enter();
                feedback.ClearFires();
                var pool = feedback.GetComponentsInChildren<LaserFallenFire>(true);
                Check(pool.Length == 12 && pool.All(f =>
                    f.GetComponentsInChildren<ParticleSystem>(true).Length == 4),
                    "Pool contains 12 copies of the four-part FallenFire prefab");
                Check(pool.All(f => f.GetComponentsInChildren<Renderer>(true).All(r =>
                    r.sharedMaterial.shader.name.StartsWith("Universal Render Pipeline/"))),
                    "Every FallenFire renderer uses a URP material");
                Vector3 point = new Vector3(0, 1, 46);
                Check(!feedback.SpawnFire(Vector3.zero, Vector3.up)
                    && feedback.ActiveFireCount == 0, "No physical fire can spawn in the lobby");
                feedback.laserFireChance = 1;
                LaserImpactFeedback.LaserFire(point, Vector3.up, 0);
                Check(feedback.ActiveFireCount == 0, "Laser without destroyed voxels does not drop fire");
                feedback.laserFireChance = 0;
                for (int i = 0; i < 20; i++)
                    LaserImpactFeedback.LaserFire(point, Vector3.up, 1);
                Check(feedback.ActiveFireCount == 0, "Laser chance gate can reject fire drops");
                feedback.laserFireChance = 1;
                LaserImpactFeedback.LaserFire(point, Vector3.up, 1);
                Check(feedback.ActiveFireCount == 1, "Successful laser drop spawns one FallenFire");
                var fire = pool.First(f => f.Active);
                Vector3 start = fire.transform.position;
                Check(fire.Body.useGravity && !fire.Body.isKinematic && fire.Body.linearVelocity.y > 0
                    && new Vector2(fire.Body.linearVelocity.x, fire.Body.linearVelocity.z).magnitude > 2,
                    "Fire launches upwards and sideways as a dynamic Rigidbody");
                Check(fire.Sound.isPlaying && fire.Sound.loop && fire.Sound.spatialBlend == 1
                    && fire.Sound.clip.length > 2, "Downloaded crackling sound loops in 3D while fire is active");
                await Task.Delay(2300);
                Check(fire.LandingCount > 0 && Vector3.Distance(start, fire.transform.position) > .5f,
                    "Fire moves under real physics and collides with arena terrain");
                feedback.ClearFires();
                LaserImpactFeedback.ExplosionFire(point, 0);
                Check(feedback.ActiveFireCount == 0, "Empty explosion does not spawn fire");
                LaserImpactFeedback.ExplosionFire(point, 5);
                Check(feedback.ActiveFireCount == 3, "Explosion with destruction ejects three fires");
                Check(pool.Where(f => f.Active).Select(f =>
                    f.Body.linearVelocity).Distinct().Count() == 3, "Explosion fires launch in different random directions");
                for (int i = 0; i < 30; i++)
                    feedback.SpawnFire(point, Vector3.up);
                Check(feedback.ActiveFireCount == 12
                    && feedback.GetComponentsInChildren<LaserFallenFire>(true).Length == 12,
                    "Repeated drops remain within the fixed pool budget");
                foreach (var f in pool)
                    f.lifetime = .1f;
                await Task.Delay(350);
                Check(feedback.ActiveFireCount == 0 && pool.All(f =>
                    !f.Sound.isPlaying), "Expired fire returns to the pool and stops its audio");
                foreach (var f in pool)
                    f.lifetime = 8;
                Check(feedback.SpawnFire(point, Vector3.up), "Expired objects can be reused without creating new instances");
                feedback.ClearFires();
                target = new GameObject("FallenFire explosion test");
                target.transform.position = new Vector3(-34, 2, 44);
                var body = target.AddComponent<LaserVoxelBody>();
                body.style = 3;
                body.dimensions = new Vector3Int(4, 4, 4);
                body.cellSize = .5f;
                body.hitPoints = 2;
                body.requiresGroundSupport = false;
                body.GetComponent<MeshRenderer>().sharedMaterials = LaserVoxelBody.Active.First(b =>
                    b.terrain).GetComponent<MeshRenderer>().sharedMaterials.Take(3).ToArray();
                body.Generate();
                hero.Teleport(new Vector3(-34, 1, 41));
                Physics.SyncTransforms();
                var ability = hero.GetComponent<LaserArenaAbilities>();
                Check(ability.TryExplode(new Vector3(-33, 2.5f, 45)) && ability.LastDestroyed > 0
                    && feedback.ActiveFireCount == 3, "Actual explosion ability destroys voxels and spawns FallenFire");
                feedback.ClearFires();
                UnityEngine.Object.Destroy(target);
                target = null;
                string beforeMenu = JsonUtility.ToJson(progress.Data);
                var open = hud.transform.Find("Сброс прогресса").GetComponent<UnityEngine.UI.Button>();
                open.onClick.Invoke();
                var panel = hud.transform.Find("Reset progress");
                Check(panel.gameObject.activeSelf && hero.MenuOpen, "Reset button opens a confirmation modal");
                panel.Find("Отмена").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Check(!panel.gameObject.activeSelf && JsonUtility.ToJson(progress.Data) == beforeMenu, "Cancel preserves progress");
                progress.Data.cash = 99999;
                progress.Data.pending = 777;
                progress.Data.level = 20;
                progress.Data.xp = 15;
                progress.Data.ownedLasers = 63;
                progress.Data.equipped = 5;
                progress.Data.ownedAuras = 31;
                progress.Data.aura = 4;
                progress.Data.rebirths = 3;
                progress.Data.moneyUpgrade = 5;
                progress.Data.speedUpgrade = 4;
                progress.Data.offlineUpgrade = 2;
                open.onClick.Invoke();
                panel.Find("Удалить сохранение").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                var d = progress.Data;
                Check(d.level == 1 && d.cash == 0 && d.pending == 0 && d.xp == 0 && d.ownedLasers == 1
                    && d.equipped == 0 && d.aura == -1 && d.ownedAuras == 0 && d.rebirths == 0 && d.moneyUpgrade == 0
                    && d.speedUpgrade == 0 && d.offlineUpgrade == 0, "Confirm resets currencies, level, equipment, auras, rebirths and upgrades");
                Check(!LaserArena.Instance.Inside && !hero.MenuOpen
                    && LaserArena.Instance.RemainingTime > 239, "Fresh start returns to lobby and resets the arena round");
                Check(PlayerPrefs.GetString("LaserHero.Save.v1", "") == disk,
                    "Verification leaves the player's actual saved progress intact");
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
                if (target != null)
                    UnityEngine.Object.Destroy(target);
                feedback.ClearFires();
                foreach (var f in feedback.GetComponentsInChildren<LaserFallenFire>(true))
                    f.lifetime = 8;
                feedback.laserFireChance = chance;
                hud.CloseMenus();
                progress.Data = JsonUtility.FromJson<LaserSave>(saved);
                progress.SuppressSaving = suppress;
                hero.Teleport(position);
                hero.yaw = yaw;
                hero.pitch = pitch;
                hero.enabled = motor;
                eyes.enabled = eyesEnabled;
                UnityEngine.Random.state = random;
                Directory.CreateDirectory("LaserHeroQA");
                File.WriteAllLines("LaserHeroQA/fallen-fire-verification.txt", checks);
                Debug.Log("FALLEN_FIRE_VERIFICATION: " + Status);
            }
        }
    }
}
