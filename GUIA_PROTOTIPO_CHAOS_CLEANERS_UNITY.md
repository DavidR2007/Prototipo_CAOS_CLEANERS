33# GUÍA MAESTRA DE DESARROLLO DEL PROTOTIPO EN UNITY

## PROYECTO: CHAOS CLEANERS (IDEA 2) — 2º CURSO DE VIDEOJUEGOS (ENTI-UB)

---

## 1. RESUMEN EJECUTIVO Y ALCANCE DEL PROTOTIPO (MVP)

### 1.1. Visión y Propósito del Prototipo

El objetivo de este prototipo **NO** es construir un juego final con decenas de niveles, skins y menús complejos, sino validar y demostrar de manera contundente la **Mecánica Principal (Core Mechanic)** y el **Elemento Diferencial (USP / Hook)** en un entorno jugable estable y divertido.

- **Título del Proyecto:** _CHAOS CLEANERS (Magnetic Recyclers)_
- **Motor:** Unity (Versión recomendada: 2022.3 LTS o Unity 6 LTS con Universal Render Pipeline - URP).
- **Género:** 3D Physics-based Party Brawler / Arena Cleaner Local.
- **Mecánica Principal a Validar:** El ciclo de recolección magnética por absorción física, crecimiento de masa, trade-off de movilidad (_Dilema del Tamaño_), combate por embestidas (Dash) para robo de chatarra, defensa por Parry y bancarización con multiplicador en la trituradora central.
- **Duración de la Demostración:** Arco cerrado de **5 minutos** (1 minuto de Onboarding/Calentamiento + 3 minutos de Partida Competitiva + 1 minuto de Clímax y Podio de Resultados).
- **Modalidad:** Multijugador local en pantalla compartida única (1 a 4 jugadores con mandos/teclado) + Modo Dummy/Bot para testeo individual continuo.

---

## 2. OBJETIVOS ACADÉMICOS Y CUMPLIMIENTO NORMATIVO ENTI-UB

Para garantizar la máxima calificación ante el tribunal evaluador (Rúbrica VIOD / AJEI de ENTI-UB), el prototipo integra de manera nativa todos los requisitos innegociables:

| Criterio ENTI-UB        | Requisito Exigido                             | Solución Implementada en este Guion                                                                                                                                                                                                               |
| :---------------------- | :-------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Normativa de Red**    | Prohibido multijugador online en tiempo real  | **Multijugador local en pantalla compartida única** (Shared Screen, sin split-screen).                                                                                                                                                            |
| **Controles**           | Esquema estándar PC (Mando / Teclado)         | Soporte nativo de hasta 4 Gamepads (Xbox/PlayStation) mediante el _New Input System_ de Unity + Mapeo simultáneo en Teclado.                                                                                                                      |
| **Plataforma**          | Build nativa para PC Windows                  | Ejecución nativa en PC a 60 FPS estables con cámara cenital/isométrica.                                                                                                                                                                           |
| **Dimensión Gráfica**   | Entorno 3D real y física mínima 2.5D/3D       | Escenario 3D industrial con simulación de físicas por Rigidbody y colisiones dinámicas.                                                                                                                                                           |
| **Arco Cerrado**        | Introducción, Nudo y Desenlace                | **Intro:** Onboarding orgánico (sala de desguace con dummies).<br>**Nudo:** Partida de 3 minutos con clímax a los últimos 30 segundos.<br>**Desenlace:** Fin de partida, podio con reciclaje cómico de los perdedores y pantalla de estadísticas. |
| **Menús Obligatorios**  | Splash screen, Menú Principal y Menú de Pausa | - Splash screen con logos.<br>- Menú principal: _Iniciar Partida_, _Créditos_, _Salir_.<br>- Menú de pausa funcional con _AudioMixer_ (volumen música, volumen SFX, sensibilidad y botón reanudar/salir).                                         |
| **UI No-Stock**         | Prohibido UI estándar de Unity por defecto    | Interfaz estilizada cartoon/industrial usando _TextMeshPro_, iconos de color por jugador y barras dinámicas de Dash/Parry.                                                                                                                        |
| **Código Propio (95%)** | Arquitectura modular, limpia y comentada      | Scripts C# modulares basados en principios SOLID, desacoplados por eventos y fuertemente comentados para defensa técnica.                                                                                                                         |

---

## 3. DESGLOSE MECÁNICO: "EL DILEMA DEL TAMAÑO" (CORE LOOP)

El núcleo jugable fusiona la satisfacción de absorción progresiva de **Katamari Damacy / LocoRoco** con el impacto cinético y agresivo de **Ape Out**:

```
 ┌────────────────────────────────────────────────────────┐
 │                   BUCLE DE JUEGO NUCLEAR              │
 └────────────────────────────────────────────────────────┘
                            │
                            ▼
              ┌───────────────────────────┐
              │ 1. RECOLECCIÓN MAGNÉTICA  │
              │  Aspirar chatarra según   │
              │   tamaño actual del robot │
              └─────────────┬─────────────┘
                            │ (Aumenta volumen y masa)
                            ▼
              ┌───────────────────────────┐
              │ 2. EL DILEMA DEL TAMAÑO   │
              │  + Capacidad de puntos    │
              │  - 40% Velocidad y giro   │
              │  - No cabe en atajos      │
              └─────────────┬─────────────┘
                            │
        ┌───────────────────┴───────────────────┐
        ▼                                       ▼
┌───────────────────────────┐       ┌───────────────────────────┐
│ 3A. COMBATE Y ROBO (DASH) │       │ 3B. DEFENSA ACTIVA (PARRY)│
│ Embestir a un rival para  │  vs   │ Escudo temporal en ventana│
│ fracturar su esfera y     │       │ de frames: aturde al rival│
│ robar la chatarra suelta  │       │ y salva tu cargamento     │
└─────────────┬─────────────┘       └─────────────┬─────────────┘
              │                                   │
              └───────────────────┬───────────────┘
                                  │
                                  ▼
              ┌───────────────────────────┐
              │  4. BANCARIZACIÓN CRUSHER │
              │ Entregar en la trituradora│
              │ Multiplicador por volumen │
              │ Reseteo a tamaño rápido   │
              └───────────────────────────┘
```

