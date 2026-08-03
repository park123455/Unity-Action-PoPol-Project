using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class GunnerAnimationController : MonoBehaviour
{
    [Serializable]
    private sealed class EventTriggerBinding
    {
        [SerializeField] private CharacterPresentationEvent eventType;
        [SerializeField] private string animatorTrigger;

        public CharacterPresentationEvent EventType => eventType;
        public string AnimatorTrigger => animatorTrigger;
    }

    [Serializable]
    private sealed class StateBoolBinding
    {
        [SerializeField] private CharacterPresentationState state;
        [SerializeField] private string animatorBool;

        public CharacterPresentationState State => state;
        public string AnimatorBool => animatorBool;

        public StateBoolBinding(
            CharacterPresentationState state,
            string animatorBool)
        {
            this.state = state;
            this.animatorBool = animatorBool;
        }
    }

    [Header("Momentary Events")]
    [Tooltip("Optional mappings for events represented by Animator Triggers, such as Hit or Reload.")]
    [SerializeField] private List<EventTriggerBinding> eventTriggers =
        new List<EventTriggerBinding>();

    [Header("Continuous States")]
    [Tooltip("Mappings for states represented by Animator Bools.")]
    [SerializeField] private List<StateBoolBinding> stateBools =
        new List<StateBoolBinding>
        {
            new StateBoolBinding(CharacterPresentationState.Firing, "IsFiring")
        };

    [Header("Attack Loop")]
    [SerializeField, Min(0.01f)] private float attackLoopClipDuration = 0.6f;
    [SerializeField] private string attackLoopSpeedParameter = "AttackLoopSpeed";

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public int Play(CharacterPresentationEvent eventType)
    {
        EnsureAnimator();

        int playedCount = 0;
        foreach (EventTriggerBinding binding in eventTriggers)
        {
            if (binding.EventType != eventType ||
                string.IsNullOrWhiteSpace(binding.AnimatorTrigger))
            {
                continue;
            }

            int parameterHash = Animator.StringToHash(binding.AnimatorTrigger);
            if (!HasParameter(parameterHash, AnimatorControllerParameterType.Trigger))
            {
                Debug.LogWarning(
                    $"[{nameof(GunnerAnimationController)}] Animator Trigger '{binding.AnimatorTrigger}' was not found on '{name}'.",
                    this);
                continue;
            }

            animator.SetTrigger(parameterHash);
            playedCount++;
        }

        return playedCount;
    }

    public int SetState(CharacterPresentationState state, bool active)
    {
        EnsureAnimator();

        int changedCount = 0;
        foreach (StateBoolBinding binding in stateBools)
        {
            if (binding.State != state ||
                string.IsNullOrWhiteSpace(binding.AnimatorBool))
            {
                continue;
            }

            int parameterHash = Animator.StringToHash(binding.AnimatorBool);
            if (!HasParameter(parameterHash, AnimatorControllerParameterType.Bool))
            {
                Debug.LogWarning(
                    $"[{nameof(GunnerAnimationController)}] Animator Bool '{binding.AnimatorBool}' was not found on '{name}'.",
                    this);
                continue;
            }

            animator.SetBool(parameterHash, active);
            changedCount++;
        }

        return changedCount;
    }

    public void SetAttackInterval(float attackInterval)
    {
        EnsureAnimator();

        if (string.IsNullOrWhiteSpace(attackLoopSpeedParameter))
        {
            return;
        }

        int parameterHash = Animator.StringToHash(attackLoopSpeedParameter);
        if (!HasParameter(parameterHash, AnimatorControllerParameterType.Float))
        {
            Debug.LogWarning(
                $"[{nameof(GunnerAnimationController)}] Animator Float '{attackLoopSpeedParameter}' was not found on '{name}'.",
                this);
            return;
        }

        float loopSpeed = attackLoopClipDuration /
            Mathf.Max(0.01f, attackInterval);
        animator.SetFloat(parameterHash, loopSpeed);
    }

    private bool HasParameter(
        int parameterHash,
        AnimatorControllerParameterType parameterType)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash &&
                parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureAnimator()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void OnDisable()
    {
        if (animator != null)
        {
            SetState(CharacterPresentationState.Firing, false);
        }
    }

    private void OnValidate()
    {
        attackLoopClipDuration = Mathf.Max(0.01f, attackLoopClipDuration);
    }
}
