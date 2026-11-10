using UnityEngine;

// Makes a clock announce the hour by opening and shutting something, once per hour on the
// dial, in time with a recording of it doing exactly that.
//
// **The recording is the only clock in here.** It used to keep a stopwatch of its own and cut
// the sound to fit it, which put the door on one rhythm and the voice on another: the door
// swung every 1.1 seconds while the recording called every 0.8, and every swing restarted the
// clip part way through. Nothing lined up, and it sounded like it. Now the door is a pure
// read of the playhead — open when the playhead is near a call, shut when it is not — so the
// two cannot drift apart, because there is only one of them.
//
// It counts out loud in the only currency this game has: a thing that moves. A clock that is
// not running strikes nothing at all, which is the trap the parlour is built on — a count of
// none is still a digit.
//
// Putting power back into it is what sets it off, so the player chooses when to listen, and
// it does not care whether that was a crank, a key or a pulled chain.
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class ClockStriker : MonoBehaviour
{
    [SerializeField] private ClockMovement movement;

    [SerializeField]
    [Tooltip("The little door, flap or shutter that moves on each strike.")]
    private Openable door;

    [Header("Where the calls are in the recording")]
    [SerializeField, Min(0f)]
    [Tooltip("When the first call sounds. Everything before it is the clock gathering " +
             "itself, and it plays with the door still shut.")]
    private float firstCall = 1.035f;

    [SerializeField, Min(0.05f)]
    [Tooltip("How long the recording leaves between one call and the next.")]
    private float callSpacing = 0.805f;

    [SerializeField, Min(1)]
    [Tooltip("How many calls the recording actually holds. A clock wanting more strikes " +
             "than this has no voice for them, and stops when the recording runs out.")]
    private int callsInClip = 11;

    [Header("How the door keeps up")]
    [SerializeField, Min(0f)]
    [Tooltip("How far ahead of a call the door starts opening, so whatever is behind it is " +
             "out by the time the note sounds.")]
    private float openLead = 0.20f;

    [SerializeField, Min(0.05f)]
    [Tooltip("How long it stays open after the note. This plus the lead has to leave the " +
             "door time to shut again before the next call.")]
    private float openHold = 0.32f;

    private AudioSource _voice;
    private IPowerStore _power;
    private int _calls;

    // The last call this strike will make, in the recording's own time.
    private float LastCall => firstCall + (_calls - 1) * callSpacing;

    public bool IsStriking => _calls > 0;

    // Twelve rather than nought, the way a clock strikes midnight.
    public int Strikes => movement.Hour == 0 ? 12 : movement.Hour;

    private void Awake()
    {
        _voice = GetComponent<AudioSource>();

        // Found, not wired. Whatever holds this clock's power, filling it sets the bird off
        // — and this has never been told whether that is a spring, a weight or a battery.
        _power = movement.GetComponentInChildren<IPowerStore>(true);
    }

    private void OnEnable() => _power.Filled += Strike;

    private void Update()
    {
        if (!IsStriking)
            return;

        // Done when the last window has closed — or when the recording has run out from
        // under it, which is what happens if the numbers above are set past what the clip
        // actually holds. Either way the door is shut on the way out: leaving it wherever it
        // stood is how a bird ends up hanging out of its hole for the rest of the game.
        if (!_voice.isPlaying || _voice.time > LastCall + openHold)
        {
            _voice.Stop();
            door.SetOpen(false);
            _calls = 0;
            return;
        }

        // Which call the playhead is nearest, and whether it is near enough for the door to
        // be open. Rounding rather than counting: no tally to fall out of step with the sound.
        float at = _voice.time;
        int nearest = Mathf.Clamp(Mathf.RoundToInt((at - firstCall) / callSpacing), 0, _calls - 1);
        float call = firstCall + nearest * callSpacing;

        door.SetOpen(at > call - openLead && at < call + openHold);
    }

    private void OnDisable()
    {
        _power.Filled -= Strike;
        _calls = 0;
    }

    private void Strike()
    {
        if (!movement.IsRunning || IsStriking)
            return;

        _calls = Mathf.Min(Strikes, callsInClip);
        _voice.time = 0f;
        _voice.Play();
    }
}
