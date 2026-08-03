using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GunnerBasicAttack))]
public sealed class GunnerSoundController : MonoBehaviour
{
    [Header("Sound Emitters")]
    [Tooltip("All sounds owned by this character. Multiple emitters can use the same event.")]
    [SerializeField] private List<SoundEmitter> soundEmitters = new List<SoundEmitter>();

    [SerializeField] private bool autoCollectFromChildren = true;

    [Tooltip("Disable this when a future presentation controller dispatches the events.")]
    [SerializeField] private bool listenToBasicAttack;

    public IReadOnlyList<SoundEmitter> SoundEmitters => soundEmitters;

    private GunnerBasicAttack basicAttack;

    private void Awake()
    {
        basicAttack = GetComponent<GunnerBasicAttack>();
        RefreshSoundEmitters();
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

        basicAttack.AttackPerformed += PlayGunShotSounds;
    }

    private void OnDisable()
    {
        if (basicAttack != null)
        {
            basicAttack.AttackPerformed -= PlayGunShotSounds;
        }
    }

    public void PlayGunShotSound()
    {
        PlayGunShotSounds();
    }

    public void PlayGunShotSounds()
    {
        PlaySounds(CharacterPresentationEvent.GunShot);
    }

    public int PlaySounds(CharacterPresentationEvent eventType)
    {
        return PlaySounds(eventType, null);
    }

    public int PlaySounds(
        CharacterPresentationEvent eventType,
        Transform originOverride)
    {
        RemoveMissingEmitters();

        int playedCount = 0;
        foreach (SoundEmitter emitter in soundEmitters)
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

        return playedCount;
    }

    public void RegisterEmitter(SoundEmitter emitter)
    {
        if (emitter != null && !soundEmitters.Contains(emitter))
        {
            soundEmitters.Add(emitter);
        }
    }

    public void UnregisterEmitter(SoundEmitter emitter)
    {
        soundEmitters.Remove(emitter);
    }

    [ContextMenu("Refresh Sound Emitters")]
    public void RefreshSoundEmitters()
    {
        RemoveMissingEmitters();

        if (!autoCollectFromChildren)
        {
            return;
        }

        SoundEmitter[] foundEmitters =
            GetComponentsInChildren<SoundEmitter>(true);

        foreach (SoundEmitter emitter in foundEmitters)
        {
            RegisterEmitter(emitter);
        }
    }

    private void RemoveMissingEmitters()
    {
        soundEmitters.RemoveAll(emitter => emitter == null);
    }

    private void Reset()
    {
        RefreshSoundEmitters();
    }
}
