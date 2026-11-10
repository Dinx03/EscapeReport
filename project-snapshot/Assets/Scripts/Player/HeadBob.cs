using UnityEngine;

// The small rise and sway of the eyes while walking.
//
// It reads the controller's own velocity rather than the input, so walking into a wall
// stops the bob without anything having to tell it: the player is pressing W, and going
// nowhere, and the eyes should be still.
//
// MouseLook owns the pivot's rotation and never touches its position, which is what leaves
// the position free for this. Two of these on one pivot would fight; hence the attribute.
[DisallowMultipleComponent]
public class HeadBob : MonoBehaviour
{
    [SerializeField] private CharacterController body;

    [SerializeField, Min(0f)]
    private float rise = 0.018f;

    [SerializeField, Min(0f)]
    [Tooltip("Side to side. Without it the bob reads as a lift rather than as a walk.")]
    private float sway = 0.012f;

    [SerializeField, Min(0.1f)]
    [Tooltip("Footfalls per metre. What the player actually feels is this times the walking " +
             "speed — the cadence — so set it by the rhythm you want and not by a real " +
             "stride. At 2.5 m/s a true 65 cm stride is nearly four footfalls a second, " +
             "which is a sprint, and reads as a shudder rather than as a walk.")]
    private float stepsPerMetre = 0.85f;

    [SerializeField, Min(0.1f)]
    [Tooltip("How fast the bob fades in and out when the player starts and stops.")]
    private float settleSpeed = 7f;

    private PlayerMovement _mover;
    private Vector3 _home;
    private float _walked;
    private float _weight;

    private void Awake()
    {
        _mover = GetComponentInParent<PlayerMovement>();
        _home = transform.localPosition;
    }

    private void LateUpdate()
    {
        // A CharacterController reports the velocity of the last Move it was given, and goes
        // on reporting it for as long as nobody moves it again. Take movement away — to hold
        // something up to the eyes, or to open the bag — and the controller swears blind the
        // player is still walking at 2.5 m/s, so the eyes bob on for ever. Nobody moving the
        // player is a player who is not moving; the fade below then settles the eyes.
        //
        // Horizontal only. Gravity keeps a grounded controller sinking a millimetre a
        // frame, and counting that as movement bobs the head of a player standing still.
        Vector3 flat = _mover.enabled ? body.velocity : Vector3.zero;
        flat.y = 0f;

        bool walking = body.isGrounded && flat.magnitude > 0.15f;
        _walked += flat.magnitude * stepsPerMetre * Mathf.PI * Time.deltaTime;
        _weight = Mathf.MoveTowards(_weight, walking ? 1f : 0f, settleSpeed * Time.deltaTime);

        // Two dips per stride, one sway: the dip is the footfall, the sway is the hips.
        transform.localPosition = _home + _weight * new Vector3(
            Mathf.Sin(_walked * 0.5f) * sway,
            -Mathf.Abs(Mathf.Sin(_walked)) * rise,
            0f);
    }

    // Accumulated wobble is state, and state that survives a disable comes back as a
    // camera parked off its mark.
    private void OnDisable()
    {
        transform.localPosition = _home;
        _walked = 0f;
        _weight = 0f;
    }
}
