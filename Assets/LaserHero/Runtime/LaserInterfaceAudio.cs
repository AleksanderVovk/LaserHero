using System.Collections.Generic;
using UnityEngine;

namespace LaserHero
{
    [DefaultExecutionOrder(1000)]
    public sealed class LaserInterfaceAudio : MonoBehaviour
    {
        public LaserProgress progress;
        public AudioClip buyTheme, levelUp, buyFail, click;
        public AudioSource resultVoice, levelVoice, clickVoice;
        [Tooltip("Buttons under these objects are controls and do not play UI clicks.")]
        public Transform[] controlRoots = System.Array.Empty<Transform>();
        [Range(0, 1)]
        public float purchaseVolume = .65f, levelVolume = .65f, failureVolume = .6f, clickVolume = .45f;
        readonly Dictionary<UnityEngine.UI.Button, UnityEngine.Events.UnityAction> buttons =
            new Dictionary<UnityEngine.UI.Button, UnityEngine.Events.UnityAction>();
        float nextScan;
        void OnEnable()
        {
            if (progress != null)
            {
                progress.PurchaseSucceeded += PlayPurchase;
                progress.PurchaseRejected += PlayFailure;
                progress.LevelRaised += PlayLevel;
            }

            RegisterButtons();
        }

        void Start()
        {
            RegisterButtons();
        }

        void Update()
        {
            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + .5f;
                RegisterButtons();
            }
        }

        public bool IsControl(UnityEngine.UI.Button button)
        {
            if (button == null)
                return true;
            foreach (var root in controlRoots)
                if (root != null && button.transform.IsChildOf(root))
                    return true;
            // LaserFlightControls lives on the shared HUD Canvas, not on the control subtree.
            if (button.GetComponentInParent<LaserFlightHoldButton>(true) != null)
                return true;
            for (var t = button.transform; t != null; t = t.parent)
                if (t.name == "Controllers" || t.name == "Mobile controls" || t.name == "Flight Controls" || t.name == "Joystick")
                    return true;
            return false;
        }

        public void RegisterButtons()
        {
            foreach (var button in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (buttons.ContainsKey(button) || IsControl(button))
                    continue;
                var target = button;
                UnityEngine.Events.UnityAction action = () =>
                {
                    if (target != null && target.interactable && !IsControl(target))
                        PlayClick();
                };
                buttons.Add(button, action);
                button.onClick.AddListener(action);
            }
        }

        public void PlayPurchase()
        {
            if (resultVoice != null && buyTheme != null)
                resultVoice.PlayOneShot(buyTheme, purchaseVolume);
        }

        public void PlayFailure()
        {
            if (resultVoice != null && buyFail != null)
                resultVoice.PlayOneShot(buyFail, failureVolume);
        }

        public void PlayLevel()
        {
            if (levelVoice != null && levelUp != null)
                levelVoice.PlayOneShot(levelUp, levelVolume);
        }

        public void PlayClick()
        {
            if (clickVoice != null && click != null)
                clickVoice.PlayOneShot(click, clickVolume);
        }

        void OnDisable()
        {
            if (progress != null)
            {
                progress.PurchaseSucceeded -= PlayPurchase;
                progress.PurchaseRejected -= PlayFailure;
                progress.LevelRaised -= PlayLevel;
            }

            foreach (var item in buttons)
                if (item.Key != null)
                    item.Key.onClick.RemoveListener(item.Value);
            buttons.Clear();
        }
    }
}
