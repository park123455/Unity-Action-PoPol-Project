using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(GunnerInput))]
public sealed class GunnerBasicAttack : MonoBehaviour
{
    private const string AttackStateTag = "Attack";
    private static readonly int IsFiringHash = Animator.StringToHash("IsFiring");
    private static readonly int AttackLoopSpeedHash = Animator.StringToHash("AttackLoopSpeed");

    [Header("Attack Speed")]
    [Tooltip("Attack speed multiplier. 8 means eight times the original attack rate.")]
    [SerializeField, Min(0.1f)] private float attackSpeedMultiplier = 8f;

    [Tooltip("Original attack06 duration (43 frames at 30 fps). Keep this as the base interval.")]
    [SerializeField, Min(0.01f)] private float baseAttackInterval = 1.4333334f;

    [Tooltip("Duration of the sliced aiming/recoil loop (frames 9-27 at 30 fps).")]
    [SerializeField, Min(0.01f)] private float attackLoopClipDuration = 0.6f;

    [Header("Firing Feel")]
    [Tooltip("Keeps the weapon raised briefly between rapid mouse clicks.")]
    [SerializeField, Min(0f)] private float firingInputGraceDuration = 0.22f;

    [Tooltip("Delay before the first shot while the arms raise into the aiming pose.")]
    [SerializeField, Min(0f)] private float initialShotDelay = 0.15f;

    [Header("Attack Event")]
    [Tooltip("Connect projectile, muzzle flash, sound, or damage logic to this event.")]
    [SerializeField] private UnityEvent attackPerformed = new UnityEvent();

    public float AttackSpeedMultiplier => attackSpeedMultiplier;
    public float AttackInterval => baseAttackInterval / attackSpeedMultiplier;
    public bool CanAttack => isFiring && Time.time >= nextAttackTime;
    public bool IsFiring => isFiring;
    public bool IsAttacking
    {
        get
        {
            if (isFiring)
            {
                return true;
            }

            if (animator == null)
            {
                return false;
            }

            if (animator.GetCurrentAnimatorStateInfo(0).IsTag(AttackStateTag))
            {
                return true;
            }

            return animator.IsInTransition(0)
                && animator.GetNextAnimatorStateInfo(0).IsTag(AttackStateTag);
        }
    }

    public event Action AttackPerformed;

    private Animator animator;
    private GunnerInput gunnerInput;
    private float nextAttackTime;
    private float firingInputExpiresAt = float.NegativeInfinity;
    private bool isFiring;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        gunnerInput = GetComponent<GunnerInput>();
        ApplyAttackSpeed();
    }

    private void Update()
    {
        bool receivedFiringInput =
            gunnerInput.IsAttackHeld || gunnerInput.AttackPressedThisFrame;

        if (receivedFiringInput)
        {
            firingInputExpiresAt = Time.time + firingInputGraceDuration;
        }

        bool shouldKeepFiring =
            receivedFiringInput || Time.time <= firingInputExpiresAt;

        if (shouldKeepFiring != isFiring)
        {
            SetFiringState(shouldKeepFiring);
        }

        if (CanAttack)
        {
            PerformShot();
        }
    }

    public bool TryAttack()
    {
        firingInputExpiresAt = Time.time + firingInputGraceDuration;

        if (!isFiring)
        {
            SetFiringState(true);
        }

        if (!CanAttack)
        {
            return false;
        }

        PerformShot();
        return true;
    }

    public void SetAttackSpeedMultiplier(float multiplier)
    {
        attackSpeedMultiplier = Mathf.Max(0.1f, multiplier);

        if (animator != null)
        {
            ApplyAttackSpeed();
        }
    }

    private void PerformShot()
    {
        ApplyAttackSpeed();
        nextAttackTime = Time.time + AttackInterval;

        attackPerformed.Invoke();
        AttackPerformed?.Invoke();
    }

    private void SetFiringState(bool firing)
    {
        isFiring = firing;
        animator.SetBool(IsFiringHash, firing);

        if (firing)
        {
            ApplyAttackSpeed();
            nextAttackTime = Time.time + initialShotDelay;
        }
    }

    private void ApplyAttackSpeed()
    {
        float loopSpeed = attackLoopClipDuration / AttackInterval;
        animator.SetFloat(AttackLoopSpeedHash, loopSpeed);
    }

    private void OnDisable()
    {
        nextAttackTime = 0f;
        firingInputExpiresAt = float.NegativeInfinity;
        isFiring = false;

        if (animator != null)
        {
            animator.SetBool(IsFiringHash, false);
        }
    }

    private void OnValidate()
    {
        attackSpeedMultiplier = Mathf.Max(0.1f, attackSpeedMultiplier);
        baseAttackInterval = Mathf.Max(0.01f, baseAttackInterval);
        attackLoopClipDuration = Mathf.Max(0.01f, attackLoopClipDuration);
        firingInputGraceDuration = Mathf.Max(0f, firingInputGraceDuration);
        initialShotDelay = Mathf.Max(0f, initialShotDelay);
    }
}
