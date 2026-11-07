using System;
using UnityEngine;
using YG;

namespace LaserHero
{
    [Serializable]
    public class LaserSave
    {
        public int version = 3, level = 1, equipped, ownedLasers = 1, ownedTargets;
        public int ownedAuras, aura = -1, rebirths, moneyUpgrade, speedUpgrade, offlineUpgrade;
        public int ownedDrones;
        public int ownedBoosts;
        public int drone = -1;
        public long savedAt;
        public double cash, pending, xp;
        public string cashExact, pendingExact, xpExact, levelExact;
        [NonSerialized]
        string cachedCash, cachedPending, cachedXP, cachedLevel;
        [NonSerialized]
        LaserAmount cashValue, pendingValue, xpValue;
        [NonSerialized]
        System.Numerics.BigInteger levelValue;
        static LaserAmount Read(ref string exact, ref string cache, ref LaserAmount value, double legacy)
        {
            LaserAmount Legacy() => double.IsNaN(legacy) || double.IsInfinity(legacy) ? 0 : Math.Max(0, legacy);
            if (string.IsNullOrWhiteSpace(exact))
                exact = Legacy().ToString();
            if (cache != exact)
            {
                try
                {
                    value = LaserAmount.Parse(exact);
                }
                catch (FormatException)
                {
                    value = Legacy();
                    exact = value.ToString();
                }
                catch (OverflowException)
                {
                    value = Legacy();
                    exact = value.ToString();
                }

                cache = exact;
            }

            return value;
        }

        public LaserAmount Cash
        {
            get => Read(ref cashExact, ref cachedCash, ref cashValue, cash);
            set
            {
                cashExact = value.ToString();
                cachedCash = cashExact;
                cashValue = value;
                cash = 0;
            }
        }

        public LaserAmount Pending
        {
            get => Read(ref pendingExact, ref cachedPending, ref pendingValue, pending);
            set
            {
                pendingExact = value.ToString();
                cachedPending = pendingExact;
                pendingValue = value;
                pending = 0;
            }
        }

        public LaserAmount XP
        {
            get => Read(ref xpExact, ref cachedXP, ref xpValue, xp);
            set
            {
                xpExact = value.ToString();
                cachedXP = xpExact;
                xpValue = value;
                xp = 0;
            }
        }

        public System.Numerics.BigInteger Level
        {
            get
            {
                if (string.IsNullOrWhiteSpace(levelExact))
                    levelExact = Math.Max(1, level).ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (cachedLevel != levelExact)
                {
                    if (!System.Numerics.BigInteger.TryParse(levelExact,
                        System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture,
                        out levelValue) || levelValue < 1)
                    {
                        levelValue = Math.Max(1, level);
                        levelExact = levelValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }

                    cachedLevel = levelExact;
                }

                return levelValue;
            }

            set
            {
                levelValue = System.Numerics.BigInteger.Max(1, value);
                levelExact = levelValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                cachedLevel = levelExact;
                level = levelValue > int.MaxValue ? int.MaxValue : (int)levelValue;
            }
        }

        public System.Collections.Generic.List<string> iapTransactions = new System.Collections.Generic.List<string>();
    }

