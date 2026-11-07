using TMPro;
using UnityEngine;

namespace LaserHero
{
    public class LaserMapVoteUI : MonoBehaviour
    {
        public LaserArenaRotation rotation;
        public GameObject dialog;
        public TMP_Text heading;
        public GameObject[] cards = new GameObject[3];
        public TMP_Text[] titles = new TMP_Text[3];
        public UnityEngine.UI.Image[] thumbnails = new UnityEngine.UI.Image[3];
        public LaserVoteSymbol[] symbols = new LaserVoteSymbol[3];
        [Header("Vote marks (empty = default graphics)")]
        public Sprite checkSprite;
        public Sprite crossSprite;
        public Vector2 designSize = new Vector2(1800, 885);
        [Min(300)]
        public float preferredWidth = 900;
        int lastSecond = -1;
        Vector2 previousCanvas;
        float previousWidth;
        public bool IsOpen => dialog != null && dialog.activeSelf;

        void OnEnable()
        {
            if (rotation != null)
                rotation.Changed += RefreshVote;
        }

        void OnDisable()
        {
            if (rotation != null)
                rotation.Changed -= RefreshVote;
        }

        public void HideDialog()
        {
            if (dialog != null)
                dialog.SetActive(false);
            lastSecond = -1;
        }

        public void VoteFirst() => rotation.Vote(0);
        public void VoteSecond() => rotation.Vote(1);
        public void VoteThird() => rotation.Vote(2);
        public void RefreshVote()
        {
            if (rotation == null)
                return;
            for (int i = 0; i < cards.Length; i++)
            {
                bool show = rotation.Phase == LaserArenaPhase.Voting && i < rotation.Candidates.Count;
                if (cards[i] != null)
                    cards[i].SetActive(show);
                if (symbols[i] != null)
                    symbols[i].gameObject.SetActive(show && rotation.SelectedSlot >= 0);
                if (!show)
                    continue;
                var map = rotation.maps[rotation.Candidates[i]];
                titles[i].text = map.title;
                thumbnails[i].sprite = map.icon;
                symbols[i].symbol = rotation.SelectedSlot == i ? LaserVoteSymbol.Kind.Check : LaserVoteSymbol.Kind.Cross;
                symbols[i].spriteOverride = rotation.SelectedSlot == i ? checkSprite : crossSprite;
                symbols[i].SetVerticesDirty();
                symbols[i].SetMaterialDirty();
            }

            lastSecond = -1;
            UpdateHeading();
            ApplySize();
        }

        void LateUpdate()
        {
            if (!IsOpen)
                return;
            UpdateHeading();
            ApplySize();
        }

        public void ApplySize()
        {
            var parent = dialog.transform.parent as RectTransform;
            if (parent == null)
                return;
            Vector2 size = parent.rect.size;
            if (size == previousCanvas && previousWidth == preferredWidth)
                return;
            previousCanvas = size;
            previousWidth = preferredWidth;
            float scale = Mathf.Min(preferredWidth / designSize.x, Mathf.Min((size.x - 70) / designSize.x,
                (size.y - 60) / designSize.y));
            dialog.transform.localScale = Vector3.one * Mathf.Max(.1f, scale);
        }

        void UpdateHeading()
        {
            if (heading == null || rotation == null)
                return;
            int seconds = Mathf.CeilToInt(rotation.RemainingTime);
            if (seconds == lastSecond)
                return;
            lastSecond = seconds;
            int last = seconds % 10;
            string unit = seconds % 100 >= 11 && seconds % 100 <= 14 ? "секунд" : last == 1 ? "секунда"
                : last >= 2 && last <= 4 ? "секунды" : "секунд";
            heading.text = "Голосование за карту (" + seconds + " " + unit + ")";
        }
    }
}
