using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public sealed class GunnerInput : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public bool IsRunHeld { get; private set; }
    public bool AttackPressedThisFrame { get; private set; }
    public bool IsAttackHeld { get; private set; }
    public bool AimPressedThisFrame { get; private set; }
    public bool AimReleasedThisFrame { get; private set; }
    public bool IsAimHeld { get; private set; }
    public Vector2 AimLookDelta { get; private set; }

    private void Update()
    {
        MoveInput = Vector2.ClampMagnitude(
            new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")),
            1f);

        IsRunHeld = MoveInput.sqrMagnitude > 0.01f
            && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        AttackPressedThisFrame = Input.GetMouseButtonDown(0);
        IsAttackHeld = Input.GetMouseButton(0);

        AimPressedThisFrame = Input.GetMouseButtonDown(1);
        AimReleasedThisFrame = Input.GetMouseButtonUp(1);
        IsAimHeld = Input.GetMouseButton(1);
        AimLookDelta = IsAimHeld
            ? new Vector2(
                Input.GetAxisRaw("Mouse X"),
                Input.GetAxisRaw("Mouse Y"))
            : Vector2.zero;
    }

    private void OnDisable()
    {
        MoveInput = Vector2.zero;
        IsRunHeld = false;
        AttackPressedThisFrame = false;
        IsAttackHeld = false;
        AimPressedThisFrame = false;
        AimReleasedThisFrame = false;
        IsAimHeld = false;
        AimLookDelta = Vector2.zero;
    }
}