    public class LaserProgress : MonoBehaviour
    {
        // Append new entries to preserve the saved ownership bits and equipped indices.
        public static readonly string[] Names =
        {
            "Огонь",
            "Жёлтый огонь",
            "Синий огонь",
            "Тёмно-синий огонь",
            "Зелёный огонь",
            "Розовый огонь",
            "Белый огонь",
            "Оранжевый огонь",
            "Золотистый огонь",
            "Голубой огонь",
            "Индиговый огонь",
            "Изумрудный огонь",
            "Малиновый огонь",
            "Серебристый огонь"
        };
        public static readonly Color[] Colors =
        {
            new Color(1, .18f, .04f),
            new Color(1, .8f, .05f),
            new Color(.1f, .75f, 1),
            new Color(.2f, .2f, 1),
            new Color(.15f, 1, .4f),
            new Color(1, .15f, .7f),
            new Color(.94f, .97f, 1),
            new Color(1, .42f, .04f),
            new Color(1, .73f, .12f),
            new Color(.08f, .85f, 1),
            new Color(.29f, .16f, .95f),
            new Color(.05f, .9f, .36f),
            new Color(1, .04f, .48f),
            new Color(.73f, .83f, .95f)
        };
        public static readonly double[] Prices =
        {
            0,
            600,
            12000,
            75000,
            250000,
            750000,
            6600000,
            33000000,
            330000000,
            2600000000d,
            4100000000d,
            7700000000d,
            18200000000d,
            40600000000d
        };
        public static readonly float[] BaseDamage =
        {
            12,
            22,
            40,
            60,
            75,
            140,
            700,
            3500,
            17000,
            80000,
            380000,
            2160000,
            10200000,
            48000000
        };
        public static readonly float[] EnergyGain =
        {
            1,
            3,
            12,
            55,
            260,
            1250,
            7000,
            35000,
            170000,
            800000,
            3800000,
            21600000,
            102000000,
            480000000
        };
        public static readonly string[] AuraNames =
        {
            "Аура пламени",
            "Синяя аура",
            "Зелёная аура",
            "Розовая аура",
            "Аура пустоты"
        };
        public static readonly double[] AuraPrices =
        {
            15000,
            2000000,
            400000000,
            7000000000,
            52500000000
        };
        public static readonly float[] AuraBoosts =
        {
            1.5f,
            2,
            3,
            4,
            5
        };
        public LaserSave Data = new LaserSave();
        [Tooltip("Non-consumable Yandex product IDs for targets x2, x4, x8. Leave blank until configured.")]
        public string[] premiumProductIds =
        {
            "",
            "",
            ""
        };
        public event Action Changed;
        public event Action<string> Message;
        public event Action<string, NotificationType> Notification;
        public event Action<LaserAmount> EnergyGained;
        public event Action<LaserAmount> MoneyEarned;
        public event Action PurchaseSucceeded, PurchaseRejected, LevelRaised;
        public void ReportPurchaseSucceeded() => PurchaseSucceeded?.Invoke();
        public void ReportPurchaseRejected() => PurchaseRejected?.Invoke();
        public void ReportLevelRaised() => LevelRaised?.Invoke();
        void RejectPurchase(string message)
        {
            ReportPurchaseRejected();
            Notify(message, NotificationType.Error);
        }

        public float Damage => (float)Math.Min(1e12, BaseDamage[Data.equipped] * (1 + .32 * Math.Min(1e12,
            (double)Data.Level - 1)) * PowerMultiplier);
        public float PowerMultiplier => Mathf.Pow(2, Data.rebirths);
        public float AuraXPMultiplier => Data.aura >= 0 ? AuraBoosts[Data.aura] : 1;
        public float TrainingGain => EnergyGain[Data.equipped] * PowerMultiplier * AuraXPMultiplier;

        public const float MoneyUpgradeBonusPerLevel = .45f;
        public static float MoneyUpgradeMultiplier(int level) => 1 + MoneyUpgradeBonusPerLevel * Mathf.Clamp(level, 0, 20);
        public float MoneyMultiplier => (1 + Data.rebirths) * MoneyUpgradeMultiplier(Data.moneyUpgrade);
        public float LaserSizeMultiplier => OwnsBoost(0) ? 2f : 1f;
        public float HitSpeedMultiplier => OwnsBoost(1) ? 2f : 1f;
        public float CooldownMultiplier => Mathf.Pow(.912f, Data.speedUpgrade) / HitSpeedMultiplier;
        // Each next requirement grows by 7 more levels: 8, 16, 31, 53, 82, ...
        public int RebirthLevel => 8 + 8 * Data.rebirths + 7 * Data.rebirths * (Data.rebirths - 1) / 2;
        public bool CanRebirth => Data.rebirths < 20 && Data.Level >= RebirthLevel;
        public float RebirthPower => Mathf.Pow(2, Data.rebirths);
        public LaserAmount OfflineEarned { get; private set; }
        public float BaseRadius => (float)Math.Min(1.8, .32 + .035 * Math.Min(100, (double)Data.Level - 1));
        public float Radius => BaseRadius * LaserSizeMultiplier;

        System.Numerics.BigInteger requiredAtLevel;
        LaserAmount requiredValue;
        public LaserAmount RequiredXP
        {
            get
            {
                if (requiredAtLevel != Data.Level)
                {
                    requiredAtLevel = Data.Level;
                    requiredValue = LaserLevelCurve.Required(requiredAtLevel);
                }

                return requiredValue;
            }
        }

        public Color BeamColor => Colors[Data.equipped];

