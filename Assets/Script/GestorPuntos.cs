using UnityEngine;

public class GestorPuntos : MonoBehaviour
{
    public static int puntosTotales = 0;

    public static void SumarPuntos(int puntos)
    {
        puntosTotales += puntos;

        Debug.Log("Puntuacion total: " + puntosTotales);
    }
}