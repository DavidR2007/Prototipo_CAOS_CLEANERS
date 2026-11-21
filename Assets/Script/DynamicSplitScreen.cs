using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor de Pantalla Partida Dinámica para Chaos Cleaners.
/// Arquitectura escalable de 2 a 4 jugadores (2 mitades o 4 cuadrantes).
/// 
/// Buenas prácticas aplicadas:
/// - Histeresis en distancias (splitDistance vs mergeDistance) para erradicar cualquier parpadeo de cámara en bordes.
/// - Interpolación suave tanto de posición como de tamaño de Viewport Rects.
/// - Optimización de rendimiento: desactiva cámaras secundarias cuando la pantalla está completamente unida.
/// - Tolerante a fallos: Auto-detección en escena y verificación de valores nulos si un jugador se desconecta/elimina.
/// </summary>
public class DynamicSplitScreen : MonoBehaviour
{
    [System.Serializable]
    public class PlayerCameraSlot
    {
        public Transform player;
        public Camera camera;
        [HideInInspector] public Rect targetSplitRect;
    }

    [Header("Cámaras Principales (Auto-detectables)")]
    public Camera cameraP1;
    public Camera cameraP2;

    [Header("Jugadores (Auto-detectables)")]
    public Transform player1;
    public Transform player2;

    [Header("Lista Escalable (Hasta 4 Jugadores)")]
    [Tooltip("Permite registrar 2, 3 o 4 jugadores con sus respectivas cámaras.")]
    public List<PlayerCameraSlot> playerSlots = new List<PlayerCameraSlot>();

    [Header("Ajustes de Encuadre")]
    [Tooltip("Distancia relativa cenital respecto al personaje")]
    public Vector3 cameraOffset = new Vector3(0f, 12f, -8f);
    public float smoothFollowSpeed = 10f;

    [Header("Umbrales de Separación (Histeresis)")]
    [Tooltip("Distancia máxima entre jugadores para comenzar a dividir la pantalla")]
    public float splitDistance = 10.5f;

    [Tooltip("Distancia a la que deben acercarse para unificarse (debe ser menor a splitDistance)")]
    public float mergeDistance = 7.5f;

    [Tooltip("Velocidad de transición al abrir/cerrar la división de pantalla")]
    public float transitionSpeed = 4f;

    // Estado interno
    private bool isSplit = true;
    private float splitProgress = 1f; // 0 = 1 sola pantalla completa, 1 = pantalla dividida

    public bool IsSplit => isSplit;
    public float SplitProgress => splitProgress;

    private void Awake()
    {
        ResolveReferences();
        InitializePlayerSlots();
    }

    /// <summary>
    /// Resuelve de forma segura y automática las referencias en escena si no fueron arrastradas en el Inspector.
    /// </summary>
    private void ResolveReferences()
    {
        // 1. Detectar cámaras por nombre estándar
        if (cameraP1 == null)
        {
            var p1 = GameObject.Find("P1_Camera") ?? GameObject.Find("Camera_P1");
            if (p1 != null) cameraP1 = p1.GetComponent<Camera>();
            if (cameraP1 == null) cameraP1 = GetComponent<Camera>();
        }

        if (cameraP2 == null)
        {
            var p2 = GameObject.Find("P2_Camera") ?? GameObject.Find("Camera_P2");
            if (p2 != null) cameraP2 = p2.GetComponent<Camera>();
        }

        // 2. Detectar jugadores mediante el componente Player_Movement
        if (player1 == null || player2 == null)
        {
            var movements = FindObjectsByType<Player_Movement>(FindObjectsInactive.Exclude);
            // Ordenar por PlayerIndex para consistencia
            System.Array.Sort(movements, (a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));

            foreach (var mov in movements)
            {
                if (mov.PlayerIndex == 0 && player1 == null) player1 = mov.transform;
                else if (mov.PlayerIndex == 1 && player2 == null) player2 = mov.transform;
            }
        }
    }

