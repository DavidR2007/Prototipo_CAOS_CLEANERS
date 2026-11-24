using UnityEngine;

/// <summary>
/// Zona de eliminación / límite de caída (DeadZone) para Chaos Cleaners.
/// 
/// Cómo usarlo en Unity:
/// 1. Crea un GameObject vacío debajo del escenario (ej: en Y = -10).
/// 2. Añádele un BoxCollider grande que cubra todo el suelo y marca la casilla 'Is Trigger'.
/// 3. Añádele este script 'DeadZone'.
/// 
/// Comportamiento:
/// - Si cae un Jugador (Player_Movement): lo devuelve a su posición de inicio y frena su velocidad a 0.
/// - Si cae Basura (Trash): la destruye inmediatamente para no acumular objetos en el vacío.
/// - Si cae cualquier otro objeto físico: también lo destruye como medida de seguridad.
/// </summary>
public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. Detectar si lo que cayó es un jugador
        Player_Movement player = other.GetComponent<Player_Movement>() ?? other.GetComponentInParent<Player_Movement>();
        if (player != null)
        {
            player.Respawn();
            Debug.Log($"[DeadZone] Jugador {player.PlayerIndex + 1} cayó fuera del mapa y reapareció en su posición inicial.");
            return;
        }

        // 2. Detectar si lo que cayó es basura
        Trash basura = other.GetComponent<Trash>() ?? other.GetComponentInParent<Trash>();
        if (basura != null)
        {
            Destroy(basura.gameObject);
            return;
        }

        // 3. Fallback: cualquier objeto con rigidbody que caiga al vacío se destruye para limpiar memoria
        if (other.attachedRigidbody != null)
        {
            Destroy(other.attachedRigidbody.gameObject);
        }
        else
        {
            Destroy(other.gameObject);
        }
    }
}
