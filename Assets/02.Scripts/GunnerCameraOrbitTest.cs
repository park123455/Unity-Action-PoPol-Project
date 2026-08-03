using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineOrbitalFollow))]
[DefaultExecutionOrder(100)]
public sealed class GunnerCameraOrbitTest : MonoBehaviour
{
    [Header("Test Orbit")]
    [Tooltip("Angle swept to each side from the starting camera angle.")]
    [Range(0f, 180f)]
    [SerializeField] private float halfArcAngle = 45f;

    [Tooltip("Seconds required for one complete left-right-left cycle.")]
    [Min(0.1f)]
    [SerializeField] private float cycleDuration = 6f;

    [Tooltip("Disable mouse orbit input while this test is active.")]
    [SerializeField] private bool disableManualInput = true;

    private CinemachineOrbitalFollow orbitalFollow;
    private CinemachineInputAxisController inputController;
    private float startingAngle;
    private float elapsedTime;
    private bool inputWasEnabled;

    private void OnEnable()
    {
        orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        inputController = GetComponent<CinemachineInputAxisController>();

        startingAngle = orbitalFollow.HorizontalAxis.Value;
        elapsedTime = 0f;

        if (disableManualInput && inputController != null)
        {
            inputWasEnabled = inputController.enabled;
            inputController.enabled = false;
        }
    }

    private void Update()
    {
        if (orbitalFollow == null)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        float safeDuration = Mathf.Max(0.1f, cycleDuration);
        float phase = elapsedTime * Mathf.PI * 2f / safeDuration;

        // Cinemachine keeps the camera on its orbit around the Gunner.
        // Only the horizontal orbit angle is swept back and forth.
        orbitalFollow.HorizontalAxis.Value =
            startingAngle + Mathf.Sin(phase) * halfArcAngle;
    }

    private void OnDisable()
    {
        if (orbitalFollow != null)
        {
            orbitalFollow.HorizontalAxis.Value = startingAngle;
        }

        if (disableManualInput && inputController != null)
        {
            inputController.enabled = inputWasEnabled;
        }
    }
}
