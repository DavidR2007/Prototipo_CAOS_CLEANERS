using UnityEngine;

/// <summary>
/// Gestiona la recolección de basura por parte de cada jugador y actualiza su puntuación.
/// </summary>
public class Player_TrashCollector : MonoBehaviour
{
    private int puntosJugador = 0;
    private Player_Movement playerMovement;

    public int PuntosJugador => puntosJugador;

    private void Awake()
    {
        playerMovement = GetComponent<Player_Movement>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Trash basura = other.GetComponent<Trash>() ?? other.GetComponentInParent<Trash>();

        if (basura != null)
        {
            int index = playerMovement != null ? playerMovement.PlayerIndex : 0;
            puntosJugador += basura.puntos;

            // Recoger la basura (suma puntos en GestorPuntos y actualiza la UI del HUD)
            basura.Recoger(index);

            Debug.Log($"[Collector] Jugador {index + 1} recogió basura (+{basura.puntos} pts). Total Jugador: {puntosJugador}");
        }
    }
}
