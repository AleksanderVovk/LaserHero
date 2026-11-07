using System;
using UnityEngine;

[Serializable]
public class StrengthLevel
{
    [Header("Threshold"), Tooltip("Total strength required to reach this level."), Min(0)]
    public int requiredStrength;
    [Header("Capacity"), Tooltip("Maximum carried cubes at this level."), Range(1, 100)]
    public int capacity = 1;
}

public class StrengthUpgradeSystem : MonoBehaviour
{
    [Header("Progression"), Tooltip("Strictly increasing thresholds, starting at zero. Capacity cannot decrease.")]
    public StrengthLevel[] levels = {
        new StrengthLevel { requiredStrength = 0, capacity = 1 }, new StrengthLevel { requiredStrength = 5, capacity = 2 },
        new StrengthLevel { requiredStrength = 15, capacity = 3 }, new StrengthLevel { requiredStrength = 30, capacity = 4 },
        new StrengthLevel { requiredStrength = 50, capacity = 5 }, new StrengthLevel { requiredStrength = 80, capacity = 6 },
        new StrengthLevel { requiredStrength = 120, capacity = 7 }, new StrengthLevel { requiredStrength = 170, capacity = 8 }
    };
    [Header("Persistence"), Tooltip("Save strength in PlayerPrefs. WebGL stores locally in this browser.")]
    public bool saveProgress = true;
    [Header("Save key"), Tooltip("Unique key for this game's strength save.")]
    public string saveKey = "BlockBuilding.Strength.v1";
    [Header("Feedback"), Tooltip("AudioSource for the assigned stock level-up clip.")]
    public AudioSource levelAudio;
    [Header("Effect"), Tooltip("Optional level-up particle system.")]
    public ParticleSystem levelEffect;
    private int strength, index;
    [Header("Level sound"), Tooltip("Downloaded stock audio used when a new strength level is reached.")]
    public AudioClip levelUpClip;
    [Header("Extended progression"), Tooltip("Continue gaining levels after the authored table ends.")]
    public bool extendLevels=true;
    [Header("Extra level cost"), Tooltip("Each level after the table increases the required step by this amount."), Min(1)]
    public int extraCostGrowth=25;
    private int extraLevels;
    private bool initialized;
    private bool testing; private int beforeTesting;
    public event Action Changed;
    public event Action LeveledUp;
    public bool IsTesting { get { return testing; } }
    public void ApplySavedStrength(int value)
    {
        Initialize(); if(testing) return; strength=Mathf.Max(0,value); Recalculate();
        if(saveProgress) PlayerPrefs.SetInt(saveKey,strength);
        Changed?.Invoke();
    }
    public int Strength { get { return strength; } }
    public int Level { get { return index + 1 + extraLevels; } }
    public int Capacity { get { return Mathf.Min(PlayerBlockCarrier.AdminMaximum,levels[index].capacity+extraLevels); } }
    public int MaximumCapacity { get { return extendLevels?PlayerBlockCarrier.AdminMaximum:levels[levels.Length-1].capacity; } }
    public int NextThreshold { get { if(index+1<levels.Length) return levels[index+1].requiredStrength; if(!extendLevels || strength==int.MaxValue) return -1; return (int)Math.Min(int.MaxValue,TailThreshold(extraLevels+1)); } }
    public float Progress { get { return NextThreshold<0?1:Mathf.InverseLerp((int)TailThreshold(extraLevels),NextThreshold,strength); } }
    private long TailThreshold(int extra)
    {
        if(index<levels.Length-1) return levels[index].requiredStrength;
        int last=levels.Length-1;
        long step=last>0?Math.Max(1,levels[last].requiredStrength-levels[last-1].requiredStrength):5;
        return levels[last].requiredStrength+step*extra+(long)Math.Max(1,extraCostGrowth)*extra*(extra+1)/2;
    }
    private void Awake() { Initialize(); }
    public void Initialize()
    {
        if (initialized) return;
        if (levels == null || levels.Length == 0 || levels[0] == null || levels[0].requiredStrength != 0) throw new ArgumentException("Strength table must start at zero.");
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null || levels[i].capacity < 1 || levels[i].capacity > 100 || (i > 0 &&
                (levels[i].requiredStrength <= levels[i-1].requiredStrength || levels[i].capacity < levels[i-1].capacity)))
                throw new ArgumentException("Invalid strength progression table.");
        }
        strength = saveProgress ? Mathf.Max(0, PlayerPrefs.GetInt(saveKey, 0)) : 0;
        Recalculate();
        initialized = true;
    }
    public void AddStrength(int amount)
    {
        Initialize();
        if (amount <= 0) return;
        int previous = Level;
        strength = (int)Math.Min(int.MaxValue, (long)strength + amount);
        Recalculate();
        if (saveProgress && !testing) PlayerPrefs.SetInt(saveKey, strength);
        Changed?.Invoke();
        if (Level != previous)
        {
            if (levelAudio != null && levelUpClip != null) levelAudio.PlayOneShot(levelUpClip,0.5f);
            if (levelEffect != null) levelEffect.Play();
            LeveledUp?.Invoke();
        }
    }
    public void SetStrengthForTesting(int value)
    {
        Initialize(); if(!testing) { beforeTesting=strength; testing=true; }
        strength=Mathf.Max(0,value); Recalculate(); Changed?.Invoke();
    }
    public void RestoreStrengthAfterTesting()
    {
        if(!testing) return; strength=beforeTesting; testing=false; Recalculate(); Changed?.Invoke();
    }
    private void Recalculate()
    {
        index=0; extraLevels=0;
        while(index+1<levels.Length && strength>=levels[index+1].requiredStrength) index++;
        if(extendLevels && index==levels.Length-1)
        {
            int lo=0,hi=65536;
            while(lo<hi) { int mid=lo+(hi-lo+1)/2; if(TailThreshold(mid)<=strength) lo=mid; else hi=mid-1; }
            extraLevels=lo;
        }
    }
    private void OnApplicationPause(bool paused) { if (paused && saveProgress) PlayerPrefs.Save(); }
    private void OnDisable() { if (saveProgress && initialized) PlayerPrefs.Save(); }
}
