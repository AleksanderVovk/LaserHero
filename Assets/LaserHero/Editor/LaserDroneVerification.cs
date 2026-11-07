using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserDroneVerification
    {
        [Serializable]
        class Snapshot
        {
            public LaserSave save;
            public Vector3 position;
            public float yaw, pitch;
        }

        const string SnapshotPath = "LaserHeroQA/drone-save-backup.json";
        const string ReportPath = "LaserHeroQA/drone-verification.txt";
        static LaserDroneCompanion Companion => UnityEngine.Object.FindFirstObjectByType<LaserDroneCompanion>();

        static void Check(bool value, string message)
        {
            if (!value)
                throw new Exception(message);
            File.AppendAllText(ReportPath, "PASS: " + message + "\n");
        }

        public static string Begin()
        {
            Directory.CreateDirectory("LaserHeroQA");
            var c = Companion;
            var p = c.progress;
            File.WriteAllText(SnapshotPath, JsonUtility.ToJson(new Snapshot { save = p.Data, position =
                c.hero.transform.position, yaw = c.hero.yaw, pitch = c.hero.pitch }));
            File.WriteAllText(ReportPath, "");
            Check(c.settings.drones.Length == 2 && c.settings.drones.All(d => d.prefab != null
                && d.icon != null), "Two prefabs and matching UI icons configured");
            Check(c.settings.buzz != null && c.settings.shot != null, "Downloaded buzz and shot clips assigned");
            Check(c.settings.drones[0].prefab.GetComponent<LaserDroneVisual>().rotors.Length == 4
                && c.settings.drones[1].prefab.GetComponent<LaserDroneVisual>().rotors.Length == 6,
                "Four normal rotors and six guarded mega rotors");
            Check(UnityEngine.Object.FindObjectsByType<LaserDroneShopTrigger>(FindObjectsSortMode.None)
                .Length == 2, "Two physical shop triggers in lobby");
            var store = p.GetComponent<LaserIAPStore>();
            Check(store.Ready && store.catalog.IsValid(out _)
                && store.catalog.items.Length == 13, "IAP has 13 unique products including both drones");
            var data = new LaserSave
            {
                cash = 123
            };
            foreach (int i in new[]
            {
                0,
                1
            }

            )
            {
                var item = store.catalog.Find(LaserStoreKind.Drone, i);
                LaserIAPStore.ApplyEntitlement(data, item, true);
                Check((data.ownedDrones & (1 << i)) != 0 && data.drone == i
                    && data.cash == 123, "IAP maps drone " + i + " without spending game money");
                string first = JsonUtility.ToJson(data);
                LaserIAPStore.ApplyEntitlement(data, item, true);
                Check(JsonUtility.ToJson(data) == first, "Drone " + i + " duplicate delivery is idempotent");
            }

            Check(data.ownedDrones == 3 && data.drone == 1, "Owning both still equips exactly one drone");
            Check(JsonUtility.FromJson<LaserSave>(JsonUtility.ToJson(data)).drone == 1,
                "Drone equipment survives save serialization");
            p.Data = new LaserSave();
            c.hero.enabled = false;
            UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>().CloseMenus();
            LaserArena.Instance.Lobby();
            store.Buy(LaserStoreKind.Drone, 0);
            return "Click Fake Store Buy to verify drone purchase";
        }

        public static string Purchased()
        {
            var c = Companion;
            Check(c.progress.OwnsDrone(0) && c.progress.Data.drone == 0, "Fake Store Buy grants and equips normal drone");
            Check(c.progress.Data.iapTransactions.Any(t => t.StartsWith("editor-test:")), "Fake purchase journal saved");
            c.progress.SuppressSaving = true;
            return "Waiting in safe lobby";
        }

        public static string LobbyAndArena()
        {
            var c = Companion;
            Check(c.Equipped == 0 && c.Visual != null, "One normal companion spawned");
            Check(c.ShotsFired == 0 && !c.BeamVisible, "Safe lobby never fires");
            c.progress.Data.ownedDrones = 3;
            c.progress.EquipDrone(1);
            Check(c.progress.Data.drone == 1, "Switching replaces equipped index");
            c.progress.EquipDrone(1);
            Check(c.progress.Data.drone == -1, "Equipped drone can be removed");
            c.progress.EquipDrone(0);
            LaserArena.Instance.Enter();
            c.hero.MenuOpen = false;
            return "Normal drone attacking in arena";
        }

        public static string NormalDone()
        {
            var c = Companion;
            Check(c.ShotsFired > 0, "Normal drone fires at arena voxels");
            Check(c.BlocksDestroyed > 0, "Drone destruction awards wallet money");
            c.progress.EquipDrone(1);
            return "Mega drone attacking in arena";
        }

        public static string MegaDone()
        {
            var c = Companion;
            Check(c.Equipped == 1 && c.Visual != null, "Mega replaces normal visual");
            Check(UnityEngine.Object.FindObjectsByType<LaserDroneVisual>(FindObjectsSortMode.None).Count(v =>
                v.name.Contains("companion")) == 1, "Only one flying companion exists");
            c.hero.MenuOpen = true;
            return "Menu pauses attacks";
        }

        public static string End()
        {
            var c = Companion;
            Check(!c.BeamVisible, "Menu hides drone laser");
            var backup = JsonUtility.FromJson<Snapshot>(File.ReadAllText(SnapshotPath));
            c.progress.Data = backup.save;
            c.progress.SuppressSaving = false;
            c.progress.Save();
            if (Application.isPlaying)
            {
                c.hero.Teleport(backup.position);
                c.hero.yaw = backup.yaw;
                c.hero.pitch = backup.pitch;
                c.hero.enabled = true;
                UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>().CloseMenus();
            }

            return "PASS: original save and player restored; " + File.ReadAllLines(ReportPath).Length + " drone checks";
        }

        public static string Recover()
        {
            var c = Companion;
            var backup = JsonUtility.FromJson<Snapshot>(File.ReadAllText(SnapshotPath));
            c.progress.Data = backup.save;
            c.progress.SuppressSaving = false;
            c.progress.Save();
            if (Application.isPlaying)
                c.hero.Teleport(backup.position);
            c.hero.enabled = true;
            return "Original save restored: cash=" + backup.save.cash + ", drones=" + backup.save.ownedDrones;
        }
    }
}
