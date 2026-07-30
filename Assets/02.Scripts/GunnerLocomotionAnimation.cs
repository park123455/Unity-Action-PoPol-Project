using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(GunnerMovement))]
public sealed class GunnerLocomotionAnimation : MonoBehaviour
{
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");

    [Header("Animation")]
    [SerializeField, Min(0f)] private float animationDampTime = 0.12f;
    [SerializeField, Range(0.1f, 0.9f)] private float walkBlendRadius = 0.5f;
    [SerializeField, Range(0.5f, 1.5f)] private float runBlendRadius = 1f;

    private Animator animator;
    private GunnerMovement movement;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<GunnerMovement>();
        animator.applyRootMotion = false;
    }

    private void LateUpdate()
    {
        Vector3 planarVelocity = movement.PlanarVelocity;
        float speed = planarVelocity.magnitude;
        float targetSpeed = movement.TargetSpeed;
        float blendRadius = movement.IsRunning ? runBlendRadius : walkBlendRadius;
        float normalizedSpeed = targetSpeed > 0f
            ? Mathf.Clamp01(speed / targetSpeed)
            : 0f;

        Vector3 localVelocity = transform.InverseTransformDirection(planarVelocity);
        Vector2 localDirection = new Vector2(localVelocity.x, localVelocity.z);
        if (localDirection.sqrMagnitude > 0.001f)
        {
            localDirection.Normalize();
        }

        Vector2 blendPosition = localDirection * (blendRadius * normalizedSpeed);
        float deltaTime = Time.deltaTime;
        animator.SetFloat(MoveXHash, blendPosition.x, animationDampTime, deltaTime);
        animator.SetFloat(MoveYHash, blendPosition.y, animationDampTime, deltaTime);
    }

    private void OnValidate()
    {
        animationDampTime = Mathf.Max(0f, animationDampTime);
    }
}
