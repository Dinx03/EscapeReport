using UnityEngine;

// The sound of a bench test running.
//
// It has no length of its own. It starts when the run starts and stops when the run stops, so
// however long `TestRun` is set to, the ticking is exactly that long and the two cannot come
// apart. The one number it owns is how much of the recording to use.
//
// **The clip is shorter than the run, so a piece of it is played over and over.** Ten seconds
// of test against nine seconds of recording leaves a second of silence at the end, and that
// is the second the player is staring hardest at the glass.
//
// The piece has to be a whole number of ticks or the seam swallows a beat. Measured off the
// clip, the ticks sit on a grid of 0.255 s — some passages run at that, some at twice it —
// so 5.1 s is twenty of them and lands the join exactly where a tick was going to fall
// anyway. Twice through covers a ten second run with two tenths to spare.
[DisallowMultipleComponent]
[RequireComponent(typeof(TestRun))]
[RequireComponent(typeof(AudioSource))]
public class RunTicking : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    [Tooltip("How much of the clip to play before starting it again. A whole number of " +
             "ticks, or the join drops a beat.")]
    private float segmentSeconds = 5.1f;

    private TestRun _run;
    private AudioSource _ticking;
    private bool _wasRunning;

    private void Awake()
    {
        _run = GetComponent<TestRun>();
        _ticking = GetComponent<AudioSource>();
    }

    private void Update()
    {
        bool running = _run.Allows;

        if (running && !_wasRunning)
            _ticking.Play();
        else if (!running && _wasRunning)
            _ticking.Stop();
        else if (running && _ticking.time >= segmentSeconds)
            _ticking.time = 0f;

        _wasRunning = running;
    }

    private void OnDisable()
    {
        // Unity stops a source that goes away with its own object; the flag does not clear
        // itself, and a run that was under way would read as a fresh one on the way back in.
        _wasRunning = false;
    }
}
