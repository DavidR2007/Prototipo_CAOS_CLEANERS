using System.IO;
using UnityEngine;

/// <summary>
/// Lector de configuración del Launcher para Chaos Cleaners.
/// 
/// Se ejecuta en la escena MAIN_MENU (índice 0) antes que ningún otro script
/// (ExecutionOrder -100). Lee el archivo launcher_config.json que el Launcher
/// deposita junto al ejecutable del juego y aplica resolución, pantalla completa
/// y calidad gráfica.
/// 
/// Si el archivo no existe (ej. primer arranque sin launcher o desde el editor),
/// usa los valores guardados en PlayerPrefs o la resolución nativa del monitor.
/// </summary>
[DefaultExecutionOrder(-100)]
public class BootstrapManager : MonoBehaviour
{
    // Singleton: solo una instancia, persiste entre escenas
    public static BootstrapManager Instance { get; private set; }

    // Configuración activa cargada desde el JSON (accesible globalmente)
    public static LauncherConfig Config { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Config = LoadConfig();
        ApplyConfig(Config);
    }

    // ─── Ruta del JSON ───────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve la ruta absoluta del launcher_config.json.
    /// En build: junto al .exe del juego (un nivel arriba de _Data/).
    /// En editor: en la raíz del proyecto.
    /// </summary>
    public static string GetConfigPath()
    {
#if UNITY_EDITOR
        return Path.Combine(Application.dataPath, "..", "launcher_config.json");
#else
        // Application.dataPath en build standalone = ruta a ChaosCleaners_Data/
        // El .exe y el launcher están un nivel arriba
        return Path.Combine(Application.dataPath, "..", "launcher_config.json");
#endif
    }

    // ─── Carga ───────────────────────────────────────────────────────────────

    private LauncherConfig LoadConfig()
    {
        string path = GetConfigPath();

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                LauncherConfig loaded = JsonUtility.FromJson<LauncherConfig>(json);
                Debug.Log($"[BootstrapManager] Config del Launcher cargada: {loaded.resolutionWidth}x{loaded.resolutionHeight}, fullscreen={loaded.fullscreen}, quality={loaded.qualityLevel}");
                return loaded;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BootstrapManager] Error al leer launcher_config.json: {ex.Message}. Usando fallback.");
            }
        }
        else
        {
            Debug.Log("[BootstrapManager] launcher_config.json no encontrado. Usando resolución nativa.");
        }

        // Fallback: resolución nativa del monitor
        return LauncherConfig.CreateDefault();
    }

    // ─── Aplicación ──────────────────────────────────────────────────────────

    private void ApplyConfig(LauncherConfig cfg)
    {
        // Sanity check: evitar resoluciones extrañas
        if (cfg.resolutionWidth < 1280 || cfg.resolutionHeight < 720)
        {
            Debug.LogWarning("[BootstrapManager] Resolución en config demasiado pequeña. Usando nativa.");
            cfg = LauncherConfig.CreateDefault();
            Config = cfg;
        }

        FullScreenMode mode = cfg.fullscreen
            ? FullScreenMode.ExclusiveFullScreen
            : FullScreenMode.Windowed;

        // Construir la tasa de refresco desde el config (si está en 0, usa la nativa)
        RefreshRate refreshRate = new RefreshRate
        {
            numerator = cfg.refreshRateNumerator > 0 ? cfg.refreshRateNumerator : Screen.currentResolution.refreshRateRatio.numerator,
            denominator = cfg.refreshRateDenominator > 0 ? cfg.refreshRateDenominator : Screen.currentResolution.refreshRateRatio.denominator
        };

        Screen.SetResolution(cfg.resolutionWidth, cfg.resolutionHeight, mode, refreshRate);

        int clampedQuality = Mathf.Clamp(cfg.qualityLevel, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(clampedQuality, true);

        Debug.Log($"[BootstrapManager] Resolución aplicada: {cfg.resolutionWidth}x{cfg.resolutionHeight} {mode}, quality={clampedQuality}");
    }
}

// ─── Clase de datos del JSON ──────────────────────────────────────────────────

[System.Serializable]
public class LauncherConfig
{
    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;
    public bool fullscreen = true;
    public int qualityLevel = 2;
    public uint refreshRateNumerator = 0;   // 0 = usar la nativa del monitor
    public uint refreshRateDenominator = 0;

    /// <summary>Crea una configuración por defecto con la resolución nativa del monitor.</summary>
    public static LauncherConfig CreateDefault()
    {
        return new LauncherConfig
        {
            resolutionWidth = Screen.currentResolution.width > 0 ? Screen.currentResolution.width : 1920,
            resolutionHeight = Screen.currentResolution.height > 0 ? Screen.currentResolution.height : 1080,
            fullscreen = true,
            qualityLevel = QualitySettings.GetQualityLevel(),
            refreshRateNumerator = Screen.currentResolution.refreshRateRatio.numerator,
            refreshRateDenominator = Screen.currentResolution.refreshRateRatio.denominator
        };
    }
}
