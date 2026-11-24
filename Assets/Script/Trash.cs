using UnityEngine;

/// <summary>
/// Representa un objeto de basura recogible en la arena.
/// </summary>
public class Trash : MonoBehaviour
{
    [Tooltip("Puntos que otorga esta pieza de basura al ser recogida")]
    public int puntos = 10;

    /// <summary>
    /// Llamado cuando un jugador recoge esta basura.
    /// Suma los puntos tanto al jugador como al marcador global y destruye el objeto.
    /// </summary>
    public void Recoger(int playerIndex = 0)
    {
        GestorPuntos.SumarPuntos(playerIndex, puntos);
        Destroy(gameObject);
    }
}
