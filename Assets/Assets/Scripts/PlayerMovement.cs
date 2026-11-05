
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.8f;
    [SerializeField] private float sprintSpeed = 5.6f;
    [SerializeField] private float groundAcceleration = 18f;
    [SerializeField] private float groundBraking = 23f;
    [SerializeField] private float airMoveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -20f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private static readonly int Locomotion = Animator.StringToHash("Locomotion");
    private static readonly int Grounded = Animator.StringToHash("Grounded");

    private CharacterController controller;
    private float verticalVelocity;
    private Vector3 planarVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        // 1. Nhận input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical);
        direction = Vector3.ClampMagnitude(direction, 1f);

        // 2. Chạy nhanh khi giữ Shift
        float speed = Input.GetKey(KeyCode.LeftShift)
            ? sprintSpeed
            : moveSpeed;

        // 3. Xoay nhân vật theo hướng di chuyển
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // 4. Kiểm tra chạm đất
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // 5. Nhảy
        if (controller.isGrounded &&
            Input.GetKeyDown(KeyCode.Space))
        {
            verticalVelocity =
                Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 6. Trọng lực
        verticalVelocity += gravity * Time.deltaTime;

        bool airborne = !controller.isGrounded || verticalVelocity > 0f;
        float travelSpeed = airborne ? Mathf.Max(airMoveSpeed, speed) : speed;
        float acceleration = direction.sqrMagnitude > 0.01f ? groundAcceleration : groundBraking;
        planarVelocity = Vector3.MoveTowards(planarVelocity, direction * travelSpeed,
            acceleration * Time.deltaTime);
        Vector3 velocity = planarVelocity;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
        UpdateAnimation();
    }

    public void ResetMotion()
    {
        planarVelocity = Vector3.zero;
        verticalVelocity = 0f;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat(Locomotion, 0f);
            animator.SetBool(Grounded, true);
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        Vector3 actualVelocity = controller.velocity;
        float planarSpeed = new Vector2(actualVelocity.x, actualVelocity.z).magnitude;
        float blend = Mathf.InverseLerp(0f, moveSpeed, planarSpeed)
                    + Mathf.InverseLerp(moveSpeed, sprintSpeed, planarSpeed);

        animator.SetFloat(Locomotion, blend, 0.1f, Time.deltaTime);
        animator.SetBool(Grounded, controller.isGrounded);
    }
}