### 3.1. Niveles de Chatarra (Tiers)

Para que el crecimiento tenga sentido lúdico, la chatarra se divide en 3 Tiers con pesos y umbrales de absorción:

- **Tier 1 (Chatarra Menuda):** Tornillos, tuercas, cables, latas aplastadas.
  - _Peso:_ 1 kg cada una.
  - _Requisito:_ Accesible desde tamaño inicial (Radio 0.6m).
  - _Propósito:_ Combustible de arranque de partida.
- **Tier 2 (Chatarra Mediana):** Monitores rotos, baterías de coche, cajas de metal, tuberías.
  - _Peso:_ 5 kg cada una.
  - _Requisito:_ Requiere haber alcanzado al menos 15 kg de carga (Radio ~0.9m).
- **Tier 3 (Maquinaria Pesada):** Turbinas averiadas, bidones industriales, carcasas de servidores.
  - _Peso:_ 15 kg cada una.
  - _Requisito:_ Requiere al menos 40 kg de carga (Radio ~1.3m).
  - _Propósito:_ Objetos de alto valor situados en zonas disputadas.

### 3.2. Fórmulas Matemáticas de Escalado

A medida que el robot acumula kilogramos de chatarra ($M_{scrap}$):

1. **Radio del Colisionador:**
   $$R(M) = R_{base} \cdot \left(1 + \frac{M_{scrap}}{100}\right)^{0.4}$$
   _El crecimiento utiliza una raíz para simular volumen esférico real, evitando que la esfera se vuelva inmanejable de golpe._
2. **Velocidad Máxima (Hándicap de movilidad):**
   $$V(M) = \max\left(V_{min}, V_{base} - \left(\frac{M_{scrap}}{M_{max}}\right) \cdot (V_{base} \cdot 0.40)\right)$$
   _Un robot lleno pierde exactamente hasta el 40% de su velocidad máxima._
3. **Inercia y Frenada (Drag físico):**
   La masa del `Rigidbody` aumenta proporcionalmente:
   $$Masa_{total} = Masa_{base} + M_{scrap} \cdot 0.5$$
   _Esto genera que a mayor tamaño, mayor sea la distancia necesaria para frenar y más difícil sea maniobrar en curvas._

### 3.3. Mecánica de Dash (Embestida) y Robo Físico

- Al pulsar el botón de acción, el robot genera una aceleración instantánea impulsada hacia adelante durante 0.25 segundos.
- **Cooldown:** 2.5 segundos (indicado mediante un arco radial en la UI sobre el robot).
- **Colisión con otro jugador durante el Dash:**
  - Si el atacante impacta contra un rival que tiene chatarra acumulada, se produce una **fractura magnética**.
  - El rival pierde el **40% de su chatarra adherida**, la cual se desprende y sale despedida en un cono físico radial con fuerza hacia el suelo.
  - El rival sufre un aturdimiento (_Stun_) de 0.6 segundos donde pierde el control de giro.
  - Ambos jugadores (y los demás alrededor) pueden lanzarse inmediatamente a aspirar la chatarra suelta.

### 3.4. Mecánica de Parry (Contraataque / Escudo Deflector)

- Si un jugador ve venir una embestida, puede presionar el botón de defensa.
- Se activa un destello magnético azul durante una ventana estricta de **0.25 segundos (15 frames a 60 FPS)**.
- **Si el atacante colisiona durante el Parry:**
  - El atacante es rechazado hacia atrás con el **doble de fuerza**.
  - El atacante queda aturdido durante 1.2 segundos y pierde el **25% de su propia chatarra**.
  - El jugador defensor emite un sonido metálico glorioso (_Clang!_) y mantiene el 100% de su carga.
  - **Cooldown de fallo:** Si el jugador pulsa Parry y no bloquea nada, sufre un tiempo de penalización de 1.8 segundos sin poder volver a usarlo, evitando el _spam_.

### 3.5. La Trituradora Industrial (Bancarización y Multiplicador)

- En el centro del mapa se ubica un gran pozo con rodillos giratorios industriales y luces de advertencia.
- La chatarra adherida a la esfera son **puntos en riesgo**. Solo suben al marcador oficial cuando se depositan en la trituradora.
- **Tiempo de depósito:** Entrar en el área de la trituradora activa la succión automática: la chatarra se despega del robot y es tragada a razón de 30 kg por segundo.
- **Tabla de Multiplicadores por Entrega (Risk vs. Reward):**
  - Entrega < 25 kg: Multiplicador $\times 1.0$ (Entrega segura pero de bajo rendimiento).
  - Entrega entre 25 kg y 60 kg: Multiplicador $\times 1.25$.
  - Entrega > 60 kg: Multiplicador $\times 1.50$ (¡Máximo riesgo de ser embestido en la puerta!).
- **Efecto de reseteo:** Al vaciarse, el robot recupera instantáneamente su tamaño diminuto, su masa baja y su velocidad punta máxima, permitiéndole escapar a toda velocidad de la trituradora antes de que otro gigante lo aplaste.

