using UnityEngine;

// The escapement, and the tell Puzzle 1 rests on.
//
// It is a part of the movement, not decoration on it: a movement asks the pendulum whether
// it may run, so a seized one stops the clock however full its spring is. That is what makes
// a longcase a longcase — take the pendulum out and the case is a cupboard.
//
// It swings while the movement runs, and the player has to spot which clocks were alive
// before touching anything. A swinging bob reads from further away than a second hand does.
[DisallowMultipleComponent]
public class Pendulum : MonoBehaviour, IClockPart
{

    [SerializeField]
    [Tooltip("A pendulum held, bent or jammed. The clock will not run with one of these on " +
             "it, whatever else is right.")]
    private bool seized;

    [SerializeField, Range(0f, 25f)]
    private float swingDegrees = 7f;

    [SerializeField, Min(0.1f)]
    [Tooltip("Seconds for one full left-right-left swing.")]
    private float secondsPerSwing = 2f;

    [SerializeField, Min(0.1f)]
    [Tooltip("How quickly it gives up once the clock stops.")]
    private float settleSpeed = 2f;

    private ClockMovement _movement;
    private float _phase;
    private float _amplitude;

    // A part that has been switched off is a part that is not fitted. That is what lets a
    // clock be assembled in play: the pendulum is here all along, just not in it yet.
    public bool Allows => !seized && isActiveAndEnabled;

    // It regulates rather than drives: it says whether, not how fast.
    public float RateScale => 1f;

    private void Awake()
    {
        // Found, not wired: bolting a pendulum on is the whole of fitting one, which is
        // what lets it ship as a prefab a recipe can list rather than a reference a
        // person has to drag in the Inspector.
        _movement = GetComponentInParent<ClockMovement>();
    }

    private void Update()
    {
        // Amplitude rather than phase, so a stopped clock winds down to upright instead
        // of freezing mid-swing like a paused video.
        float target = _movement.IsRunning ? 1f : 0f;
        _amplitude = Mathf.MoveTowards(_amplitude, target, settleSpeed * Time.deltaTime);

        if (_amplitude <= 0f)
        {
            transform.localRotation = Quaternion.identity;
            return;
        }

        _phase += Time.deltaTime / secondsPerSwing * Mathf.PI * 2f;
        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_phase) * swingDegrees * _amplitude);
    }

    public void Prime(bool foundRunning)
    {
        // A pendulum keeps nothing in reserve; how it was found is the spring's business.
    }

    public void Draw(float minutes)
    {
    }
}