    /// <summary>
    /// Configura los slots de jugadores y sus cuadrantes de pantalla partida (hasta 4 jugadores).
    /// </summary>
    private void InitializePlayerSlots()
    {
        if (playerSlots == null || playerSlots.Count == 0)
        {
            playerSlots = new List<PlayerCameraSlot>();

            // Registrar P1 y P2 si están presentes
            if (player1 != null && cameraP1 != null)
            {
                playerSlots.Add(new PlayerCameraSlot { player = player1, camera = cameraP1 });
            }

            if (player2 != null && cameraP2 != null)
            {
                playerSlots.Add(new PlayerCameraSlot { player = player2, camera = cameraP2 });
            }

            // Buscar si existen P3 y P4 en escena
            var allMovements = FindObjectsByType<Player_Movement>(FindObjectsInactive.Exclude);
            System.Array.Sort(allMovements, (a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));

            foreach (var mov in allMovements)
            {
                if (mov.PlayerIndex >= 2 && mov.PlayerIndex <= 3)
                {
                    string camName = $"P{mov.PlayerIndex + 1}_Camera";
                    var camObj = GameObject.Find(camName);
                    if (camObj != null && camObj.TryGetComponent<Camera>(out var extraCam))
                    {
                        playerSlots.Add(new PlayerCameraSlot { player = mov.transform, camera = extraCam });
                    }
                }
            }
        }

        ConfigureTargetRects();
    }

    /// <summary>
    /// Asigna los rectángulos ideales según si son 2 jugadores (mitades) o 3-4 (cuadrantes).
    /// </summary>
    private void ConfigureTargetRects()
    {
        int count = playerSlots.Count;

        if (count <= 2)
        {
            // 2 Jugadores: Mitad izquierda y mitad derecha
            if (count > 0) playerSlots[0].targetSplitRect = new Rect(0f, 0f, 0.5f, 1f);
            if (count > 1) playerSlots[1].targetSplitRect = new Rect(0.5f, 0f, 0.5f, 1f);
        }
        else
        {
            // 3 o 4 Jugadores: Cuadrantes 2x2
            // P1: Arriba Izquierda
            playerSlots[0].targetSplitRect = new Rect(0f, 0.5f, 0.5f, 0.5f);
            // P2: Arriba Derecha
            playerSlots[1].targetSplitRect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
            // P3: Abajo Izquierda
            if (count > 2) playerSlots[2].targetSplitRect = new Rect(0f, 0f, 0.5f, 0.5f);
            // P4: Abajo Derecha
            if (count > 3) playerSlots[3].targetSplitRect = new Rect(0.5f, 0f, 0.5f, 0.5f);
        }
    }

    private void LateUpdate()
    {
        // Limpiar posibles elementos nulos si un jugador fue destruido
        playerSlots.RemoveAll(s => s == null || s.player == null || s.camera == null);

        if (playerSlots.Count == 0) return;

        UpdateSplitHysteresis();
        UpdateViewportsSmooth();
        UpdateCameraPositions();
    }

    /// <summary>
    /// Comprueba la distancia máxima entre todos los jugadores para decidir si unificar o partir la pantalla.
    /// </summary>
    private void UpdateSplitHysteresis()
    {
        float maxDistance = GetMaxDistanceBetweenPlayers();

        if (isSplit)
        {
            // Solo se une si TODOS los jugadores están a menos de mergeDistance
            if (maxDistance < mergeDistance)
            {
                isSplit = false;
            }
        }
        else
        {
            // Se parte si CUALQUIER pareja de jugadores supera splitDistance
            if (maxDistance > splitDistance)
            {
                isSplit = true;
            }
        }

        // Interpolación lineal del factor de división
        float target = isSplit ? 1f : 0f;
        splitProgress = Mathf.MoveTowards(splitProgress, target, Time.deltaTime * transitionSpeed);
    }