---

## 4. ESTRUCTURA Y GUION DE LA PARTIDA (5 MINUTOS)

Para cumplir con la exigencia formal de ENTI de un **arco cerrado de 5 minutos**:

```
[0:00 - 0:45] ONBOARDING / LOBBY DINÁMICO
  - Zona de pruebas con suelo marcado y carteles holográficos simples.
  - Generador de chatarra infinita y 1 Dummy estático para probar el Dash.
  - Paso de compuerta para validar que todos los jugadores saben moverse y usar el botón.
       │
       ▼
[0:45 - 3:45] RONDA COMPETITIVA PRINCIPAL (3 MINUTOS)
  - 0:45 a 1:45 (Fase Inicial): Spawn masivo de Tier 1. Disputa por los primeros 25 kg.
  - 1:45 a 2:45 (Fase Media): Aparición de Tier 2 y Tier 3. Comienzan las primeras entregas.
  - 2:45 a 3:45 (Fase Clímax / Últimos 60s):
      * Luz de emergencia estroboscópica roja en la arena.
      * Multiplicador global en la trituradora aumentado un +20%.
      * Música acelerada (estilo percusión de Ape Out).
      * Caída de "Contenedor Dorado" de chatarra especial en el centro.
       │
       ▼
[3:45 - 5:00] DESENLACE Y PODIO DE RESULTADOS
  - Silbatazo / Alarma de fin de tiempo: Físicas congeladas.
  - Cámara viaja a la plataforma de premiación en lo alto de la estación.
  - El robot ganador baila en el puesto #1 con confeti de chispas.
  - Los 3 robots perdedores son empujados cómicamente por brazos mecánicos a la trituradora.
  - Pantalla UI con desglose de estadísticas (Kg reciclados, Robos con éxito, Parries ejecutados).
  - Botones de "Jugar de Nuevo" o "Volver al Menú Principal".
```

---

## 5. ARQUITECTURA TÉCNICA EN UNITY (C#)

Para asegurar la máxima nota en el apartado de **Proyecto (Calidad de código, buenas prácticas y arquitectura)**, el proyecto sigue una arquitectura modular en componentes desacoplados:

```
Assets/
├── _Project/
│   ├── Audio/               # SFX y Música con AudioMixer configurado
│   ├── Materials/           # Materiales URP (colores vivos cartoon)
│   ├── Prefabs/
│   │   ├── Players/         # Prefab de Robot con variantes de color (1-4)
│   │   ├── Scrap/           # Prefabs de chatarra Tier 1, 2 y 3
│   │   ├── Props/           # Puertas, cintas transportadoras, trituradora
│   │   └── UI/              # HUD y Canvas
│   ├── Scenes/
│   │   ├── 00_SplashScreen
│   │   ├── 01_MainMenu
│   │   └── 02_ArenaPrototipo
│   └── Scripts/
│       ├── Core/            # GameManager, MatchTimer, AudioManager
│       ├── Player/          # PlayerController, MagneticCollector, PlayerCombat
│       ├── Scrap/           # ScrapItem, ScrapSpawner
│       ├── Environment/     # CrusherZone, SizeGate, ConveyorBelt
│       ├── Camera/          # SharedCamera
│       └── UI/              # MainMenuUI, PauseMenuUI, HUDController, GameOverUI
```

---

## 6. CÓDIGOS FUENTE C# LISTOS PARA IMPLEMENTAR

A continuación se incluyen las implementaciones completas de los scripts nucleares del prototipo, diseñadas específicamente sin dependencias externas complejas para que puedas copiarlas y empezar a probar hoy mismo.

### 6.1. Definición de la Chatarra: `ScrapItem.cs`

Este script gestiona cada objeto recogible, su peso, su nivel y su transición de objeto libre en el suelo a objeto magnéticamente absorbido.

```csharp
using UnityEngine;

public enum ScrapTier
{
    Tier1_Small,   // Tornillos, latas (1 kg)
    Tier2_Medium,  // Monitores, cajas (5 kg)
    Tier3_Heavy    // Motores, maquinaria (15 kg)
}

[RequireComponent(typeof(Collider))]
public class ScrapItem : MonoBehaviour
{
    [Header("Propiedades de Chatarra")]
    public ScrapTier tier = ScrapTier.Tier1_Small;
    public float weightKg = 1f;
    public float minWeightToAbsorb = 0f; // Peso mínimo del robot para poder absorberlo

    [Header("Efectos")]
    [SerializeField] private AudioClip collectSfx;

    private Rigidbody rb;
    private Collider col;
    private bool isCollected = false;

    public bool IsCollected => isCollected;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    /// <summary>
    /// Desactiva las físicas pesadas al ser absorbido y lo emparenta a la esfera del robot.
    /// Esto evita problemas de rendimiento y explosiones de físicas en Unity.
    /// </summary>
    public void Collect(Transform playerTarget, Vector3 surfacePoint)
    {
        isCollected = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        col.isTrigger = true;
        transform.SetParent(playerTarget);
        transform.position = surfacePoint;
    }

    /// <summary>
    /// Desprende la chatarra del robot tras una embestida y le aplica un impulso físico en abanico.
    /// </summary>
    public void Scatter(Vector3 scatterImpulse)
    {
        isCollected = false;
        transform.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.AddForce(scatterImpulse, ForceMode.Impulse);
        }

        col.isTrigger = false;
    }
}
```

---

### 6.2. Controlador Físico del Robot: `PlayerController.cs`

