using System;
using UnityEngine;
using YG;

public class ActivityChestReward : MonoBehaviour
{
    [Serializable] private class SaveData { public int progress,pending; public long revision; }
    [Header("Session"),Tooltip("Existing world and menu state.")] public BuildLevelSession session;
    [Header("Strength"),Tooltip("The player's existing strength progression.")] public StrengthUpgradeSystem strength;
    [Header("Goal"),Tooltip("Placed blocks required for each gift."),Min(1)] public int blocksPerReward=100;
    [Header("Reward"),Tooltip("Fraction of current strength granted per opening."),Range(.01f,1)] public float strengthFraction=.25f;
    [Header("Range"),Tooltip("Horizontal interaction radius."),Min(1)] public float radius=3.5f;
    [Header("Persistence"),Tooltip("Save locally and into YG2 saves.")] public bool saveProgress=true;
    [Header("Save key"),Tooltip("Separate chest progress key.")] public string saveKey="BlockBuilding.ActivityChest.v1";
    private SaveData data;
    private bool dirty,subscribed;
    private float nextSave,nextCloud,openedUntil;
    public event Action Changed;
    public event Action<int> Claimed;
    public int Progress {get{Load();return data.progress;}}
    public int Pending {get{Load();return data.pending;}}
    public bool Opening {get{return Time.unscaledTime<openedUntil;}}
    public int RewardAmount {get{return strength==null?0:CalculateReward(strength.Strength,strengthFraction);}}
    public static int CalculateReward(int value,float fraction)
    { return value==int.MaxValue?0:(int)Math.Min(int.MaxValue-(long)Math.Max(0,value),Math.Max(1,Math.Floor(Math.Max(0,value)*(double)fraction))); }
    public bool IsInRange
    {
        get
        {
            if(session==null || session.runtime.carrier.player==null)return false;
            Vector3 d=session.runtime.carrier.player.position-transform.position;
            return d.x*d.x+d.z*d.z<=radius*radius && Mathf.Abs(d.y)<4;
        }
    }
    private void Load(){if(data==null)data=Read(saveProgress?PlayerPrefs.GetString(saveKey,""):"");}
    private SaveData Read(string json)
    {
        SaveData d=null;try{if(!string.IsNullOrEmpty(json))d=JsonUtility.FromJson<SaveData>(json);}catch(ArgumentException){}
        if(d==null)d=new SaveData();d.progress=Mathf.Clamp(d.progress,0,Mathf.Max(1,blocksPerReward)-1);d.pending=Mathf.Max(0,d.pending);d.revision=Math.Max(0,d.revision);return d;
    }
    private void Start()
    {
        Load();session.runtime.BlockPlaced+=Placed;strength.Changed+=StrengthChanged;YG2.onGetSDKData+=SyncCloud;subscribed=true;
        if(YG2.isSDKEnabled)SyncCloud();Changed?.Invoke();
    }
    private void Placed(BlockType type,Vector3 point){RecordBlock();}
    public void RecordBlock()
    {
        Load();data.progress++;
        if(data.progress>=Mathf.Max(1,blocksPerReward)){data.progress=0;if(data.pending<int.MaxValue)data.pending++;}
        Touch();Changed?.Invoke();
    }
    public bool TryClaim()
    {
        Load();
        if(session==null || session.MenuOpen || session.AdminOverlayOpen || session.training.IsTraining || !IsInRange || Opening || data.pending==0 || RewardAmount<=0)return false;
        int before=strength.Strength;int amount=RewardAmount;
        data.pending--;openedUntil=Time.unscaledTime+2.2f;
        strength.AddStrength(amount);amount=strength.Strength-before;
        Touch();Flush(true);Changed?.Invoke();Claimed?.Invoke(amount);return true;
    }
    private void StrengthChanged(){Changed?.Invoke();}
    private void Touch(){data.revision++;dirty=true;}
    private void Update(){if(dirty && Time.unscaledTime>=nextSave){nextSave=Time.unscaledTime+2;Flush(Time.unscaledTime>=nextCloud);}}
    private void Flush(bool cloud)
    {
        if(!saveProgress || data==null)return;
        string json=JsonUtility.ToJson(data);PlayerPrefs.SetString(saveKey,json);PlayerPrefs.Save();dirty=false;
        if(YG2.isSDKEnabled && YG2.saves!=null && cloud)
        {
            YG2.saves.activityChestJson=json;
            if(strength!=null && !strength.IsTesting)YG2.saves.gymStrength=strength.Strength;
            YG2.SaveProgress();nextCloud=Time.unscaledTime+10;
        }
        else if(YG2.isSDKEnabled && !cloud)dirty=true;
    }
    private void SyncCloud()
    {
        if(!saveProgress || YG2.saves==null)return;Load();var remote=Read(YG2.saves.activityChestJson);
        if(remote.revision>data.revision){data=remote;dirty=true;Flush(false);Changed?.Invoke();}
        else if(data.revision>remote.revision)Flush(true);
    }
    private void OnApplicationPause(bool paused){if(paused)Flush(true);}
    private void OnApplicationQuit(){Flush(true);}
    private void OnDisable(){if(dirty)Flush(true);}
    private void OnDestroy(){if(!subscribed)return;session.runtime.BlockPlaced-=Placed;strength.Changed-=StrengthChanged;YG2.onGetSDKData-=SyncCloud;}
}
