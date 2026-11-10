using UnityEngine;

// The player can see their own arms and legs, so the model has to keep animating.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAnimation : MonoBehaviour
{
    private static readonly int WalkingHash = Animator.StringToHash("Walking");

    [SerializeField]
    private Animator animator;

    private PlayerMovement _movement;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        // The mover being switched off is the whole of "not walking". Reading only its speed
        // trusts it to have zeroed that on the way out, and anything left in there — a frame
        // of it, or a disable that never ran — keeps the legs striding while the player
        // stands still holding a note up to their face.
        animator.SetBool(WalkingHash, _movement.enabled && _movement.IsMoving);
    }
}
