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

    [Tooltip("Line effect placed at the exact center of the aim pointer circle.")]
    [SerializeField] private GameObject pointerCenterLineEffectPrefab;

    [Header("Aim Sphere")]
    [Tooltip("Local center of the sphere used to calculate the firing direction.")]
    [SerializeField] private Vector3 aimCenterOffset = new Vector3(0f, 0.8f, 0f);

    [Tooltip("Distance from the aim center to the direction marker.")]
    [SerializeField, Min(0.1f)] private float sphereRadius = 1.6f;

    [Header("Firing Alignment")]
    [Tooltip("Actual transform used as the firing origin. The pointer is placed where this transform's forward ray meets the aim sphere.")]
    [SerializeField] private Transform firingOrigin;

    [SerializeField] private bool alignPointerToFiringRay = true;

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
    [SerializeField, Min(0.01f)] private float centerLineScaleMultiplier = 1f;

    [Tooltip("Moves the pointer root slightly inside the calculated sphere so the visible laser base touches the shield surface.")]
    [SerializeField, Min(0f)] private float pointerSurfaceInset = 0.5f;

    [Tooltip("Additional local rotation applied after the pointer's up axis is aligned to the sphere normal.")]
    [SerializeField] private Vector3 pointerEulerOffset;

    public bool IsAiming => gunnerInput != null && gunnerInput.IsAimHeld;
    public float AimYaw => aimYaw;
    public float AimPitch => aimPitch;

    public Vector3 AimDirection
    {
        get
        {
            if (alignPointerToFiringRay && firingOrigin != null)
            {
                return firingOrigin.forward.normalized;
            }

            Vector3 direction = GetLocalAimDirection();
            return transform.TransformDirection(direction).normalized;
        }
    }

    public Vector3 AimPoint => transform.TransformPoint(
        GetSphereSurfaceLocalPosition(
            sphereRadius,
            GetLocalAimDirection()));

    private GunnerInput gunnerInput;
    private GameObject shieldEffectInstance;
    private GameObject aimPointerEffectInstance;
    private GameObject pointerCenterLineEffectInstance;
    private Vector3 shieldBaseScale = Vector3.one;
    private Vector3 pointerBaseScale = Vector3.one;
    private Vector3 centerLineBaseScale = Vector3.one;
    private float aimYaw;
    private float aimPitch;
    private bool visualsActive;
    private bool cursorStateCaptured;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    private void Awake()
    {
        gunnerInput = GetComponent<GunnerInput>();
        ResolveFiringOrigin();
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

    private Vector3 GetSphereSurfaceLocalPosition(
        float radius,
        Vector3 fallbackLocalDirection)
    {
        radius = Mathf.Max(0f, radius);

        if (alignPointerToFiringRay && firingOrigin != null)
        {
            Vector3 localRayOrigin = transform.InverseTransformPoint(
                firingOrigin.position);
            Vector3 localRayDirection = transform.InverseTransformDirection(
                firingOrigin.forward).normalized;
            Vector3 originFromCenter = localRayOrigin - aimCenterOffset;
            float projectedOrigin = Vector3.Dot(
                originFromCenter,
                localRayDirection);
            float discriminant =
                projectedOrigin * projectedOrigin -
                (originFromCenter.sqrMagnitude - radius * radius);

            if (discriminant >= 0f)
            {
                float root = Mathf.Sqrt(discriminant);
                float nearDistance = -projectedOrigin - root;
                float farDistance = -projectedOrigin + root;
                float rayDistance = nearDistance >= 0f
                    ? nearDistance
                    : farDistance;

                if (rayDistance >= 0f)
                {
                    return localRayOrigin +
                        localRayDirection * rayDistance;
                }
            }
        }

        return aimCenterOffset + fallbackLocalDirection * radius;
    }

    private void ResolveFiringOrigin()
    {
        if (firingOrigin != null)
        {
            return;
        }

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "MuzzlePoint")
            {
                firingOrigin = child;
                return;
            }
        }

        if (alignPointerToFiringRay)
        {
            Debug.LogWarning(
                $"[{nameof(GunnerAimVisualizer)}] Firing origin was not found on '{name}'.",
                this);
        }
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

        if (pointerCenterLineEffectInstance == null &&
            pointerCenterLineEffectPrefab != null)
        {
            pointerCenterLineEffectInstance = Instantiate(
                pointerCenterLineEffectPrefab,
                transform);
            pointerCenterLineEffectInstance.name =
                "Aim Pointer Center (SingleLine-LightSaber 1)";
            centerLineBaseScale =
                pointerCenterLineEffectInstance.transform.localScale;
            pointerCenterLineEffectInstance.SetActive(false);
        }

        if (shieldEffectPrefab == null ||
            aimPointerEffectPrefab == null ||
            pointerCenterLineEffectPrefab == null)
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
            Vector3 pointerSurfacePosition =
                GetSphereSurfaceLocalPosition(
                    sphereRadius,
                    localDirection);
            Vector3 pointerSurfaceNormal =
                (pointerSurfacePosition - aimCenterOffset).normalized;
            pointerTransform.localPosition =
                pointerSurfacePosition -
                pointerSurfaceNormal * pointerSurfaceInset;
            pointerTransform.localRotation =
                Quaternion.FromToRotation(
                    Vector3.up,
                    pointerSurfaceNormal) *
                Quaternion.Euler(pointerEulerOffset);
            pointerTransform.localScale =
                pointerBaseScale * pointerScaleMultiplier;

            if (pointerCenterLineEffectInstance != null)
            {
                Transform centerLineTransform =
                    pointerCenterLineEffectInstance.transform;
                centerLineTransform.localPosition =
                    pointerTransform.localPosition;
                centerLineTransform.localRotation =
                    pointerTransform.localRotation;
                centerLineTransform.localScale =
                    centerLineBaseScale * centerLineScaleMultiplier;
            }
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

        if (pointerCenterLineEffectInstance != null)
        {
            pointerCenterLineEffectInstance.SetActive(active);
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

        if (pointerCenterLineEffectInstance != null)
        {
            Destroy(pointerCenterLineEffectInstance);
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
        centerLineScaleMultiplier = Mathf.Max(
            0.01f,
            centerLineScaleMultiplier);
        pointerSurfaceInset = Mathf.Clamp(
            pointerSurfaceInset,
            0f,
            sphereRadius);
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
