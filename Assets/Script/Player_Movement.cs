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

    // Propiedades públicas para consultar estado desde otros sistemas
    public int PlayerIndex => (int)playerID;
    public Vector3 MoveDirection => currentInputDirection;
    public Rigidbody RigidbodyComponent => rb;

    private void Awake()
    {
        InitializePhysics();

        // Forzar al robot a levantarse erguido (0 grados en X y Z) si estaba inclinado en la escena
        Vector3 currentAngles = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, currentAngles.y, 0f);
    }

    private void InitializePhysics()
    {
        rb = GetComponent<Rigidbody>();

        // REGLA CRUCIAL: Congelar rotación física en TODOS los ejes (X, Y, Z).
        // Si Y no está congelado en físicas, las colisiones le darán inercia de peonza/trompo infinito.
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Anular cualquier rotación física residual
        rb.angularVelocity = Vector3.zero;

        // Interpolación para movimiento fluido sin tirones visuales de cámara
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Detección continua para colisiones a alta velocidad
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Update()
    {
        ReadInputForPlayer();
    }

    private void FixedUpdate()
    {
        // Cancelar absolutamente cualquier momento angular físico para que nunca gire como peonza al estar quieto
        rb.angularVelocity = Vector3.zero;

        // Corrección de seguridad: si alguna colisión física desvía X o Z, corregir inmediatamente a vertical
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
}