Gestiona la conducción analógica de la bola, el cálculo del hándicap por masa y el escalado dimensional.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Identificación")]
    public int playerIndex = 0; // 0=P1, 1=P2, 2=P3, 3=P4
    public Color playerColor = Color.yellow;

    [Header("Parámetros de Movimiento")]
    [SerializeField] private float baseSpeed = 14f;
    [SerializeField] private float minSpeed = 8.4f; // 40% de reducción a máxima masa
    [SerializeField] private float turnSmoothing = 15f;
    [SerializeField] private float maxWeightCapacity = 100f;

    [Header("Escalado Físico")]
    [SerializeField] private float baseRadius = 0.6f;
    [SerializeField] private float baseMass = 10f;
    [SerializeField] private Transform visualModel;

    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private Vector2 inputVector;
    private float currentWeightKg = 0f;
    private bool isStunned = false;

    public float CurrentWeight => currentWeightKg;
    public float MaxWeight => maxWeightCapacity;
    public Rigidbody Rb => rb;
    public bool IsStunned => isStunned;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        rb.mass = baseMass;
        sphereCollider.radius = baseRadius;
    }

    public void OnMove(InputValue value)
    {
        if (isStunned) return;
        inputVector = value.Get<Vector2>();
    }

    // Soporte alternativo directo por teclado/código para testeo rápido sin Input System configurado
    public void SetRawInput(Vector2 dir)
    {
        if (isStunned)
        {
            inputVector = Vector2.zero;
            return;
        }
        inputVector = dir;
    }

    private void FixedUpdate()
    {
        if (isStunned) return;

        MoveCharacter();
    }

    private void MoveCharacter()
    {
        Vector3 direction = new Vector3(inputVector.x, 0f, inputVector.y).normalized;

        if (direction.magnitude >= 0.1f)
        {
            // Cálculo de velocidad según peso actual
            float weightT = Mathf.Clamp01(currentWeightKg / maxWeightCapacity);
            float effectiveSpeed = Mathf.Lerp(baseSpeed, minSpeed, weightT);

            Vector3 targetVelocity = direction * effectiveSpeed;
            targetVelocity.y = rb.linearVelocity.y; // Conservar gravedad

            rb.linearVelocity = targetVelocity;

            // Orientar el modelo visual suavemente hacia donde avanza
            Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
            visualModel.rotation = Quaternion.Slerp(visualModel.rotation, targetRot, turnSmoothing * Time.fixedDeltaTime);
        }
        else
        {
            // Amortiguación de inercia al soltar el mando
            Vector3 vel = rb.linearVelocity;
            vel.x = Mathf.MoveTowards(vel.x, 0, 20f * Time.fixedDeltaTime);
            vel.z = Mathf.MoveTowards(vel.z, 0, 20f * Time.fixedDeltaTime);
            rb.linearVelocity = vel;
        }
    }

    public void AddWeight(float weightDelta)
    {
        currentWeightKg = Mathf.Clamp(currentWeightKg + weightDelta, 0f, maxWeightCapacity);
        UpdatePhysicalScale();
    }

    public void ResetWeight()
    {
        currentWeightKg = 0f;
        UpdatePhysicalScale();
    }

    private void UpdatePhysicalScale()
    {
        // Crecimiento esférico por raíz cúbica aproximada
        float growthRatio = Mathf.Pow(1f + (currentWeightKg / 30f), 0.35f);
        float newRadius = baseRadius * growthRatio;

        sphereCollider.radius = newRadius;
        visualModel.localScale = Vector3.one * growthRatio;

        // Aumentar masa física del Rigidbody para que empuje con mayor fuerza
        rb.mass = baseMass + (currentWeightKg * 0.4f);
    }

    public void ApplyStun(float duration)
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(StunRoutine(duration));
    }

    private System.Collections.IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        inputVector = Vector2.zero;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }
}
```

---

### 6.3. Sistema de Absorción Magnética: `MagneticCollector.cs`

Detecta chatarra cercana, verifica si el robot tiene el peso requerido para ese Tier y la adhiere sin sobrecargar la CPU.

```csharp
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class MagneticCollector : MonoBehaviour
{
    [Header("Configuración Magnética")]
    [SerializeField] private float magnetBaseRadius = 1.8f;
    [SerializeField] private float pullSpeed = 12f;
    [SerializeField] private LayerMask scrapLayer;
    [SerializeField] private Transform scrapAttachmentRoot;

    private PlayerController player;
    private List<ScrapItem> attachedScrap = new List<ScrapItem>();

    public IReadOnlyList<ScrapItem> AttachedScrap => attachedScrap;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        ScanAndAttractScrap();
    }

    private void ScanAndAttractScrap()
    {
        // El radio magnético crece junto al robot
        float currentScanRadius = magnetBaseRadius * (player.transform.localScale.x);
        Collider[] hits = Physics.OverlapSphere(transform.position, currentScanRadius, scrapLayer);

        foreach (Collider hit in hits)
        {
            ScrapItem item = hit.GetComponent<ScrapItem>();
            if (item != null && !item.IsCollected)
            {
                // Comprobar si el robot tiene tamaño suficiente para absorber este Tier
                if (player.CurrentWeight >= item.minWeightToAbsorb)
                {
                    // Atraerlo magnéticamente
                    Vector3 dirToPlayer = (transform.position - item.transform.position).normalized;
                    item.transform.position = Vector3.MoveTowards(item.transform.position, transform.position, pullSpeed * Time.deltaTime);

                    // Si está en contacto con la superficie, adherirlo
                    float dist = Vector3.Distance(item.transform.position, transform.position);
                    if (dist <= (player.GetComponent<SphereCollider>().radius * 1.15f))
                    {
                        AttachScrap(item);
                    }
                }
            }
        }
    }

    private void AttachScrap(ScrapItem item)
    {
        // Punto de anclaje superficial alrededor de la bola
        Vector3 surfacePoint = transform.position + (item.transform.position - transform.position).normalized * (player.GetComponent<SphereCollider>().radius);
        item.Collect(scrapAttachmentRoot != null ? scrapAttachmentRoot : transform, surfacePoint);
        attachedScrap.Add(item);
        player.AddWeight(item.weightKg);

        // Feedback sonoro/visual
        AudioManager.Instance?.PlayClip(AudioManager.Instance.pickupSfx, 0.7f);
    }

    /// <summary>
    /// Desprende un porcentaje de la chatarra (usado tras recibir una embestida).
    /// </summary>
    public List<ScrapItem> DetachScrapPercentage(float percentage)
    {
        int countToRemove = Mathf.CeilToInt(attachedScrap.Count * Mathf.Clamp01(percentage));
        List<ScrapItem> scattered = new List<ScrapItem>();

        for (int i = 0; i < countToRemove && attachedScrap.Count > 0; i++)
        {
            int lastIndex = attachedScrap.Count - 1;
            ScrapItem item = attachedScrap[lastIndex];
            attachedScrap.RemoveAt(lastIndex);

            player.AddWeight(-item.weightKg);

            // Impulso radial en abanico
            Vector3 randomDir = (Random.insideUnitSphere + Vector3.up * 0.8f).normalized;
            item.Scatter(randomDir * Random.Range(5f, 9f));
            scattered.Add(item);
        }

        return scattered;
    }

    /// <summary>
    /// Remueve toda la chatarra (usado por la trituradora central).
    /// </summary>
    public List<ScrapItem> ExtractAllScrap()
    {
        List<ScrapItem> all = new List<ScrapItem>(attachedScrap);
        attachedScrap.Clear();
        player.ResetWeight();
        return all;
    }
}
```

---

### 6.4. Combate (Dash, Colisión y Parry): `PlayerCombat.cs`

El núcleo de la agresividad e interacción entre jugadores al estilo _Ape Out_.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(MagneticCollector))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Configuración de Dash")]
    [SerializeField] private float dashForce = 22f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashCooldown = 2.5f;

    [Header("Configuración de Parry")]
    [SerializeField] private float parryWindow = 0.25f; // Duración de la ventana de bloqueo activo
    [SerializeField] private float parryCooldown = 2.0f;
    [SerializeField] private GameObject parryShieldVisual;

    private PlayerController player;
    private MagneticCollector collector;
    private float lastDashTime = -10f;
    private float lastParryTime = -10f;
    private bool isDashing = false;
    private bool isParrying = false;

    public bool IsDashing => isDashing;
    public bool IsParrying => isParrying;
    public float DashCooldownNormalized => Mathf.Clamp01((Time.time - lastDashTime) / dashCooldown);

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        collector = GetComponent<MagneticCollector>();
        if (parryShieldVisual != null) parryShieldVisual.SetActive(false);
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed) TryDash();
    }

    public void OnParry(InputValue value)
    {
        if (value.isPressed) TryParry();
    }

    public void TryDash()
    {
        if (Time.time < lastDashTime + dashCooldown || player.IsStunned || isDashing) return;

        StartCoroutine(DashRoutine());
    }

    private System.Collections.IEnumerator DashRoutine()
    {
        isDashing = true;
        lastDashTime = Time.time;

        Vector3 dashDir = player.Rb.linearVelocity.normalized;
        if (dashDir.sqrMagnitude < 0.1f) dashDir = transform.forward;
        dashDir.y = 0;

        player.Rb.AddForce(dashDir * dashForce, ForceMode.Impulse);
        AudioManager.Instance?.PlayClip(AudioManager.Instance.dashSfx, 1f);

        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
    }

    public void TryParry()
    {
        if (Time.time < lastParryTime + parryCooldown || player.IsStunned || isDashing || isParrying) return;

        StartCoroutine(ParryRoutine());
    }

    private System.Collections.IEnumerator ParryRoutine()
    {
        isParrying = true;
        lastParryTime = Time.time;
        if (parryShieldVisual != null) parryShieldVisual.SetActive(true);

        yield return new WaitForSeconds(parryWindow);

        if (parryShieldVisual != null) parryShieldVisual.SetActive(false);
        isParrying = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Detectar si impactamos con otro robot
        PlayerController otherPlayer = collision.gameObject.GetComponent<PlayerController>();
        if (otherPlayer == null || otherPlayer == player) return;

        PlayerCombat otherCombat = otherPlayer.GetComponent<PlayerCombat>();
        if (otherCombat == null) return;

        // CASO 1: Nosotros embestimos al rival
        if (isDashing)
        {
            // ¿El rival nos hizo Parry exitoso?
            if (otherCombat.IsParrying)
            {
                // ¡Parry Exitoso del rival! El atacante sufre el castigo
                Vector3 bounceDir = -player.Rb.linearVelocity.normalized + Vector3.up * 0.4f;
                player.Rb.AddForce(bounceDir * (dashForce * 1.5f), ForceMode.Impulse);
                player.ApplyStun(1.2f);
                collector.DetachScrapPercentage(0.25f); // Pierde un 25% por arriesgarse

                CameraShake.Instance?.Shake(0.35f, 0.4f);
                AudioManager.Instance?.PlayClip(AudioManager.Instance.parryClangSfx, 1f);
            }
            else
            {
                // ¡Embestida certera! Despojar de chatarra al rival
                MagneticCollector otherCollector = otherPlayer.GetComponent<MagneticCollector>();
                if (otherCollector != null)
                {
                    otherCollector.DetachScrapPercentage(0.40f); // Robo del 40%
                }

                otherPlayer.ApplyStun(0.6f);
                Vector3 impactDir = (otherPlayer.transform.position - transform.position).normalized + Vector3.up * 0.2f;
                otherPlayer.Rb.AddForce(impactDir * (dashForce * 0.9f), ForceMode.Impulse);

                CameraShake.Instance?.Shake(0.25f, 0.3f);
                AudioManager.Instance?.PlayClip(AudioManager.Instance.bumpSfx, 1f);
            }

            isDashing = false;
        }
    }
}
```

