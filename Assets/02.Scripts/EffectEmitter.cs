using UnityEngine;

public sealed class EffectEmitter : MonoBehaviour
{
    [Header("Event")]
    [Tooltip("Character event that triggers this effect through a presentation controller.")]
    [SerializeField] private CharacterPresentationEvent presentationEvent;

    [Header("Effect")]
    [Tooltip("Prefab instantiated whenever Play is called.")]
    [SerializeField] private GameObject effectPrefab;

    [Header("Spawn")]
    [Tooltip("Effect origin. This object's transform is used when left empty.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Keeps the spawned effect attached to the spawn point.")]
    [SerializeField] private bool followSpawnPoint;

    [SerializeField] private Vector3 localPositionOffset;
    [SerializeField] private Vector3 localEulerOffset;

    [Tooltip("Uniform multiplier applied to the effect prefab's original scale.")]
    [SerializeField, Range(0f, 10f)] private float uniformScale = 1f;

    [Header("Lifetime")]
    [Tooltip("Destroys the spawned instance after this many seconds. Set to 0 when the effect prefab manages its own lifetime.")]
    [SerializeField, Min(0f)] private float autoDestroyDelay = 2f;

    public GameObject EffectPrefab => effectPrefab;
    public Transform SpawnPoint => spawnPoint != null ? spawnPoint : transform;
    public CharacterPresentationEvent PresentationEvent => presentationEvent;

    // Kept parameterless so an animation event, UnityEvent, or a future
    // presentation coordinator can invoke this emitter without knowing how it works.
    public void Play()
    {
        Spawn();
    }

    public GameObject Spawn()
    {
        return SpawnAt(SpawnPoint);
    }

    public GameObject SpawnAt(Transform origin)
    {
        if (effectPrefab == null)
        {
            Debug.LogWarning(
                $"[{nameof(EffectEmitter)}] No effect prefab is assigned to '{name}'.",
                this);
            return null;
        }

        if (origin == null)
        {
            Debug.LogWarning(
                $"[{nameof(EffectEmitter)}] No spawn point was provided for '{name}'.",
                this);
            return null;
        }

        Vector3 position = origin.TransformPoint(localPositionOffset);
        Quaternion rotation = origin.rotation * Quaternion.Euler(localEulerOffset);
        Transform parent = followSpawnPoint ? origin : null;
        GameObject instance = Instantiate(effectPrefab, position, rotation, parent);
        instance.transform.localScale *= uniformScale;

        if (!instance.activeSelf)
        {
            instance.SetActive(true);
        }

        if (autoDestroyDelay > 0f)
        {
            Destroy(instance, autoDestroyDelay);
        }

        return instance;
    }

    public void SetEffectPrefab(GameObject prefab)
    {
        effectPrefab = prefab;
    }

    public void SetPresentationEvent(CharacterPresentationEvent eventType)
    {
        presentationEvent = eventType;
    }

    public void SetSpawnPoint(Transform point)
    {
        spawnPoint = point;
    }

    public void SetUniformScale(float scale)
    {
        uniformScale = Mathf.Clamp(scale, 0f, 10f);
    }

    private void OnValidate()
    {
        autoDestroyDelay = Mathf.Max(0f, autoDestroyDelay);
        uniformScale = Mathf.Clamp(uniformScale, 0f, 10f);
    }
}
