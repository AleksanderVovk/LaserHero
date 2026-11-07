using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LaserHero
{
    [Serializable]
    public class LaserArenaOption
    {
        public string id;
        public string title;
        public bool enabledInRotation = true;
        public GameObject root;
        public Sprite icon;
        public Bounds bounds = new Bounds(new Vector3(0, 24, 102), new Vector3(128, 100, 128));
        public Vector3 entry = new Vector3(0, 1, 42);
    }

    public enum LaserArenaPhase
    {
        Loading,
        Playing,
        Voting,
        Unavailable
    }

    [DefaultExecutionOrder(-80)]
    public class LaserArenaRotation : MonoBehaviour
    {
        public LaserArena arena;
        public LaserHeroHUD hud;
        public LaserMapVoteUI votingUI;
        [Tooltip("Wall to enable during voting/loading. Keep it outside the arena roots.")]
        public GameObject lobbyBarrier;
        [Tooltip("Optional TMP text showing the countdown until map voting starts.")]
        public TMPro.TMP_Text roundTimerText;
        [Min(1)]
        public float roundIntervalSeconds = 180;
        [Min(1)]
        public float votingSeconds = 10;
        [Tooltip("Preferred first arena; falls back to the first enabled entry.")]
        public string startingArenaId = "city";
        public List<LaserArenaOption> maps = new List<LaserArenaOption>();
        [Range(1, 32)]
        public int preparationBodiesPerFrame = 8;
        [Range(.5f, 12)]
        public float preparationBudgetMilliseconds = 4;
        public LaserArenaPhase Phase { get; private set; } = LaserArenaPhase.Loading;
        public float RemainingTime { get; private set; }
        public int CurrentArenaIndex { get; private set; } = -1;
        public int SelectedSlot { get; private set; } = -1;
        public IReadOnlyList<int> Candidates => candidates;
        public bool CanEnterArena => Phase == LaserArenaPhase.Playing;

        public event Action Changed;
        readonly List<int> candidates = new List<int>(3);
        Coroutine loading;
        float retryAt;
        bool started;
        int displayedTimerSecond = -1;
        TMPro.TMP_Text displayedTimerText;
        void Awake()
        {
            if (arena == null)
                arena = GetComponentInParent<LaserArena>();
            if (hud == null)
                hud = FindFirstObjectByType<LaserHeroHUD>();
            if (arena != null)
                arena.rotation = this;
            foreach (var map in maps)
                if (map != null && map.root != null)
                    map.root.SetActive(false);
            SetBarrier(true);
        }

        IEnumerator Start()
        {
            // HUD and the original player controller finish their own initialization first.
            yield return null;
            started = true;
            int index = FindInitial();
            if (index >= 0)
                StartLoading(index);
            else
                Unavailable();
        }

        bool Available(int i) => i >= 0 && i < maps.Count && maps[i] != null && maps[i].enabledInRotation && maps[i].root != null;
        int FindInitial()
        {
            for (int i = 0; i < maps.Count; i++)
                if (Available(i) && maps[i].id == startingArenaId)
                    return i;
            for (int i = 0; i < maps.Count; i++)
                if (Available(i))
                    return i;
            return -1;
        }

        public List<int> EligibleMaps()
        {
            var pool = new List<int>();
            var seen = new HashSet<GameObject>();
            GameObject current = CurrentArenaIndex >= 0 && CurrentArenaIndex < maps.Count
                && maps[CurrentArenaIndex] != null ? maps[CurrentArenaIndex].root : null;
            for (int i = 0; i < maps.Count; i++)
                if (Available(i) && i != CurrentArenaIndex && seen.Add(maps[i].root) && maps[i].root != current)
                    pool.Add(i);
            return pool;
        }

        void Update()
        {
            if (!started)
                return;
            if (Phase != LaserArenaPhase.Playing && arena != null && arena.Inside)
                arena.Lobby();
            if (Phase == LaserArenaPhase.Unavailable)
            {
                if (Time.unscaledTime >= retryAt)
                {
                    retryAt = Time.unscaledTime + 1;
                    int initial = FindInitial();
                    if (initial >= 0)
                        StartLoading(initial);
                }

                return;
            }

            if (Phase == LaserArenaPhase.Loading)
                return;
            RemainingTime = Mathf.Max(0, RemainingTime - Time.unscaledDeltaTime);
            if (Phase == LaserArenaPhase.Playing && (RemainingTime <= 0 || !Available(CurrentArenaIndex)))
                BeginVoting();
            else if (Phase == LaserArenaPhase.Voting && RemainingTime <= 0)
                FinishVoting();
        }

        void LateUpdate()
        {
            if (roundTimerText == null)
                return;
            int seconds = Phase == LaserArenaPhase.Playing ? Mathf.CeilToInt(Mathf.Max(0, RemainingTime)) : 0;
            if (displayedTimerText == roundTimerText && displayedTimerSecond == seconds)
                return;
            displayedTimerText = roundTimerText;
            displayedTimerSecond = seconds;
            roundTimerText.text = $"Сброс карты через {seconds / 60} минут {seconds % 60} секунд!";
        }

        public void BeginVoting()
        {
            if (!Application.isPlaying || Phase == LaserArenaPhase.Loading || Phase == LaserArenaPhase.Voting)
                return;
            var pool = EligibleMaps();
            if (pool.Count == 0)
            {
                if (Available(CurrentArenaIndex))
                    StartLoading(CurrentArenaIndex);
                else
                    Unavailable();
                return;
            }

            candidates.Clear();
            SelectedSlot = -1;
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int pick = UnityEngine.Random.Range(0, i + 1);
                int temp = pool[i];
                pool[i] = pool[pick];
                pool[pick] = temp;
            }

            for (int i = 0; i < Mathf.Min(3, pool.Count); i++)
                candidates.Add(pool[i]);
            Phase = LaserArenaPhase.Voting;
            RemainingTime = Mathf.Max(1, votingSeconds);
            SetBarrier(true);
            ReturnToLobby();
            ClearRoundEffects();
            Changed?.Invoke();
            if (hud != null && votingUI != null)
                hud.OpenMapVote();
        }

        public void Vote(int slot)
        {
            if (Phase != LaserArenaPhase.Voting || RemainingTime <= 0 || slot < 0 || slot >= candidates.Count
                || !Available(candidates[slot]))
                return;
            SelectedSlot = slot;
            Changed?.Invoke();
        }

        public void FinishVoting()
        {
            if (Phase != LaserArenaPhase.Voting)
                return;
            int winner = SelectedSlot >= 0 && SelectedSlot < candidates.Count
                && Available(candidates[SelectedSlot]) ? candidates[SelectedSlot] : -1;
            if (winner < 0)
            {
                var pool = candidates.Where(Available).ToList();
                if (pool.Count == 0)
                    pool = EligibleMaps();
                if (pool.Count > 0)
                    winner = pool[UnityEngine.Random.Range(0, pool.Count)];
            }

            if (winner >= 0)
                StartLoading(winner);
            else if (Available(CurrentArenaIndex))
                StartLoading(CurrentArenaIndex);
            else
                Unavailable();
        }

        void ReturnToLobby()
        {
            if (arena != null && arena.hero != null)
            {
                arena.Lobby();
                arena.hero.ClearShake();
            }

            if (hud != null && hud.eyes != null)
            {
                hud.eyes.AutoFire = false;
                hud.eyes.touchFire = false;
            }
        }

        void ClearRoundEffects()
        {
            if (LaserImpactFeedback.Instance != null)
                LaserImpactFeedback.Instance.ClearFires();
            foreach (var body in LaserVoxelBody.Active.ToArray())
                if (body != null && body.isFragment)
                {
                    body.gameObject.SetActive(false);
                    Destroy(body.gameObject);
                }
        }

        void StartLoading(int index)
        {
            if (!Available(index))
            {
                Unavailable();
                return;
            }

            if (loading != null)
                StopCoroutine(loading);
            Phase = LaserArenaPhase.Loading;
            RemainingTime = 0;
            SetBarrier(true);
            ReturnToLobby();
            if (hud != null)
                hud.CloseMenus();
            ClearRoundEffects();
            Changed?.Invoke();
            loading = StartCoroutine(LoadArena(index));
        }

        public void RegenerateCurrentArena()
        {
            if (Phase == LaserArenaPhase.Loading)
                return;
            int index = Available(CurrentArenaIndex) ? CurrentArenaIndex : FindInitial();
            if (index >= 0)
                StartLoading(index);
            else
                Unavailable();
        }

        IEnumerator LoadArena(int index)
        {
            foreach (var map in maps)
                if (map != null && map.root != null && map.root.activeSelf)
                    map.root.SetActive(false);
            var selected = maps[index];
            arena.arenaBounds = selected.bounds;
            arena.entry = selected.entry;
            // Refresh membership each round: editable arena objects can be added or replaced at runtime.
            var list = selected.root.GetComponentsInChildren<LaserVoxelBody>(true);
            long slice = System.Diagnostics.Stopwatch.GetTimestamp();
            int prepared = 0;
            foreach (var body in list)
            {
                if (body == null || body.isFragment || !body.enabled || !Included(body.transform, selected.root.transform))
                    continue;
                body.PrepareForRound();
                prepared++;
                double elapsed = (System.Diagnostics.Stopwatch.GetTimestamp() - slice) * 1000.0
                    / System.Diagnostics.Stopwatch.Frequency;
                if (prepared >= Mathf.Max(1, preparationBodiesPerFrame) || elapsed >= Mathf.Max(.5f, preparationBudgetMilliseconds))
                {
                    yield return null;
                    slice = System.Diagnostics.Stopwatch.GetTimestamp();
                    prepared = 0;
                }
            }

            // Start() sees already prepared voxel data, avoiding a full arena rebuild on activation.
            yield return null;
            if (!Available(index))
            {
                loading = null;
                Unavailable();
                yield break;
            }

            selected.root.SetActive(true);
            CurrentArenaIndex = index;
            LaserArena.InvalidateGroundIndex();
            Physics.SyncTransforms();
            foreach (var batch in selected.root.GetComponentsInChildren<LaserWorldRenderBatcher>())
                if (batch.isActiveAndEnabled)
                    batch.Flush();
            Phase = LaserArenaPhase.Playing;
            RemainingTime = Mathf.Max(1, roundIntervalSeconds);
            SelectedSlot = -1;
            candidates.Clear();
            SetBarrier(false);
            loading = null;
            Changed?.Invoke();
        }

        static bool Included(Transform t, Transform map)
        {
            while (t != map && t != null)
            {
                if (!t.gameObject.activeSelf)
                    return false;
                t = t.parent;
            }

            return t == map;
        }

        void Unavailable()
        {
            Phase = LaserArenaPhase.Unavailable;
            RemainingTime = 0;
            SelectedSlot = -1;
            candidates.Clear();
            SetBarrier(true);
            if (hud != null)
                hud.CloseMenus();
            if (started)
                ReturnToLobby();
            retryAt = Time.unscaledTime + 1;
            Changed?.Invoke();
        }

        void SetBarrier(bool active)
        {
            if (lobbyBarrier != null && !transform.IsChildOf(lobbyBarrier.transform))
                lobbyBarrier.SetActive(active);
        }

        void OnDisable()
        {
            StopAllCoroutines();
            loading = null;
            if (Application.isPlaying)
            {
                SetBarrier(false);
                if (votingUI != null && votingUI.IsOpen && hud != null)
                    hud.CloseMenus();
                if (CurrentArenaIndex >= 0 && CurrentArenaIndex < maps.Count && maps[CurrentArenaIndex].root != null)
                    maps[CurrentArenaIndex].root.SetActive(true);
            }
        }
    }
}
