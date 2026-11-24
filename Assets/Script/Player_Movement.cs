using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador de movimiento para Chaos Cleaners.
/// Arquitectura escalable de 1 a 4 jugadores con soporte simultáneo de Teclado y Gamepads.
/// 
/// Buenas prácticas aplicadas:
/// - Físicas sólidas: Rigidbody con restricciones en X y Z para evitar que las cápsulas/robots vuelquen.
/// - Inercia controlada: Amortiguación suave al soltar controles para evitar efecto "hielo".
/// - Detección continua de colisiones (ContinuousDynamic) para evitar atravesar paredes.
/// - Entrada desacoplada: cada jugador tiene su canal de teclado y gamepad independiente.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Player_Movement : MonoBehaviour
{
    public enum PlayerID
    {
        Player1 = 0, // WASD       | Gamepad 1
        Player2 = 1, // Flechas    | Gamepad 2
        Player3 = 2, // I-J-K-L    | Gamepad 3
        Player4 = 3  // T-F-G-H    | Gamepad 4 (o Numpad 8-4-5-6)
    }

    [Header("Identificación")]
    [Tooltip("Índice de jugador (P1 a P4)")]
    public PlayerID playerID = PlayerID.Player1;

    [Header("Indicador de Base (Círculo de Color)")]
    [Tooltip("Muestra un círculo de color bajo los pies del robot para diferenciarlo al instante.")]
    public bool showBaseCircle = true;

    [Tooltip("Diámetro del círculo bajo el robot")]
    [SerializeField] private float baseCircleDiameter = 1.4f;

    [Tooltip("Color personalizado (si se deja transparente, usa el color oficial asignado a este PlayerID)")]
    public Color customCircleColor = Color.clear;

    // Paleta oficial compartida para sincronía perfecta con el Radar y la guía
    public static readonly Color[] DefaultPlayerColors = new Color[4]
    {
        new Color(1f, 0.85f, 0.2f, 1f), // P1: Amarillo / Dorado
        new Color(0.2f, 0.85f, 1f, 1f), // P2: Cian
        new Color(1f, 0.35f, 0.75f, 1f),// P3: Magenta
        new Color(0.35f, 1f, 0.4f, 1f)  // P4: Verde Lima
    };

    [Header("Parámetros de Conducción")]
    [Tooltip("Velocidad de traslación horizontal")]
    [SerializeField] private float speed = 12f;

    [Tooltip("Velocidad de giro suave hacia la dirección de avance")]
    [SerializeField] private float turnSpeed = 16f;

    [Tooltip("Fuerza de frenada/amortiguación al soltar los controles")]
    [SerializeField] private float deceleration = 25f;

    [Header("Modelo Visual (Opcional)")]
    [Tooltip("Si tienes un modelo hijo que deba rotar o teñirse, asígnalo aquí; de lo contrario rotará la propia cápsula")]
    [SerializeField] private Transform visualTransform;

    // Componentes internos
    private Rigidbody rb;
    private Vector3 currentInputDirection;
    private bool isInputActive;
    private SpriteRenderer baseCircleRenderer;
    private static Sprite baseRingSprite;
    private static Texture2D baseRingTexture;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // Propiedades públicas para consultar estado desde otros sistemas
    public int PlayerIndex => (int)playerID;
    public Vector3 MoveDirection => currentInputDirection;
    public Rigidbody RigidbodyComponent => rb;

    private void Awake()
    {
        InitializePhysics();

        // Guardar posición y orientación inicial para reaparición en caso de caída a la DeadZone
        initialPosition = transform.position;
        initialRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        // Forzar al robot a levantarse erguido (0 grados en X y Z) si estaba inclinado en la escena
        transform.rotation = initialRotation;
    }

    /// <summary>
    /// Devuelve al jugador a su posición inicial y anula cualquier inercia física (velocidad lineal y angular).
    /// </summary>
    public void Respawn()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        currentInputDirection = Vector3.zero;
        isInputActive = false;
    }

    private void Start()
    {
        if (showBaseCircle)
        {
            SetupBaseCircleIndicator();
        }
    }

    private void InitializePhysics()
    {
        rb = GetComponent<Rigidbody>();


        rb.constraints = RigidbodyConstraints.FreezeRotation;

        rb.angularVelocity = Vector3.zero;


        rb.interpolation = RigidbodyInterpolation.Interpolate;


        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Update()
    {
        ReadInputForPlayer();
    }

    private void FixedUpdate()
    {

        rb.angularVelocity = Vector3.zero;


        Vector3 euler = transform.eulerAngles;
        if (Mathf.Abs(euler.x) > 0.01f || Mathf.Abs(euler.z) > 0.01f)
        {
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
        }

        ApplyMovement();
    }

    /// <summary>
    /// Lee la entrada asignada a este jugador de forma completamente aislada para evitar conflictos.
    /// Soporta hasta 4 esquemas de teclado en la misma máquina y asignación a 4 mandos.
    /// </summary>
    private void ReadInputForPlayer()
    {
        float horizontal = 0f;
        float vertical = 0f;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            switch (playerID)
            {
                case PlayerID.Player1:
                    // WASD
                    if (keyboard.aKey.isPressed) horizontal -= 1f;
                    if (keyboard.dKey.isPressed) horizontal += 1f;
                    if (keyboard.wKey.isPressed) vertical += 1f;
                    if (keyboard.sKey.isPressed) vertical -= 1f;
                    break;

                case PlayerID.Player2:
                    // Flechas del cursor
                    if (keyboard.leftArrowKey.isPressed) horizontal -= 1f;
                    if (keyboard.rightArrowKey.isPressed) horizontal += 1f;
                    if (keyboard.upArrowKey.isPressed) vertical += 1f;
                    if (keyboard.downArrowKey.isPressed) vertical -= 1f;
                    break;

                case PlayerID.Player3:
                    // IJKL
                    if (keyboard.jKey.isPressed) horizontal -= 1f;
                    if (keyboard.lKey.isPressed) horizontal += 1f;
                    if (keyboard.iKey.isPressed) vertical += 1f;
                    if (keyboard.kKey.isPressed) vertical -= 1f;
                    break;

                case PlayerID.Player4:
                    // TFGH o Teclado Numérico (8, 4, 5, 6)
                    if (keyboard.fKey.isPressed || keyboard.numpad4Key.isPressed) horizontal -= 1f;
                    if (keyboard.hKey.isPressed || keyboard.numpad6Key.isPressed) horizontal += 1f;
                    if (keyboard.tKey.isPressed || keyboard.numpad8Key.isPressed) vertical += 1f;
                    if (keyboard.gKey.isPressed || keyboard.numpad5Key.isPressed) vertical -= 1f;
                    break;
            }
        }

        // Lectura de Gamepad correspondiente al índice del jugador (Gamepad 0, 1, 2, 3)
        int padIndex = (int)playerID;
        if (Gamepad.all.Count > padIndex && Gamepad.all[padIndex] != null)
        {
            Vector2 stick = Gamepad.all[padIndex].leftStick.ReadValue();
            if (stick.magnitude > 0.15f) // Zona muerta contra stick drift
            {
                horizontal = stick.x;
                vertical = stick.y;
            }
        }

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical);
        if (inputDir.sqrMagnitude > 1f)
        {
            inputDir.Normalize();
        }

        currentInputDirection = inputDir;
        isInputActive = currentInputDirection.sqrMagnitude > 0.05f;
    }

    /// <summary>
    /// Aplica velocidad física lineal conservando la gravedad natural en el eje Y.
    /// </summary>
    private void ApplyMovement()
    {
        Vector3 currentVel = rb.linearVelocity;

        if (isInputActive)
        {
            // Conducción activa hacia la dirección deseada
            Vector3 targetVelocity = new Vector3(
                currentInputDirection.x * speed,
                currentVel.y,
                currentInputDirection.z * speed
            );

            rb.linearVelocity = targetVelocity;

            // Orientación suave EXCLUSIVAMENTE en el eje Y (vertical)
            // Esto bloquea a 0 grados X y Z matemáticamente, impidiendo que el robot se incline o tumbe
            float targetAngle = Mathf.Atan2(currentInputDirection.x, currentInputDirection.z) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.y;
            float smoothedAngle = Mathf.LerpAngle(currentAngle, targetAngle, turnSpeed * Time.fixedDeltaTime);

            transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
        }
        else
        {
            // Amortiguación gradual en el plano horizontal (X/Z) sin frenazo seco ni patinaje excesivo
            float newX = Mathf.MoveTowards(currentVel.x, 0f, deceleration * Time.fixedDeltaTime);
            float newZ = Mathf.MoveTowards(currentVel.z, 0f, deceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(newX, currentVel.y, newZ);
        }
    }

    /// <summary>
    /// Configura el círculo de color brillante en la base del robot.
    /// </summary>
    public void SetupBaseCircleIndicator()
    {
        Transform existing = transform.Find("BaseColorCircle");
        GameObject ringObj;

        if (existing != null)
        {
            ringObj = existing.gameObject;
        }
        else
        {
            ringObj = new GameObject("BaseColorCircle");
            ringObj.transform.SetParent(transform, false);
        }

        ringObj.transform.localPosition = new Vector3(0f, 0.02f, 0f); 
        ringObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); 
        ringObj.transform.localScale = Vector3.one * baseCircleDiameter;

        
        if (!ringObj.TryGetComponent<SpriteRenderer>(out baseCircleRenderer) || baseCircleRenderer == null)
        {
            baseCircleRenderer = ringObj.AddComponent<SpriteRenderer>();
        }

        if (baseCircleRenderer != null)
        {
            baseCircleRenderer.sprite = GetOrCreateRingSprite();
            baseCircleRenderer.color = GetPlayerColor();
            baseCircleRenderer.sortingOrder = 1; // Se dibuja sobre el suelo
        }

        ringObj.SetActive(showBaseCircle);
    }

    public Color GetPlayerColor()
    {
        if (customCircleColor.a > 0.05f) return customCircleColor;
        int idx = Mathf.Clamp((int)playerID, 0, DefaultPlayerColors.Length - 1);
        return DefaultPlayerColors[idx];
    }

    private static Sprite GetOrCreateRingSprite()
    {
        if (baseRingSprite != null) return baseRingSprite;

        int size = 128;
        baseRingTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        baseRingTexture.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = (size * 0.5f) - 4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float norm = dist / radius;

                if (norm > 1f)
                {
                    cols[y * size + x] = Color.clear;
                }
                else if (norm >= 0.74f && norm <= 0.96f)
                {
                    
                    float edgeAlpha = 1f;
                    if (norm > 0.90f) edgeAlpha = Mathf.InverseLerp(0.96f, 0.90f, norm);
                    else if (norm < 0.78f) edgeAlpha = Mathf.InverseLerp(0.74f, 0.78f, norm);

                    cols[y * size + x] = new Color(1f, 1f, 1f, edgeAlpha * 0.95f);
                }
                else if (norm < 0.74f)
                {
                    
                    float innerGlow = Mathf.Lerp(0.20f, 0.08f, norm / 0.74f);
                    cols[y * size + x] = new Color(1f, 1f, 1f, innerGlow);
                }
                else
                {
                    cols[y * size + x] = Color.clear;
                }
            }
        }

        baseRingTexture.SetPixels(cols);
        baseRingTexture.Apply();
        baseRingSprite = Sprite.Create(baseRingTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return baseRingSprite;
    }

    private void OnValidate()
    {
        if (baseCircleRenderer != null)
        {
            baseCircleRenderer.gameObject.SetActive(showBaseCircle);
            baseCircleRenderer.color = GetPlayerColor();
            baseCircleRenderer.transform.localScale = Vector3.one * baseCircleDiameter;
        }
    }
}
