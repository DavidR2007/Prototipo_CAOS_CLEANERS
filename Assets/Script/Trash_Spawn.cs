
using UnityEngine;

public class Trash_Spawn : MonoBehaviour
{
    [System.Serializable]
    public class TipoBasura
    {
        public GameObject prefab;
        public int probabilidad;
    }

    public TipoBasura[] prefabsBasura;

    public int cantidadBasura = 10;

    private BoxCollider zona;

    void Start()
    {
        zona = GetComponent<BoxCollider>();

        for (int i = 0; i < cantidadBasura; i++)
        {
            SpawnBasura();
        }
    }

    void SpawnBasura()
    {
        Vector3 posicion = new Vector3(
            Random.Range(zona.bounds.min.x, zona.bounds.max.x),
            zona.bounds.center.y,
            Random.Range(zona.bounds.min.z, zona.bounds.max.z)
        );

        int totalProbabilidad = 0;

        foreach (TipoBasura basura in prefabsBasura)
        {
            totalProbabilidad += basura.probabilidad;
        }

        int aleatorio = Random.Range(0, totalProbabilidad);

        foreach (TipoBasura basura in prefabsBasura)
        {
            aleatorio -= basura.probabilidad;

            if (aleatorio < 0)
            {
                Instantiate(
                    basura.prefab,
                    posicion,
                    Quaternion.Euler(0, Random.Range(0, 360), 0)
                );

                break;
            }
        }
    }
}
