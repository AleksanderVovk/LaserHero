using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LaserHero
{
    public class LaserHeroHUD : MonoBehaviour
    {
        public LaserHeroPlayer hero;
        public LaserEyes eyes;
        public LaserProgress progress;
        public TMP_FontAsset font;
        public Material outlinedText;
        public Material menuBlurMaterial;
        public Sprite[] icons = new Sprite[6];
        public Sprite[] extraIcons = new Sprite[6];
        [Tooltip("Neutral grayscale aura sprite; each shop card applies its own color.")]
        public Sprite auraTintIcon;
        [Tooltip("Neutral flame sprite tinted with each laser's color in the shop.")]
        public Sprite laserTintIcon;
        public Sprite orangePanel, greenPanel, purplePanel, bluePanel;
        public Sprite iconPlate;
        public GameObject settingsPanel;
        public LaserMapVoteUI mapVotingUI;
        [SerializeField, HideInInspector]
        TMP_Text money, level, power, toast, rebirthText, rebirthCount;
        [SerializeField, HideInInspector]
        UnityEngine.UI.Image xpFill;
        [SerializeField, HideInInspector]
        TMP_Text xpNumbers;
        [SerializeField, HideInInspector]
        UnityEngine.UI.RawImage blurredWorld;
        RenderTexture menuTexture;
        int votingShadeSibling = -1;
        [SerializeField, HideInInspector]
        GameObject shop, upgrade, auras, rebirth, store, reset, menuShade, drones, mobileControlsRoot;
        [SerializeField, HideInInspector]
        UnityEngine.UI.Button[] droneButtons = new UnityEngine.UI.Button[2];
        [SerializeField, HideInInspector]
        TMP_Text[] shopLabels = new TMP_Text[LaserProgress.Names.Length], auraLabels =
            new TMP_Text[5], upgradeLabels = new TMP_Text[3], upgradeValues = new TMP_Text[3];
        [SerializeField, HideInInspector]
        UnityEngine.UI.Button rebirthButton;
        [SerializeField, HideInInspector]
        TMP_Text[] rebirthCards = new TMP_Text[4];
        [SerializeField, HideInInspector]
        RectTransform rebirthFill;
        float toastUntil;
        [SerializeField, HideInInspector]
        RectTransform walletTarget;
        [SerializeField, HideInInspector]
        GameObject levelRoot;
        LaserArenaAbilities abilities;
        LaserIAPStore purchases;
        [SerializeField, HideInInspector]
        UnityEngine.UI.Button[] laserDonationButtons =
            new UnityEngine.UI.Button[LaserProgress.Names.Length], auraDonationButtons = new UnityEngine.UI.Button[5];
        [System.Serializable]
        class RewardPopup
        {
            public RectTransform rect;
            public TMP_Text label;
            public UnityEngine.UI.Image icon;
            public CanvasGroup group;
            public Vector2 start;
            public float time;
            public bool money;
        }

        [SerializeField, HideInInspector]
        RewardPopup[] popups = new RewardPopup[12];
        int popupIndex;
        [SerializeField, HideInInspector]
        bool sceneLayout;
        public bool ShopOpen => shop != null && shop.activeSelf;

        void Start()
        {
            abilities = hero.GetComponent<LaserArenaAbilities>();
            purchases = progress.GetComponent<LaserIAPStore>();
            if (!sceneLayout)
                BuildLayout();
            CloseMenus();
            if (mobileControlsRoot != null)
                mobileControlsRoot.SetActive(Application.isMobilePlatform);
            SubscribeRewards();
            progress.Notification -= ShowNotification;
            progress.Notification += ShowNotification;
            Refresh();
            if (progress.OfflineEarned > 0)
                ShowToast("Офлайн доход: +$" + LaserProgress.Format(progress.OfflineEarned));
        }

        public void CreateSceneLayout()
        {
            if (Application.isPlaying)
                throw new System.InvalidOperationException("Build scene HUD in Edit Mode");
            if (sceneLayout)
                return;
            if (transform.childCount > 0)
                throw new System.InvalidOperationException("HUD already has children; preserve them before migrating");
            BuildLayout();
            sceneLayout = true;
        }

        void BuildLayout()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var bar = Box("Level bar", transform, new Vector2(.5f, 1), new Vector2(0, -12), new Vector2(820,
                80), new Color(.18f, .35f, .39f, .85f), bluePanel);
            levelRoot = bar.gameObject;
            abilities = hero.GetComponent<LaserArenaAbilities>();
            purchases = progress.GetComponent<LaserIAPStore>();
            var fill = Box("XP fill", bar, new Vector2(0, 1), new Vector2(5, -5), new Vector2(810, 70), Color.white, orangePanel);
            xpFill = fill.GetComponent<UnityEngine.UI.Image>();
            Icon(bar, 0, new Vector2(-66, 6), new Vector2(110, 115));
            level = Text(bar, "", new Vector2(28, -7), new Vector2(450, 65), 46, Color.white);
            xpNumbers = Text(bar, "", new Vector2(475, -7), new Vector2(315, 65), 42, Color.white);
            xpNumbers.alignment = TextAlignmentOptions.Right;
            xpNumbers.enableAutoSizing = true;
            xpNumbers.fontSizeMin = 27;
            xpNumbers.fontSizeMax = 42;
            power = Text(transform, "", Vector2.zero, new Vector2(850, 62), 43, Color.white);
            Place(power.rectTransform, new Vector2(.5f, 1), new Vector2(0, -103), new Vector2(850, 62));
            power.alignment = TextAlignmentOptions.Center;
            var wallet = Box("Wallet", transform, new Vector2(0, 0), new Vector2(22, 24), new Vector2(365, 78),
                new Color(0, 0, 0, .08f));
            Icon(wallet, 1, new Vector2(0, -4), new Vector2(82, 70));
            money = Text(wallet, "", new Vector2(86, -2), new Vector2(280, 72), 48, new Color(.5f, 1, .06f));
            walletTarget = wallet;
            var rebWallet = Box("Rebirth currency", transform, new Vector2(0, 0), new Vector2(22, 108),
                new Vector2(300, 65), new Color(0, 0, 0, .08f));
            ExtraIcon(rebWallet, 5, new Vector2(0, 0), new Vector2(68, 60));
            rebirthCount = Text(rebWallet, "", new Vector2(80, -5), new Vector2(190, 55), 40, Color.white);
            Tile("Магазин", 6, orangePanel, new Vector2(22, 110), ToggleStore);
            Tile("Лазеры", 0, purplePanel, new Vector2(180, 110), ToggleShop);
            Tile("Ауры", 7, purplePanel, new Vector2(22, -58), ToggleAuras);
            Tile("Перерождение", 8, bluePanel, new Vector2(180, -58), ToggleRebirth);
            var u = Button("Улучшения", transform, new Vector2(0, .5f), new Vector2(22, -236), new Vector2(303,
                75), greenPanel, ToggleUpgrade);
            Icon(u.transform, 2, new Vector2(3, -5), new Vector2(65, 65));
            var ut = u.GetComponentInChildren<TMP_Text>();
            ut.rectTransform.anchoredPosition = new Vector2(58, -5);
            ut.rectTransform.sizeDelta = new Vector2(240, 65);
            ut.fontSize = 31;
            toast = Text(transform, "", Vector2.zero, new Vector2(900, 55), 32, new Color(1, 1, .3f));
            Place(toast.rectTransform, new Vector2(.5f, 1), new Vector2(0, -190), new Vector2(900, 55));
            toast.alignment = TextAlignmentOptions.Center;
            BuildRewardPopups();
            MobileControls();
            mobileControlsRoot.SetActive(Application.isMobilePlatform);
            menuShade = Box("Modal background", transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero,
                new Color(.03f, .03f, .08f, .48f)).gameObject;
            var shadeRect = (RectTransform)menuShade.transform;
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
            menuShade.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            menuShade.SetActive(false);
            var blurObject = new GameObject("Blurred world", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            blurObject.transform.SetParent(menuShade.transform, false);
            var blurRect = (RectTransform)blurObject.transform;
            blurRect.anchorMin = Vector2.zero;
            blurRect.anchorMax = Vector2.one;
            blurRect.offsetMin = blurRect.offsetMax = Vector2.zero;
            blurredWorld = blurObject.GetComponent<UnityEngine.UI.RawImage>();
            blurredWorld.raycastTarget = false;
            blurredWorld.material = menuBlurMaterial;
            blurredWorld.color = new Color(.68f, .68f, .75f, 1);
            blurredWorld.enabled = false;
            BuildShop();
            BuildUpgrade();
            BuildAuras();
            BuildDrones();
            BuildRebirth();
            BuildStore();
            Refresh();
            bar.SetAsLastSibling();
            power.transform.SetAsLastSibling();
            wallet.SetAsLastSibling();
            rebWallet.SetAsLastSibling();
            toast.transform.SetAsLastSibling();
        }

        void OnEnable()
        {
            if (progress != null && toast != null)
            {
                progress.Notification -= ShowNotification;
                progress.Notification += ShowNotification;
                SubscribeRewards();
            }
        }

        void SubscribeRewards()
        {
            progress.EnergyGained -= ShowEnergy;
            progress.MoneyEarned -= ShowMoney;
            progress.EnergyGained += ShowEnergy;
            progress.MoneyEarned += ShowMoney;
        }

        void OnDisable()
        {
            if (progress != null)
            {
                progress.Notification -= ShowNotification;
                progress.EnergyGained -= ShowEnergy;
                progress.MoneyEarned -= ShowMoney;
            }
        }

        void Tile(string title, int icon, Sprite panel, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var b = Button(title, transform, new Vector2(0, .5f), position, new Vector2(145, 150), panel, action);
            if (icon >= 6)
                ExtraIcon(b.transform, icon - 6, Vector2.zero, new Vector2(90, 90));
            else
                Icon(b.transform, icon, Vector2.zero, new Vector2(90, 90));
            var picture = b.transform.Find("Icon") as RectTransform;
            if (picture != null)
                Place(picture, new Vector2(.5f, .5f), Vector2.zero, new Vector2(90, 90));
            var t = b.GetComponentInChildren<TMP_Text>();
            t.rectTransform.anchoredPosition = new Vector2(-6, -120);
            t.rectTransform.sizeDelta = new Vector2(157, 30);
            t.fontSize = 27;
            t.enableAutoSizing = true;
            t.fontSizeMin = 16;
            t.fontSizeMax = 27;
        }

        void MobileControls()
        {
            var root = Box("Mobile controls", transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, Color.clear);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            mobileControlsRoot = root.gameObject;
            var joy = Box("Joystick", root, new Vector2(0, 0), new Vector2(35, 150), new Vector2(160, 160),
                new Color(.2f, .5f, 1, .6f), bluePanel);
            joy.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var knob = Box("Knob", joy, new Vector2(.5f, .5f), Vector2.zero, new Vector2(60, 60), Color.white, bluePanel);
            var pad = joy.gameObject.AddComponent<LaserTouchPad>();
            pad.movement = true;
            pad.player = hero;
            pad.knob = knob;
            Button("Прыжок", root, new Vector2(1, 0), new Vector2(-25, 135), new Vector2(160, 75), bluePanel,
                () => hero.touchJump = true);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
                ToggleShop();
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseMenus();
            Refresh();
            AnimateRewards();
        }

        void Refresh()
        {
            money.text = "$" + LaserProgress.Format(progress.Data.Cash);
            level.text = "Уровень " + LaserProgress.Format(LaserAmount.FromInteger(progress.Data.Level));
            xpNumbers.text = LaserProgress.Format(progress.Data.XP) + " / " + LaserProgress.Format(progress.RequiredXP);
            power.text = LaserProgress.Format(progress.Damage) + " Мощность";
            rebirthCount.text = progress.Data.rebirths.ToString();
            xpFill.rectTransform.sizeDelta = new Vector2(Mathf.Max(4,
                810 * Mathf.Clamp01((float)(progress.Data.XP / progress.RequiredXP))), 70);
            var arena = LaserArena.Instance;
            bool inside = arena != null && arena.Inside;
            levelRoot.SetActive(true);
            power.gameObject.SetActive(!inside);
            toast.text = Time.unscaledTime < toastUntil ? toast.text : "";
            if (shop != null && shop.activeSelf)
                for (int i = 0; i < LaserProgress.Names.Length; i++)
                    shopLabels[i].text = progress.Data.equipped == i ? "Надет" : progress.OwnsLaser(i)
                        ? "Надеть" : "$" + LaserProgress.Format(LaserProgress.Prices[i]);
            if (auras != null && auras.activeSelf)
                for (int i = 0; i < auraLabels.Length; i++)
                    auraLabels[i].text = progress.Data.aura == i ? "Снять" : progress.OwnsAura(i)
                        ? "Надеть" : "$" + LaserProgress.Format(LaserProgress.AuraPrices[i]);
            if (shop != null && shop.activeSelf)
                RefreshDonations(laserDonationButtons, LaserStoreKind.Laser);
            if (auras != null && auras.activeSelf)
                RefreshDonations(auraDonationButtons, LaserStoreKind.Aura);
            if (drones != null && drones.activeSelf)
                for (int i = 0; i < 2; i++)
                {
                    bool owned = progress.OwnsDrone(i);
                    var b = droneButtons[i];
                    b.interactable = owned || (purchases != null && purchases.CanBuy(LaserStoreKind.Drone, i));
                    b.GetComponentInChildren<TMP_Text>().text = owned ? (progress.Data.drone == i
                        ? "Снять" : "Надеть") : (purchases != null ? purchases.Price(LaserStoreKind.Drone, i) : "—");
                    var panel = b.GetComponent<LaserStudPanel>();
                    panel.topColor = owned ? new Color(.62f, 1, .13f) : new Color(.94f, .22f, 1);
                    panel.bottomColor = owned ? new Color(.05f, .72f, .02f) : new Color(.57f, 0, .94f);
                    panel.SetVerticesDirty();
                }

            if (upgrade != null && upgrade.activeSelf)
                for (int i = 0; i < 3; i++)
                {
                    int l = progress.UpgradeLevel(i);
                    upgradeLabels[i].text = l >= 20 ? "Максимум" : "$" + LaserProgress.Format(progress.UpgradePrice(i));
                    upgradeValues[i].text = i == 0 ? (l >= 20
                        ? LaserProgress.MoneyUpgradeMultiplier(l).ToString("0.###") + "x"
                        : LaserProgress.MoneyUpgradeMultiplier(l).ToString("0.###") + "x → "
                        + LaserProgress.MoneyUpgradeMultiplier(l + 1).ToString("0.###") + "x") : i == 1
                        ? (.5f * Mathf.Pow(.912f, l)).ToString("0.###") + "с → " + (.5f * Mathf.Pow(.912f,
                        l + 1)).ToString("0.###") + "с" : (4 + l * .25f).ToString("0.##") + "ч → " + (4 + (l + 1)
                        * .25f).ToString("0.##") + "ч";
                }

            if (rebirth != null && rebirth.activeSelf)
            {
                rebirthCards[0].text = "×" + LaserProgress.Format(progress.RebirthPower) + " Мощность";
                rebirthCards[1].text = "×" + LaserProgress.Format(progress.RebirthPower * 2) + " Мощность";
                rebirthCards[2].text = "×" + (1 + progress.Data.rebirths) + " Денег";
                rebirthCards[3].text = "×" + (2 + progress.Data.rebirths) + " Денег";
                rebirthText.text = "Уровень: " + LaserProgress.Format(LaserAmount.FromInteger(progress.Data.Level)) + " / " + progress.RebirthLevel;
                rebirthFill.sizeDelta = new Vector2(700 * Mathf.Clamp01((float)progress.Data.Level / progress.RebirthLevel), 54);
                rebirthButton.interactable = progress.CanRebirth;
            }
        }

        void BuildRewardPopups()
        {
            for (int i = 0; i < popups.Length; i++)
            {
                var root = new GameObject("Reward popup", typeof(RectTransform), typeof(CanvasGroup));
                root.transform.SetParent(transform, false);
                var rect = (RectTransform)root.transform;
                Place(rect, new Vector2(.5f, .5f), Vector2.zero, new Vector2(240, 70));
                var label = Text(rect, "", new Vector2(0, -5), new Vector2(175, 60), 42, Color.white);
                label.alignment = TextAlignmentOptions.Right;
                var icon = Box("Reward icon", rect, new Vector2(0, 1), new Vector2(180, -5), new Vector2(60,
                    60), Color.white, icons[0]).GetComponent<UnityEngine.UI.Image>();
                icon.preserveAspect = true;
                popups[i] = new RewardPopup
                {
                    rect = rect,
                    label = label,
                    icon = icon,
                    group = root.GetComponent<CanvasGroup>()
                };
                popups[i].group.blocksRaycasts = false;
                root.SetActive(false);
            }
        }

        Vector2 RewardPosition(Vector3 world)
        {
            Vector3 screen = hero.view.WorldToScreenPoint(world);
            if (screen.z <= 0)
                screen = new Vector3(Screen.width * .5f, Screen.height * .5f, 0);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screen, null, out var local);
            return local;
        }

        void ShowEnergy(LaserAmount amount) => ShowReward(amount, false);
        void ShowMoney(LaserAmount amount) => ShowReward(amount, true);
        void ShowReward(LaserAmount amount, bool cash)
        {
            if (popups[0] == null || hero.MenuOpen)
                return;
            var p = popups[popupIndex++ % popups.Length];
            p.money = cash;
            p.time = Time.unscaledTime;
            Vector3 world = cash && Time.time - eyes.LastDamageAt < .25f ? eyes.LastImpactPoint
                : hero.transform.position + Vector3.up * 1.8f;
            p.start = RewardPosition(world) + new Vector2(Random.Range(-90, 90), Random.Range(10, 55));
            p.rect.anchoredPosition = p.start;
            p.rect.localRotation = Quaternion.Euler(0, 0, cash ? Random.Range(-16, 16) : 0);
            p.label.text = "+" + LaserProgress.Format(amount);
            p.label.color = cash ? new Color(.45f, 1, .02f) : Color.white;
            p.icon.sprite = icons[cash ? 1 : 0];
            p.group.alpha = 1;
            p.rect.gameObject.SetActive(true);
        }

        void AnimateRewards()
        {
            foreach (var p in popups)
            {
                if (p == null || !p.rect.gameObject.activeSelf)
                    continue;
                float t = (Time.unscaledTime - p.time) / .9f;
                if (t >= 1)
                {
                    p.rect.gameObject.SetActive(false);
                    continue;
                }

                Vector2 end = p.money ? (Vector2)transform.InverseTransformPoint(walletTarget.TransformPoint(walletTarget.rect.center)) : p.start + Vector2.up * 95;
                p.rect.anchoredPosition = Vector2.Lerp(p.start, end, t * t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 50;
                p.group.alpha = 1 - t * t * t;
                p.rect.localScale = Vector3.one * ((p.money ? Mathf.Lerp(1, .3f, t * t) : 1) + .18f * Mathf.Sin(t * Mathf.PI));
            }
        }

        void ShowToast(string text)
        {
            ShowNotification(text, NotificationType.Success);
        }

        void ShowNotification(string text, NotificationType type)
        {
            if (type == NotificationType.Success)
                return;
            if (NotificationManager.Instance != null)
            {
                NotificationManager.ShowNotification(text, type);
                return;
            }

            toast.text = text;
            toast.color = type == NotificationType.Error ? Color.red : type == NotificationType.Success
                ? Color.green : Color.yellow;
            toastUntil = Time.unscaledTime + 3;
        }

        public void CloseMenus()
        {
            foreach (var panel in new[]
            {
                shop,
                upgrade,
                auras,
                rebirth,
                store,
                reset,
                drones,
                settingsPanel,
                menuShade
            }

            )
                if (panel != null)
                    panel.SetActive(false);
            if (mapVotingUI != null)
                mapVotingUI.HideDialog();
            if (votingShadeSibling >= 0 && menuShade != null)
            {
                menuShade.transform.SetSiblingIndex(votingShadeSibling);
                votingShadeSibling = -1;
            }

            hero.MenuOpen = false;
        }

        public void OpenMapVote()
        {
            if (mapVotingUI == null || mapVotingUI.rotation == null || mapVotingUI.rotation.Phase != LaserArenaPhase.Voting)
                return;
            mapVotingUI.RefreshVote();
            if (!mapVotingUI.IsOpen)
                Toggle(mapVotingUI.dialog);
            if (votingShadeSibling < 0)
                votingShadeSibling = menuShade.transform.GetSiblingIndex();
            menuShade.transform.SetAsLastSibling();
            mapVotingUI.dialog.transform.SetAsLastSibling();
        }

        void BuildReset()
        {
            var r = Modal("Reset progress", "Сброс прогресса", orangePanel, 0);
            reset = r.gameObject;
            var description = Text(r, "Начать игру с нуля?\n\nБудут сброшены деньги, уровень, лазеры, ауры, дроны и улучшения. Арена восстановится.", new Vector2(45, -150), new Vector2(690, 255), 34, Color.white);
            description.alignment = TextAlignmentOptions.Center;
            description.textWrappingMode = TextWrappingModes.Normal;
            Button("Отмена", r, new Vector2(0, 0), new Vector2(35, 35), new Vector2(330, 78), bluePanel, CloseMenus);
            var confirm = Button("Удалить сохранение", r, new Vector2(1, 0), new Vector2(-35, 35),
                new Vector2(360, 78), orangePanel, () =>
            {
                CloseMenus();
                eyes.AutoFire = false;
                eyes.touchFire = false;
                if (LaserImpactFeedback.Instance != null)
                    LaserImpactFeedback.Instance.ClearFires();
                progress.ResetProgress();
                if (LaserArena.Instance != null)
                {
                    LaserArena.Instance.Lobby();
                    LaserArena.Instance.ResetArena();
                }
            });
            var confirmLabel = confirm.GetComponentInChildren<TMP_Text>();
            confirmLabel.enableAutoSizing = true;
            confirmLabel.fontSizeMin = 20;
            confirmLabel.fontSizeMax = 28;
            reset.SetActive(false);
        }

        void Toggle(GameObject panel)
        {
            bool next = !panel.activeSelf;
            CloseMenus();
            if (next)
            {
                if (menuBlurMaterial != null)
                {
                    int w = Mathf.Max(160, Screen.width / 4), h = Mathf.Max(90, Screen.height / 4);
                    if (menuTexture == null || menuTexture.width != w || menuTexture.height != h)
                    {
                        if (menuTexture != null)
                        {
                            menuTexture.Release();
                            Destroy(menuTexture);
                        }

                        menuTexture = new RenderTexture(w, h, 24)
                        {
                            name = "Menu world snapshot"
                        };
                    }

                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(hero.view,
                        new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination =
                        menuTexture });
                    blurredWorld.texture = menuTexture;
                    blurredWorld.enabled = true;
                }

                var scroll = panel.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
                if (scroll != null)
                    scroll.verticalNormalizedPosition = 1;
            }

            panel.SetActive(next);
            menuShade.SetActive(next);
            hero.MenuOpen = next;
            eyes.touchFire = false;
        }

        public void ToggleShop() => Toggle(shop);
        public void ToggleUpgrade() => Toggle(upgrade);
        public void ToggleAuras() => Toggle(auras);
        public void ToggleRebirth() => Toggle(rebirth);
        public void ToggleStore() => Toggle(store);
        public void ToggleSettings()
        {
            if (settingsPanel != null)
                Toggle(settingsPanel);
        }

        public void OpenDrones(int featured = 0)
        {
            if (drones == null)
                return;
            if (!drones.activeSelf)
                Toggle(drones);
            var scroll = drones.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = featured == 0 ? 1 : 0;
        }

        void BuildDrones()
        {
            var companion = hero.GetComponent<LaserDroneCompanion>();
            if (companion == null || companion.settings == null)
                return;
            var r = Modal("Drones", "Дроны", bluePanel, 6);
            drones = r.gameObject;
            RestoreButton(r);
            r.Find("Header/Icon").GetComponent<UnityEngine.UI.Image>().sprite = companion.settings.drones[0].icon;
            var content = ScrollContent(r);
            for (int i = 0; i < 2; i++)
            {
                int id = i;
                var def = companion.settings.drones[i];
                var row = Row(content, "Drone " + i, i == 0 ? bluePanel : orangePanel);
                row.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 234;
                row.sizeDelta = new Vector2(718, 234);
                Box("Icon plate", row, new Vector2(0, 1), new Vector2(12, -12), new Vector2(165, 210), Color.white, iconPlate);
                var icon = Box("Drone icon", row, new Vector2(0, 1), new Vector2(16, -39), new Vector2(157,
                    155), Color.white, def.icon).GetComponent<UnityEngine.UI.Image>();
                icon.preserveAspect = true;
                Text(row, def.title, new Vector2(192, -16), new Vector2(480, 53), 38, Color.white);
                Text(row, "Луч: 0.25 с\nВыстрел раз в " + def.cooldown.ToString("0.0",
                    System.Globalization.CultureInfo.InvariantCulture) + " с\nДальность: " + def.range + " м",
                    new Vector2(192, -83), new Vector2(280, 120), 22, Color.white);
                var b = Button("", row, new Vector2(1, .5f), new Vector2(-16, 0), new Vector2(210, 70), purplePanel, () =>
                {
                    if (progress.OwnsDrone(id))
                        progress.EquipDrone(id);
                    else if (purchases != null)
                        purchases.Buy(LaserStoreKind.Drone, id);
                });
                droneButtons[i] = b;
            }

            drones.SetActive(false);
        }

        RectTransform Modal(string name, string title, Sprite panel, int icon)
        {
            var r = Box(name, transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(780, 610),
                new Color(.11f, .12f, .17f, .98f));
            r.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var header = Box("Header", r, new Vector2(0, 1), Vector2.zero, new Vector2(780, 100), Color.white, panel);
            if (icon >= 6)
                ExtraIcon(header, icon - 6, new Vector2(12, -8), new Vector2(82, 82));
            else
                Icon(header, icon, new Vector2(12, -8), new Vector2(82, 82));
            Text(header, title, new Vector2(103, -10), new Vector2(580, 77), 52, Color.white);
            var close = Button("X", header, new Vector2(1, 1), new Vector2(-12, -10), new Vector2(78, 78), orangePanel, CloseMenus);
            close.GetComponent<UnityEngine.UI.Image>().color = new Color(1, .32f, .35f);
            return r;
        }

        RectTransform ScrollContent(RectTransform modal)
        {
            var root = Box("Scroll view", modal, new Vector2(0, 1), new Vector2(12, -112), new Vector2(756, 486), Color.clear);
            var scroll = root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 38;
            var viewport = Box("Viewport", root, new Vector2(0, 1), Vector2.zero, new Vector2(728, 486),
                new Color(.12f, .12f, .16f, .8f));
            viewport.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
            var content = new GameObject("Content", typeof(RectTransform),
                typeof(UnityEngine.UI.VerticalLayoutGroup), typeof(UnityEngine.UI.ContentSizeFitter)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.spacing = 14;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.GetComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit =
                UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            var track = Box("Scrollbar", root, new Vector2(1, 1), Vector2.zero, new Vector2(18, 486), new Color(0, 0, 0, .5f));
            var handle = Box("Handle", track, new Vector2(.5f, .5f), Vector2.zero, new Vector2(18, 80), new Color(.55f, .5f, .7f));
            var bar = track.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
            bar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            bar.handleRect = handle;
            bar.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
            handle.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            handle.sizeDelta = Vector2.zero;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.verticalScrollbar = bar;
            return content;
        }

        RectTransform Row(Transform content, string name, Sprite panel)
        {
            var r = Box(name, content, new Vector2(0, 1), Vector2.zero, new Vector2(718, 194), Color.white, panel);
            r.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 194;
            return r;
        }

        void BuildShop()
        {
            shopLabels = new TMP_Text[LaserProgress.Names.Length];
            laserDonationButtons = new UnityEngine.UI.Button[LaserProgress.Names.Length];
            var r = Modal("Laser shop", "Lasers", purplePanel, 0);
            RestoreButton(r);
            shop = r.gameObject;
            var content = ScrollContent(r);
            for (int i = 0; i < LaserProgress.Names.Length; i++)
            {
                int id = i;
                var row = Row(content, "Laser " + i, purplePanel);
                row.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 234;
                row.sizeDelta = new Vector2(718, 234);
                Box("Icon plate", row, new Vector2(0, 1), new Vector2(12, -12), new Vector2(165, 210), Color.white, iconPlate);
                Icon(row, 0, new Vector2(20, -44), new Vector2(132, 132), LaserProgress.Colors[i]);
                if (laserTintIcon != null)
                    row.Find("Icon plate/Icon").GetComponent<UnityEngine.UI.Image>().sprite = laserTintIcon;
                var name = Text(row, LaserProgress.Names[i] + "\nОбычный", new Vector2(192, -14),
                    new Vector2(268, 84), 33, Color.white);
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.enableAutoSizing = true;
                name.fontSizeMin = 24;
                name.fontSizeMax = 33;
                Icon(row, 0, new Vector2(195, -150), new Vector2(58, 66));
                Text(row, "+" + LaserProgress.Format(LaserProgress.EnergyGain[i]), new Vector2(260, -153),
                    new Vector2(174, 60), 42, Color.white);
                var button = Button("", row, new Vector2(1, .5f), new Vector2(-16, 36), new Vector2(230,
                    64), greenPanel, () => progress.BuyLaser(id));
                shopLabels[i] = button.GetComponentInChildren<TMP_Text>();
                laserDonationButtons[i] = DonationButton(row, LaserStoreKind.Laser, id, 230);
            }

            shop.SetActive(false);
        }

        void BuildUpgrade()
        {
            var r = Modal("Upgrades", "Upgrades", greenPanel, 2);
            upgrade = r.gameObject;
            var content = ScrollContent(r);
            string[] names =
            {
                "Множитель денег",
                "Скорость лазера",
                "Время офлайн"
            };
            for (int i = 0; i < 3; i++)
            {
                int id = i;
                var row = Row(content, "Upgrade " + i, greenPanel);
                Box("Icon plate", row, new Vector2(0, 1), new Vector2(12, -12), new Vector2(165, 170), Color.white, iconPlate);
                if (i == 0)
                    Icon(row, 1, new Vector2(24, -25), new Vector2(140, 140));
                else
                    ExtraIcon(row, i == 1 ? 3 : 4, new Vector2(24, -25), new Vector2(140, 140));
                Text(row, names[i], new Vector2(195, -14), new Vector2(490, 57), 31, Color.white);
                upgradeValues[i] = Text(row, "", new Vector2(195, -100), new Vector2(500, 65), 31, new Color(.65f, 1, .2f));
                var button = Button("", row, new Vector2(1, .5f), new Vector2(-15, 5), new Vector2(230, 66),
                    greenPanel, () => progress.BuyUpgrade(id));
                upgradeLabels[i] = button.GetComponentInChildren<TMP_Text>();
                // Keep the before/after values in the left column, clear of the purchase button.
                upgradeValues[i].rectTransform.sizeDelta = new Vector2(280, 80);
                upgradeValues[i].fontSize = 25;
                upgradeValues[i].textWrappingMode = TextWrappingModes.Normal;
            }

            upgrade.SetActive(false);
        }

        void BuildAuras()
        {
            var r = Modal("Auras", "Auras", purplePanel, 7);
            TintAuraPanel(r.Find("Header"));
            RestoreButton(r);
            auras = r.gameObject;
            var content = ScrollContent(r);
            for (int i = 0; i < LaserProgress.AuraNames.Length; i++)
            {
                int id = i;
                var row = Row(content, "Aura " + i, purplePanel);
                row.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 234;
                row.sizeDelta = new Vector2(718, 234);
                TintAuraPanel(row);
                Box("Icon plate", row, new Vector2(0, 1), new Vector2(12, -12), new Vector2(165, 210), Color.white, iconPlate);
                ExtraIcon(row, 1, new Vector2(20, -44), new Vector2(132, 132), LaserProgress.Colors[i == 0
                    ? 0 : i == 1 ? 2 : i == 2 ? 4 : i == 3 ? 5 : 3]);
                if (auraTintIcon != null)
                    row.Find("Icon plate/Icon").GetComponent<UnityEngine.UI.Image>().sprite = auraTintIcon;
                var title = Text(row, LaserProgress.AuraNames[i], new Vector2(192, -16), new Vector2(308, 62), 34, Color.white);
                title.enableAutoSizing = true;
                title.fontSizeMin = 27;
                title.fontSizeMax = 34;
                Icon(row, 0, new Vector2(192, -150), new Vector2(52, 60));
                var boost = Text(row, "Усиление на " + LaserProgress.AuraBoosts[i].ToString("0.##",
                    System.Globalization.CultureInfo.InvariantCulture) + "x", new Vector2(250, -152), new Vector2(244, 60),
                    25, Color.white);
                boost.alignment = TextAlignmentOptions.MidlineLeft;
                boost.enableAutoSizing = true;
                boost.fontSizeMin = 19;
                boost.fontSizeMax = 25;
                var button = Button("", row, new Vector2(1, .5f), new Vector2(-16, 36), new Vector2(190,
                    64), purplePanel, () => progress.BuyAura(id));
                TintAuraPanel(button.transform, true);
                auraLabels[i] = button.GetComponentInChildren<TMP_Text>();
                auraDonationButtons[i] = DonationButton(row, LaserStoreKind.Aura, id, 190);
            }

            auras.SetActive(false);
        }

        UnityEngine.UI.Button DonationButton(Transform row, LaserStoreKind kind, int index, float width)
        {
            var button = Button("Premium purchase", row, new Vector2(1, .5f), new Vector2(-16, -36),
                new Vector2(width, 64), purplePanel, () =>
            {
                if (purchases != null)
                    purchases.Buy(kind, index);
            });
            var panel = button.GetComponent<LaserStudPanel>();
            panel.topColor = new Color(.94f, .22f, 1);
            panel.bottomColor = new Color(.57f, 0, .94f);
            panel.SetVerticesDirty();
            var colors = button.colors;
            colors.disabledColor = new Color(.86f, .86f, .86f, 1);
            button.colors = colors;
            button.interactable = purchases != null && purchases.CanBuy(kind, index);
            button.GetComponentInChildren<TMP_Text>().text = purchases != null ? purchases.Price(kind, index) : "—";
            return button;
        }

        void RefreshDonations(UnityEngine.UI.Button[] buttons, LaserStoreKind kind)
        {
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i] != null)
                {
                    buttons[i].interactable = purchases != null && purchases.CanBuy(kind, i);
                    buttons[i].GetComponentInChildren<TMP_Text>().text = purchases != null ? purchases.Price(kind, i) : "—";
                }
        }

        void RestoreButton(RectTransform modal)
        {
            var b = Button("Восстановить", modal.Find("Header"), new Vector2(1, 1), new Vector2(-103, -26),
                new Vector2(174, 48), purplePanel, () =>
            {
                if (purchases != null)
                    purchases.RestorePurchases();
            });
            b.GetComponentInChildren<TMP_Text>().fontSize = 18;
        }

        void TintAuraPanel(Transform target, bool purchase = false)
        {
            var panel = target.GetComponent<LaserStudPanel>();
            if (panel == null)
                return;
            panel.topColor = purchase ? new Color(.89f, .34f, 1) : new Color(1, .61f, .95f);
            panel.bottomColor = purchase ? new Color(.59f, .06f, .87f) : new Color(.8f, .24f, .93f);
            panel.SetVerticesDirty();
        }

        void BuildRebirth()
        {
            var r = Modal("Rebirth", "Rebirth", bluePanel, 8);
            rebirth = r.gameObject;
            Text(r, "До:", new Vector2(32, -120), new Vector2(310, 45), 30, Color.white).alignment = TextAlignmentOptions.Center;
            Text(r, "После:", new Vector2(438, -120), new Vector2(310, 45), 30, Color.white).alignment =
                TextAlignmentOptions.Center;
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? 32 : 438, y = i < 2 ? -168 : -252;
                var card = Box("Rebirth bonus", r, new Vector2(0, 1), new Vector2(x, y), new Vector2(310,
                    72), Color.white, i < 2 ? orangePanel : greenPanel);
                rebirthCards[i] = Text(card, "", new Vector2(8, -8), new Vector2(294, 56), 31, Color.white);
                rebirthCards[i].alignment = TextAlignmentOptions.Center;
            }

            Text(r, "→", new Vector2(353, -166), new Vector2(72, 72), 48, Color.white).alignment = TextAlignmentOptions.Center;
            Text(r, "→", new Vector2(353, -250), new Vector2(72, 72), 48, Color.white).alignment = TextAlignmentOptions.Center;
            Text(r, "Перерождение сбрасывает уровень и опыт", new Vector2(30, -351), new Vector2(720, 40), 24,
                new Color(1, .3f, .35f)).alignment = TextAlignmentOptions.Center;
            var track = Box("Rebirth level", r, new Vector2(0, 1), new Vector2(35, -408), new Vector2(710, 64),
                new Color(.2f, .25f, .3f), bluePanel);
            rebirthFill = Box("Progress", track, new Vector2(0, 1), new Vector2(5, -5), new Vector2(700, 54),
                Color.white, orangePanel);
            rebirthText = Text(track, "", new Vector2(8, -7), new Vector2(694, 50), 31, Color.white);
            rebirthText.alignment = TextAlignmentOptions.Center;
            rebirthButton = Button("Перерождение", r, new Vector2(.5f, 0), new Vector2(0, 24), new Vector2(670,
                78), greenPanel, () =>
            {
                if (progress.Rebirth())
                    CloseMenus();
            });
            rebirth.SetActive(false);
        }

        void BuildStore()
        {
            var r = Modal("Store", "Магазин", orangePanel, 6);
            store = r.gameObject;
            var content = ScrollContent(r);
            string[] titles =
            {
                "Лазеры",
                "Ауры",
                "Тренировка"
            };
            string[] details =
            {
                "Новые цвета и базовая энергия",
                "Постоянное усиление опыта",
                "Одна бесплатная мишень в лобби"
            };
            for (int i = 0; i < 3; i++)
            {
                int id = i;
                var row = Row(content, "Category " + i, i == 1 ? purplePanel : bluePanel);
                if (i == 1)
                    ExtraIcon(row, 1, new Vector2(18, -20), new Vector2(150, 150));
                else
                    Icon(row, i == 0 ? 0 : 3, new Vector2(18, -20), new Vector2(150, 150));
                Text(row, titles[i], new Vector2(192, -14), new Vector2(450, 55), 36, Color.white);
                var t = Text(row, details[i], new Vector2(192, -75), new Vector2(460, 50), 23, Color.white);
                t.textWrappingMode = TextWrappingModes.Normal;
                Button("Открыть", row, new Vector2(1, 0), new Vector2(-14, 15), new Vector2(240, 58), greenPanel, () =>
                {
                    if (id == 0)
                        ToggleShop();
                    else if (id == 1)
                        ToggleAuras();
                    else
                    {
                        CloseMenus();
                        LaserArena.Instance.Lobby();
                        hero.Teleport(new Vector3(-20.8f, 1, -14));
                        hero.yaw = 270;
                    }
                });
            }

            store.SetActive(false);
        }

        void ExtraIcon(Transform parent, int index, Vector2 pos, Vector2 size, Color? tint = null)
        {
            if (extraIcons == null || index >= extraIcons.Length || extraIcons[index] == null)
                return;
            var r = Box("Icon", parent, new Vector2(0, 1), pos, size, tint ?? Color.white, extraIcons[index]);
            r.GetComponent<UnityEngine.UI.Image>().preserveAspect = true;
        }

        void Icon(Transform parent, int index, Vector2 pos, Vector2 size, Color? tint = null)
        {
            if (icons == null || index >= icons.Length || icons[index] == null)
                return;
            var r = Box("Icon", parent, new Vector2(0, 1), pos, size, tint ?? Color.white, icons[index]);
            r.GetComponent<UnityEngine.UI.Image>().preserveAspect = true;
        }

        static void Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size,
            Color color, Sprite sprite = null)
        {
            if (name == "Icon plate")
            {
                position.x = 20;
                position.y -= (size.y - size.x) * .5f;
                size.y = size.x;
            }

            bool studs = sprite != null && (sprite == orangePanel || sprite == greenPanel
                || sprite == purplePanel || sprite == bluePanel);
            var go = new GameObject(name, typeof(RectTransform), studs ? typeof(LaserStudPanel) : typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            Place(r, anchor, position, size);
            var im = go.GetComponent<UnityEngine.UI.Image>();
            im.color = color;
            im.sprite = sprite;
            if (name == "Icon plate" && sprite != null)
                im.type = UnityEngine.UI.Image.Type.Sliced;
            if (im is LaserStudPanel panel)
            {
                panel.topColor = sprite == orangePanel ? new Color(1, .77f, .22f) : sprite == greenPanel
                    ? new Color(.62f, 1, .13f) : sprite == purplePanel ? new Color(.78f, .63f, 1) : new Color(.38f, .88f, 1);
                panel.bottomColor = sprite == orangePanel ? new Color(1, .27f, .035f)
                    : sprite == greenPanel ? new Color(.05f, .72f, .02f) : sprite == purplePanel
                    ? new Color(.49f, .3f, .9f) : new Color(.06f, .52f, .94f);
            }

            if (name == "Icon plate" && parent.TryGetComponent<LaserStudPanel>(out var rowPanel))
            {
                rowPanel.studLeftInset = Mathf.Max(rowPanel.studLeftInset, position.x + size.x - 12);
                rowPanel.SetVerticesDirty();
            }

            if ((name == "Icon" || name == "Drone icon") && position.x < 180 && parent.Find("Icon plate")is RectTransform plate)
            {
                r.SetParent(plate, false);
                Place(r, new Vector2(.5f, .5f), new Vector2(0, 6), size);
            }

            im.raycastTarget = false;
            return r;
        }

        TMP_Text Text(Transform parent, string value, Vector2 pos, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            Place(r, new Vector2(0, 1), pos, size);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.font = font;
            if (outlinedText != null)
                t.fontSharedMaterial = outlinedText;
            t.text = value;
            t.fontSize = fontSize;
            t.color = color;
            t.fontStyle = FontStyles.Bold;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.UpdateMeshPadding();
            return t;
        }

        UnityEngine.UI.Button Button(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size,
            Sprite panel, UnityEngine.Events.UnityAction action)
        {
            var r = Box(name, parent, anchor, pos, size, Color.white, panel);
            var img = r.GetComponent<UnityEngine.UI.Image>();
            img.raycastTarget = true;
            var b = r.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(action);
            var t = Text(r, name, new Vector2(6, -5), size - new Vector2(12, 10), 32, Color.white);
            t.alignment = TextAlignmentOptions.Center;
            return b;
        }

        void OnDestroy()
        {
            if (progress != null)
            {
                progress.Notification -= ShowNotification;
                progress.EnergyGained -= ShowEnergy;
                progress.MoneyEarned -= ShowMoney;
            }

            if (menuTexture != null)
            {
                menuTexture.Release();
                Destroy(menuTexture);
            }
        }
    }
}
