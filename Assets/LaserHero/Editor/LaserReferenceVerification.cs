using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserReferenceVerification
    {
        public static string Status = "Not run";
        [UnityEditor.MenuItem("Laser Hero/Verify observed systems (Play Mode)")]
        public static async void Run()
        {
            if (!Application.isPlaying)
            {
                Status = "Enter Play Mode first";
                return;
            }

            var p = UnityEngine.Object.FindFirstObjectByType<LaserProgress>();
            var hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            var eyes = hud.eyes;
            string saved = JsonUtility.ToJson(p.Data);
            bool suppress = p.SuppressSaving;
            var lines = new List<string>();
            Status = "Running";
            void Check(bool ok, string label)
            {
                if (!ok)
                    throw new Exception(label);
                lines.Add("PASS: " + label);
            }

            try
            {
                p.SuppressSaving = true;
                p.Data = new LaserSave();
                Check(!p.BuyAura(0) && !p.BuyUpgrade(0)
                    && !p.Rebirth(), "Unaffordable purchases and early rebirth leave progress unchanged");
                p.Data.cash = 15000;
                float energy = p.TrainingGain, damage = p.Damage;
                Check(p.BuyAura(0) && p.Data.cash == 0 && p.TrainingGain == energy * 1.5f
                    && p.Damage == damage, "Aura purchase charges once and multiplies earned XP");
                Check(p.BuyAura(0) && p.Data.aura == -1
                    && p.TrainingGain == energy, "Owned aura can be unequipped without a charge");
                Check(p.BuyAura(0) && p.Data.cash == 0, "Owned aura equips without repurchase");
                p.Data.cash = 14000;
                Check(p.BuyUpgrade(0) && p.Data.cash == 9000, "Money upgrade uses observed first price");
                p.Reward(8);
                Check(p.Data.cash == 9027 && p.Data.pending == 0, "Money multiplier affects immediate wallet earnings");
                Check(p.BuyUpgrade(1) && p.Data.cash == 0
                    && Mathf.Abs(eyes.EffectiveCooldown - .456f) < .00001f,
                    "Speed upgrade changes the actual damage interval to 0.456 seconds");
                p.Data.level = 8;
                p.Data.xp = 12;
                int owned = p.Data.ownedLasers;
                Check(p.Rebirth() && p.Data.rebirths == 1 && p.Data.level == 1
                    && p.Data.xp == 0, "Rebirth resets level and XP only when eligible");
                Check(p.Data.ownedLasers == owned && p.OwnsAura(0) && p.Data.moneyUpgrade == 1
                    && p.Data.speedUpgrade == 1 && p.TrainingGain == 3 && p.MoneyMultiplier == 2.25f,
                    "Rebirth preserves purchases and grants persistent multipliers");
                var copy = JsonUtility.FromJson<LaserSave>(JsonUtility.ToJson(p.Data));
                Check(copy.rebirths == 1 && copy.aura == 0 && copy.speedUpgrade == 1
                    && copy.ownedAuras == 1, "New progression fields survive save round trip");
                p.Data = new LaserSave
                {
                    savedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 86400
                };
                var offline = typeof(LaserProgress).GetMethod("ApplyOfflineIncome", BindingFlags.Instance | BindingFlags.NonPublic);
                offline.Invoke(p, null);
                Check(p.Data.cash == 720, "Offline earnings stop at the initial four hour cap");
                offline.Invoke(p, null);
                Check(p.Data.cash == 720, "Offline earnings cannot be claimed twice from the same loaded session");
                typeof(LaserProgress).GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(p, new object[] { "{\"version\":1,\"level\":5,\"equipped\":3,\"ownedLasers\":25,\"cash\":123,\"xp\":10}" });
                Check(p.Data.version == 2 && p.Data.equipped == 4 && p.OwnsLaser(4) && p.OwnsLaser(5)
                    && p.Data.cash == 123 && p.Data.level == 5 && p.Data.aura == -1,
                    "Version-one save migrates green and plasma purchases without losing cash or levels");
                hud.ToggleShop();
                await Task.Delay(180);
                var scroll = hud.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().Single();
                Canvas.ForceUpdateCanvases();
                Check(scroll.content.rect.height > scroll.viewport.rect.height * 2
                    && scroll.content.childCount == 6, "Laser catalogue clips six rows inside a scrollable viewport");
                scroll.verticalNormalizedPosition = 0;
                Canvas.ForceUpdateCanvases();
                Check(scroll.verticalNormalizedPosition < .01f, "Last laser row is reachable by scrolling");
                Check(hud.hero.MenuOpen && !eyes.LaserLoopPlaying, "Modal menu blocks gameplay and continuous laser audio");
                hud.CloseMenus();
                hud.ToggleAuras();
                await Task.Delay(60);
                Check(hud.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().Single()
                    .content.childCount == 5, "Aura window contains five functional catalogue rows");
                hud.CloseMenus();
                hud.ToggleUpgrade();
                await Task.Delay(60);
                Check(hud.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().Single()
                    .content.childCount == 3, "Upgrade window contains money, laser speed and offline rows");
                hud.CloseMenus();
                Check(hud.extraIcons.Length == 6 && hud.extraIcons.All(i =>
                    i != null), "Six generated simulator icons are assigned");
                Check(UnityEngine.Object.FindObjectsByType<LaserLobbyKiosk>(FindObjectsSortMode.None)
                    .Any(k => k.menu == 2) && UnityEngine.Object.FindObjectsByType<LaserLobbyKiosk>(FindObjectsSortMode.None).Any(k => k.menu == 3), "Aura and rebirth kiosks exist in the protected lobby");
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
                hud.CloseMenus();
                p.Data = JsonUtility.FromJson<LaserSave>(saved);
                p.SuppressSaving = suppress;
                Directory.CreateDirectory("LaserHeroQA");
                File.WriteAllLines("LaserHeroQA/reference-verification.txt", lines);
                Debug.Log("REFERENCE_VERIFICATION: " + Status);
            }
        }
    }
}
