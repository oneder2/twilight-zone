using UnityEngine;
using UnityEngine.UI; // Required for Slider

public class SettingMenu : MonoBehaviour // Or your settings menu script name
{
    [Header("Audio Settings UI")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    // Optional: Add TextMeshProUGUI labels to show percentage
    // [SerializeField] private TMPro.TextMeshProUGUI musicVolumeLabel;
    // [SerializeField] private TMPro.TextMeshProUGUI sfxVolumeLabel;

    void Start()
    {
        // Ensure AudioManager instance exists before accessing it
        if (AudioManager.Instance == null)
        {
            Debug.LogError("AudioManager instance not found! Cannot initialize settings menu audio controls.");
            // Disable sliders if manager is missing?
            if (musicVolumeSlider != null) musicVolumeSlider.interactable = false;
            if (sfxVolumeSlider != null) sfxVolumeSlider.interactable = false;
            return;
        }

        // Initialize sliders with current values from AudioManager
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.value = AudioManager.Instance.GetCurrentMusicVolume();
            // Update label if exists
            // UpdateMusicVolumeLabel(musicVolumeSlider.value);
            // Add listener for value changes
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.value = AudioManager.Instance.GetCurrentSfxVolume();
            // Update label if exists
            // UpdateSfxVolumeLabel(sfxVolumeSlider.value);
            // Add listener for value changes
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }
    }

    /// <summary>
    /// Called when the music volume slider's value changes.
    /// </summary>
    /// <param name="value">The new slider value (0.0 to 1.0).</param>
    public void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
        // Update label if exists
        // UpdateMusicVolumeLabel(value);
    }

    /// <summary>
    /// Called when the SFX volume slider's value changes.
    /// </summary>
    /// <param name="value">The new slider value (0.0 to 1.0).</param>
    public void OnSfxVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxVolume(value);
            // Optional: Play a test sound effect when slider changes
            // AudioManager.Instance.PlaySFX(testSfxClip); // Need a reference to a test clip
        }
        // Update label if exists
        // UpdateSfxVolumeLabel(value);
    }

    // Optional methods to update text labels
    /*
    private void UpdateMusicVolumeLabel(float value)
    {
        if (musicVolumeLabel != null) musicVolumeLabel.text = $"{Mathf.RoundToInt(value * 100)}%";
    }
    private void UpdateSfxVolumeLabel(float value)
    {
        if (sfxVolumeLabel != null) sfxVolumeLabel.text = $"{Mathf.RoundToInt(value * 100)}%";
    }
    */

    // Remember to remove listeners if the settings menu object is destroyed before AudioManager
    // void OnDestroy()
    // {
    //     if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
    //     if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
    // }
}
