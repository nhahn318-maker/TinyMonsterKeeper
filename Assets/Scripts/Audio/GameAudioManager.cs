using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameAudioManager : MonoBehaviour
{
    private const string MusicEnabledKey = "audio.music.enabled";
    private const string SfxEnabledKey = "audio.sfx.enabled";

    public static GameAudioManager Instance { get; private set; }
    public static bool MusicEnabled => PlayerPrefs.GetInt(MusicEnabledKey, 1) != 0;
    public static bool SfxEnabled => PlayerPrefs.GetInt(SfxEnabledKey, 1) != 0;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioClip background;
    private AudioClip pop;
    private AudioClip levelUp;
    private AudioClip harvest;
    private AudioClip collect;
    private AudioClip cookingComplete;
    private AudioClip cookedItemCollect;
    private readonly HashSet<Button> wiredButtons = new HashSet<Button>();
    private float nextButtonScanAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject root = new GameObject("GameAudioManager");
        DontDestroyOnLoad(root);
        root.AddComponent<GameAudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        musicSource = CreateSource(true);
        sfxSource = CreateSource(false);
        LoadClips();
        ApplySettings();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < nextButtonScanAt)
            return;

        nextButtonScanAt = Time.unscaledTime + 1f;
        WireSceneButtons();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        wiredButtons.RemoveWhere(button => button == null);
        nextButtonScanAt = 0f;
    }

    private void WireSceneButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || !wiredButtons.Add(button))
                continue;

            button.onClick.AddListener(PlayPop);
        }
    }

    private AudioSource CreateSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    private void LoadClips()
    {
        background = Load("background_sound");
        pop = Load("pop_sound");
        levelUp = Load("levelup");
        harvest = Load("harvest_sound");
        collect = Load("collect_coin_and_item");
        cookingComplete = Load("cooking_complete");
        cookedItemCollect = Load("cooked_item_collect");
    }

    private static AudioClip Load(string name)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Audio/SFX/{name}");
        if (clip == null)
            Debug.LogWarning($"Audio clip is missing: Audio/SFX/{name}");
        return clip;
    }

    public static void SetMusicEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        Instance?.ApplySettings();
    }

    public static void SetSfxEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(SfxEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplySettings()
    {
        if (musicSource == null)
            return;

        if (!MusicEnabled || background == null)
        {
            musicSource.Stop();
            return;
        }

        if (musicSource.clip != background)
            musicSource.clip = background;
        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    private void Play(AudioClip clip, float volume = 1f)
    {
        if (SfxEnabled && clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip, volume);
    }

    public static void PlayPop() => Instance?.Play(Instance.pop, 0.8f);
    public static void PlayLevelUp() => Instance?.Play(Instance.levelUp);
    public static void PlayHarvest() => Instance?.Play(Instance.harvest, 0.85f);
    public static void PlayCollect() => Instance?.Play(Instance.collect, 0.9f);
    public static void PlayCookingComplete() => Instance?.Play(Instance.cookingComplete);
    public static void PlayCookedItemCollect() => Instance?.Play(Instance.cookedItemCollect);
}
