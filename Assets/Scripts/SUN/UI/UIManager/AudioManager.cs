using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

/// <summary>
/// ตัวคุมเสียงของเกม (Singleton ข้าม Scene วางไว้ใน MainMenu): เพลงตาม Scene, SFX/UI, และ Volume ผ่าน Audio Mixer
/// ชื่อ Scene ใช้ค่าจาก SceneNames
/// </summary>
public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music (ตาม Scene)")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip mainMenuMusic;
    [FormerlySerializedAs("lobbyMusic")]
    [SerializeField] private AudioClip baseMusic;
    [SerializeField] private AudioClip tutorialMusic;
    [SerializeField] private AudioClip level1Music;
    [SerializeField] private AudioClip level2Music;
    [SerializeField] private AudioClip level3Music;

    [Header("Audio Mixer Groups")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;

    [Header("Mixer parameters (ชื่อ Exposed Parameter ใน Audio Mixer)")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    [SerializeField] private string musicVolumeParameter = "MusicVolume";
    [SerializeField] private string sfxVolumeParameter = "SfxVolume";
    [SerializeField] private string uiVolumeParameter = "UIVolume";

    [Header("Sound Effects")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;
    [Tooltip("เล่นตอนจบด่านสำเร็จ (LevelManager.CompleteLevel)")]
    [SerializeField] private AudioClip victorySfx;
    [Tooltip("เล่นตอนรอบล้มเหลว/ผู้เล่นตาย (LevelManager.OnRunFailed)")]
    [SerializeField] private AudioClip loseSfx;
    [SerializeField] private AudioClip itemPickupSfx;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip uiHoverSfx;
    [SerializeField] private AudioClip uiClickSfx;

    private const string MasterPrefsKey = "MasterVolume";
    private const string MusicPrefsKey = "MusicVolume";
    private const string SfxPrefsKey = "SfxVolume";
    private const string UiPrefsKey = "UIVolume";

    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private float uiVolume = 1f;

    private bool settingsDirty;
    private bool warnedMissingMixer;
    private readonly HashSet<string> warnedParameters = new HashSet<string>();

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public float UIVolume => uiVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null || sfxSource == null)
        {
            Debug.LogError("AudioManager needs Music Source and SFX Source assigned.", this);
            return;
        }

        musicSource.outputAudioMixerGroup = musicGroup;
        sfxSource.outputAudioMixerGroup = sfxGroup;

        if (uiSource != null)
            uiSource.outputAudioMixerGroup = uiGroup;

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource.playOnAwake = false;

        if (uiSource != null)
            uiSource.playOnAwake = false;

        masterVolume = PlayerPrefs.GetFloat(MasterPrefsKey, 1f);
        musicVolume = PlayerPrefs.GetFloat(MusicPrefsKey, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SfxPrefsKey, 1f);
        uiVolume = PlayerPrefs.GetFloat(UiPrefsKey, 1f);

        ApplyMixerVolume(masterVolumeParameter, masterVolume);
        ApplyMixerVolume(musicVolumeParameter, musicVolume);
        ApplyMixerVolume(sfxVolumeParameter, sfxVolume);
        ApplyMixerVolume(uiVolumeParameter, uiVolume);

        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SaveSettings();
        Instance = null;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveSettings();
    }

    private void OnApplicationQuit()
    {
        SaveSettings();
    }

    // ---------------------------------------------------------------- Music

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Scene ที่โหลดซ้อน (เช่น OptionsMenu) ไม่ต้องเปลี่ยนเพลง
        if (mode == LoadSceneMode.Additive)
            return;

        SaveSettings();
        PlayMusicForScene(scene.name);
    }

    private void PlayMusicForScene(string sceneName)
    {
        if (musicSource == null || !TryGetMusicForScene(sceneName, out AudioClip nextClip))
            return; // Scene ที่ไม่รู้จัก: ปล่อยเพลงเดิมเล่นต่อ

        if (musicSource.clip == nextClip)
            return;

        musicSource.Stop();
        musicSource.clip = nextClip;

        if (nextClip != null)
            musicSource.Play();
    }

    private bool TryGetMusicForScene(string sceneName, out AudioClip clip)
    {
        clip = null;

        if (sceneName == SceneNames.MainMenu) { clip = mainMenuMusic; return true; }
        if (sceneName == SceneNames.Base) { clip = baseMusic; return true; }
        if (sceneName == SceneNames.Tutorial) { clip = tutorialMusic; return true; }

        for (int i = 0; i < SceneNames.Levels.Length; i++)
        {
            if (sceneName != SceneNames.Levels[i])
                continue;

            clip = i == 0 ? level1Music : i == 1 ? level2Music : i == 2 ? level3Music : null;
            return true;
        }

        return false;
    }

    // ------------------------------------------------------------------ SFX

    public void PlayVictorySfx() => PlaySfx(victorySfx);

    public void PlayLoseSfx() => PlaySfx(loseSfx);

    /// <summary>เล่นเสียงเก็บของ ถ้าไม่ระบุ clip จะใช้เสียงเริ่มต้นของ AudioManager</summary>
    public void PlayItemPickupSfx(AudioClip clip = null)
    {
        PlaySfx(clip != null ? clip : itemPickupSfx);
    }

    public void PlayUISfx(AudioClip clip)
    {
        if (clip != null && uiSource != null)
            uiSource.PlayOneShot(clip);
    }

    // Helper สำหรับปุ่ม (UIButtonHoverEffect, PressAnyButtonScreen)
    public void PlayUIHover() => PlayUISfx(uiHoverSfx);
    public void PlayUIClick() => PlayUISfx(uiClickSfx);

    private void PlaySfx(AudioClip clip)
    {
        // ไม่ใส่ clip = ไม่เล่น (ถือว่าเสียงนี้เป็น optional)
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// เล่นเสียงเก็บของผ่าน SFX mixer ถ้ามี AudioManager (Slider SFX จึงคุมเสียงนี้ได้)
    /// ถ้าไม่มี (เช่น กด Play จากด่านโดยตรง) ใช้ PlayClipAtPoint แทน
    /// </summary>
    public static void PlayPickup(AudioClip clip, Vector3 position)
    {
        if (clip == null)
            return;

        if (Instance != null && Instance.sfxSource != null)
            Instance.sfxSource.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, position);
    }

    /// <summary>
    /// ให้ AudioSource ของสคริปต์อื่น (เสียงเดิน, เสียงปืน, เสียงศัตรู) ออกเสียงผ่านกลุ่ม SFX
    /// เพื่อให้ Slider SFX คุมเสียงเหล่านี้ได้ด้วย ถ้าไม่มี AudioManager จะไม่ทำอะไร
    /// </summary>
    public static void RouteToSfx(AudioSource source)
    {
        if (source != null && Instance != null && Instance.sfxGroup != null)
            source.outputAudioMixerGroup = Instance.sfxGroup;
    }

    // --------------------------------------------------------------- Volume

    public void SetMasterVolume(float value)
    {
        masterVolume = SetVolume(masterVolumeParameter, MasterPrefsKey, value);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = SetVolume(musicVolumeParameter, MusicPrefsKey, value);
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = SetVolume(sfxVolumeParameter, SfxPrefsKey, value);
    }

    public void SetUIVolume(float value)
    {
        uiVolume = SetVolume(uiVolumeParameter, UiPrefsKey, value);
    }

    /// <summary>เขียน PlayerPrefs ลงดิสก์ (เรียกตอนปิดหน้า Option / เปลี่ยน Scene / ออกเกม)</summary>
    public void SaveSettings()
    {
        if (!settingsDirty)
            return;

        PlayerPrefs.Save();
        settingsDirty = false;
    }

    private float SetVolume(string parameter, string prefsKey, float value)
    {
        value = Mathf.Clamp01(value);
        ApplyMixerVolume(parameter, value);

        PlayerPrefs.SetFloat(prefsKey, value);
        settingsDirty = true;
        return value;
    }

    private void ApplyMixerVolume(string parameter, float value)
    {
        if (audioMixer == null)
        {
            if (!warnedMissingMixer)
            {
                warnedMissingMixer = true;
                Debug.LogWarning("Audio Mixer is not assigned.", this);
            }

            return;
        }

        // Slider 0 คือปิดเสียง; ค่าอื่นแปลงเป็น decibels
        float decibels = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;

        if (!audioMixer.SetFloat(parameter, decibels) && warnedParameters.Add(parameter))
            Debug.LogWarning($"ไม่พบ Exposed Parameter '{parameter}' ตรวจชื่อใน Audio Mixer", this);
    }
}