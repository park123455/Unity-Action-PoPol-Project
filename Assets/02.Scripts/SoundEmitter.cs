using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public sealed class SoundEmitter : MonoBehaviour
{
    public enum ClipSelectionMode
    {
        Random,
        Sequential
    }

    [Header("Event")]
    [Tooltip("Character event that triggers this sound through a presentation controller.")]
    [SerializeField] private CharacterPresentationEvent presentationEvent;

    [Header("Clips")]
    [Tooltip("Variations of the same sound. One clip is selected each time Play is called.")]
    [SerializeField] private AudioClip[] clips = new AudioClip[0];
    [SerializeField] private ClipSelectionMode clipSelection = ClipSelectionMode.Random;
    [SerializeField] private bool avoidImmediateRepeat = true;

    [Header("Origin")]
    [Tooltip("3D sound origin. This object's transform is used when left empty.")]
    [SerializeField] private Transform soundPoint;
    [SerializeField] private bool followSoundPoint;

    [Header("Playback")]
    [SerializeField] private AudioMixerGroup outputMixerGroup;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField, Range(0.1f, 3f)] private float pitch = 1f;
    [SerializeField, Range(0f, 0.5f)] private float randomPitchRange;
    [SerializeField] private bool loop;
    [SerializeField] private bool stopOnDisable = true;

    [Header("3D Sound")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
    [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    [SerializeField, Min(0f)] private float minDistance = 1f;
    [SerializeField, Min(0.01f)] private float maxDistance = 25f;
    [SerializeField, Range(0f, 5f)] private float dopplerLevel;

    public CharacterPresentationEvent PresentationEvent => presentationEvent;
    public Transform SoundPoint => soundPoint != null ? soundPoint : transform;

    private readonly List<AudioSource> activeSources = new List<AudioSource>();
    private int lastClipIndex = -1;
    private int sequentialClipIndex;

    public void Play()
    {
        Spawn();
    }

    public AudioSource Spawn()
    {
        return SpawnAt(SoundPoint);
    }

    public AudioSource SpawnAt(Transform origin)
    {
        RemoveFinishedSources();

        if (loop && activeSources.Count > 0)
        {
            return activeSources[0];
        }

        AudioClip clip = SelectClip();
        if (clip == null)
        {
            Debug.LogWarning(
                $"[{nameof(SoundEmitter)}] No audio clip is assigned to '{name}'.",
                this);
            return null;
        }

        if (origin == null)
        {
            Debug.LogWarning(
                $"[{nameof(SoundEmitter)}] No sound point was provided for '{name}'.",
                this);
            return null;
        }

        GameObject audioObject = new GameObject($"Audio - {clip.name}");
        audioObject.transform.SetPositionAndRotation(origin.position, origin.rotation);

        if (followSoundPoint)
        {
            audioObject.transform.SetParent(origin, true);
        }

        AudioSource source = audioObject.AddComponent<AudioSource>();
        ConfigureSource(source, clip);
        source.Play();
        activeSources.Add(source);

        if (!loop)
        {
            float lifetime = clip.length / Mathf.Max(0.01f, Mathf.Abs(source.pitch));
            Destroy(audioObject, lifetime + 0.1f);
        }

        return source;
    }

    public void StopAll()
    {
        foreach (AudioSource source in activeSources)
        {
            if (source == null)
            {
                continue;
            }

            source.Stop();
            Destroy(source.gameObject);
        }

        activeSources.Clear();
    }

    public void SetPresentationEvent(CharacterPresentationEvent eventType)
    {
        presentationEvent = eventType;
    }

    public void SetSoundPoint(Transform point)
    {
        soundPoint = point;
    }

    public void SetClips(AudioClip[] audioClips)
    {
        clips = audioClips ?? new AudioClip[0];
        lastClipIndex = -1;
        sequentialClipIndex = 0;
    }

    private AudioClip SelectClip()
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        if (clips.Length == 1)
        {
            lastClipIndex = 0;
            return clips[0];
        }

        int index;
        if (clipSelection == ClipSelectionMode.Sequential)
        {
            index = sequentialClipIndex;
            sequentialClipIndex = (sequentialClipIndex + 1) % clips.Length;
        }
        else
        {
            index = Random.Range(0, clips.Length);

            if (avoidImmediateRepeat && index == lastClipIndex)
            {
                index = (index + Random.Range(1, clips.Length)) % clips.Length;
            }
        }

        lastClipIndex = index;
        return clips[index];
    }

    private void ConfigureSource(AudioSource source, AudioClip clip)
    {
        float randomizedPitch = pitch +
            Random.Range(-randomPitchRange, randomPitchRange);

        source.playOnAwake = false;
        source.clip = clip;
        source.outputAudioMixerGroup = outputMixerGroup;
        source.volume = volume;
        source.pitch = Mathf.Max(0.01f, randomizedPitch);
        source.loop = loop;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = rolloffMode;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = dopplerLevel;
    }

    private void RemoveFinishedSources()
    {
        activeSources.RemoveAll(source => source == null);
    }

    private void OnDisable()
    {
        if (stopOnDisable)
        {
            StopAll();
        }
    }

    private void OnValidate()
    {
        volume = Mathf.Clamp01(volume);
        pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        randomPitchRange = Mathf.Clamp(randomPitchRange, 0f, 0.5f);
        spatialBlend = Mathf.Clamp01(spatialBlend);
        minDistance = Mathf.Max(0f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
        dopplerLevel = Mathf.Clamp(dopplerLevel, 0f, 5f);
    }
}
