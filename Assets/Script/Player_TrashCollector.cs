using UnityEngine;

public class Player_TrashCollector : MonoBehaviour
{
    private int puntosJugador = 0;

    private Player_Movement playerMovement;

    private void Awake()
    {
        playerMovement = GetComponent<Player_Movement>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Trash basura = other.GetComponent<Trash>();

        if (basura != null)
        {
            puntosJugador += basura.puntos;

            Debug.Log("Player " + (playerMovement.PlayerIndex + 1) +
                      " ha conseguido " + basura.puntos +
                      " puntos. Total: " + puntosJugador);

            Destroy(basura.gameObject);
        }
    }
}