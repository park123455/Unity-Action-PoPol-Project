using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(GunnerAimVisualizer))]
[RequireComponent(typeof(GunnerBasicAttack))]
public sealed class GunnerAimAnimation : MonoBehaviour
{
    private static readonly int IsAimingHash =
        Animator.StringToHash("IsAiming");
    private static readonly int AimXHash = Animator.StringToHash("AimX");
    private static readonly int AimYHash = Animator.StringToHash("AimY");
    private static readonly int IsFiringHash =
        Animator.StringToHash("IsFiring");

    [Header("Animator")]
    [SerializeField] private string aimLayerName = "Aim Upper Body";

    [Header("Source Pose Angles")]
    [Tooltip("Yaw angle represented by the source left and right poses. The InsaneGunner side poses are approximately 90 degrees.")]
    [SerializeField, Min(0.01f)] private float maximumYaw = 90f;

    [Tooltip("Effective source yaw at the top and bottom edges. The diagonal InsaneGunner poses turn less than the pure side poses.")]
    [SerializeField, Min(0.01f)] private float verticalEdgeMaximumYaw = 70f;

    [Tooltip("Pitch angle represented by the up and down edge poses.")]
    [SerializeField, Min(0.01f)] private float maximumPitch = 45f;

    [Header("Blending")]
    [SerializeField, Min(0f)] private float parameterDampTime = 0.08f;
    [SerializeField, Min(0.01f)] private float layerBlendSpeed = 8f;

    [Header("Firing Compatibility")]
    [Tooltip("Prevents the forward-only attack06 animation from turning the weapon away from the selected aim direction.")]
    [SerializeField] private bool suppressFiringAnimationWhileAiming = true;

    private Animator animator;
    private GunnerAimVisualizer aimVisualizer;
    private GunnerBasicAttack basicAttack;
    private int aimLayerIndex = -1;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        aimVisualizer = GetComponent<GunnerAimVisualizer>();
        basicAttack = GetComponent<GunnerBasicAttack>();
        ResolveAimLayer();
    }

    private void Update()
    {
        if (!suppressFiringAnimationWhileAiming ||
            animator == null ||
            aimVisualizer == null ||
            basicAttack == null)
        {
            return;
        }

        bool playFiringAnimation =
            basicAttack.IsFiring && !aimVisualizer.IsAiming;
        animator.SetBool(IsFiringHash, playFiringAnimation);
    }

    private void LateUpdate()
    {
        if (animator == null || aimVisualizer == null)
        {
            return;
        }

        if (aimLayerIndex < 0)
        {
            ResolveAimLayer();
            if (aimLayerIndex < 0)
            {
                return;
            }
        }

        bool isAiming = aimVisualizer.IsAiming;
        float normalizedPitch = Mathf.Clamp01(
            Mathf.Abs(aimVisualizer.AimPitch) / maximumPitch);
        float calibratedMaximumYaw = Mathf.Lerp(
            maximumYaw,
            verticalEdgeMaximumYaw,
            normalizedPitch);
        float targetAimX = isAiming
            ? Mathf.Clamp(
                aimVisualizer.AimYaw / calibratedMaximumYaw,
                -1f,
                1f)
            : 0f;
        float targetAimY = isAiming
            ? Mathf.Clamp(aimVisualizer.AimPitch / maximumPitch, -1f, 1f)
            : 0f;
        float deltaTime = Time.deltaTime;

        animator.SetBool(IsAimingHash, isAiming);
        animator.SetFloat(
            AimXHash,
            targetAimX,
            parameterDampTime,
            deltaTime);
        animator.SetFloat(
            AimYHash,
            targetAimY,
            parameterDampTime,
            deltaTime);

        float targetLayerWeight = isAiming ? 1f : 0f;
        float layerWeight = Mathf.MoveTowards(
            animator.GetLayerWeight(aimLayerIndex),
            targetLayerWeight,
            layerBlendSpeed * deltaTime);
        animator.SetLayerWeight(aimLayerIndex, layerWeight);
    }

    private void ResolveAimLayer()
    {
        if (animator == null || string.IsNullOrWhiteSpace(aimLayerName))
        {
            aimLayerIndex = -1;
            return;
        }

        aimLayerIndex = animator.GetLayerIndex(aimLayerName);
        if (aimLayerIndex < 0)
        {
            Debug.LogWarning(
                $"[{nameof(GunnerAimAnimation)}] Animator layer " +
                $"'{aimLayerName}' was not found on '{name}'.",
                this);
        }
    }

    private void OnDisable()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsAimingHash, false);
        animator.SetFloat(AimXHash, 0f);
        animator.SetFloat(AimYHash, 0f);

        if (aimLayerIndex >= 0 && aimLayerIndex < animator.layerCount)
        {
            animator.SetLayerWeight(aimLayerIndex, 0f);
        }
    }

    private void OnValidate()
    {
        maximumYaw = Mathf.Max(0.01f, maximumYaw);
        verticalEdgeMaximumYaw = Mathf.Max(
            0.01f,
            verticalEdgeMaximumYaw);
        maximumPitch = Mathf.Max(0.01f, maximumPitch);
        parameterDampTime = Mathf.Max(0f, parameterDampTime);
        layerBlendSpeed = Mathf.Max(0.01f, layerBlendSpeed);
    }
}
