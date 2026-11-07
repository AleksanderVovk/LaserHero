using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserCurrencyVerification
    {
        public static string Run()
        {
            var lines = new List<string>();
            void Check(bool ok, string message)
            {
                if (!ok)
                    throw new Exception(message);
                lines.Add("PASS " + message);
            }

            var owner = new GameObject("Inactive currency verification");
            owner.SetActive(false);
            var progress = owner.AddComponent<LaserProgress>();
            progress.SuppressSaving = true;
            try
            {
                var huge = LaserAmount.Parse("1e1000");
                var value = huge + LaserAmount.Parse("1.125");
                Check(value - huge == LaserAmount.Parse("1.125"), "A 1001-digit wallet retains small grants exactly");
                Check(value - 1 - huge == LaserAmount.Parse("0.125"), "Spending on a huge wallet is exact");
                var saved = new LaserSave();
                saved.Cash = value;
                saved.XP = huge;
                saved.Level = BigInteger.Pow(10, 100);
                var restored = JsonUtility.FromJson<LaserSave>(JsonUtility.ToJson(saved));
                Check(restored.Cash == value && restored.XP == huge
                    && restored.Level == saved.Level, "Money, energy and large levels survive JSON round-trip");
                var load = typeof(LaserProgress).GetMethod("Load",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                progress.Data = new LaserSave();
                load.Invoke(progress, new object[] { "{\"version\":2,\"level\":200,\"cash\":1234567890123.25,\"pending\":5.5,\"xp\":5800000000}" });
                Check(progress.Data.Cash == LaserAmount.Parse("1234567890128.75")
                    && progress.Data.Level > 200, "Version 2 migrates wallet, pending income and energy beyond level 200");
                var data = new LaserSave();
                var total = LaserLevelCurve.Required(1) + LaserLevelCurve.Required(2) + 12.5;
                LaserLevelCurve.AddXP(data, total);
                Check(data.Level == 3 && data.XP == 12.5,
                    "Multiple levels consume their own cost and preserve the exact remainder");
                var hugeProgress = new LaserSave();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                LaserLevelCurve.AddXP(hugeProgress, huge);
                Check(hugeProgress.Level > int.MaxValue && hugeProgress.XP >= 0
                    && hugeProgress.XP < LaserLevelCurve.Required(hugeProgress.Level),
                    "An enormous energy grant calculates a valid level without int overflow");
                Check(LaserLevelCurve.Cumulative(hugeProgress.Level) + hugeProgress.XP.Units == huge.Units,
                    "No energy is lost when crossing enormous numbers of levels");
                lines.Add("Large energy calculation: " + watch.ElapsedMilliseconds + " ms");
                var catalog = UnityEngine.Object.FindFirstObjectByType<LaserIAPStore>().catalog;
                Check(catalog.IsValid(out _) && catalog.items.Count(i =>
                    i.IsConsumable) == 8, "Eight valid, unique currency products");
                foreach (var item in catalog.items.Where(i => i.IsConsumable))
                {
                    var wallet = new LaserSave();
                    string tx = "test:" + item.productId;
                    var amount = LaserAmount.Parse(item.grantAmount);
                    LaserIAPStore.GrantTransaction(wallet, item, tx, 1, false);
                    var received = item.kind == LaserStoreKind.Money ? wallet.Cash
                        : LaserAmount.FromUnits(LaserLevelCurve.Cumulative(wallet.Level) + wallet.XP.Units);
                    Check(received == amount, "Correct reward: " + item.productId);
                    string once = JsonUtility.ToJson(wallet);
                    Check(!LaserIAPStore.GrantTransaction(wallet, item, tx, 1, false)
                        && once == JsonUtility.ToJson(wallet), "Repeated transaction grants nothing: " + item.productId);
                    wallet = JsonUtility.FromJson<LaserSave>(once);
                    Check(!LaserIAPStore.GrantTransaction(wallet, item, tx, 1, false),
                        "Journal prevents replay after restart: " + item.productId);
                    LaserIAPStore.GrantTransaction(wallet, item, tx + ":new", 2, false);
                    var repeated = item.kind == LaserStoreKind.Money ? wallet.Cash
                        : LaserAmount.FromUnits(LaserLevelCurve.Cumulative(wallet.Level) + wallet.XP.Units);
                    Check(repeated == amount * 3, "New purchases and quantity are granted again: " + item.productId);
                }

                progress.Data = hugeProgress;
                Check(!float.IsNaN(progress.Damage) && !float.IsInfinity(progress.Damage)
                    && !float.IsNaN(progress.Radius) && !float.IsInfinity(progress.Radius),
                    "Gameplay damage and radius remain finite at huge levels");
                Check(!LaserProgress.Format(huge).Contains("Infinity")
                    && LaserProgress.Format(huge).Length < 20, "Huge balances have compact finite HUD labels");
                return "PASS (" + lines.Count + " checks)";
            }
            catch (Exception e)
            {
                lines.Add("FAIL " + e);
                throw;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                Directory.CreateDirectory("LaserHeroQA");
                File.WriteAllLines("LaserHeroQA/currency-verification.txt", lines);
            }
        }
    }
}
