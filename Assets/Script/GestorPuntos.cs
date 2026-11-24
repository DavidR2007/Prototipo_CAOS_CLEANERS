using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Gestor de Puntuación visual en Canvas para Chaos Cleaners.
/// 
/// Cómo funciona:
/// 1. Asignación directa en el Canvas: Arrastras los textos de tus jugadores (Texto P1, Texto P2, etc.)
///    a las casillas del Inspector.
/// 2. Adaptable a 2, 3 o 4 jugadores: Detecta automáticamente cuántos robots (Player_Movement) hay activos
///    en la partida y oculta (SetActive false) los marcadores de los jugadores que no estén jugando.
/// 3. Respaldo automático: Si no asignas nada en el Inspector, el script crea el objeto
///    "HUD_Puntuaciones" en tu Canvas para que puedas verlo en la jerarquía y colocarlo a tu gusto.
/// </summary>
public class GestorPuntos : MonoBehaviour
{
    public static GestorPuntos Instance { get; private set; }

    [Header("Marcadores en el Canvas")]
    [Tooltip("Texto para la puntuación del Jugador 1")]
    public TextMeshProUGUI textoP1;

    [Tooltip("Texto para la puntuación del Jugador 2")]
    public TextMeshProUGUI textoP2;

    [Tooltip("Texto para la puntuación del Jugador 3")]
    public TextMeshProUGUI textoP3;

    [Tooltip("Texto para la puntuación del Jugador 4")]
    public TextMeshProUGUI textoP4;

    [Header("Valores en Tiempo Real (Solo Lectura)")]
    [SerializeField] private int puntosP1 = 0;
    [SerializeField] private int puntosP2 = 0;
    [SerializeField] private int puntosP3 = 0;
    [SerializeField] private int puntosP4 = 0;

    // Array interno para acceder por índice de jugador (0=P1, 1=P2, 2=P3, 3=P4)
    private readonly int[] puntosJugadores = new int[4];
    private TextMeshProUGUI[] textosJugadores;

    // Colores oficiales por jugador de Player_Movement
    private static readonly Color[] coloresJugador = new Color[]
    {
        new Color(1.0f, 0.84f, 0.0f),  // P1: 1.0f, 0.84f, 0.0f Amarillo  
        new Color(0.0f, 0.90f, 1.0f),  // P2: 0.0f, 0.90f, 1.0f Cyan
        new Color(1.0f, 0.24f, 0.0f),  // P3: 1.0f, 0.24f, 0.0f Rojo Coral
        new Color(0.0f, 0.90f, 0.46f)  // P4: 0.0f, 0.90f, 0.46f Verde Lima 
    };

