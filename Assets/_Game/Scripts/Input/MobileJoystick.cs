using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shadow Zone Mobile — Hareket Joystick'i
/// Canvas üzerindeki JoystickBG objesine eklenir.
/// ThirdPersonPlayerController bu scriptin Direction değerini okur.
/// </summary>
public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Referanslar")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Header("Ayarlar")]
    [SerializeField] private float handleRange = 100f;
    [SerializeField] private float deadZone = 0.1f;

    public Vector2 Direction { get; private set; }
    public bool IsPressed { get; private set; }

    private void Awake()
    {
        if (background == null)
            background = GetComponent<RectTransform>();

        if (handle == null && transform.childCount > 0)
            handle = transform.GetChild(0).GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);

        if (handle != null)
            handle.anchoredPosition = clamped;

        Vector2 dir = clamped / handleRange;
        Direction = dir.magnitude < deadZone ? Vector2.zero : dir;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
        Direction = Vector2.zero;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }
}
