using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Menú de Pausa en partida para Chaos Cleaners.
/// Cumple con la normativa académica de ENTI-UB (Sección 6.9 de la Guía).
/// 
/// Características:
/// - Se abre/cierra pulsando Escape en Teclado o el botón Start/Menu en Gamepad.
/// - Congela y reanuda el tiempo de físicas (Time.timeScale).
/// - Permite reanudar, reiniciar la partida, abrir opciones o salir al menú principal.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Paneles UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Primer Botón Seleccionado (Soporte Mando)")]
    [SerializeField] private Button firstPauseButton;
    [SerializeField] private Button firstOptionsButton;

    [Header("Navegación")]
    [Tooltip("Nombre de la escena del Menú Principal")]
    [SerializeField] private string mainMenuSceneName = "MAIN_MENU";

    private bool isPaused = false;

    public bool IsPaused => isPaused;

    private void Start()
    {
        CloseAllPanels();
        Time.timeScale = 1f;
    }

    private void Update()
    {
        CheckPauseInput();
    }

    private void CheckPauseInput()
    {
        if (GameTimer.Instance != null && GameTimer.Instance.PartidaTerminada) return;

        bool pausePressed = false;

        // Teclado (Tecla Escape)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            pausePressed = true;
        }

        // Mandos conectados (Botón Start / Options / Menu)
        if (!pausePressed && Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
        {
            pausePressed = true;
        }

        if (pausePressed)
        {
            // Si está en opciones, volver al menú de pausa; de lo contrario alternar pausa
            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                CloseOptions();
            }
            else
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
            if (isPaused) pausePanel.transform.SetAsLastSibling();
        }
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (isPaused)
        {
            SelectButton(firstPauseButton);
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        CloseAllPanels();
    }

    public void RestartMatch()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OpenOptions()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
            optionsPanel.transform.SetAsLastSibling();
        }
        SelectButton(firstOptionsButton);
    }

    public void CloseOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            pausePanel.transform.SetAsLastSibling();
        }
        SelectButton(firstPauseButton);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            // Fallback al índice 0 (convención habitual para el menú principal)
            SceneManager.LoadScene(0);
        }
    }

    private void CloseAllPanels()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    private void SelectButton(Button btn)
    {
        if (btn != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }
    }
}
