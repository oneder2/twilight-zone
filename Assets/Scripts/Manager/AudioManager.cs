using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// --- Define Music Track Identifiers ---
public enum MusicTrack
{
    None, 
    MainMenuTheme, // Main menu
    Stage0_beginner,
    Stage1_crushsis, 
    Stage2_friend, 
    Stage3_crush,
    Stage4_teacher, 
    Stage5_mc, 
    Ending,
    GameOverStinger
    // Add all your distinct music tracks/cues here
}

/// <summary>
/// Helper class to associate MusicTrack enum with AudioClip in the Inspector.
/// </summary>
[System.Serializable]
public class MusicLibraryEntry
{
    public MusicTrack trackId = MusicTrack.None;
    public AudioClip audioClip = null;
    [Range(0f, 1f)]
    public float defaultVolume = 0.8f; // Default playback volume for this track
    public bool loop = true; // Should this track loop by default?
}

/// <summary>
/// Manages background music and sound effect playback, volume control, and fading.
/// Should be a Singleton, likely persistent across scenes (DontDestroyOnLoad).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : Singleton<AudioManager> // Use your Singleton base class
{
    [Header("Volume Control")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1.0f; // Master volume for music
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1.0f;   // Master volume for sound effects
    // Keys for saving/loading volume settings
    private const string MUSIC_VOL_KEY = "MusicVolume";
    private const string SFX_VOL_KEY = "SfxVolume";


    [Header("Music Settings")]
    [Tooltip("Default fade duration when switching tracks.")]
    [SerializeField] private float defaultFadeDuration = 1.0f;
    [Tooltip("AudioSource component used for playing background music.")]
    [SerializeField] private AudioSource musicSource; // Assign or let Awake find it


    [Header("Sound Effects (Optional)")]
    [Tooltip("AudioSource dedicated to playing sound effects (optional). If null, PlayOneShot on music source might be used.")]
    [SerializeField] private AudioSource sfxSource;
    [Range(0f, 1f)]
    [SerializeField] private float defaultSfxVolumeScale = 1.0f; // General scaler for SFX


    [Header("Music Library")]
    [Tooltip("Assign your music tracks here, linking a MusicTrack ID to an AudioClip.")]
    [SerializeField] private List<MusicLibraryEntry> musicLibrary = new List<MusicLibraryEntry>();


    // --- Internal State ---
    private Dictionary<MusicTrack, MusicLibraryEntry> musicLookup;
    private Coroutine musicFadeCoroutine;
    public MusicTrack CurrentTrack { get; private set; } = MusicTrack.None;


    // --- Initialization ---
    protected override void Awake()
    {
        base.Awake(); // Handles Singleton logic and potentially DontDestroyOnLoad

        // Ensure AudioSources are assigned or created
        if (musicSource == null) musicSource = GetComponent<AudioSource>();
        if (musicSource == null) { musicSource = gameObject.AddComponent<AudioSource>(); Debug.Log("AudioManager added missing music AudioSource."); }

        // If no dedicated SFX source assigned, try to find one on the same GameObject or children,
        // otherwise PlaySFX will use the musicSource.
        if (sfxSource == null) sfxSource = GetComponentInChildren<AudioSource>(true); // Find inactive too?
        if (sfxSource == musicSource) sfxSource = null; // Don't use the same source for dedicated SFX

        // Configure music source defaults
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        if (sfxSource != null) sfxSource.playOnAwake = false;

        // Build the lookup dictionary
        BuildLookupDictionary();

        // Load saved volume settings
        LoadVolumeSettings();
    }

    private void BuildLookupDictionary()
    {
        musicLookup = new Dictionary<MusicTrack, MusicLibraryEntry>();
        foreach (var entry in musicLibrary)
        {
            if (entry.trackId != MusicTrack.None && entry.audioClip != null)
            {
                if (!musicLookup.ContainsKey(entry.trackId)) musicLookup.Add(entry.trackId, entry);
                else Debug.LogWarning($"[AudioManager] Duplicate MusicTrack ID '{entry.trackId}' in library.", this);
            }
            else if (entry.trackId != MusicTrack.None) Debug.LogWarning($"[AudioManager] MusicTrack ID '{entry.trackId}' missing AudioClip.", this);
        }
        Debug.Log($"[AudioManager] Music lookup built with {musicLookup.Count} entries.");
    }

    private void LoadVolumeSettings()
    {
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 1.0f); // Default to 1 if not saved
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOL_KEY, 1.0f);   // Default to 1 if not saved
        Debug.Log($"[AudioManager] Loaded Volumes - Music: {musicVolume:P0}, SFX: {sfxVolume:P0}");

        // Apply the loaded volume to the currently playing music (if any)
        ApplyMusicVolume();
        // Note: SFX volume is applied dynamically in PlaySFX
    }

    // --- Public Volume Control Methods ---

    /// <summary>
    /// Sets the master volume for background music.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        Debug.Log($"[AudioManager] Set Music Volume: {musicVolume:P0}");
        ApplyMusicVolume(); // Apply change to current music
        PlayerPrefs.SetFloat(MUSIC_VOL_KEY, musicVolume); // Save setting
        // Optional: PlayerPrefs.Save(); // Force save immediately if needed
        // Optional: Trigger an event
        // EventManager.Instance?.TriggerEvent(new MusicVolumeChangedEvent(musicVolume));
    }

    /// <summary>
    /// Sets the master volume for sound effects.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        Debug.Log($"[AudioManager] Set SFX Volume: {sfxVolume:P0}");
        // SFX volume is applied dynamically in PlaySFX, no immediate action needed on sources typically.
        PlayerPrefs.SetFloat(SFX_VOL_KEY, sfxVolume); // Save setting
        // Optional: PlayerPrefs.Save();
        // Optional: Trigger an event
        // EventManager.Instance?.TriggerEvent(new SfxVolumeChangedEvent(sfxVolume));
    }

    /// <summary>Gets the current master music volume.</summary>
    public float GetCurrentMusicVolume() => musicVolume;

    /// <summary>Gets the current master SFX volume.</summary>
    public float GetCurrentSfxVolume() => sfxVolume;


    // --- Private Volume Application ---

    /// <summary>
    /// Applies the current master musicVolume to the musicSource,
    /// considering the current track's default volume.
    /// Called when volume changes or music starts.
    /// </summary>
    private void ApplyMusicVolume()
    {
        if (musicSource == null) return;

        // Calculate the target volume based on the track's default and the master setting
        float targetVolume = 0f;
        if (CurrentTrack != MusicTrack.None && musicLookup.TryGetValue(CurrentTrack, out var entry))
        {
            targetVolume = entry.defaultVolume * musicVolume;
        }
        else
        {
            // If no track is set, volume should be 0, but master volume is stored anyway.
            // We only apply volume to the source *when playing*.
            // Fades will handle targeting the correct volume.
            // Let's just log the master volume change was registered.
             Debug.Log($"[AudioManager] Master Music Volume is now {musicVolume:P0}. Current track volume will be adjusted.");
             // If music is currently playing and NOT fading, adjust volume directly:
             if (musicSource.isPlaying && musicFadeCoroutine == null) {
                  musicSource.volume = targetVolume;
             }
             return; // Exit, let fades handle their targets
        }

        // If not currently fading, apply the volume directly
        if (musicFadeCoroutine == null && musicSource.isPlaying)
        {
            musicSource.volume = targetVolume;
        }
        // If fading, the fade coroutine should now target this new volume level.
        // (Our current fade coroutines recalculate target based on entry, so they need updating)
        // --> Modification needed in fade coroutines to respect current musicVolume.

         Debug.Log($"[AudioManager] Applied Music Volume. Master: {musicVolume:P0}, Track Default: {entry?.defaultVolume ?? 0f:P0}, Target Source Vol: {targetVolume:P2}");
    }


    // --- Public Music Control Methods (Modified for Volume) ---

    public void PlayMusic(MusicTrack trackId, bool fade = true, float fadeDurationOverride = -1f)
    {
        float duration = (fadeDurationOverride >= 0) ? fadeDurationOverride : defaultFadeDuration;
        if (trackId == MusicTrack.None) { StopMusic(fade, duration); return; }

        if (musicLookup.TryGetValue(trackId, out MusicLibraryEntry entry))
        {
            // Calculate target volume incorporating master volume
            float targetPlayVolume = entry.defaultVolume * musicVolume;

            // Check if already playing this track (compare clip and check if volume is already correct)
            if (CurrentTrack == trackId && musicSource.isPlaying && Mathf.Approximately(musicSource.volume, targetPlayVolume))
            {
                return; // Already playing correctly
            }

            Debug.Log($"[AudioManager] Requesting music: {trackId} (TargetVol: {targetPlayVolume:P0})");
            if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); musicFadeCoroutine = null; }

            musicFadeCoroutine = StartCoroutine(FadeAndPlayCoroutine(entry, targetPlayVolume, fade, duration));
            CurrentTrack = trackId;
        }
        else { Debug.LogWarning($"[AudioManager] Music track '{trackId}' not found."); StopMusic(fade, duration); }
    }

    public void StopMusic(bool fade = true, float fadeDurationOverride = -1f)
    {
        if (!musicSource.isPlaying && CurrentTrack == MusicTrack.None) return;
        float duration = (fadeDurationOverride >= 0) ? fadeDurationOverride : defaultFadeDuration;
        Debug.Log($"[AudioManager] Stopping music{(fade ? " with fade" : "")}.");

        if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); musicFadeCoroutine = null; }

        if (fade && duration > 0 && musicSource.isPlaying)
        {
            musicFadeCoroutine = StartCoroutine(FadeOutCoroutine(duration));
        }
        else { musicSource.Stop(); musicSource.clip = null; CurrentTrack = MusicTrack.None; }
    }


    // --- Coroutines for Fading (Modified for Volume) ---

    private IEnumerator FadeAndPlayCoroutine(MusicLibraryEntry entry, float targetPlayVolume, bool fade, float duration)
    {
        // Fade out current music if necessary
        if (fade && musicSource.isPlaying && duration > 0 && musicSource.volume > 0.01f)
        {
            yield return StartCoroutine(FadeOutCoroutine(duration / 2f));
        } else { musicSource.Stop(); } // Stop immediately if not fading out

        // Set new clip properties
        musicSource.clip = entry.audioClip;
        musicSource.loop = entry.loop;

        // Play and potentially fade in
        if (fade && duration > 0)
        {
            musicSource.volume = 0f; // Start at zero volume for fade in
            musicSource.Play();
            yield return StartCoroutine(FadeInCoroutine(targetPlayVolume, duration / 2f)); // Fade IN to the calculated target volume
        }
        else
        {
            musicSource.volume = targetPlayVolume; // Set volume directly
            musicSource.Play();
        }
        musicFadeCoroutine = null; // Mark fade as complete
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        float startVolume = musicSource.volume;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }
        musicSource.volume = 0f;
        musicSource.Stop();
        musicSource.clip = null;
        CurrentTrack = MusicTrack.None;
        musicFadeCoroutine = null; // Ensure flag is cleared after fade out too
    }

    private IEnumerator FadeInCoroutine(float targetVolume, float duration)
    {
        float startVolume = 0f;
        float timer = 0f;
        if (!musicSource.isPlaying) musicSource.Play(); // Ensure playing

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            yield return null;
        }
        musicSource.volume = targetVolume;
        // Fade in is complete, but music continues playing.
        // musicFadeCoroutine will be set to null by the calling coroutine (FadeAndPlayCoroutine)
    }


    // --- Sound Effects (Modified for Volume) ---

    /// <summary>
    /// Plays a sound effect once, applying the master SFX volume.
    /// </summary>
    /// <param name="sfxClip">The AudioClip to play.</param>
    /// <param name="volumeScale">Optional multiplier relative to default SFX volume.</param>
    public void PlaySFX(AudioClip sfxClip, float volumeScale = 1.0f)
    {
        if (sfxClip == null) { Debug.LogWarning("[AudioManager] PlaySFX called with a null AudioClip."); return; }

        // Calculate final volume including master SFX volume and individual scale
        float finalVolume = defaultSfxVolumeScale * volumeScale * sfxVolume;

        // Use dedicated source if available, otherwise use PlayOneShot on music source
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(sfxClip, finalVolume); // PlayOneShot volume is absolute for dedicated source
        }
        else
        {
            // PlayOneShot volume parameter acts as a *scale* of the AudioSource's current volume.
            // To make it behave predictably relative to master SFX volume, we pass the calculated finalVolume
            // assuming the musicSource volume might be anything (or we could temporarily set musicSource volume).
            // A simpler approach if no sfxSource exists is just:
             musicSource.PlayOneShot(sfxClip, finalVolume); // Hope musicSource volume isn't 0!
             // A more robust way without sfxSource is harder. A dedicated source is recommended.
        }
    }
}
