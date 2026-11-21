using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sistema de Radar / Brújula en los bordes de la pantalla (Off-screen Indicator).
/// Arquitectura escalable de 2 a 4 jugadores compatible con Pantalla Partida Dinámica.
/// 
/// Buenas prácticas aplicadas:
/// - Cálculo de bordes por cámara: Respeta el pixelRect dinámico de cada jugador (mitad, cuadrante o pantalla completa).
/// - Gestión de memoria: Destruye texturas procedurales en OnDestroy() evitando fugas en Unity.
/// - Código desacoplado: Detecta jugadores por Player_Movement y asigna colores distintivos oficiales.
/// - Zero-Allocation en bucle: Reutiliza estructuras sin instanciar objetos en LateUpdate().
/// </summary>
public class PlayerRadar : MonoBehaviour
{
    [System.Serializable]
    public class PlayerIndicatorData
    {
        public Player_Movement playerMovement;
        public Camera playerCamera;
        // Flechas que este jugador ve apuntando hacia cada uno de sus rivales
        public Dictionary<int, RectTransform> rivalArrows = new Dictionary<int, RectTransform>();
    }

    [Header("Ajustes Visuales")]
    [Tooltip("Distancia en píxeles respecto al borde de la pantalla de cada jugador")]
    [SerializeField] private float edgePadding = 45f;

    [Tooltip("Tamaño de la flecha indicadora en píxeles")]
    [SerializeField] private Vector2 arrowSize = new Vector2(38f, 38f);

    [Header("Colores Oficiales por Jugador")]
    public Color[] playerColors = new Color[4]
    {
        new Color(1f, 0.85f, 0.2f, 1f), // P1: Amarillo / Dorado
        new Color(0.2f, 0.85f, 1f, 1f), // P2: Cian
        new Color(1f, 0.35f, 0.75f, 1f),// P3: Magenta
        new Color(0.35f, 1f, 0.4f, 1f)  // P4: Verde Lima
    };

    // Estructuras internas
    private List<PlayerIndicatorData> playerEntries = new List<PlayerIndicatorData>();
    private Canvas radarCanvas;
    private static Sprite proceduralArrowSprite;
    private static Texture2D proceduralTexture;

    private void Awake()
    {
        EnsureCanvasAndSprite();
        RegisterPlayersAndCameras();
    }