---

### 6.5. Zona de Bancarización: `CrusherZone.cs`

Ubicado en el centro del mapa. Aspira la chatarra, calcula el multiplicador y suma puntos al marcador del equipo.

```csharp
using UnityEngine;

public class CrusherZone : MonoBehaviour
{
    [Header("Ajustes de Trituración")]
    [SerializeField] private float pointsPerKg = 10f;
    [SerializeField] private GameObject crusherVfx;
    [SerializeField] private Transform sinkCenter;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.CurrentWeight > 0)
        {
            ProcessPlayerDelivery(player);
        }
    }

    private void ProcessPlayerDelivery(PlayerController player)
    {
        MagneticCollector collector = player.GetComponent<MagneticCollector>();
        if (collector == null) return;

        float deliveredWeight = player.CurrentWeight;

        // Calcular multiplicador según volumen entregado (Risk vs Reward)
        float multiplier = 1.0f;
        if (deliveredWeight >= 60f)
        {
            multiplier = 1.50f; // Alto riesgo
        }
        else if (deliveredWeight >= 25f)
        {
            multiplier = 1.25f; // Riesgo medio
        }

        int finalPoints = Mathf.RoundToInt(deliveredWeight * pointsPerKg * multiplier);

        // Despegar y destruir visualmente los objetos reciclados
        var scrapList = collector.ExtractAllScrap();
        foreach (var item in scrapList)
        {
            if (crusherVfx != null)
            {
                Instantiate(crusherVfx, item.transform.position, Quaternion.identity);
            }
            Destroy(item.gameObject);
        }

        // Sumar al Game Manager
        GameManager.Instance?.AddScore(player.playerIndex, finalPoints, deliveredWeight);

        // Feedback audiovisual
        CameraShake.Instance?.Shake(0.15f, 0.2f);
        AudioManager.Instance?.PlayClip(AudioManager.Instance.crushSfx, 1f);
    }
}
```

