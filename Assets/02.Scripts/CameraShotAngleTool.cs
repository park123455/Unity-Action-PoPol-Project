using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class CameraShotAngleTool : MonoBehaviour
{
    [Header("Shooting Angle")]
    [Tooltip("Up/down camera angle in degrees.")]
    [Range(-89f, 89f)]
    [SerializeField] private float pitch;

    [Tooltip("Left/right camera angle in degrees.")]
    [Range(-180f, 180f)]
    [SerializeField] private float yaw;

    [Tooltip("Camera tilt in degrees.")]
    [Range(-180f, 180f)]
    [SerializeField] private float roll;

    [Header("Lens")]
    [Tooltip("Vertical field of view. Smaller values zoom in; larger values show more of the scene.")]
    [Range(1f, 179f)]
    [SerializeField] private float fieldOfView = 60f;

    [Header("Play Mode")]
    [Tooltip("Keep this angle locked while the game is running. Leave off to allow FreeCamera control.")]
    [SerializeField] private bool lockWhilePlaying;

    private void Reset()
    {
        SyncFromCamera();
        ApplyAngle();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ApplyAngle();
        }
    }

    private void LateUpdate()
    {
        if (Application.isPlaying && lockWhilePlaying)
        {
            ApplyAngle();
        }
    }

    [ContextMenu("Apply Shooting Angle")]
    public void ApplyAngle()
    {
        if (!TryGetComponent(out Camera targetCamera))
        {
            return;
        }

        pitch = Mathf.Clamp(pitch, -89f, 89f);
        yaw = Mathf.Clamp(yaw, -180f, 180f);
        roll = Mathf.Clamp(roll, -180f, 180f);
        fieldOfView = Mathf.Clamp(fieldOfView, 1f, 179f);

        Quaternion desiredRotation = Quaternion.Euler(pitch, yaw, roll);
        if (Quaternion.Angle(transform.localRotation, desiredRotation) > 0.001f)
        {
            transform.localRotation = desiredRotation;
        }

        if (!Mathf.Approximately(targetCamera.fieldOfView, fieldOfView))
        {
            targetCamera.fieldOfView = fieldOfView;
        }
    }

    [ContextMenu("Sync From Current Camera")]
    public void SyncFromCamera()
    {
        if (!TryGetComponent(out Camera targetCamera))
        {
            return;
        }

        Vector3 euler = transform.localEulerAngles;
        pitch = ToSignedAngle(euler.x);
        yaw = ToSignedAngle(euler.y);
        roll = ToSignedAngle(euler.z);
        fieldOfView = targetCamera.fieldOfView;
    }

    private static float ToSignedAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}
