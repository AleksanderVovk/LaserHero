using TMPro;
using UnityEngine;

namespace LaserHero
{
    public sealed class LaserSuperStrikeHUD : MonoBehaviour
    {
        public LaserArenaAbilities ability;
        public GameObject bar;
        public LaserRainbowCharge fill;
        public TMP_Text label;
        void Update()
        {
            bool visible = ability != null && LaserArena.Instance != null && LaserArena.Instance.Inside
                && !LaserArena.Instance.hero.MenuOpen;
            if (bar.activeSelf != visible)
                bar.SetActive(visible);
            if (!visible)
                return;
            fill.amount = ability.Charge01;
            fill.SetVerticesDirty();
            label.color = Color.Lerp(Color.white, Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * .65f, 1), .8f, 1), .4f);
        }
    }
}
