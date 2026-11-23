using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Componente para añadir sonido de Hover y Click a cualquier botón de la UI.
/// Se enruta automáticamente al grupo SFX a través del AudioManager.
/// 
/// Buenas prácticas:
/// - Anti-doble ejecución (Debounce): evita que coincidan OnPointerClick y OnSubmit en el mismo clic.
/// - Compatible tanto con Ratón (Hover/Click) como con Mando y Teclado (Select/Submit).
/// - Si el botón está desactivado (interactable = false), no reproduce sonido.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class UIButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISelectHandler, ISubmitHandler
{
    [Header("Sonidos Personalizados (Opcional)")]
    [Tooltip("Deja vacío para usar el sonido por defecto de AudioManager, o arrastra uno exclusivo")]
    [SerializeField] private AudioClip customHoverSfx;
    [SerializeField] private AudioClip customClickSfx;

    [Header("Ajustes")]
    [Range(0f, 1f)] [SerializeField] private float volume = 0.6f;
    [Tooltip("Variación sutil de pitch para que clics rápidos no suenen robóticos")]
    [SerializeField] private bool useRandomPitch = false;

    private Selectable selectable;
    private float lastHoverTime = -10f;
    private float lastClickTime = -10f;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    // RATÓN: Al pasar el cursor por encima
    public void OnPointerEnter(PointerEventData eventData)
    {
        TriggerHover();
    }

    // MANDO / TECLADO: Al seleccionar el botón con la cruceta o flechas
    public void OnSelect(BaseEventData eventData)
    {
        TriggerHover();
    }

    // RATÓN: Al hacer clic izquierdo
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            TriggerClick();
        }
    }

    // MANDO / TECLADO: Al pulsar confirmación (Enter / Barra Espaciadora / Botón A o X del mando)
    public void OnSubmit(BaseEventData eventData)
    {
        TriggerClick();
    }

    private void TriggerHover()
    {
        if (!IsInteractable()) return;

        // Anti-doble audio en Hover
        if (Time.unscaledTime - lastHoverTime < 0.08f) return;
        lastHoverTime = Time.unscaledTime;

        float pitch = useRandomPitch ? Random.Range(0.96f, 1.04f) : 1f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonHover(customHoverSfx, volume, pitch);
        }
    }

    private void TriggerClick()
    {
        if (!IsInteractable()) return;

        // CRUCIAL: Anti-doble audio en Click (evita que OnPointerClick y OnSubmit se ejecuten juntos)
        if (Time.unscaledTime - lastClickTime < 0.15f) return;
        lastClickTime = Time.unscaledTime;

        float pitch = useRandomPitch ? Random.Range(0.96f, 1.04f) : 1f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick(customClickSfx, volume, pitch);
        }
    }

    private bool IsInteractable()
    {
        return selectable != null && selectable.interactable;
    }
}
