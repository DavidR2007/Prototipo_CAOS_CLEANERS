using System.IO;
using UnityEngine;

/// <summary>
/// Lector de configuración del Launcher para Chaos Cleaners.
/// 
/// Se ejecuta en la escena MAIN_MENU (índice 0) antes que ningún otro script
/// (ExecutionOrder -100). Lee el archivo launcher_config.json que el Launcher
/// deposita junto al ejecutable del juego y aplica resolución, pantalla completa,
/// calidad gráfica y resolución de texturas (globalTextureMipmapLimit).
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
    /// Busca de forma inteligente tanto si el juego está en la misma carpeta que el launcher,
    /// como si está dentro de una subcarpeta (ej: Launcher/Main_Game/PROT.exe).
    /// </summary>
    public static string GetConfigPath()
    {
#if UNITY_EDITOR
        string editorPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "launcher_config.json"));
        if (File.Exists(editorPath)) return editorPath;
        return editorPath;
#else
        // 1. Probar en la misma carpeta que el .exe (un nivel arriba de _Data/)
        string nextToExe = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "launcher_config.json"));
        if (File.Exists(nextToExe)) return nextToExe;

        // 2. Probar en la carpeta padre (por si el juego está dentro de una subcarpeta, ej: Launcher/Main_Game/)
        string parentFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "launcher_config.json"));
        if (File.Exists(parentFolder)) return parentFolder;

        // 3. Probar en el directorio de trabajo activo
        string cwdPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "launcher_config.json"));
        if (File.Exists(cwdPath)) return cwdPath;

        return nextToExe;
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
                Debug.Log($"[BootstrapManager] Config del Launcher cargada con éxito desde '{path}': {loaded.resolutionWidth}x{loaded.resolutionHeight}, fullscreen={loaded.fullscreen}, quality={loaded.qualityLevel}");
                return loaded;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BootstrapManager] Error al leer launcher_config.json: {ex.Message}. Usando fallback.");
            }
        }
        else
        {
            Debug.Log($"[BootstrapManager] launcher_config.json no encontrado en '{path}'. Usando resolución nativa.");
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

        // Control explícito de la resolución de texturas y filtrado según nivel:
        // 0 (Bajo): Texturas a 1/4 de resolución (visiblemente ligero y con máximo rendimiento)
        // 1 (Medio): Texturas a la mitad (1/2) de resolución
        // 2 (Alto) / 3 (Ultra): Texturas al 100% (resolución completa)
        if (cfg.qualityLevel == 0)
        {
            QualitySettings.globalTextureMipmapLimit = 2;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        }
        else if (cfg.qualityLevel == 1)
        {
            QualitySettings.globalTextureMipmapLimit = 1;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        }
        else
        {
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        }

        Debug.Log($"[BootstrapManager] Ajustes aplicados: {cfg.resolutionWidth}x{cfg.resolutionHeight} {mode}, quality={clampedQuality}, MipmapLimit={QualitySettings.globalTextureMipmapLimit}");
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