    private float GetMaxDistanceBetweenPlayers()
    {
        if (playerSlots.Count < 2) return 0f;

        float maxDist = 0f;
        for (int i = 0; i < playerSlots.Count; i++)
        {
            for (int j = i + 1; j < playerSlots.Count; j++)
            {
                float d = Vector3.Distance(playerSlots[i].player.position, playerSlots[j].player.position);
                if (d > maxDist) maxDist = d;
            }
        }
        return maxDist;
    }

    /// <summary>
    /// Anima suavemente los Viewport Rects de las cámaras entre pantalla unificada y dividida.
    /// </summary>
    private void UpdateViewportsSmooth()
    {
        // Cámara 1 (Líder cuando está junta):
        // Pasa de pantalla completa (0,0, 1, 1) a su cuadrante/mitad asignada
        Rect fullScreenRect = new Rect(0f, 0f, 1f, 1f);
        Rect p1Target = playerSlots[0].targetSplitRect;

        playerSlots[0].camera.rect = InterpolateRect(fullScreenRect, p1Target, splitProgress);

        // Cámaras secundarias: Se habilitan solo cuando la división está activa
        bool secondaryCamerasActive = (splitProgress > 0.005f);

        for (int i = 1; i < playerSlots.Count; i++)
        {
            var slot = playerSlots[i];
            if (slot.camera == null) continue;

            slot.camera.enabled = secondaryCamerasActive;

            if (secondaryCamerasActive)
            {
                // Aparecen desde el borde derecho o inferior
                Rect collapsedRect = new Rect(1f, slot.targetSplitRect.y, 0f, slot.targetSplitRect.height);
                slot.camera.rect = InterpolateRect(collapsedRect, slot.targetSplitRect, splitProgress);
            }
        }
    }

    private Rect InterpolateRect(Rect from, Rect to, float t)
    {
        return new Rect(
            Mathf.Lerp(from.x, to.x, t),
            Mathf.Lerp(from.y, to.y, t),
            Mathf.Lerp(from.width, to.width, t),
            Mathf.Lerp(from.height, to.height, t)
        );
    }

    /// <summary>
    /// Sigue suavemente a cada personaje con su respectiva cámara.
    /// Cuando la pantalla está unida, la Cámara 1 viaja hacia el centroide de todos los jugadores.
    /// </summary>
    private void UpdateCameraPositions()
    {
        Vector3 centroid = CalculateCentroid();

        // Cámara 1: interpola entre el centroide común y la posición individual del P1
        Vector3 p1Goal = Vector3.Lerp(centroid, playerSlots[0].player.position, splitProgress);
        Vector3 cam1DesiredPos = p1Goal + cameraOffset;

        playerSlots[0].camera.transform.position = Vector3.Lerp(
            playerSlots[0].camera.transform.position,
            cam1DesiredPos,
            smoothFollowSpeed * Time.deltaTime
        );
        playerSlots[0].camera.transform.LookAt(p1Goal + Vector3.up * 1f);

        // Cámaras 2 a 4: siguen a su respectivo robot
        for (int i = 1; i < playerSlots.Count; i++)
        {
            var slot = playerSlots[i];
            if (!slot.camera.enabled) continue;

            Vector3 desiredPos = slot.player.position + cameraOffset;
            slot.camera.transform.position = Vector3.Lerp(
                slot.camera.transform.position,
                desiredPos,
                smoothFollowSpeed * Time.deltaTime
            );
            slot.camera.transform.LookAt(slot.player.position + Vector3.up * 1f);
        }
    }

    private Vector3 CalculateCentroid()
    {
        if (playerSlots.Count == 0) return Vector3.zero;

        Vector3 sum = Vector3.zero;
        for (int i = 0; i < playerSlots.Count; i++)
        {
            sum += playerSlots[i].player.position;
        }
        return sum / playerSlots.Count;
    }
}