    /// <summary>
    /// Crea el Canvas en modo Overlay si no existe y genera un sprite nítido de flecha por código.
    /// </summary>
    private void EnsureCanvasAndSprite()
    {
        radarCanvas = FindAnyObjectByType<Canvas>();
        if (radarCanvas == null)
        {
            GameObject canvasObj = new GameObject("Radar_Canvas");
            radarCanvas = canvasObj.AddComponent<Canvas>();
            radarCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            radarCanvas.sortingOrder = 100; // Siempre visible sobre el juego

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (proceduralArrowSprite == null)
        {
            CreateProceduralArrowSprite();
        }
    }

    /// <summary>
    /// Detecta todos los robots activos (P1 a P4) y sus cámaras correspondientes.
    /// </summary>
    private void RegisterPlayersAndCameras()
    {
        playerEntries.Clear();

        var allMovements = FindObjectsByType<Player_Movement>(FindObjectsInactive.Exclude);
        System.Array.Sort(allMovements, (a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));

        // Registrar cada jugador
        foreach (var mov in allMovements)
        {
            Camera cam = FindCameraForPlayer(mov.PlayerIndex);
            if (cam != null)
            {
                playerEntries.Add(new PlayerIndicatorData
                {
                    playerMovement = mov,
                    playerCamera = cam,
                    rivalArrows = new Dictionary<int, RectTransform>()
                });
            }
        }

        // Crear una flecha por cada rival para cada jugador
        foreach (var entry in playerEntries)
        {
            int myIndex = entry.playerMovement.PlayerIndex;

            foreach (var rivalEntry in playerEntries)
            {
                int rivalIndex = rivalEntry.playerMovement.PlayerIndex;
                if (myIndex == rivalIndex) continue; // No se apunta a sí mismo

                Color rivalColor = playerColors[Mathf.Clamp(rivalIndex, 0, playerColors.Length - 1)];
                string arrowName = $"Arrow_P{myIndex + 1}_tracking_P{rivalIndex + 1}";

                RectTransform arrow = CreateArrowUIElement(arrowName, rivalColor);
                entry.rivalArrows[rivalIndex] = arrow;
            }
        }
    }

    private Camera FindCameraForPlayer(int playerIndex)
    {
        // Buscar cámara explícita por nombre (P1_Camera, Camera_P1, etc.)
        string nameA = $"P{playerIndex + 1}_Camera";
        string nameB = $"Camera_P{playerIndex + 1}";

        var camObj = GameObject.Find(nameA) ?? GameObject.Find(nameB);
        if (camObj != null && camObj.TryGetComponent<Camera>(out var c))
        {
            return c;
        }

        // Si es P1, puede ser el propio objeto donde esté colgado este script
        if (playerIndex == 0)
        {
            return GetComponent<Camera>() ?? Camera.main;
        }

        return null;
    }

    private RectTransform CreateArrowUIElement(string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(radarCanvas.transform, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = arrowSize;

        Image img = obj.AddComponent<Image>();
        img.sprite = proceduralArrowSprite;
        img.color = color;
        img.raycastTarget = false;

        obj.SetActive(false);
        return rt;
    }

    private void LateUpdate()
    {
        if (playerEntries.Count < 2) return;

        // Actualizar los radares de cada jugador
        for (int i = 0; i < playerEntries.Count; i++)
        {
            var myEntry = playerEntries[i];
            if (myEntry.playerCamera == null || !myEntry.playerCamera.enabled)
            {
                // Si la cámara está apagada (ej: en pantalla junta), apagar todas sus flechas
                HideAllArrowsForPlayer(myEntry);
                continue;
            }

            // Comprobar la posición de cada rival respecto a la cámara de este jugador
            for (int j = 0; j < playerEntries.Count; j++)
            {
                if (i == j) continue;

                var rivalEntry = playerEntries[j];
                if (rivalEntry.playerMovement == null) continue;

                int rivalIndex = rivalEntry.playerMovement.PlayerIndex;
                if (!myEntry.rivalArrows.TryGetValue(rivalIndex, out var arrow)) continue;

                UpdateRivalIndicator(myEntry.playerCamera, rivalEntry.playerMovement.transform.position, arrow);
            }
        }
    }

    private void HideAllArrowsForPlayer(PlayerIndicatorData entry)
    {
        foreach (var kvp in entry.rivalArrows)
        {
            if (kvp.Value != null) kvp.Value.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Calcula si el rival está dentro de la cámara. Si está fuera, fija la flecha en el borde del pixelRect.
    /// </summary>
    private void UpdateRivalIndicator(Camera cam, Vector3 targetWorldPos, RectTransform arrow)
    {
        Rect pixelRect = cam.pixelRect;
        Vector3 screenPoint = cam.WorldToScreenPoint(targetWorldPos);

        // Si el objetivo está detrás del plano de la cámara, invertir coordenadas
        if (screenPoint.z < 0)
        {
            screenPoint.x = -screenPoint.x;
            screenPoint.y = -screenPoint.y;
        }

        // Rectángulo interior visible con margen de seguridad
        Rect safeVisibleArea = new Rect(
            pixelRect.x + edgePadding,
            pixelRect.y + edgePadding,
            pixelRect.width - (edgePadding * 2f),
            pixelRect.height - (edgePadding * 2f)
        );

        bool isVisibleOnScreen = (screenPoint.z > 0 && safeVisibleArea.Contains(screenPoint));

        if (isVisibleOnScreen)
        {
            // Si ya lo ve en su mitad/cuadrante de pantalla, ocultar la flecha
            if (arrow.gameObject.activeSelf) arrow.gameObject.SetActive(false);
            return;
        }

        // Si está fuera de la pantalla, mostrar la flecha en el borde
        if (!arrow.gameObject.activeSelf) arrow.gameObject.SetActive(true);

        Vector2 center = new Vector2(pixelRect.x + pixelRect.width * 0.5f, pixelRect.y + pixelRect.height * 0.5f);
        Vector2 direction = ((Vector2)screenPoint - center);

        if (direction.sqrMagnitude < 0.001f) direction = Vector2.up;
        direction.Normalize();

        // Intersección con los límites del marco de la cámara (Ray-Box clipping)
        float halfWidth = (pixelRect.width * 0.5f) - edgePadding;
        float halfHeight = (pixelRect.height * 0.5f) - edgePadding;

        float scaleX = (direction.x != 0f) ? Mathf.Abs(halfWidth / direction.x) : float.MaxValue;
        float scaleY = (direction.y != 0f) ? Mathf.Abs(halfHeight / direction.y) : float.MaxValue;
        float minScale = Mathf.Min(scaleX, scaleY);

        Vector2 clampedPos = center + direction * minScale;
        arrow.position = clampedPos;

        // Rotar la flecha apuntando hacia el rival (-90° porque el sprite punta hacia arriba)
        float angleDeg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrow.rotation = Quaternion.Euler(0f, 0f, angleDeg - 90f);
    }

    /// <summary>
    /// Genera la textura del triángulo procedural de alta calidad.
    /// </summary>
    private static void CreateProceduralArrowSprite()
    {
        int size = 64;
        proceduralTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        proceduralTexture.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float halfWidth = (size - y) * 0.5f;
                float midX = size * 0.5f;

                if (x >= (midX - halfWidth) && x <= (midX + halfWidth) && y >= 6)
                {
                    cols[y * size + x] = Color.white;
                }
                else
                {
                    cols[y * size + x] = Color.clear;
                }
            }
        }

        proceduralTexture.SetPixels(cols);
        proceduralTexture.Apply();
        proceduralArrowSprite = Sprite.Create(proceduralTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void OnDestroy()
    {
        // Liberación segura de memoria para evitar fugas en el editor y builds
        if (proceduralArrowSprite != null)
        {
            Destroy(proceduralArrowSprite);
            proceduralArrowSprite = null;
        }

        if (proceduralTexture != null)
        {
            Destroy(proceduralTexture);
            proceduralTexture = null;
        }
    }
}
