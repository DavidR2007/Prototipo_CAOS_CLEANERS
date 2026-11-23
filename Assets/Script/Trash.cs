using UnityEngine;

public class Trash : MonoBehaviour
{
    public int puntos = 10;

    public void Recoger()
    {
        GestorPuntos.SumarPuntos(puntos);

        Destroy(gameObject);
    }
}