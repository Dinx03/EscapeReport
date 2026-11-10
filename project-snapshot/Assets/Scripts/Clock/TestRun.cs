using UnityEngine;

// Arthur's bench test: E starts the movement, and it runs until the run times itself out.
//
// It is an `IClockPart`, so it starts nothing — it *stops* things, and lifts its veto for
// the length of a run. That is all "start the mechanism" means to a movement that is
// otherwise sound and wound: everything else about the clock is still the clock's business,
// and a watch that is slow will be slow for the whole of the run.
//
// Every run begins from the time the watch was found at. A bench test tells a clockmaker how
// a movement keeps time; it is not supposed to change what the watch says, and a dial that
// read 4:40 has to still read 4:40 after twenty tests.
[DisallowMultipleComponent]
public class TestRun : MonoBehaviour, IClockPart, IInteractable, IInHandOnly, ITimedTest
{
    [SerializeField, Min(1f)]
    [Tooltip("How far a run goes, given as the seconds a sound movement takes over it — the " +
             "glass's ten. Every watch on the wall has to hold the same number, or the test " +
             "measures the watch against itself rather than against the sand.")]
    private float runSeconds = 10f;

    private ClockMovement _movement;
    private float _found;
    private float _ran;
    private bool _running;

    // A run ends when the hands have covered the ten minutes, however long this movement
    // takes over them: a sound watch ten seconds, one running at four tenths twenty-five. So
    // every watch comes to rest on the same mark and the wall ends up looking identical —
    // what the player is being shown is not where the hand stops but *when* it gets there.
    // The sand runs out with eleven watches already still and one crawling on alone.
    //
    // A clock that cannot reach the mark at all is timed the plain way instead. A dead
    // movement would otherwise leave a run going for ever, waiting on hands that never move.
    private float Length
    {
        get
        {
            float rate = _movement.ConditionRate;
            return rate > 0f ? runSeconds / rate : runSeconds;
        }
    }

    public bool Allows => _running;

    // The frame the run ends on is paid for in part. A frame straddling the end would
    // otherwise be spent whole, and the hand would stop a fraction past the mark by however
    // long that frame happened to be — which is a reading that changes with the machine it
    // is read on. Scaling is how a part says "only this much of it counts" without driving
    // anything itself: the movement still decides how fast it goes, so a sound watch spends
    // that last sliver reaching the mark, and so, later, does a slow one.
    public float RateScale => _running ? Mathf.Clamp01((Length - _ran) / Time.deltaTime) : 1f;

    public string Prompt => _running ? "[E] เริ่มจับเวลาใหม่" : "[E] เริ่มจับเวลา";

    private void Awake()
    {
        _movement = GetComponentInParent<ClockMovement>();
    }

    private void Start()
    {
        // After the movement's own Awake, so this is the time the watch was found at
        // rather than whatever a field happened to be initialised to.
        _found = _movement.Minutes;
    }

    // After every movement has taken its share of the frame. Both used to count in Update,
    // where the order between two components is undefined: whichever ran first decided
    // whether the last frame of a run was spent or skipped, so the hand stopped a frame
    // either side of the mark and which side was not ours to choose.
    private void LateUpdate()
    {
        if (!_running)
            return;

        _ran += Time.deltaTime;

        if (_ran >= Length)
            _running = false;
    }

    public void Prime(bool foundRunning)
    {
        // Nothing is held in reserve here; what a run costs is the spring's business.
    }

    public void Draw(float minutes)
    {
    }

    public void Interact(IActor actor)
    {
        // Always a fresh run rather than a toggle: the glass beside it is turned by the
        // same press, and a press that starts one and stops the other is a test nobody
        // can read.
        Abandon();
        _running = true;
    }

    // Hung back on the wall part way through, which ends the run where it stands: the hands
    // go back to the time the watch was found at and nothing is counting any more.
    //
    // A run left going in a watch nobody is holding walks its own hands on round the dial
    // and takes its ticking with the player into the next room. Both of those are the sound
    // and the state of a test that is not happening.
    public void Abandon()
    {
        _movement.Advance(_found - _movement.Minutes);
        _ran = 0f;
        _running = false;
    }
}
