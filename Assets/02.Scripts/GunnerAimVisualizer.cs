using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GunnerInput))]
public sealed class GunnerAimVisualizer : MonoBehaviour
{
    [Header("Effect Prefabs")]
    [Tooltip("Sphere effect displayed around the Gunner while right-click aiming.")]
    [SerializeField] private GameObject shieldEffectPrefab;

    [Tooltip("Effect that marks the selected firing direction on the sphere surface.")]
    [SerializeField] private GameObject aimPointerEffectPrefab;

    [Header("Aim Sphere")]
    [Tooltip("Local center of the sphere used to calculate the firing direction.")]
    [SerializeField] private Vector3 aimCenterOffset = new Vector3(0f, 0.8f, 0f);

    [Tooltip("Distance from the aim center to the direction marker.")]
    [SerializeField, Min(0.1f)] private float sphereRadius = 1.6f;

    [Tooltip("Total horizontal field of aim. 90 means 45 degrees to either side of forward.")]
    [SerializeField, Range(1f, 180f)] private float horizontalAimAngle = 90f;

    [Tooltip("Total vertical field of aim. 90 means 45 degrees up and down.")]
    [SerializeField, Range(1f, 180f)] private float verticalAimAngle = 90f;

    [Header("Mouse Aim")]
    [Tooltip("Degrees applied for one unit of legacy Mouse X or Mouse Y input.")]
    [SerializeField, Min(0.01f)] private float mouseSensitivity = 6f;

    [SerializeField] private bool invertVertical;

    [Tooltip("Starts each right-click aim from directly in front of the character.")]
    [SerializeField] private bool resetDirectionOnAimBegin = true;

    [Tooltip("Locks and hides the cursor while right-click aiming, then restores it on release.")]
    [SerializeField] private bool lockCursorWhileAiming = true;

    [Header("Effect Tuning")]
    [SerializeField, Min(0.01f)] private float shieldScaleMultiplier = 1f;
    [SerializeField, Min(0.01f)] private float pointerScaleMultiplier = 0.55f;

    [Tooltip("Additional local rotation applied after the pointer's up axis is aligned to the sphere normal.")]
    [SerializeField] private Vector3 pointerEulerOffset;

    public bool IsAiming => gunnerInput != null && gunnerInput.IsAimHeld;
    public float AimYaw => aimYaw;
    public float AimPitch => aimPitch;

    public Vector3 AimDirection
    {
        get
        {
            Vector3 direction = GetLocalAimDirection();
            return transform.TransformDirection(direction).normalized;
        }
    }

    public Vector3 AimPoint => transform.TransformPoint(
        aimCenterOffset + GetLocalAimDirection() * sphereRadius);

    private GunnerInput gunnerInput;
    private GameObject shieldEffectInstance;
    private GameObject aimPointerEffectInstance;
    private Vector3 shieldBaseScale = Vector3.one;
    private Vector3 pointerBaseScale = Vector3.one;
    private float aimYaw;
    private float aimPitch;
    private bool visualsActive;
    private bool cursorStateCaptured;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    private void Awake()
    {
        gunnerInput = GetComponent<GunnerInput>();
    }

    private void LateUpdate()
    {
        if (gunnerInput == null)
        {
            return;
        }

        if (gunnerInput.AimPressedThisFrame)
        {
            BeginAim();
        }

        if (gunnerInput.IsAimHeld)
        {
            UpdateAimAngles(gunnerInput.AimLookDelta);
            UpdateEffectTransforms();
        }

        if (gunnerInput.AimReleasedThisFrame)
        {
            EndAim();
        }
    }

    private void BeginAim()
    {
        if (resetDirectionOnAimBegin)
        {
            aimYaw = 0f;
            aimPitch = 0f;
        }

        CreateEffectInstancesIfNeeded();
        SetVisualsActive(true);
        CaptureAndLockCursor();
        UpdateEffectTransforms();
    }

    private void EndAim()
    {
        SetVisualsActive(false);
        RestoreCursor();
    }

    private void UpdateAimAngles(Vector2 mouseDelta)
    {
        float verticalSign = invertVertical ? -1f : 1f;
        aimYaw += mouseDelta.x * mouseSensitivity;
        aimPitch += mouseDelta.y * mouseSensitivity * verticalSign;

        float halfHorizontalAngle = horizontalAimAngle * 0.5f;
        float halfVerticalAngle = verticalAimAngle * 0.5f;
        aimYaw = Mathf.Clamp(aimYaw, -halfHorizontalAngle, halfHorizontalAngle);
        aimPitch = Mathf.Clamp(aimPitch, -halfVerticalAngle, halfVerticalAngle);
    }

