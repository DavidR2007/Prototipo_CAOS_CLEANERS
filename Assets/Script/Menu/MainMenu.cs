using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Controlador del Menú Principal (Main Menu) para Chaos Cleaners.
/// Gestiona la navegación de paneles (Principal, Opciones, Créditos),
/// la carga de la escena de juego y la salida segura de la aplicación.
/// 
/// Buenas prácticas:
/// - Soporte nativo para navegación por Mando y Teclado (selecciona automáticamente el primer botón al cambiar de panel).
/// - Cierre seguro de aplicación tanto en Build ejecutable como en el Editor de Unity.
/// - Desacoplado: puede abrir opciones compartiendo el mismo panel con OptionsMenu.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Paneles de Navegación")]
    [Tooltip("Panel principal con los botones Play, Opciones, Créditos, Salir")]
    [SerializeField] private GameObject mainPanel;

    [Tooltip("Panel con ajustes de audio, gráficos y resolución")]
    [SerializeField] private GameObject optionsPanel;

    [Tooltip("Panel con créditos del equipo y autoría")]
    [SerializeField] private GameObject creditsPanel;

    [Header("Primer Botón Seleccionado (Para soporte de Mando/Teclado)")]
    [SerializeField] private Button firstMainButton;
    [SerializeField] private Button firstOptionsButton;
    [SerializeField] private Button firstCreditsButton;

    [Header("Configuración de Escena")]
    [Tooltip("Nombre de la escena de juego/arena que cargará el botón Play")]
    [SerializeField] private string gameSceneName = "MAIN";

    [Header("Feedback de Audio (Opcional)")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip buttonClickSfx;

    private void Start()
    {
        // Corrección automática de UI: asegurar que el Canvas escale a 1920x1080 y no se vea pegado ni deformado
        EnsureCanvasScaling();

        // Asegurar que solo el panel principal esté activo al iniciar
        ShowOnlyMainPanel();

        // Restaurar timeScale por si venimos de una partida pausada
        Time.timeScale = 1f;

        SelectButton(firstMainButton);
    }

    private void EnsureCanvasScaling()
    {
        CanvasScaler scaler = GetComponentInParent<CanvasScaler>() ?? FindAnyObjectByType<CanvasScaler>();
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    /// <summary>
    /// Inicia la partida cargando la escena de la arena.
    /// </summary>
    public void PlayGame()
    {
        PlaySound();

        if (!string.IsNullOrEmpty(gameSceneName) && Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            // Si la escena no está en Build Settings por nombre, intentar cargar el siguiente índice
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextIndex);
            }
            else
            {
                Debug.LogWarning($"[MainMenu] La escena '{gameSceneName}' no está en Build Settings. Añádela en File > Build Settings.");
            }
        }
    }

    public void OpenOptions()
    {
        PlaySound();
        SetPanelState(options: true);
        SelectButton(firstOptionsButton);
    }

    public void CloseOptions()
    {
        PlaySound();
        SetPanelState(options: false);
        SelectButton(firstMainButton);
    }

    public void OpenCredits()
    {
        PlaySound();
        SetPanelState(credits: true);
        SelectButton(firstCreditsButton);
    }

    public void CloseCredits()
    {
        PlaySound();
        SetPanelState(credits: false);
        SelectButton(firstMainButton);
    }

    /// <summary>
    /// Cierra la aplicación de forma limpia tanto en el juego final como dentro del editor de Unity.
    /// </summary>
    public void QuitGame()
    {
        PlaySound();
        Debug.Log("[MainMenu] Saliendo del juego...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowOnlyMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    private void SetPanelState(bool options = false, bool credits = false)
    {
        if (mainPanel != null) mainPanel.SetActive(!options && !credits);
        if (optionsPanel != null) optionsPanel.SetActive(options);
        if (creditsPanel != null) creditsPanel.SetActive(credits);
    }

    private void SelectButton(Button btn)
    {
        if (btn != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }
    }

    private void PlaySound()
    {
        // Solo reproducir como fallback si no existe AudioManager (para evitar sonido doble con UIButtonAudio)
        if (AudioManager.Instance == null && uiAudioSource != null && buttonClickSfx != null)
        {
            uiAudioSource.PlayOneShot(buttonClickSfx);
        }
    }
}