---

### 6.6. Cámara Compartida Dinámica: `SharedCamera.cs`

Encuadra a todos los robots activos sin partir la pantalla (Shared-Screen), calculando el centroide y aplicando zoom suave.

```csharp
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SharedCamera : MonoBehaviour
{
    [Header("Objetivos a Seguir")]
    public List<Transform> targets = new List<Transform>();

    [Header("Ajustes de Encuadre")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 16f, -12f);
    [SerializeField] private float smoothTime = 0.25f;
    [SerializeField] private float minZoom = 12f;
    [SerializeField] private float maxZoom = 28f;
    [SerializeField] private float zoomLimiter = 25f;

    private Vector3 velocity;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (targets.Count == 0) return;

        MoveCamera();
        ZoomCamera();
    }

    private void MoveCamera()
    {
        Vector3 centerPoint = GetCenterPoint();
        Vector3 targetPos = centerPoint + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
    }

    private void ZoomCamera()
    {
        float greatestDistance = GetGreatestDistance();
        float newZoom = Mathf.Lerp(minZoom, maxZoom, greatestDistance / zoomLimiter);

        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, newZoom, Time.deltaTime * 3f);
        }
        else
        {
            // Ajustar posición en Y/Z si la cámara es de perspectiva
            float targetFieldOfView = Mathf.Lerp(45f, 68f, greatestDistance / zoomLimiter);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFieldOfView, Time.deltaTime * 3f);
        }
    }

    private Vector3 GetCenterPoint()
    {
        if (targets.Count == 1) return targets[0].position;

        var bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null && targets[i].gameObject.activeInHierarchy)
            {
                bounds.Encapsulate(targets[i].position);
            }
        }
        return bounds.center;
    }

    private float GetGreatestDistance()
    {
        var bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null && targets[i].gameObject.activeInHierarchy)
            {
                bounds.Encapsulate(targets[i].position);
            }
        }
        return Mathf.Max(bounds.size.x, bounds.size.z);
    }

    public void RegisterTarget(Transform target)
    {
        if (!targets.Contains(target)) targets.Add(target);
    }
}
```

---

### 6.7. IA de Prueba para Testeo Individual: `DummyBot.cs`

**Imprescindible para el desarrollo individual:** Permite probar el prototipo en cualquier momento tú solo, sin necesitar 3 personas para verificar que las embestidas, el parry y el robo funcionan.

