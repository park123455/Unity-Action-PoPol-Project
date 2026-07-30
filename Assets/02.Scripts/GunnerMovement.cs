using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(GunnerInput))]
[RequireComponent(typeof(GunnerBasicAttack))]
public sealed class GunnerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 2.2f;
    [SerializeField, Min(0f)] private float runSpeed = 5f;
    [SerializeField, Min(0f)] private float acceleration = 14f;
    [SerializeField, Range(0f, 0.5f)] private float attackMoveSpeedMultiplier = 0.15f;
    [SerializeField, Min(0f)] private float attackBrakingAcceleration = 40f;
    [SerializeField, Min(0.01f)] private float rotationSmoothTime = 0.1f;

    [Header("Grounding")]
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedVelocity = -2f;

    [Header("Reference")]
    [Tooltip("비워두면 Main Camera를 자동으로 사용합니다.")]
    [SerializeField] private Transform cameraTransform;

    public Vector3 PlanarVelocity => planarVelocity;
    public float TargetSpeed { get; private set; }
    public bool IsRunning { get; private set; }

    private GunnerInput gunnerInput;
    private GunnerBasicAttack basicAttack;
    private CharacterController characterController;
    private Vector3 planarVelocity;
    private float verticalVelocity;
    private float rotationVelocity;

    private void Awake()
    {
        gunnerInput = GetComponent<GunnerInput>();
        basicAttack = GetComponent<GunnerBasicAttack>();
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        Vector2 moveInput = gunnerInput.MoveInput;
        bool isAttacking =
            gunnerInput.IsAttackHeld || basicAttack.IsAttacking;

        IsRunning = gunnerInput.IsRunHeld;
        float normalTargetSpeed = IsRunning ? runSpeed : walkSpeed;
        TargetSpeed = isAttacking
            ? normalTargetSpeed * attackMoveSpeedMultiplier
            : normalTargetSpeed;

        Vector3 moveDirection = GetCameraRelativeDirection(moveInput);
        Vector3 targetVelocity = moveDirection * TargetSpeed;
        float velocityChangeRate = isAttacking
            ? attackBrakingAcceleration
            : acceleration;
        planarVelocity = Vector3.MoveTowards(
            planarVelocity,
            targetVelocity,
            velocityChangeRate * deltaTime);

        RotateTowards(moveDirection);
        ApplyGravity(deltaTime);
        characterController.Move(
            (planarVelocity + Vector3.up * verticalVelocity) * deltaTime);
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        if (input.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return (forward * input.y + right * input.x).normalized;
    }

    private void RotateTowards(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
        float smoothedAngle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y,
            targetAngle,
            ref rotationVelocity,
            rotationSmoothTime);
        transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
    }

    private void ApplyGravity(float deltaTime)
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedVelocity;
            return;
        }

        verticalVelocity += gravity * deltaTime;
    }

    private void OnValidate()
    {
        walkSpeed = Mathf.Max(0f, walkSpeed);
        runSpeed = Mathf.Max(walkSpeed, runSpeed);
        acceleration = Mathf.Max(0f, acceleration);
        attackMoveSpeedMultiplier = Mathf.Clamp(
            attackMoveSpeedMultiplier,
            0f,
            0.5f);
        attackBrakingAcceleration = Mathf.Max(0f, attackBrakingAcceleration);
        rotationSmoothTime = Mathf.Max(0.01f, rotationSmoothTime);
    }
}