        [NonSerialized]
        public bool SuppressSaving;
#if UNITY_EDITOR
        [NonSerialized]
        public Action<LaserSave> EditorSaveOverride;
#endif
        const string Key = "LaserHero.Save.v1";
        const string ResetCloudKey = "LaserHero.ResetPendingCloud";
        bool dirty;
        float nextSave;
        void Awake()
        {
            // Runtime progress comes from the save, never from stale Inspector-serialized caches.
            Data = new LaserSave();
            Load(PlayerPrefs.GetString(Key, ""));
            ApplyOfflineIncome();
            YG2.onGetSDKData += LoadCloud;
            YG2.onPurchaseSuccess += PurchaseSuccess;
            YG2.onPurchaseFailed += PurchaseFailed;
            if (YG2.isSDKEnabled)
                LoadCloud();
        }

        void Load(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;
            try
            {
                var d = JsonUtility.FromJson<LaserSave>(json);
                if (d != null && (d.version == 1 || d.version == 2 || d.version == 3))
                    Data = d;
            }
            catch (Exception)
            {
                Debug.LogWarning("Laser Hero: save could not be read; retaining current progress.");
            }

            if (Data.version == 1)
            {
                // Preserve purchased colours when inserting dark blue into the catalogue.
                int[] map =
                {
                    0,
                    1,
                    2,
                    4,
                    5
                };
                int owned = 0;
                for (int i = 0; i < map.Length; i++)
                    if ((Data.ownedLasers & (1 << i)) != 0)
                        owned |= 1 << map[i];
                Data.equipped = map[Mathf.Clamp(Data.equipped, 0, 4)];
                Data.ownedLasers = owned;
                Data.aura = -1;
                Data.version = 2;
            }

            Data.Level = System.Numerics.BigInteger.Max(1, Data.Level);
            Data.equipped = Mathf.Clamp(Data.equipped, 0, Names.Length - 1);
            Data.rebirths = Mathf.Clamp(Data.rebirths, 0, 20);
            Data.moneyUpgrade = Mathf.Clamp(Data.moneyUpgrade, 0, 20);
            Data.speedUpgrade = Mathf.Clamp(Data.speedUpgrade, 0, 20);
            Data.offlineUpgrade = Mathf.Clamp(Data.offlineUpgrade, 0, 20);
            Data.aura = Mathf.Clamp(Data.aura, -1, AuraNames.Length - 1);
            if (Data.aura >= 0 && !OwnsAura(Data.aura))
                Data.aura = -1;
            Data.drone = Mathf.Clamp(Data.drone, -1, 1);
            if (Data.drone >= 0 && !OwnsDrone(Data.drone))
                Data.drone = -1;
            Data.ownedLasers |= 1;
            if (!OwnsLaser(Data.equipped))
                Data.equipped = 0;
            if (Data.Cash < 0)
                Data.Cash = 0;
            if (Data.Pending < 0)
                Data.Pending = 0;
            if (Data.XP < 0)
                Data.XP = 0;
            // Move uncollected earnings from older saves into the wallet once.
            Data.Cash += Data.Pending;
            Data.Pending = 0;
            LaserLevelCurve.AddXP(Data, 0);
            Data.version = 3;
            if (Data.iapTransactions == null)
                Data.iapTransactions = new System.Collections.Generic.List<string>();
        }

        static double Safe(double n) => double.IsNaN(n) || double.IsInfinity(n) ? 0 : Math.Max(0, n);
        void LoadCloud()
        {
            if (YG2.saves == null)
                return;
            // A reset performed before SDK initialization must replace the old cloud save.
            if (PlayerPrefs.GetInt(ResetCloudKey, 0) == 1)
            {
                Save();
                return;
            }

            // A valid cloud save is authoritative once the SDK has loaded.
            Load(YG2.saves.laserHeroJson);
            ApplyOfflineIncome();
            Touch();
        }

        void ApplyOfflineIncome()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Data.savedAt > 0)
            {
                double seconds = Math.Max(0, Math.Min(now - Data.savedAt, (4 + Data.offlineUpgrade * .25) * 3600));
                if (seconds >= 60)
                {
                    OfflineEarned = LaserAmount.FromInteger(Data.Level) * (Math.Floor(seconds / 60) * 3 * MoneyMultiplier);
                    Data.Cash += OfflineEarned;
                }
            }