```csharp
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerCombat))]
public class DummyBot : MonoBehaviour
{
    [Header("Comportamiento del Dummy")]
    [SerializeField] private bool isActiveAI = true;
    [SerializeField] private float dashInterval = 5f;
    [SerializeField] private Transform patrolCenter;
    [SerializeField] private float patrolRadius = 8f;

    private PlayerController controller;
    private PlayerCombat combat;
    private Vector3 currentDestination;
    private float nextDashTimer;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        combat = GetComponent<PlayerCombat>();
    }

    private void Start()
    {
        PickNewDestination();
        nextDashTimer = Time.time + dashInterval;
    }

    private void Update()
    {
        if (!isActiveAI || controller.IsStunned) return;

        // Moverse hacia el punto aleatorio
        Vector3 dir = (currentDestination - transform.position);
        dir.y = 0;

        if (dir.magnitude < 1.5f)
        {
            PickNewDestination();
        }
        else
        {
            Vector2 inputDir = new Vector2(dir.normalized.x, dir.normalized.z);
            controller.SetRawInput(inputDir);
        }

        // Ejecutar Dash periódicamente si ve a otro jugador delante
        if (Time.time >= nextDashTimer)
        {
            combat.TryDash();
            nextDashTimer = Time.time + Random.Range(dashInterval * 0.8f, dashInterval * 1.4f);
        }
    }

    private void PickNewDestination()
    {
        Vector2 randomPoint = Random.insideUnitCircle * patrolRadius;
        Vector3 basePos = patrolCenter != null ? patrolCenter.position : Vector3.zero;
        currentDestination = new Vector3(basePos.x + randomPoint.x, transform.position.y, basePos.z + randomPoint.y);
    }
}
```

---

### 6.8. Gestor de Partida y Temporizador: `GameManager.cs`

Controla el flujo de la partida, los 3 minutos de juego, la alarma del clímax y el resultado final.

```csharp
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Duración de Partida")]
    [SerializeField] private float matchDurationSeconds = 180f; // 3 minutos
    [SerializeField] private float climaxAlertThreshold = 45f;   // Últimos 45 segundos

    [Header("Marcador (4 Jugadores)")]
    private int[] playerScores = new int[4];
    private float[] playerTonnage = new float[4];

    private float timer;
    private bool isMatchActive = false;
    private bool isClimaxTriggered = false;

    public float RemainingTime => Mathf.Max(0f, timer);
    public bool IsMatchActive => isMatchActive;

    public static event Action<int, int> OnScoreUpdated; // (playerIndex, newScore)
    public static event Action OnClimaxStarted;
    public static event Action<int[]> OnMatchEnded;      // scores array

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        StartMatch();
    }

    public void StartMatch()
    {
        timer = matchDurationSeconds;
        isMatchActive = true;
        isClimaxTriggered = false;
        Array.Clear(playerScores, 0, playerScores.Length);
        Array.Clear(playerTonnage, 0, playerTonnage.Length);
    }

    private void Update()
    {
        if (!isMatchActive) return;

        timer -= Time.deltaTime;

        // Comprobación de Clímax
        if (!isClimaxTriggered && timer <= climaxAlertThreshold)
        {
            isClimaxTriggered = true;
            OnClimaxStarted?.Invoke();
            AudioManager.Instance?.PlayClip(AudioManager.Instance.climaxAlarmSfx, 1f);
        }

        // Fin de la partida
        if (timer <= 0f)
        {
            EndMatch();
        }
    }

    public void AddScore(int playerIndex, int scoreDelta, float weightKg)
    {
        if (playerIndex < 0 || playerIndex >= playerScores.Length) return;

        playerScores[playerIndex] += scoreDelta;
        playerTonnage[playerIndex] += weightKg;
        OnScoreUpdated?.Invoke(playerIndex, playerScores[playerIndex]);
    }

    private void EndMatch()
    {
        isMatchActive = false;
        OnMatchEnded?.Invoke(playerScores);
        Time.timeScale = 0.2f; // Cámara lenta dramática final
    }
}
```

---

### 6.9. Menú de Pausa con Control de Audio: `PauseMenuController.cs`

