using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Temporizador de partida y gestión de victoria para Chaos Cleaners.
/// 
/// Características:
/// - Cuenta atrás configurable (60s por defecto).
/// - Muestra el tiempo restante en formato MM:SS en el Canvas.
/// - Al terminar el tiempo, calcula automáticamente qué jugador tiene más puntos.
/// - Soporta empates entre 2 o más jugadores.
/// - Muestra el panel de ganador con el color oficial del robot vencedor.
/// - Botones integrados para reiniciar la partida o volver al Menú Principal.
/// - Respaldo automático: si no se configuran objetos en el Canvas, los crea solo.
/// </summary>
public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    [Header("Duración de la Partida")]
    [Tooltip("Duración de la partida en segundos (ej: 60 = 1 minuto, 90 = 1:30)")]
    [SerializeField] private float duracionPartida = 60f;

    [Header("Referencias en el Canvas (Arrastra tus objetos aquí)")]
    [Tooltip("Texto para el tiempo restante (MM:SS)")]
    public TextMeshProUGUI textoTimer;

    [Tooltip("Panel que se activa al terminar el tiempo")]
    public GameObject panelGanador;

    [Tooltip("Texto donde se anuncia el jugador ganador y sus puntos")]
    public TextMeshProUGUI textoGanador;

    [Header("Botones de Fin de Partida (Opcionales)")]
    public Button botonReiniciar;
    public Button botonMenuPrincipal;

    private float tiempoRestante;
    private bool partidaTerminada = false;

    // Colores oficiales por jugador de Player_Movement
    private static readonly Color[] coloresJugador = new Color[]
    {
        new Color(0.0f, 0.90f, 1.0f),  // P1: Cyan
        new Color(1.0f, 0.24f, 0.0f),  // P2: Rojo Coral
        new Color(1.0f, 0.84f, 0.0f),  // P3: Amarillo Ámbar
        new Color(0.0f, 0.90f, 0.46f)  // P4: Verde Lima
    };

    public float TiempoRestante => tiempoRestante;
    public bool PartidaTerminada => partidaTerminada;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        tiempoRestante = duracionPartida;
        partidaTerminada = false;
    }

    private void Start()
    {
        // 1. Respaldo: asegurar que existan los elementos en el Canvas
        EnsureCanvasUI();

        // 2. Asegurarse de que el panel de ganador empiece oculto
        if (panelGanador != null)
        {
            panelGanador.SetActive(false);
        }

        // 3. Vincular botones
        if (botonReiniciar != null)
        {
            botonReiniciar.onClick.RemoveAllListeners();
            botonReiniciar.onClick.AddListener(ReiniciarPartida);
        }

        if (botonMenuPrincipal != null)
        {
            botonMenuPrincipal.onClick.RemoveAllListeners();
            botonMenuPrincipal.onClick.AddListener(VolverAlMenu);
        }

        Time.timeScale = 1f;
        ActualizarTextoTimer();
    }

    private void Update()
    {
        if (partidaTerminada) return;

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            ActualizarTextoTimer();
            FinalizarPartida();
        }
        else
        {
            ActualizarTextoTimer();
        }
    }

    private void ActualizarTextoTimer()
    {
        if (textoTimer == null) return;

        int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60f);

        textoTimer.text = $"{minutos:00}:{segundos:00}";

        // Tinte rojo de advertencia en los últimos 10 segundos
        if (tiempoRestante <= 10f && tiempoRestante > 0f)
        {
            textoTimer.color = Color.red;
        }
        else if (tiempoRestante > 10f)
        {
            textoTimer.color = Color.white;
        }
    }

    private void FinalizarPartida()
    {
        partidaTerminada = true;
        Time.timeScale = 0f; // Detener físicas y movimiento

        // 1. Detectar qué jugadores están activos en la partida
        Player_Movement[] jugadoresEnEscena = FindObjectsByType<Player_Movement>(FindObjectsInactive.Exclude);
        List<int> jugadoresActivos = new List<int>();

        foreach (var jug in jugadoresEnEscena)
        {
            if (!jugadoresActivos.Contains(jug.PlayerIndex))
            {
                jugadoresActivos.Add(jug.PlayerIndex);
            }
        }

        // Fallback mínimo de 2 jugadores si no los encontró
        if (jugadoresActivos.Count == 0)
        {
            jugadoresActivos.Add(0);
            jugadoresActivos.Add(1);
        }

        // 2. Determinar la puntuación máxima y ganadores
        int maxPuntos = -1;
        List<int> ganadores = new List<int>();

        foreach (int pIndex in jugadoresActivos)
        {
            int pts = GestorPuntos.GetPuntosJugador(pIndex);

            if (pts > maxPuntos)
            {
                maxPuntos = pts;
                ganadores.Clear();
                ganadores.Add(pIndex);
            }
            else if (pts == maxPuntos)
            {
                ganadores.Add(pIndex);
            }
        }

        // 3. Formatear mensaje de victoria
        if (textoGanador != null)
        {
            if (ganadores.Count == 1)
            {
                int pGanador = ganadores[0];
                textoGanador.text = $"¡VICTORIA!\n<size=80%>¡JUGADOR {pGanador + 1} GANA!</size>\n<size=60%>{maxPuntos} PUNTOS</size>";
                textoGanador.color = coloresJugador[pGanador];
            }
            else
            {
                // Empate
                string nombresEmpate = "";
                for (int i = 0; i < ganadores.Count; i++)
                {
                    nombresEmpate += $"JUGADOR {ganadores[i] + 1}";
                    if (i < ganadores.Count - 1) nombresEmpate += " Y ";
                }

                textoGanador.text = $"¡EMPATE!\n<size=70%>{nombresEmpate}</size>\n<size=60%>{maxPuntos} PUNTOS</size>";
                textoGanador.color = Color.yellow;
            }
        }

        // 4. Mostrar el panel de ganador siempre por encima de todo
        if (panelGanador != null)
        {
            panelGanador.SetActive(true);
            panelGanador.transform.SetAsLastSibling();
        }

        // 5. Seleccionar automáticamente el botón de reinicio para soporte con Mando
        if (botonReiniciar != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(botonReiniciar.gameObject);
        }

        Debug.Log($"[GameTimer] ¡Fin de partida! Ganador(es) con {maxPuntos} puntos.");
    }

    public void ReiniciarPartida()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MAIN_MENU");
    }

    /// <summary>
    /// Respaldo: si el usuario no arrastró objetos en el Inspector, busca o crea
    /// los elementos visuales en el Canvas para que se vean en la jerarquía.
    /// </summary>
    private void EnsureCanvasUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // 1. Texto del Temporizador (Arriba al centro)
        if (textoTimer == null)
        {
            Transform timerExistente = canvas.transform.Find("Texto_Timer");
            if (timerExistente == null)
            {
                GameObject objTimer = new GameObject("Texto_Timer", typeof(RectTransform), typeof(TextMeshProUGUI));
                objTimer.transform.SetParent(canvas.transform, false);
                objTimer.transform.SetAsFirstSibling(); // Detrás de menús de pausa y victoria

                RectTransform rt = objTimer.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -15f);
                rt.sizeDelta = new Vector2(200f, 50f);

                textoTimer = objTimer.GetComponent<TextMeshProUGUI>();
                textoTimer.fontSize = 38;
                textoTimer.fontStyle = FontStyles.Bold;
                textoTimer.alignment = TextAlignmentOptions.Center;
                textoTimer.color = Color.white;
            }
            else
            {
                textoTimer = timerExistente.GetComponent<TextMeshProUGUI>();
            }
        }

        // 2. Panel de Ganador
        if (panelGanador == null)
        {
            Transform panelExistente = canvas.transform.Find("Panel_Ganador");
            if (panelExistente == null)
            {
                // Fondo oscuro centrado
                GameObject pnl = new GameObject("Panel_Ganador", typeof(RectTransform), typeof(Image));
                pnl.transform.SetParent(canvas.transform, false);

                RectTransform rtp = pnl.GetComponent<RectTransform>();
                rtp.anchorMin = new Vector2(0.5f, 0.5f);
                rtp.anchorMax = new Vector2(0.5f, 0.5f);
                rtp.pivot = new Vector2(0.5f, 0.5f);
                rtp.anchoredPosition = Vector2.zero;
                rtp.sizeDelta = new Vector2(600f, 380f);

                Image img = pnl.GetComponent<Image>();
                img.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

                // Texto de Ganador
                GameObject objTxt = new GameObject("Texto_Ganador", typeof(RectTransform), typeof(TextMeshProUGUI));
                objTxt.transform.SetParent(pnl.transform, false);

                RectTransform rtt = objTxt.GetComponent<RectTransform>();
                rtt.anchorMin = new Vector2(0f, 0.4f);
                rtt.anchorMax = new Vector2(1f, 1f);
                rtt.offsetMin = Vector2.zero;
                rtt.offsetMax = Vector2.zero;

                textoGanador = objTxt.GetComponent<TextMeshProUGUI>();
                textoGanador.fontSize = 42;
                textoGanador.fontStyle = FontStyles.Bold;
                textoGanador.alignment = TextAlignmentOptions.Center;

                // Botón Reiniciar
                GameObject btnReiniciarObj = CrearBoton(pnl.transform, "Boton_Reiniciar", "JUGAR DE NUEVO", new Vector2(-130f, -120f));
                botonReiniciar = btnReiniciarObj.GetComponent<Button>();

                // Botón Menú Principal
                GameObject btnMenuObj = CrearBoton(pnl.transform, "Boton_MenuPrincipal", "MENÚ PRINCIPAL", new Vector2(130f, -120f));
                botonMenuPrincipal = btnMenuObj.GetComponent<Button>();

                panelGanador = pnl;
            }
            else
            {
                panelGanador = panelExistente.gameObject;
            }
        }
    }

    private GameObject CrearBoton(Transform padre, string nombre, string texto, Vector2 pos)
    {
        GameObject btnObj = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(padre, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(220f, 50f);

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.2f, 0.22f, 0.28f, 1f);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);

        RectTransform rtt = txtObj.GetComponent<RectTransform>();
        rtt.anchorMin = Vector2.zero;
        rtt.anchorMax = Vector2.one;
        rtt.offsetMin = Vector2.zero;
        rtt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btnObj;
    }
}
