using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserArenaObservationVerification
    {
        public static string Status = "Not run";
        public static async void Run()
        {
            if (!Application.isPlaying)
                return;
            var hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            var hero = hud.hero;
            var progress = hud.progress;
            var abilities = hero.GetComponent<LaserArenaAbilities>();
            string saved = JsonUtility.ToJson(progress.Data);
            bool suppress = progress.SuppressSaving;
            Vector3 position = hero.transform.position;
            float yaw = hero.yaw, pitch = hero.pitch;
            var checks = new List<string>();
            var targets = new List<GameObject>();
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
                hud.CloseMenus();
                LaserArena.Instance.Lobby();
                Check(!hero.ToggleFlight() && !abilities.TryExplode(Vector3.zero),
                    "Flight and explosion are unavailable in the protected lobby");
                hero.Teleport(new Vector3(12, 6, 41));
                hero.yaw = 0;
                hero.pitch = -15;
                Check(hero.ToggleFlight() && hero.Flying, "Flight toggles on inside the arena");
                Vector3 start = hero.transform.position;
                hero.touchMove = Vector2.up;
                int frame = Time.frameCount;
                await Task.Delay(260);
                hero.touchMove = Vector2.zero;
                Check(Time.frameCount > frame + 3 && hero.transform.position.z > start.z + .3f
                    && hero.transform.position.y > start.y, "Flight movement follows the camera and counters gravity");
                hero.Teleport(new Vector3(14, 6, 41));
                Check(!hero.Flying, "Teleport cancels flight cleanly");
                hero.enabled = false;
                var materials = LaserVoxelBody.Active.First(b => b.terrain).GetComponent<MeshRenderer>().sharedMaterials;
                for (int i = 0; i < 2; i++)
                {
                    var go = new GameObject("Observation verification body");
                    targets.Add(go);
                    go.transform.position = new Vector3(15 + i * .2f, 6, 42);
                    var body = go.AddComponent<LaserVoxelBody>();
                    body.dimensions = new Vector3Int(4, 4, 4);
                    body.style = 3;
                    body.hitPoints = 2;
                    body.requiresGroundSupport = false;
                    body.displayName = "Observation verification";
                    body.GetComponent<MeshRenderer>().sharedMaterials = materials;
                    body.Generate();
                }

                await Task.Delay(80);
                hud.ToggleShop();
                Check(!abilities.TryExplode(new Vector3(16, 7, 43)), "Menu blocks explosion activation");
                hud.CloseMenus();
                Check(!abilities.TryExplode(new Vector3(0, 1, 0)), "Explosion cannot target the lobby from the arena");
                double cash = progress.Data.cash;
                Check(abilities.TryExplode(new Vector3(16, 7, 43)) && abilities.LastDestroyed > 0
                    && abilities.LastDestroyed <= 48, "Explosion damages real voxel bodies with a global 48-cell budget");
                int remaining = targets.Sum(g => g.GetComponent<LaserVoxelBody>().Remaining);
                Check(!abilities.TryExplode(new Vector3(16, 7, 43)) && targets.Sum(g =>
                    g.GetComponent<LaserVoxelBody>().Remaining) == remaining, "Cooldown prevents repeat damage");
                Check(progress.Data.cash > cash && progress.Data.pending == 0
                    && abilities.RemainingCooldown > 7, "Explosion credits the wallet and starts the eight-second cooldown");
                await Task.Delay(80);
                Check(!hud.transform.Find("Level bar").gameObject.activeSelf
                    && hud.transform.Find("Collect") == null, "Arena HUD has no manual collect button");
                Check(hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t =>
                    t.text.StartsWith("+")), "Damage produces visible floating reward text");
                Check(LaserImpactFeedback.Instance.GetComponentsInChildren<ParticleSystem>().Length == 9,
                    "Impact fire and collapse dust use bounded particle pools");
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
                foreach (var go in targets)
                    if (go != null)
                        UnityEngine.Object.Destroy(go);
                foreach (var body in LaserVoxelBody.Active.Where(b =>
                    b.displayName == "Observation verification" && b.isFragment).ToArray())
                    UnityEngine.Object.Destroy(body.gameObject);
                hud.CloseMenus();
                hero.touchMove = Vector2.zero;
                hero.enabled = true;
                hero.Teleport(position);
                hero.yaw = yaw;
                hero.pitch = pitch;
                progress.Data = JsonUtility.FromJson<LaserSave>(saved);
                progress.SuppressSaving = suppress;
                Directory.CreateDirectory("LaserHeroQA");
                File.WriteAllLines("LaserHeroQA/arena-observation-verification.txt", checks);
                Debug.Log("ARENA_OBSERVATION_VERIFICATION: " + Status);
            }
        }
    }
}
