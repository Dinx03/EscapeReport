using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(InputReader))]
public class MouseLook : MonoBehaviour
{
    [SerializeField]
    private Transform pivot;

    [SerializeField, Min(0f)]
    private float sensitivity = 0.1f;

    [SerializeField, Range(0f, 89f)]
    private float lookUpLimit = 85f;

    [SerializeField, Range(0f, 89f)]
    private float lookDownLimit = 60f;   // past this the player's own chest fills the screen

    private InputReader _input;
    private float _yaw;
    private float _pitch;

    private void Awake()
    {
        _input = GetComponent<InputReader>();

        // Local, to match how it is written back below. Reading world rotation here
        // would drift the moment the player is parented to anything that moves.
        _yaw = transform.localEulerAngles.y;
    }

    private void LateUpdate()
    {
        // Mouse delta already covers the frame, so no Time.deltaTime here.
        Vector2 look = _input.Look * sensitivity;

        _yaw += look.x;
        _pitch = Mathf.Clamp(_pitch - look.y, -lookUpLimit, lookDownLimit);

        // Yaw turns the body so movement follows the view. Pitch stays on the pivot
        // alone, or the CharacterController capsule tips over with it.
        transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        pivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }
}