    public static event Action<int, int> OnPuntosJugadorCambiados;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < puntosJugadores.Length; i++)
        {
            puntosJugadores[i] = 0;
        }

        textosJugadores = new TextMeshProUGUI[] { textoP1, textoP2, textoP3, textoP4 };
    }

    private void Start()
    {
        // 1. Si no hay textos asignados a mano en el Inspector, vincularlos o crearlos en el Canvas
        EnsureCanvasTextos();

        // 2. Detectar si juegan 2, 3 o 4 jugadores y ocultar los que no estén en la partida
        ConfigurarJugadoresActivos();

        // 3. Actualizar textos con valor 0 inicial
        RefrescarTodo();
    }

    /// <summary>
    /// Escanea los robots activos en la escena. Si juegan 2, oculta P3 y P4 para que no aparezcan.
    /// Si juegan 3, oculta P4. Si juegan 4, activa todos.
    /// </summary>
    private void ConfigurarJugadoresActivos()
    {
        Player_Movement[] jugadoresEnEscena = FindObjectsByType<Player_Movement>(FindObjectsInactive.Exclude);

        HashSet<int> indicesPresentes = new HashSet<int>();
        foreach (var jug in jugadoresEnEscena)
        {
            indicesPresentes.Add(jug.PlayerIndex);
        }

        // Si aún no detectó ninguno, asumir como mínimo P1 y P2
        if (indicesPresentes.Count == 0)
        {
            indicesPresentes.Add(0);
            indicesPresentes.Add(1);
        }

        for (int i = 0; i < textosJugadores.Length; i++)
        {
            if (textosJugadores[i] != null)
            {
                bool estaEnPartida = indicesPresentes.Contains(i);
                textosJugadores[i].gameObject.SetActive(estaEnPartida);

                if (estaEnPartida)
                {
                    textosJugadores[i].color = coloresJugador[i];
                }
            }
        }

        Debug.Log($"[GestorPuntos] HUD adaptado a {indicesPresentes.Count} jugadores activos en la partida.");
    }

    public static void SumarPuntos(int playerIndex, int puntos)
    {
        if (Instance == null)
        {
            GameObject auto = new GameObject("GestorPuntos");
            auto.AddComponent<GestorPuntos>();
        }

        Instance.SumarPuntosInterno(playerIndex, puntos);
    }

    public static void SumarPuntos(int puntos)
    {
        SumarPuntos(0, puntos);
    }

    private void SumarPuntosInterno(int playerIndex, int puntos)
    {
        if (playerIndex >= 0 && playerIndex < puntosJugadores.Length)
        {
            puntosJugadores[playerIndex] += puntos;

            // Actualizar variables visibles en el Inspector de Unity
            puntosP1 = puntosJugadores[0];
            puntosP2 = puntosJugadores[1];
            puntosP3 = puntosJugadores[2];
            puntosP4 = puntosJugadores[3];

            ActualizarTextoJugador(playerIndex);
            OnPuntosJugadorCambiados?.Invoke(playerIndex, puntosJugadores[playerIndex]);
        }
    }

    public static int GetPuntosJugador(int playerIndex)
    {
        if (Instance != null && playerIndex >= 0 && playerIndex < Instance.puntosJugadores.Length)
            return Instance.puntosJugadores[playerIndex];
        return 0;
    }

    private void ActualizarTextoJugador(int i)
    {
        if (i >= 0 && i < textosJugadores.Length && textosJugadores[i] != null)
        {
            textosJugadores[i].text = $"P{i + 1}: {puntosJugadores[i]}";
        }
    }

    private void RefrescarTodo()
    {
        for (int i = 0; i < textosJugadores.Length; i++)
        {
            ActualizarTextoJugador(i);
        }
    }

    /// <summary>
    /// Respaldo amigable: si no arrastraste textos en el Inspector, busca o crea
    /// el objeto 'HUD_Puntuaciones' en tu Canvas para que puedas verlo en la jerarquía.
    /// </summary>
    private void EnsureCanvasTextos()
    {
        // Si el usuario ya arrastró sus textos en el Inspector, respetamos su Canvas al 100%
        if (textoP1 != null || textoP2 != null)
        {
            textosJugadores[0] = textoP1;
            textosJugadores[1] = textoP2;
            textosJugadores[2] = textoP3;
            textosJugadores[3] = textoP4;
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform hudExistente = canvas.transform.Find("HUD_Puntuaciones");
        GameObject panelHud;

        if (hudExistente == null)
        {
            panelHud = new GameObject("HUD_Puntuaciones", typeof(RectTransform));
            panelHud.transform.SetParent(canvas.transform, false);
            panelHud.transform.SetAsFirstSibling(); // Detrás de menús de pausa y paneles de victoria

            RectTransform rt = panelHud.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta = new Vector2(0f, 60f);
        }
        else
        {
            panelHud = hudExistente.gameObject;
        }

        for (int i = 0; i < 4; i++)
        {
            Transform hijoExistente = panelHud.transform.Find($"Texto_P{i + 1}");
            TextMeshProUGUI tmp;

            if (hijoExistente == null)
            {
                GameObject obj = new GameObject($"Texto_P{i + 1}", typeof(RectTransform), typeof(TextMeshProUGUI));
                obj.transform.SetParent(panelHud.transform, false);

                RectTransform r = obj.GetComponent<RectTransform>();
                r.anchorMin = new Vector2(0.12f + i * 0.20f, 0f);
                r.anchorMax = new Vector2(0.28f + i * 0.20f, 1f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = Vector2.zero;

                tmp = obj.GetComponent<TextMeshProUGUI>();
                tmp.fontSize = 28;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                tmp = hijoExistente.GetComponent<TextMeshProUGUI>();
            }

            textosJugadores[i] = tmp;
        }

        textoP1 = textosJugadores[0];
        textoP2 = textosJugadores[1];
        textoP3 = textosJugadores[2];
        textoP4 = textosJugadores[3];
    }
}
