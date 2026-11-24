using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

/// <summary>
/// Controlador del Menú de Opciones para Chaos Cleaners.
/// 
/// Gestiona SOLO: volumen de audio y calidad gráfica + fullscreen.
/// La RESOLUCIÓN se configura desde el Launcher (BootstrapManager la aplica al inicio).
/// 
/// Buenas prácticas:
/// - Persistencia mediante PlayerPrefs (volumen y calidad).
/// - Conversión logarítmica de decibelios para sliders de AudioMixer.
/// - Separación clara: el Launcher controla resolución, el menú in-game el audio y calidad.
/// </summary>
public class OptionsMenu : MonoBehaviour
{
    [Header("Audio y AudioMixer (Opcional)")]
    [Tooltip("AudioMixer del proyecto con parámetros expuestos: 'MasterVolume', 'MusicVolume', 'SfxVolume'")]
    [SerializeField] private AudioMixer mainAudioMixer;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Gráficos")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    // resolutionDropdown eliminado → gestionado por el Launcher

    private const string PREF_MASTER_VOL  = "Setting_MasterVolume";
    private const string PREF_MUSIC_VOL   = "Setting_MusicVolume";
    private const string PREF_SFX_VOL     = "Setting_SfxVolume";
    private const string PREF_FULLSCREEN  = "Setting_Fullscreen";
    private const string PREF_QUALITY     = "Setting_Quality";

    private void Start()
    {
        InitializeAudioSliders();
        InitializeGraphicsSettings();
    }

    // ─── Audio ───────────────────────────────────────────────────────────────

    private void InitializeAudioSliders()
    {
        float master = PlayerPrefs.GetFloat(PREF_MASTER_VOL, 0.8f);
        float music  = PlayerPrefs.GetFloat(PREF_MUSIC_VOL,  0.7f);
        float sfx    = PlayerPrefs.GetFloat(PREF_SFX_VOL,    0.9f);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.SetValueWithoutNotify(master);
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.SetValueWithoutNotify(music);
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.SetValueWithoutNotify(sfx);
            sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
        }

        // Aplicar al AudioMixer inmediatamente
        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSfxVolume(sfx);
    }

    // ─── Gráficos ────────────────────────────────────────────────────────────

    private void InitializeGraphicsSettings()
    {
        // La resolución ya fue aplicada por BootstrapManager; aquí solo calidad + fullscreen
        bool isFullscreen = Screen.fullScreen;

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged.RemoveAllListeners();
            qualityDropdown.ClearOptions();

            List<string> options = new List<string>(QualitySettings.names);
            qualityDropdown.AddOptions(options);

            int currentQuality = PlayerPrefs.GetInt(PREF_QUALITY, QualitySettings.GetQualityLevel());
            currentQuality = Mathf.Clamp(currentQuality, 0, options.Count - 1);
            qualityDropdown.SetValueWithoutNotify(currentQuality);
            qualityDropdown.RefreshShownValue();
            qualityDropdown.onValueChanged.AddListener(SetQuality);
        }
    }

    // ─── Setters públicos (llamados desde botones/sliders de la escena) ───────

    public void SetMasterVolume(float volume)
    {
        PlayerPrefs.SetFloat(PREF_MASTER_VOL, volume);
        ApplyMixerVolume("MasterVolume", volume);
        AudioListener.volume = volume;
    }

    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(PREF_MUSIC_VOL, volume);
        ApplyMixerVolume("MusicVolume", volume);
    }

    public void SetSfxVolume(float volume)
    {
        PlayerPrefs.SetFloat(PREF_SFX_VOL, volume);
        ApplyMixerVolume("SfxVolume", volume);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        PlayerPrefs.SetInt(PREF_FULLSCREEN, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();

        // Al cambiar fullscreen en juego, mantener la resolución del config del launcher
        int w = BootstrapManager.Config?.resolutionWidth ?? Screen.width;
        int h = BootstrapManager.Config?.resolutionHeight ?? Screen.height;
        FullScreenMode mode = isFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        Screen.SetResolution(w, h, mode);
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex, true);
        PlayerPrefs.SetInt(PREF_QUALITY, qualityIndex);
        PlayerPrefs.Save();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void ApplyMixerVolume(string paramName, float linearVolume)
    {
        if (mainAudioMixer == null) return;
        float dB = Mathf.Log10(Mathf.Max(0.0001f, linearVolume)) * 20f;
        mainAudioMixer.SetFloat(paramName, dB);
    }
}
