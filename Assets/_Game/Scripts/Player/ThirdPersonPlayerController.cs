using UnityEngine;

/// <summary>
/// Shadow Zone Mobile — Third-Person Oyuncu Hareketi
/// MobileJoystick yönünü kameranın baktığı yöne göre dünyaya çevirir.
/// Shooter olduğu için karakter her zaman kameranın yatay yönüne döner.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class ThirdPersonPlayerController : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private MobileJoystick joystick;
    [SerializeField] private TouchCameraController cameraController;
    [SerializeField] private Animator animator;

    [Header("Hareket")]
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float rotationSpeed = 14f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private float verticalVelocity;

    public Vector3 CurrentVelocity { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Move();
        RotateTowardsCamera();
        UpdateAnimator();
    }

    private void Move()
    {
        Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;

        float cameraYaw = cameraController != null ? cameraController.Yaw : transform.eulerAngles.y;

        Quaternion camYaw = Quaternion.Euler(0f, cameraYaw, 0f);
        Vector3 moveDir = camYaw * new Vector3(input.x, 0f, input.y);

        if (moveDir.magnitude > 1f)
            moveDir.Normalize();

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDir * moveSpeed + Vector3.up * verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        CurrentVelocity = velocity;
    }

    private void RotateTowardsCamera()
    {
        if (cameraController == null) return;

        Quaternion targetRot = Quaternion.Euler(0f, cameraController.Yaw, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;

        animator.SetFloat("MoveX", input.x, 0.1f, Time.deltaTime);
        animator.SetFloat("MoveY", input.y, 0.1f, Time.deltaTime);
        animator.SetBool("IsMoving", input.sqrMagnitude > 0.01f);
    }
}
