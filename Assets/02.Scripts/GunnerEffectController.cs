using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GunnerBasicAttack))]
public sealed class GunnerEffectController : MonoBehaviour
{
    [Header("Effect Emitters")]
    [Tooltip("All effects owned by this character. Multiple emitters can use the same event.")]
    [SerializeField] private List<EffectEmitter> effectEmitters = new List<EffectEmitter>();

    [Tooltip("Automatically includes EffectEmitter components on this object and its children.")]
    [SerializeField] private bool autoCollectFromChildren = true;

    [Tooltip("Disable this when a future presentation controller dispatches the events.")]
    [SerializeField] private bool listenToBasicAttack;

    [SerializeField] private bool warnWhenEventHasNoEffect;

    public IReadOnlyList<EffectEmitter> EffectEmitters => effectEmitters;

    private GunnerBasicAttack basicAttack;

    private void Awake()
    {
        basicAttack = GetComponent<GunnerBasicAttack>();
        RefreshEffectEmitters();
    }

    private void OnEnable()
    {
        if (!listenToBasicAttack)
        {
            return;
        }

        if (basicAttack == null)
        {
            basicAttack = GetComponent<GunnerBasicAttack>();
        }

        basicAttack.AttackPerformed += PlayGunShotEffects;
    }

    private void OnDisable()
    {
        if (basicAttack != null)
        {
            basicAttack.AttackPerformed -= PlayGunShotEffects;
        }
    }

    // Kept for existing UnityEvent or external code connections.
    public void PlayGunShotEffect()
    {
        PlayGunShotEffects();
    }

    public void PlayGunShotEffects()
    {
        PlayEffects(CharacterPresentationEvent.GunShot);
    }

    public int PlayEffects(CharacterPresentationEvent eventType)
    {
        return PlayEffects(eventType, null);
    }

    public int PlayEffects(
        CharacterPresentationEvent eventType,
        Transform originOverride)
    {
        RemoveMissingEmitters();

        int playedCount = 0;
        foreach (EffectEmitter emitter in effectEmitters)
        {
            if (emitter.PresentationEvent != eventType)
            {
                continue;
            }

            if (originOverride == null)
            {
                emitter.Play();
            }
            else
            {
                emitter.SpawnAt(originOverride);
            }
            playedCount++;
        }

        if (playedCount == 0 && warnWhenEventHasNoEffect)
        {
            Debug.LogWarning(
                $"[{nameof(GunnerEffectController)}] No effect is registered for '{eventType}' on '{name}'.",
                this);
        }

        return playedCount;
    }

    public void RegisterEmitter(EffectEmitter emitter)
    {
        if (emitter != null && !effectEmitters.Contains(emitter))
        {
            effectEmitters.Add(emitter);
        }
    }

    public void UnregisterEmitter(EffectEmitter emitter)
    {
        effectEmitters.Remove(emitter);
    }

    [ContextMenu("Refresh Effect Emitters")]
    public void RefreshEffectEmitters()
    {
        RemoveMissingEmitters();

        if (!autoCollectFromChildren)
        {
            return;
        }

        EffectEmitter[] foundEmitters =
            GetComponentsInChildren<EffectEmitter>(true);

        foreach (EffectEmitter emitter in foundEmitters)
        {
            RegisterEmitter(emitter);
        }
    }

    private void RemoveMissingEmitters()
    {
        effectEmitters.RemoveAll(emitter => emitter == null);
    }

    private void Reset()
    {
        RefreshEffectEmitters();
    }
}
