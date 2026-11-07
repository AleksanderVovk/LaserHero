using TMPro;
using UnityEngine;

namespace LaserHero
{
    [DefaultExecutionOrder(-100)]
    public sealed class LaserGameSettings : MonoBehaviour
    {
        public const string SensitivityKey = "LaserHero.Settings.CameraSensitivity";
        public const string MusicKey = "LaserHero.Settings.MusicVolume";
        public const string SoundKey = "LaserHero.Settings.SoundVolume";
        public PlayerCharacteristic playerCharacteristic;
        [Tooltip("Separate 2D looping music source. Assign your music clip here.")]
        public AudioSource musicSource;
        public UnityEngine.UI.Slider sensitivitySlider, musicSlider, soundSlider;
        public TMP_Text sensitivityValue, musicValue, soundValue;
        public float defaultSensitivity = 200, minSensitivity = 25, maxSensitivity = 600;
        [Range(0, 1)]
        public float defaultMusicVolume = .65f, defaultSoundVolume = 1;
        public float Sensitivity { get; private set; }
        public float MusicVolume { get; private set; }
        public float SoundVolume { get; private set; }

        bool pendingSave;
        float saveAfter;
        void Awake()
        {
            if (playerCharacteristic == null)
                playerCharacteristic = FindFirstObjectByType<PlayerCharacteristic>();
            Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, defaultSensitivity), minSensitivity, maxSensitivity);
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, defaultMusicVolume));
            SoundVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SoundKey, defaultSoundVolume));
            if (musicSource != null)
            {
                musicSource.loop = true;
                musicSource.spatialBlend = 0;
                musicSource.ignoreListenerVolume = true;
            }

            Apply();
            SyncUI();
        }

        void OnEnable()
        {
            if (sensitivitySlider != null)
                sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
            if (musicSlider != null)
                musicSlider.onValueChanged.AddListener(SetMusicVolume);
            if (soundSlider != null)
                soundSlider.onValueChanged.AddListener(SetSoundVolume);
        }

        void Start()
        {
            if (musicSource != null && musicSource.clip != null && !musicSource.isPlaying)
                musicSource.Play();
        }

        void Update()
        {
            if (pendingSave && Time.unscaledTime >= saveAfter)
                SaveNow();
        }

        void OnDisable()
        {
            if (sensitivitySlider != null)
                sensitivitySlider.onValueChanged.RemoveListener(SetSensitivity);
            if (musicSlider != null)
                musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
            if (soundSlider != null)
                soundSlider.onValueChanged.RemoveListener(SetSoundVolume);
            SaveNow();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveNow();
        }

        void OnApplicationQuit()
        {
            SaveNow();
        }

        public void SetSensitivity(float value)
        {
            Sensitivity = Mathf.Clamp(value, minSensitivity, maxSensitivity);
            Changed();
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            Changed();
        }

        public void SetSoundVolume(float value)
        {
            SoundVolume = Mathf.Clamp01(value);
            Changed();
        }

        void Changed()
        {
            Apply();
            SyncUI();
            PlayerPrefs.SetFloat(SensitivityKey, Sensitivity);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            PlayerPrefs.SetFloat(SoundKey, SoundVolume);
            pendingSave = true;
            saveAfter = Time.unscaledTime + .4f;
        }

        void Apply()
        {
            if (playerCharacteristic != null)
                playerCharacteristic.CAMERA_SENSITIVITY = Sensitivity;
            // Effects preserve their individual levels, including dynamically spawned sounds.
            AudioListener.volume = SoundVolume;
            if (musicSource != null)
                musicSource.volume = MusicVolume;
        }

        void SyncUI()
        {
            if (sensitivitySlider != null)
                sensitivitySlider.SetValueWithoutNotify(Sensitivity);
            if (musicSlider != null)
                musicSlider.SetValueWithoutNotify(MusicVolume);
            if (soundSlider != null)
                soundSlider.SetValueWithoutNotify(SoundVolume);
            if (sensitivityValue != null)
                sensitivityValue.text = Mathf.RoundToInt(Sensitivity).ToString();
            if (musicValue != null)
                musicValue.text = Mathf.RoundToInt(MusicVolume * 100) + "%";
            if (soundValue != null)
                soundValue.text = Mathf.RoundToInt(SoundVolume * 100) + "%";
        }

        public void SaveNow()
        {
            if (!pendingSave)
                return;
            PlayerPrefs.Save();
            pendingSave = false;
        }
    }
}