    private Vector3 GetLocalAimDirection()
    {
        return Quaternion.Euler(-aimPitch, aimYaw, 0f) * Vector3.forward;
    }

    private void CreateEffectInstancesIfNeeded()
    {
        if (shieldEffectInstance == null && shieldEffectPrefab != null)
        {
            shieldEffectInstance = Instantiate(shieldEffectPrefab, transform);
            shieldEffectInstance.name = "Aim Sphere (Magic shield blue 1)";
            shieldBaseScale = shieldEffectInstance.transform.localScale;
            shieldEffectInstance.SetActive(false);
        }

        if (aimPointerEffectInstance == null && aimPointerEffectPrefab != null)
        {
            aimPointerEffectInstance = Instantiate(aimPointerEffectPrefab, transform);
            aimPointerEffectInstance.name = "Aim Pointer (Laser AOE 1)";
            pointerBaseScale = aimPointerEffectInstance.transform.localScale;
            aimPointerEffectInstance.SetActive(false);
        }

        if (shieldEffectPrefab == null || aimPointerEffectPrefab == null)
        {
            Debug.LogWarning(
                $"[{nameof(GunnerAimVisualizer)}] Aim effect prefabs are not fully assigned on '{name}'.",
                this);
        }
    }

    private void UpdateEffectTransforms()
    {
        Vector3 localDirection = GetLocalAimDirection();

        if (shieldEffectInstance != null)
        {
            Transform shieldTransform = shieldEffectInstance.transform;
            shieldTransform.localPosition = Vector3.zero;
            shieldTransform.localRotation = Quaternion.identity;
            shieldTransform.localScale = shieldBaseScale * shieldScaleMultiplier;
        }

        if (aimPointerEffectInstance != null)
        {
            Transform pointerTransform = aimPointerEffectInstance.transform;
            pointerTransform.localPosition =
                aimCenterOffset + localDirection * sphereRadius;
            pointerTransform.localRotation =
                Quaternion.FromToRotation(Vector3.up, localDirection) *
                Quaternion.Euler(pointerEulerOffset);
            pointerTransform.localScale =
                pointerBaseScale * pointerScaleMultiplier;
        }
    }

    private void SetVisualsActive(bool active)
    {
        visualsActive = active;

        if (shieldEffectInstance != null)
        {
            shieldEffectInstance.SetActive(active);
        }

        if (aimPointerEffectInstance != null)
        {
            aimPointerEffectInstance.SetActive(active);
        }
    }

    private void CaptureAndLockCursor()
    {
        if (!lockCursorWhileAiming || cursorStateCaptured)
        {
            return;
        }

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        cursorStateCaptured = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void RestoreCursor()
    {
        if (!cursorStateCaptured)
        {
            return;
        }

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
        cursorStateCaptured = false;
    }

    private void OnDisable()
    {
        if (visualsActive)
        {
            SetVisualsActive(false);
        }

        RestoreCursor();
    }

    private void OnDestroy()
    {
        if (shieldEffectInstance != null)
        {
            Destroy(shieldEffectInstance);
        }

        if (aimPointerEffectInstance != null)
        {
            Destroy(aimPointerEffectInstance);
        }
    }

    private void OnValidate()
    {
        sphereRadius = Mathf.Max(0.1f, sphereRadius);
        horizontalAimAngle = Mathf.Clamp(horizontalAimAngle, 1f, 180f);
        verticalAimAngle = Mathf.Clamp(verticalAimAngle, 1f, 180f);
        mouseSensitivity = Mathf.Max(0.01f, mouseSensitivity);
        shieldScaleMultiplier = Mathf.Max(0.01f, shieldScaleMultiplier);
        pointerScaleMultiplier = Mathf.Max(0.01f, pointerScaleMultiplier);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.TransformPoint(aimCenterOffset);
        Vector3 leftDirection = transform.TransformDirection(
            Quaternion.Euler(0f, -horizontalAimAngle * 0.5f, 0f) *
            Vector3.forward);
        Vector3 rightDirection = transform.TransformDirection(
            Quaternion.Euler(0f, horizontalAimAngle * 0.5f, 0f) *
            Vector3.forward);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, sphereRadius);
        Gizmos.DrawLine(center, center + leftDirection * sphereRadius);
        Gizmos.DrawLine(center, center + rightDirection * sphereRadius);
        Gizmos.DrawLine(center, AimPoint);
    }
}
