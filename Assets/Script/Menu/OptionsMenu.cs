using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

/// <summary>
/// Controlador del Menú de Opciones para Chaos Cleaners.
/// Gestiona volumen de audio (AudioMixer y PlayerPrefs), pantalla completa,
/// calidad gráfica y selector dinámico de resolución.
/// 
/// Buenas prácticas:
/// - Persistencia de datos mediante PlayerPrefs: los ajustes se guardan y cargan entre sesiones.
/// - Conversión logarítmica de decibelios para sliders de AudioMixer (0.0001 a 1 -> -80dB a 0dB).
/// - Detección automática y filtrado de resoluciones soportadas por la pantalla del usuario.
/// - Compatibilidad con pantallas 16:9 y manejo adecuado de FullScreenMode en Windows (ExclusiveFullScreen / Windowed).
/// </summary>
public class OptionsMenu : MonoBehaviour
{
    [Header("Audio y AudioMixer (Opcional)")]
    [Tooltip("AudioMixer del proyecto con parámetros expuestos: 'MasterVolume', 'MusicVolume', 'SfxVolume'")]
    [SerializeField] private AudioMixer mainAudioMixer;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Gráficos y Pantalla")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private Resolution[] availableResolutions;
    private const string PREF_MASTER_VOL = "Setting_MasterVolume";
    private const string PREF_MUSIC_VOL = "Setting_MusicVolume";
    private const string PREF_SFX_VOL = "Setting_SfxVolume";
    private const string PREF_FULLSCREEN = "Setting_Fullscreen";
    private const string PREF_QUALITY = "Setting_Quality";
    private const string PREF_RES_WIDTH = "Setting_ResolutionWidth";
    private const string PREF_RES_HEIGHT = "Setting_ResolutionHeight";

    private void Awake()
    {
        // Resolución nativa o 1920x1080 como base
        int nativeWidth = Screen.currentResolution.width > 0 ? Screen.currentResolution.width : 1920;
        int nativeHeight = Screen.currentResolution.height > 0 ? Screen.currentResolution.height : 1080;

        int width = PlayerPrefs.GetInt(PREF_RES_WIDTH, nativeWidth);
        int height = PlayerPrefs.GetInt(PREF_RES_HEIGHT, nativeHeight);

        // Sanity check: evitar resoluciones 4:3 diminutas heredadas por defecto (ej. 640x480)
        if (width < 1280 || height < 720)
        {
            width = nativeWidth;
            height = nativeHeight;
            PlayerPrefs.SetInt(PREF_RES_WIDTH, width);
            PlayerPrefs.SetInt(PREF_RES_HEIGHT, height);
            PlayerPrefs.Save();
        }

        bool isFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, 1) == 1;
        FullScreenMode mode = isFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;

