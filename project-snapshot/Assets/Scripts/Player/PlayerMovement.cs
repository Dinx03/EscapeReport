using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputReader))]
public class PlayerMovement : MonoBehaviour
{
    private const float MovingThreshold = 0.1f;   // m/s

    [Header("Walking")]
    [SerializeField, Min(0f)]
    private float walkSpeed = 2.5f;

    [SerializeField, Min(0f)]
    private float acceleration = 25f;

    [Header("Gravity")]
    [SerializeField, Min(0f)]
    private float gravity = 15f;

    [SerializeField, Min(0f)]
    private float groundedPull = 2f;   // steady downward push, stops isGrounded flickering on floor seams

    [SerializeField, Min(0f)]
    private float maxFallSpeed = 40f;  // uncapped, a long fall builds enough speed to tunnel through floors

    private CharacterController _controller;
    private InputReader _input;
    private Vector3 _velocity;
    private float _fallSpeed;

    public bool IsMoving => _velocity.sqrMagnitude > MovingThreshold * MovingThreshold;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<InputReader>();
    }

    private void Update()
    {
        // Clamped so walking diagonally is no faster than walking straight.
        Vector2 move = Vector2.ClampMagnitude(_input.Move, 1f);
        Vector3 target = (transform.right * move.x + transform.forward * move.y) * walkSpeed;

        _velocity = Vector3.MoveTowards(_velocity, target, acceleration * Time.deltaTime);

        if (_controller.isGrounded && _fallSpeed < 0f)
            _fallSpeed = -groundedPull;
        else
            _fallSpeed = Mathf.Max(_fallSpeed - gravity * Time.deltaTime, -maxFallSpeed);

        _controller.Move((_velocity + Vector3.up * _fallSpeed) * Time.deltaTime);
    }

    // Speed lives in a field, so it has to be dropped here. Otherwise the player
    // picks up where they left off and glides away when movement is switched
    // back on after the inventory closes.
    private void OnDisable()
    {
        _velocity = Vector3.zero;
        _fallSpeed = 0f;
    }
}