Cumple al 100% el requisito de tribunal de ENTI: _Menú de pausa funcional con opciones de volumen y salida._

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("Paneles UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Control de Audio (AudioMixer)")]
    [SerializeField] private AudioMixer mainAudioMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private bool isPaused = false;

    private void Start()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(false);

        // Inicializar sliders a volumen estándar
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7)) // Botón Start
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        if (!isPaused) optionsPanel.SetActive(false);

        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void OpenOptions()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void SetMusicVolume(float value)
    {
        // Conversión de slider lineal (0.0001 a 1) a decibelios (-80dB a 0dB)
        float dB = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f;
        mainAudioMixer.SetFloat("MusicVolume", dB);
    }

    public void SetSfxVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f;
        mainAudioMixer.SetFloat("SfxVolume", dB);
    }

    public void RestartMatch()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("01_MainMenu");
    }
}
```

---

## 7. DISEÑO DE LA ARENA DE PRUEBAS (GREYBOXING)

Para validar el prototipo de forma eficiente, la arena debe estructurarse en **3 Zonas Concéntricas** dentro de una nave industrial cuadrada de $36 \times 36$ metros:

```
┌────────────────────────────────────────────────────────┐
│  [ZONA 1: PERIFERIA EXTERIOR]                          │
│  - Pasillos estrechos con puertas automáticas (ancho 1.2m)│
│  - Generación constante de chatarra TIER 1 (Tornillos/Latas)│
│  - Refugio para robots pequeños que huyen de gigantes  │
│                                                        │
│       ┌────────────────────────────────────────┐       │
│       │  [ZONA 2: ANILLO INTERMEDIO]           │       │
│       │  - Cintas transportadoras móviles       │       │
│       │  - Cajas y monitores TIER 2             │       │
│       │  - Espacio de colisión y embestidas    │       │
│       │                                        │       │
│       │       ┌────────────────────────┐       │       │
│       │       │  [ZONA 3: NÚCLEO]      │       │       │
│       │       │  - TRITURADORA CENTRAL │       │       │
│       │       │  - Chatarra pesada TIER 3│      │       │
│       │       │  - Máximo riesgo       │       │       │
│       │       └────────────────────────┘       │       │
│       │                                        │       │
│       └────────────────────────────────────────┘       │
│                                                        │
└────────────────────────────────────────────────────────┘
```

### Elementos Interactivos Obligatorios en el Prototipo:

1. **Compuertas de Selección por Tamaño:**
   - Dos arcos con vigas de seguridad de 1.1 metros de ancho que conectan la periferia con el centro.
   - Un robot diminuto pasa a toda velocidad para escapar. Un robot gigante que intenta cruzar choca contra las columnas y queda atascado, forzándole a tomar el camino largo exterior donde puede ser embestido.
2. **Cintas Transportadoras:**
   - 2 franjas de suelo con un script de velocidad constante que empuja hacia el centro o hacia afuera.
   - Los robots ligeros son arrastrados fácilmente; los robots pesados tienen suficiente inercia para contrarrestar la fuerza.

---

## 8. GAME FEEL Y FEEDBACK AUDIOVISUAL ("JUICE")

El 10% de la nota de tribunal evalúa explícitamente el **Feedback audiovisual** (que el jugador entienda de forma inmediata cuándo acierta, cuándo falla y el impacto de sus acciones):

1. **Impact Stop (Hitstop de frames):**
   - Al impactar un Dash contra un rival, pausar el juego durante **0.05 segundos (50 milisegundos)** antes de aplicar la fuerza de retroceso. Da una sensación de golpe sólido y demoledor.
2. **Screen Shake (Sacudida de Cámara):**
   - Embestida con éxito: vibración de 0.25s con intensidad media.
   - Parry exitoso: vibración corta pero aguda de 0.35s con destello blanco en pantalla.
   - Entrega masiva en trituradora: retumbe continuo de baja frecuencia mientras se tritura la carga.
3. **Squash & Stretch (Animación procedural por código):**
   - Al ejecutar un Dash, la esfera se estira un 20% hacia adelante y se aplasta un 10% verticalmente.
   - Al impactar, se comprime frontalmente antes de rebotar.

---

## 9. PLAN DE PRODUCCIÓN SEMANAL (CHECKLIST DE DESARROLLO)

Para asegurar las revisiones binarias de clase y llegar al tribunal con nota máxima:

### Semana 1: Núcleo Físico y Movimiento

- [ ] Crear proyecto Unity en URP con configuración de físicas 3D estándar.
- [ ] Implementar `PlayerController.cs` con Rigidbody esférico.
- [ ] Configurar el sistema de Inputs (Mando + Teclado) para Player 1 y Dummy.
- [ ] Probar conducción básica, aceleración y frenada en suelo plano.

### Semana 2: Absorción Magnética y Escalado

- [ ] Crear prefabs de chatarra (Tier 1: tornillo/cubo pequeño; Tier 2: monitor/prisma; Tier 3: turbina/cilindro grande).
- [ ] Implementar `ScrapItem.cs` y `MagneticCollector.cs`.
- [ ] Programar la fórmula de crecimiento del radio y la reducción del 40% de velocidad punta.
- [ ] Verificar que la chatarra se adhiere sin causar explosiones de físicas.

### Semana 3: Combate (Dash, Robo y Parry)

- [ ] Implementar `PlayerCombat.cs`: impulso de Dash con cooldown.
- [ ] Programar la dispersión en abanico (_Scatter_) de la chatarra al colisionar.
- [ ] Añadir la ventana de frames de Parry y el rebote del atacante.
- [ ] Probar combate contra el `DummyBot.cs` para equilibrar fuerzas y tiempos.

### Semana 4: Bancarización, Arena y Cámara Compartida

- [ ] Montar el Greybox de la arena ($36 \times 36$ m) con la trituradora central y 2 compuertas estrechas.
- [ ] Implementar `CrusherZone.cs` con multiplicador por peso y reseteo de tamaño.
- [ ] Configurar `SharedCamera.cs` encuadrando automáticamente a los jugadores.
- [ ] Añadir temporizador de 3 minutos y alarma de clímax.

### Semana 5: Menús, UI, Audio y Pulido Final para Tribunal

- [ ] Diseñar UI estilizada (TextMeshPro) con marcadores por color y barras de Dash/Parry.
- [ ] Crear Splash Screen, Menú Principal y Menú de Pausa con sliders de audio funcionales.
- [ ] Conectar efectos de sonido (absorción, choque, parry, alarma, trituradora) y música dinámica.
- [ ] Generar build final para PC, verificar ejecución a 60 FPS estables y empaquetar en `.zip`.

---

## 10. RECOMENDACIÓN DE ASSETS GRATUITOS (ASSET STORE)

Para no perder tiempo modelando chatarra desde cero y mantener el 95% del esfuerzo en la programación y diseño de mecánicas:

1. **Kenney Game Assets (Gratis / CC0):**
   - _Kenney Space Kit_ o _Kenney Furniture Kit:_ Objetos 3D cartoon perfectos para servir de chatarra Tier 1, 2 y 3.
2. **Audio:**
   - _Kenney Impact Sounds_ y _Interface Sounds:_ Efectos de colisión metálica, timbres y clicks de UI.
3. **Fuentes tipográficas:**
   - Google Fonts (estilos cartoon/bold como _Bungee_, _Titan One_ o _Chango_).
