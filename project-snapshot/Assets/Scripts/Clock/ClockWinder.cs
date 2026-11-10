using UnityEngine;

// The crank on the back of the case. Turning it fills whatever store the clock keeps its
// power in, and while the
// handle is moving a shutter slides aside to show a number reel running past a window.
// Whatever digit it settles on is this clock's number for the hall puzzle.
[DisallowMultipleComponent]
public class ClockWinder : MonoBehaviour, IInteractable
{
    private const int DigitsOnReel = 10;

    [SerializeField] private ClockMovement movement;

    [SerializeField] private Transform crank;
    [SerializeField] private Transform shutter;
    [SerializeField] private Renderer numberReel;

    [Header("Reveal")]
    [SerializeField, Range(0, 9)]
    [Tooltip("Digit shown once the crank stops. Every clock shows one; only the clocks " +
             "already running when the player arrived show a true one.")]
    private int revealedDigit;

    [Header("Feel")]
    [SerializeField, Min(1)]
    [Tooltip("Whole turns per press. Whole, so the handle always comes to rest where it " +
             "was posed instead of stopping somewhere behind the case.")]
    private int turnsPerCrank = 3;

    [SerializeField, Min(1f)] private float turnSpeed = 620f;

    [SerializeField]
    [Tooltip("How far the shutter slides while the handle is moving.")]
    private Vector3 shutterOpenOffset = new Vector3(0f, 0.030f, 0f);

    [SerializeField, Min(0.01f)] private float shutterSpeed = 0.16f;

    private IPowerStore _power;
    private bool _revealed;
    private float _restAngle;
    private float _turned;
    private float _spinFrom;
    private float _target;
    private float _reelFrom;
    private float _reelTo;
    private Vector3 _shutterClosed;
    private Material _reelMaterial;

    public int RevealedDigit => revealedDigit;
    public bool IsTurning => !Mathf.Approximately(_turned, _target);

    public string Prompt => movement.IsRunning ? "[E] หมุนไขลานเพิ่ม" : "[E] หมุนไขลาน";

    private void Awake()
    {
        // Found, not wired: the crank fills whatever store this clock keeps its power in.
        // A clock with none turns the handle and does nothing, which is what a dummy key on
        // a decorative clock does too.
        _power = movement.GetComponentInChildren<IPowerStore>(true);

        // Turn from wherever the handle was posed in the prefab. Snapping it to zero on
        // the first crank would jerk it upright, behind the case, out of reach.
        _restAngle = crank.localEulerAngles.z;
        _shutterClosed = shutter.localPosition;
        _reelMaterial = numberReel.material;
        ShowDigit(revealedDigit);
    }

    private void Update()
    {
        bool turning = IsTurning;

        if (turning)
        {
            _turned = Mathf.MoveTowards(_turned, _target, turnSpeed * Time.deltaTime);
            // Plus, for the same reason the hands take a positive angle: from in front of
            // the case this winds the handle clockwise, the way you would actually turn it.
            crank.localRotation = Quaternion.Euler(0f, 0f, _restAngle + _turned);

            // The reel is geared to the handle rather than locked to it, which is what
            // lets the handle finish upright while the digits still land where they must.
            float progress = Mathf.InverseLerp(_spinFrom, _target, _turned);
            SetReel(Mathf.Lerp(_reelFrom, _reelTo, progress));

            if (!IsTurning)
                ShowDigit(revealedDigit);
        }

        // Stays open once it has been cranked. The digit is something the player has to
        // carry to a keypad in another room, so slamming the shutter would just mean
        // cranking every clock a second time.
        Vector3 target = _revealed ? _shutterClosed + shutterOpenOffset : _shutterClosed;
        shutter.localPosition = Vector3.MoveTowards(shutter.localPosition, target, shutterSpeed * Time.deltaTime);
    }

    public void Interact(IActor actor)
    {
        bool wasStopped = !movement.IsRunning;

        _power.Fill();

        _revealed = true;
        _spinFrom = _turned;
        _target = _turned + turnsPerCrank * 360f;

        // Run the reel through a few whole passes, then let it settle the short way
        // round onto this clock's digit.
        _reelFrom = _reelMaterial.GetTextureOffset("_BaseMap").y;

        _reelTo = _reelFrom + turnsPerCrank + Mathf.Repeat(revealedDigit / (float)DigitsOnReel - _reelFrom, 1f);

        // A broken movement takes the crank and does nothing with it, which is the point:
        // the number still appears, so the reel alone never tells you which clocks count.
        string message = !wasStopped ? "ไขลานเพิ่มแล้ว"
            : movement.IsRunning ? "นาฬิกาเริ่มเดินแล้ว"
            : "ไขลานแล้ว แต่เข็มไม่ขยับ";

        actor.Say(message);
    }

    private void ShowDigit(int digit)
    {
        SetReel(digit / (float)DigitsOnReel);
    }

    private void SetReel(float offset)
    {
        var scroll = new Vector2(0f, offset);
        _reelMaterial.SetTextureOffset("_BaseMap", scroll);

        // Emission carries the same strip so the lit plate scrolls with the printed one.
        _reelMaterial.SetTextureOffset("_EmissionMap", scroll);
    }
}