        Screen.SetResolution(width, height, mode);
    }

    private void Start()
    {
        InitializeAudioSliders();
        InitializeGraphicsSettings();
        InitializeResolutions();
    }

    private void InitializeAudioSliders()
    {
        float master = PlayerPrefs.GetFloat(PREF_MASTER_VOL, 0.8f);
        float music = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 0.7f);
        float sfx = PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.9f);

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

        // Aplicar valores cargados
        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSfxVolume(sfx);
    }

    private void InitializeGraphicsSettings()
    {
        // Pantalla completa
        bool isFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        // Calidad gráfica
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

    private void InitializeResolutions()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.ClearOptions();

        Resolution[] allResolutions = Screen.resolutions;
        List<Resolution> uniqueResolutions = new List<Resolution>();
        List<string> resolutionStrings = new List<string>();

        // Si Screen.resolutions no devuelve nada (común en ciertos editores), creamos lista estándar 16:9
        if (allResolutions == null || allResolutions.Length == 0)
        {
            (int w, int h)[] standard = new (int, int)[]
            {
                (1280, 720),
                (1366, 768),
                (1600, 900),
                (1920, 1080),
                (2560, 1440),
                (3840, 2160)
            };

            foreach (var s in standard)
            {
                Resolution r = new Resolution { width = s.w, height = s.h };
                uniqueResolutions.Add(r);
                resolutionStrings.Add($"{s.w} x {s.h}");
            }
        }
        else
        {
            // Filtrar duplicados de refresco y ordenar de menor a mayor
            for (int i = 0; i < allResolutions.Length; i++)
            {
                var res = allResolutions[i];
                // Solo añadir resoluciones utilizables (>= 1280x720 para juego moderno en PC)
                if (res.width >= 1280 && res.height >= 720)
                {
                    if (!uniqueResolutions.Exists(r => r.width == res.width && r.height == res.height))
                    {
                        uniqueResolutions.Add(res);
                        resolutionStrings.Add($"{res.width} x {res.height}");
                    }
                }
            }

            // Si el filtro eliminó todo, agregar al menos las resoluciones del monitor
            if (uniqueResolutions.Count == 0)
            {
                for (int i = 0; i < allResolutions.Length; i++)
                {
                    var res = allResolutions[i];
                    if (!uniqueResolutions.Exists(r => r.width == res.width && r.height == res.height))
                    {
                        uniqueResolutions.Add(res);
                        resolutionStrings.Add($"{res.width} x {res.height}");
                    }
                }
            }
        }

        int nativeWidth = Screen.currentResolution.width > 0 ? Screen.currentResolution.width : 1920;
        int nativeHeight = Screen.currentResolution.height > 0 ? Screen.currentResolution.height : 1080;
        int savedWidth = PlayerPrefs.GetInt(PREF_RES_WIDTH, nativeWidth);
        int savedHeight = PlayerPrefs.GetInt(PREF_RES_HEIGHT, nativeHeight);

        int currentResIndex = -1;
        for (int i = 0; i < uniqueResolutions.Count; i++)
        {
            if (uniqueResolutions[i].width == savedWidth && uniqueResolutions[i].height == savedHeight)
            {
                currentResIndex = i;
                break;
            }
        }

        // Si no se encuentra exactamente la guardada, buscar 1920x1080 o usar la última (mayor resolución)
        if (currentResIndex == -1)
        {
            for (int i = 0; i < uniqueResolutions.Count; i++)
            {
                if (uniqueResolutions[i].width == 1920 && uniqueResolutions[i].height == 1080)
                {
                    currentResIndex = i;
                    break;
                }
            }
            if (currentResIndex == -1)
            {
                currentResIndex = Mathf.Max(0, uniqueResolutions.Count - 1);
            }
        }

        availableResolutions = uniqueResolutions.ToArray();
        resolutionDropdown.AddOptions(resolutionStrings);
        resolutionDropdown.SetValueWithoutNotify(currentResIndex);
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetMasterVolume(float volume)
    {
        PlayerPrefs.SetFloat(PREF_MASTER_VOL, volume);
        ApplyMixerVolume("MasterVolume", volume);
        AudioListener.volume = volume; // Fallback directo si no hay AudioMixer
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

    private void ApplyMixerVolume(string paramName, float linearVolume)
    {
        if (mainAudioMixer == null) return;
        // Conversión estándar de slider lineal (0.0001 a 1) a decibelios (-80dB a 0dB)
        float dB = Mathf.Log10(Mathf.Max(0.0001f, linearVolume)) * 20f;
        mainAudioMixer.SetFloat(paramName, dB);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        PlayerPrefs.SetInt(PREF_FULLSCREEN, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();

        int width = PlayerPrefs.GetInt(PREF_RES_WIDTH, Screen.currentResolution.width > 0 ? Screen.currentResolution.width : 1920);
        int height = PlayerPrefs.GetInt(PREF_RES_HEIGHT, Screen.currentResolution.height > 0 ? Screen.currentResolution.height : 1080);
        FullScreenMode mode = isFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;

        Screen.SetResolution(width, height, mode);
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt(PREF_QUALITY, qualityIndex);
        PlayerPrefs.Save();
    }

    public void SetResolution(int resolutionIndex)
    {
        if (availableResolutions != null && resolutionIndex >= 0 && resolutionIndex < availableResolutions.Length)
        {
            Resolution res = availableResolutions[resolutionIndex];
            bool isFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
            FullScreenMode mode = isFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;

            Screen.SetResolution(res.width, res.height, mode, res.refreshRateRatio);
            PlayerPrefs.SetInt(PREF_RES_WIDTH, res.width);
            PlayerPrefs.SetInt(PREF_RES_HEIGHT, res.height);
            PlayerPrefs.Save();
        }
    }
}
