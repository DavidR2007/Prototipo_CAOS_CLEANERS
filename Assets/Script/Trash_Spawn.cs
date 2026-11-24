using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generador continuo de basura para Chaos Cleaners.
/// Spawnea un ítem de basura periódicamente (cada 0.5s por defecto)
/// dentro de la zona del BoxCollider, manteniendo el conteo bajo un límite máximo.
/// 
/// Características:
/// - Spawn cada 0.5 segundos (configurable en Inspector).
/// - Límite de basura simultánea en la arena (cuando se alcanza, deja de spawnear hasta que recojan basura).
/// - Probabilidades por tipo de basura.
/// - Limpieza automática de referencias nulas al destruir/recoger ítems.
/// </summary>
public class Trash_Spawn : MonoBehaviour
{
    [System.Serializable]
    public class TipoBasura
    {
        public GameObject prefab;
        [Tooltip("Peso de probabilidad (mayor número = más frecuente)")]
        public int probabilidad = 10;
    }

    [Header("Tipos de Basura y Probabilidad")]
    public TipoBasura[] prefabsBasura;

    [Header("Ritmo y Límite de Generación")]
    [Tooltip("Tiempo en segundos entre cada spawn")]
    public float intervaloSpawn = 0.5f;

    [Tooltip("Cantidad máxima de basura simultánea en la arena")]
    public int limiteCantidadBasura = 25;

    [Tooltip("Basura inicial generada al arrancar la partida")]
    public int cantidadInicial = 6;

    private BoxCollider zona;
    private readonly List<GameObject> basuraActiva = new List<GameObject>();
    private Coroutine spawnCoroutine;

    /// <summary>Cantidad de piezas de basura actualmente vivas en la escena.</summary>
    public int BasuraVivaCount => basuraActiva.Count;

    private void Awake()
    {
        zona = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        // Generar un pequeño lote inicial para que la arena no esté vacía al empezar
        int inicial = Mathf.Min(cantidadInicial, limiteCantidadBasura);
        for (int i = 0; i < inicial; i++)
        {
            SpawnBasura();
        }

        // Arrancar el ciclo de generación continua
        spawnCoroutine = StartCoroutine(RutinaSpawnContinuo());
    }

    private void OnDisable()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator RutinaSpawnContinuo()
    {
        WaitForSeconds espera = new WaitForSeconds(Mathf.Max(0.05f, intervaloSpawn));

        while (true)
        {
            yield return espera;

            // Limpiar de la lista los ítems que ya han sido recogidos o destruidos
            basuraActiva.RemoveAll(item => item == null);

            // Si aún no hemos alcanzado el límite, spawnear un nuevo ítem
            if (basuraActiva.Count < limiteCantidadBasura)
            {
                SpawnBasura();
            }
        }
    }

    public void SpawnBasura()
    {
        if (zona == null || prefabsBasura == null || prefabsBasura.Length == 0) return;

        // Calcular punto aleatorio dentro del volumen del BoxCollider
        Vector3 posicion = new Vector3(
            Random.Range(zona.bounds.min.x, zona.bounds.max.x),
            zona.bounds.center.y,
            Random.Range(zona.bounds.min.z, zona.bounds.max.z)
        );

        // Calcular probabilidad total ponderada
        int totalProbabilidad = 0;
        foreach (TipoBasura basura in prefabsBasura)
        {
            if (basura.prefab != null)
                totalProbabilidad += Mathf.Max(1, basura.probabilidad);
        }

        if (totalProbabilidad == 0) return;

        int aleatorio = Random.Range(0, totalProbabilidad);

        foreach (TipoBasura basura in prefabsBasura)
        {
            if (basura.prefab == null) continue;

            aleatorio -= Mathf.Max(1, basura.probabilidad);

            if (aleatorio < 0)
            {
                GameObject nuevoItem = Instantiate(
                    basura.prefab,
                    posicion,
                    Quaternion.Euler(0, Random.Range(0, 360), 0)
                );

                basuraActiva.Add(nuevoItem);
                break;
            }
        }
    }
}
