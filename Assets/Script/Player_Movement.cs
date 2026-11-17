using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Control de movimiento, sprint y salto del jugador en primera persona.
/// Cumple con las especificaciones de velocidad direccional, salto y movimiento aéreo.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Player_Movement : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float speed = 5f;
    public float jumpForce = 5f;
    public bool isGrounded = true;

    private Rigidbody rb;
    private InputSystem_Actions input;
    private Vector3 moveDir;
    private float currentSpeed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        input = new InputSystem_Actions();
        input.Enable();
    }

    private void OnDestroy()
    {
        input?.Dispose();
    }

    private void Update()
    {
        // 1. Detección de suelo mediante Raycast desde el centro del cuerpo hacia abajo
        int mask = ~LayerMask.GetMask("Player", "Ignore Raycast");
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.down, 1.15f, mask);

        // 2. Lectura de entrada de movimiento WASD / Stick
        Vector2 move = input != null ? input.Player.Move.ReadValue<Vector2>() : Vector2.zero;
        if (move.sqrMagnitude > 1f) move.Normalize();

        // Modificadores según dirección: adelante (100%), lados (75%), atrás (50%)
        float forward = move.y > 0 ? move.y : move.y * 0.5f;
        float strafe = move.x * 0.75f;
        moveDir = transform.right * strafe + transform.forward * forward;

        // 3. Sprint con Shift (solo en suelo y hacia adelante: velocidad al 200%)
        bool shift = (input?.Player.Sprint.IsPressed() ?? false);
        bool canSprint = isGrounded && shift && move.y > 0;
        currentSpeed = canSprint ? (speed * 2f) : speed;

        // 4. Movimiento en el aire reducido al 50%
        if (!isGrounded)
        {
            currentSpeed *= 0.5f;
        }

        // 5. Salto (solo si está en el suelo)
        bool jump = (input?.Player.Jump.WasPressedThisFrame() ?? false) || (Keyboard.current?.spaceKey.wasPressedThisFrame ?? false);
        if (jump && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            isGrounded = false;
        }
    }

    private void FixedUpdate()
    {
        Vector3 targetVel = moveDir * currentSpeed;
        targetVel.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVel;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (rb.linearVelocity.y <= 0.1f)
        {
            isGrounded = true;
        }
    }
}