            Data.savedAt = now;
            dirty = true;
        }

        public bool OwnsLaser(int i) => (Data.ownedLasers & (1 << i)) != 0;
        public bool OwnsTarget(int i) => i >= 0 && i < 32 && (Data.ownedTargets & (1 << i)) != 0;
        public bool OwnsAura(int i) => i >= 0 && (Data.ownedAuras & (1 << i)) != 0;
        public bool OwnsDrone(int i) => i >= 0 && i < 2 && (Data.ownedDrones & (1 << i)) != 0;
        public bool OwnsBoost(int i) => i >= 0 && i < 2 && (Data.ownedBoosts & (1 << i)) != 0;
        public void EquipDrone(int i)
        {
            if (!OwnsDrone(i))
            {
                Notify("Дрон ещё не куплен");
                return;
            }

            Data.drone = Data.drone == i ? -1 : i;
            Touch();
            Save();
        }

        public void GainXP(LaserAmount amount)
        {
            if (amount > 0)
                EnergyGained?.Invoke(amount);
            var previous = Data.Level;
            LaserLevelCurve.AddXP(Data, amount < 0 ? 0 : amount);
            if (Data.Level > previous)
            {
                ReportLevelRaised();
                Notify("УРОВЕНЬ " + Format(LaserAmount.FromInteger(Data.Level))
                    + "! Урон и ширина увеличены", NotificationType.Success);
            }

            Touch();
        }

        public void GrantMoney(LaserAmount value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Data.Cash += value;
            if (value > 0)
                MoneyEarned?.Invoke(value);
            Touch();
        }

        public void Reward(int blocks)
        {
            GrantMoney((double)Math.Max(0, blocks) * 3 * MoneyMultiplier);
        }

        public void Collect()
        {
            // Compatibility for old callers; gameplay now credits the wallet directly.
            if (Data.Pending <= 0)
                return;
            Data.Cash += Data.Pending;
            Data.Pending = 0;
            Touch();
            Save();
        }

        public LaserAmount PowerUpgradePrice => LaserAmount.FromInteger(100 + Data.Level * 75);

        public bool BuyPower()
        {
            if (Data.Cash < PowerUpgradePrice)
            {
                RejectPurchase("Недостаточно средств!");
                return false;
            }

            Data.Cash -= PowerUpgradePrice;
            GainXP(RequiredXP - Data.XP);
            Save();
            ReportPurchaseSucceeded();
            return true;
        }

        public bool BuyLaser(int i)
        {
            if (i < 0 || i >= Names.Length)
                return false;
            if (!OwnsLaser(i))
            {
                if (Data.Cash < Prices[i])
                {
                    RejectPurchase("Недостаточно средств!");
                    return false;
                }

                Data.Cash -= Prices[i];
                Data.ownedLasers |= 1 << i;
                ReportPurchaseSucceeded();
            }

            Data.equipped = i;
            Touch();
            Save();
            Notify("Выбран лазер: " + Names[i], NotificationType.Success);
            return true;
        }

        public bool BuyAura(int i)
        {
            if (i < 0 || i >= AuraNames.Length)
                return false;
            if (!OwnsAura(i))
            {
                if (Data.Cash < AuraPrices[i])
                {
                    RejectPurchase("Недостаточно средств!");
                    return false;
                }

                Data.Cash -= AuraPrices[i];
                Data.ownedAuras |= 1 << i;
                ReportPurchaseSucceeded();
            }

            Data.aura = Data.aura == i ? -1 : i;
            Touch();
            Save();
            return true;
        }

        public int UpgradeLevel(int kind) => kind == 0 ? Data.moneyUpgrade : kind == 1 ? Data.speedUpgrade : Data.offlineUpgrade;
        public double UpgradePrice(int kind) => (kind == 0 ? 5000 : kind == 1 ? 10800 : 8000) * Math.Pow(1.65, UpgradeLevel(kind));
        public bool BuyUpgrade(int kind)
        {
            if (kind < 0 || kind > 2 || UpgradeLevel(kind) >= 20)
                return false;
            double price = UpgradePrice(kind);
            if (Data.Cash < price)
            {
                RejectPurchase("Недостаточно средств!");
                return false;
            }

            Data.Cash -= price;
            if (kind == 0)
                Data.moneyUpgrade++;
            else if (kind == 1)
                Data.speedUpgrade++;
            else
                Data.offlineUpgrade++;
            Touch();
            Save();
            ReportPurchaseSucceeded();
            Notify("Улучшение куплено!", NotificationType.Success);
            return true;
        }

        public bool Rebirth()
        {
            if (!CanRebirth)
            {
                Notify("Для перерождения нужен уровень " + RebirthLevel);
                return false;
            }

            Data.rebirths++;
            Data.Level = 1;
            Data.XP = 0;
            Touch();
            Save();
            Notify("Перерождение! Постоянные бонусы увеличены", NotificationType.Success);
            return true;
        }

        string pendingProduct;
        float purchaseStarted;
        public void BuyTarget(int i)
        {
            var lane = LaserTrainingTarget.All.Find(t => t != null && t.tier == i && t.gameObject.activeInHierarchy);
            if (lane != null)
            {
                lane.TryUnlock();
                return;
            }

            if (i <= 0 || i > 3 || OwnsTarget(i) || pendingProduct != null)
                return;
            string id = premiumProductIds[i - 1];
            if (string.IsNullOrWhiteSpace(id))
            {
                RejectPurchase("Эта мишень ещё не доступна для покупки");
                return;
            }

            if (!YG2.isSDKEnabled)
            {
                RejectPurchase("Покупки доступны на игровой платформе");
                return;
            }

            pendingProduct = id;
            purchaseStarted = Time.unscaledTime;
            YG2.BuyPayments(id);
        }

        void PurchaseSuccess(string id)
        {
            // The SDK also calls this when restoring confirmed purchases.
            for (int i = 0; i < premiumProductIds.Length; i++)
                if (!string.IsNullOrEmpty(premiumProductIds[i]) && id == premiumProductIds[i])
                {
                    Data.ownedTargets |= 1 << (i + 1);
                    Touch();
                    Save();
                    if (pendingProduct == id)
                        ReportPurchaseSucceeded();
                    Notify("Тренировочная мишень открыта!", NotificationType.Success);
                }

            if (pendingProduct == id)
                pendingProduct = null;
        }

        void PurchaseFailed(string id)
        {
            if (id == pendingProduct)
            {
                pendingProduct = null;
                RejectPurchase("Покупка не завершена");
            }
        }

        public void Notify(string text) => Notify(text, NotificationType.Info);
        public void Notify(string text, NotificationType type)
        {
            Message?.Invoke(text);
            Notification?.Invoke(text, type);
        }

        void Touch()
        {
            dirty = true;
            Changed?.Invoke();
        }

        void Update()
        {
            if (dirty && Time.unscaledTime >= nextSave)
                Save();
            if (pendingProduct != null && Time.unscaledTime - purchaseStarted > 60)
                pendingProduct = null;
        }

        public void Save()
        {
            if (SuppressSaving)
                return;
#if UNITY_EDITOR
            if (EditorSaveOverride != null)
            {
                EditorSaveOverride(Data);
                return;
            }

#endif
            Data.savedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Data.Cash = Data.Cash;
            Data.XP = Data.XP;
            Data.Pending = Data.Pending;
            Data.Level = Data.Level;
            Data.version = 3;
            string json = JsonUtility.ToJson(Data);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
            if (YG2.isSDKEnabled && YG2.saves != null)
            {
                YG2.saves.laserHeroJson = json;
                YG2.SaveProgress();
                PlayerPrefs.DeleteKey(ResetCloudKey);
                PlayerPrefs.Save();
            }

            dirty = false;
            nextSave = Time.unscaledTime + 5;
        }

        public void ResetProgress()
        {
            Data = new LaserSave();
            OfflineEarned = 0;
            pendingProduct = null;
            if (!SuppressSaving)
                PlayerPrefs.SetInt(ResetCloudKey, 1);
            Touch();
            Save();
            Notify("Сохранение удалено. Начинаем с нуля!", NotificationType.Success);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                Save();
        }

        void OnDisable()
        {
            if (dirty)
                Save();
        }

        void OnDestroy()
        {
            YG2.onGetSDKData -= LoadCloud;
            YG2.onPurchaseSuccess -= PurchaseSuccess;
            YG2.onPurchaseFailed -= PurchaseFailed;
        }

        public static string Format(LaserAmount v) => v.Short();
    }
}

namespace YG
{
    public partial class SavesYG
    {
        public string laserHeroJson = "";
    }
}
