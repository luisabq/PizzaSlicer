using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    //PlayerPrefs keys, SoundManager reads the same ones when the game starts
    public const string MasterVolumeKey = "MasterVolume";
    public const string MusicVolumeKey = "MusicVolume";
    public const string VibrationKey = "Vibration";

    //other scripts check this before vibrating the phone (on by default)
    public static bool VibrationEnabled => PlayerPrefs.GetInt(VibrationKey, 1) == 1;

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider; //controls every sound in the game
    [SerializeField] private Slider musicVolumeSlider; //controls only the SoundManager music

    [Header("Vibration")]
    [SerializeField] private Toggle vibrationToggle; //turns phone vibration on or off

    // Connects the sliders to their volume functions once
    private void Awake()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        else
        {
            Debug.LogWarning("[SettingsMenu] Master Volume Slider is not assigned.");
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        else
        {
            Debug.LogWarning("[SettingsMenu] Music Volume Slider is not assigned.");
        }

        if (vibrationToggle != null)
        {
            vibrationToggle.onValueChanged.AddListener(SetVibration);
        }
        else
        {
            Debug.LogWarning("[SettingsMenu] Vibration Toggle is not assigned.");
        }
    }

    // Moves the sliders to the saved values every time the panel opens
    private void OnEnable()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(AudioListener.volume);
        }

        if (musicVolumeSlider != null && SoundManager.Instance != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(SoundManager.Instance.GetMusicVolume());
        }

        if (vibrationToggle != null)
        {
            vibrationToggle.SetIsOnWithoutNotify(VibrationEnabled);
        }
    }

    // Writes the settings to disk when the panel closes
    private void OnDisable()
    {
        PlayerPrefs.Save();
    }

    // Sets the volume of the whole game and remembers it
    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(MasterVolumeKey, volume);
    }

    // Sets the music volume and remembers it
    public void SetMusicVolume(float volume)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMusicVolume(volume);
        }

        PlayerPrefs.SetFloat(MusicVolumeKey, volume);
    }

    // Turns phone vibration on or off and remembers it
    public void SetVibration(bool isOn)
    {
        PlayerPrefs.SetInt(VibrationKey, isOn ? 1 : 0);
    }
}
