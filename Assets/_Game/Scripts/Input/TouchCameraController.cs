using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shadow Zone Mobile — Dokunmatik Kamera Kontrolü
/// Ekranın SAĞ yarısındaki sürüklemeyi omuz kamerasına çevirir.
/// Joystick'e veya UI butonlarına basan parmakları yok sayar.
/// </summary>
public class TouchCameraController : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Transform target;

    [Header("Hassasiyet")]
    [SerializeField] private float sensitivity = 0.15f;
    [SerializeField] private float editorSensitivity = 2f;

    [Header("Açı Sınırları")]
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Omuz Kamerası")]
    [SerializeField] private Vector3 shoulderOffset = new Vector3(0.5f, 0.4f, -2.2f);
    [SerializeField] private float followSmoothing = 15f;

    [Header("Duvar Çarpışması")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionRadius = 0.25f;

    public float Yaw { get; private set; }
    public float Pitch { get; private set; }

    public float RecoilPitch { get; set; }
    public float RecoilYaw { get; set; }

    private int activeFingerId = -1;
    private Vector2 lookDelta;

    private void Update()
    {
        ReadTouchInput();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Yaw += lookDelta.x;
        Pitch -= lookDelta.y;
        Pitch = Mathf.Clamp(Pitch, minPitch, maxPitch);
        lookDelta = Vector2.zero;

        Quaternion rotation = Quaternion.Euler(Pitch + RecoilPitch, Yaw + RecoilYaw, 0f);

        Vector3 desiredPos = target.position + rotation * shoulderOffset;

        Vector3 castDir = desiredPos - target.position;

        if (castDir.sqrMagnitude > 0.001f &&
            Physics.SphereCast(
                target.position,
                collisionRadius,
                castDir.normalized,
                out RaycastHit hit,
                castDir.magnitude,
                collisionMask,
                QueryTriggerInteraction.Ignore))
        {
            desiredPos = target.position + castDir.normalized * Mathf.Max(hit.distance - 0.05f, 0.3f);
        }

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            followSmoothing * Time.deltaTime
        );

        transform.rotation = rotation;
    }

    private void ReadTouchInput()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    bool overUI = EventSystem.current != null &&
                                  EventSystem.current.IsPointerOverGameObject(touch.fingerId);

                    if (activeFingerId == -1 &&
                        touch.position.x > Screen.width * 0.5f &&
                        !overUI)
                    {
                        activeFingerId = touch.fingerId;
                    }
                    break;

                case TouchPhase.Moved:
                    if (touch.fingerId == activeFingerId)
                    {
                        lookDelta = touch.deltaPosition * sensitivity;
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touch.fingerId == activeFingerId)
                    {
                        activeFingerId = -1;
                    }
                    break;
            }
        }

#if UNITY_EDITOR
        if (Input.GetMouseButton(1))
        {
            lookDelta = new Vector2(
                Input.GetAxis("Mouse X"),
                Input.GetAxis("Mouse Y")
            ) * editorSensitivity;
        }
#endif
    }
}
