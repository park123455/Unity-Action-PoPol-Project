using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GunnerBasicAttack))]
[RequireComponent(typeof(GunnerAnimationController))]
[RequireComponent(typeof(GunnerEffectController))]
[RequireComponent(typeof(GunnerSoundController))]
public sealed class GunnerPresentationController : MonoBehaviour
{
    [Header("Presentation Modules")]
    [SerializeField] private GunnerAnimationController animationController;
    [SerializeField] private GunnerEffectController effectController;
    [SerializeField] private GunnerSoundController soundController;

    private GunnerBasicAttack basicAttack;

    private void Awake()
    {
        basicAttack = GetComponent<GunnerBasicAttack>();

        if (animationController == null)
        {
            animationController = GetComponent<GunnerAnimationController>();
        }

        if (effectController == null)
        {
            effectController = GetComponent<GunnerEffectController>();
        }

        if (soundController == null)
        {
            soundController = GetComponent<GunnerSoundController>();
        }
    }

    private void OnEnable()
    {
        if (basicAttack == null)
        {
            basicAttack = GetComponent<GunnerBasicAttack>();
        }

        basicAttack.AttackPerformed += HandleAttackPerformed;
        basicAttack.FiringStateChanged += HandleFiringStateChanged;
        basicAttack.AttackIntervalChanged += HandleAttackIntervalChanged;

        HandleAttackIntervalChanged(basicAttack.AttackInterval);
        HandleFiringStateChanged(basicAttack.IsFiring);
    }

    private void OnDisable()
    {
        if (basicAttack != null)
        {
            basicAttack.AttackPerformed -= HandleAttackPerformed;
            basicAttack.FiringStateChanged -= HandleFiringStateChanged;
            basicAttack.AttackIntervalChanged -= HandleAttackIntervalChanged;
        }

        if (animationController != null)
        {
            animationController.SetState(
                CharacterPresentationState.Firing,
                false);
        }
    }

    public void PlayGunShotPresentation()
    {
        PlayPresentation(CharacterPresentationEvent.GunShot);
    }

    public void PlayPresentation(CharacterPresentationEvent eventType)
    {
        DispatchPresentation(eventType, null);
    }

    public void PlayPresentationAt(
        CharacterPresentationEvent eventType,
        Transform origin)
    {
        DispatchPresentation(eventType, origin);
    }

    public void SetPresentationState(
        CharacterPresentationState state,
        bool active)
    {
        animationController.SetState(state, active);
    }

    public int DispatchPresentation(
        CharacterPresentationEvent eventType,
        Transform originOverride)
    {
        int playedCount = animationController.Play(eventType);
        playedCount += effectController.PlayEffects(eventType, originOverride);
        playedCount += soundController.PlaySounds(eventType, originOverride);
        return playedCount;
    }

    private void HandleAttackPerformed()
    {
        PlayPresentation(CharacterPresentationEvent.GunShot);
    }

    private void HandleFiringStateChanged(bool isFiring)
    {
        SetPresentationState(
            CharacterPresentationState.Firing,
            isFiring);
    }

    private void HandleAttackIntervalChanged(float attackInterval)
    {
        animationController.SetAttackInterval(attackInterval);
    }

    private void Reset()
    {
        animationController = GetComponent<GunnerAnimationController>();
        effectController = GetComponent<GunnerEffectController>();
        soundController = GetComponent<GunnerSoundController>();
    }
}
