using UnityEngine;

// What lets the wall behind the longcase go.
//
// Fit the pendulum and the weights, the movement starts, and the case strikes. The wall
// gives way when the striking has run its length — the player never opens it, and there is
// no press that will: the `SealedLock` beside this refuses every one, and `SetOpen` moves an
// Openable regardless of its locks, which is how a catch with nothing on the near side is
// sprung from the far one.
//
// **The bell and the wait are one number, not two.** The wall opens when the ringing is
// done, so there is nowhere for the sound and the delay to drift apart. What is left of the
// clip after that is left to ring out: a clock does not stop striking because a wall moved.
//
// It polls rather than subscribes because a movement has no event for coming back to life —
// `RunningLock` reads the same thing the same way — and both the ringing and the release
// latch, so a spring that runs down an hour later cannot take the wall back, and a player who
// shuts the wall behind them does not have it shoved open again next frame.
[DisallowMultipleComponent]
[RequireComponent(typeof(Openable))]
public class ChimeRelease : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The clock this wall is waiting on. It opens once every part that clock needs " +
             "is fitted and it starts going.")]
    private ClockMovement movement;

    [SerializeField]
    private AudioSource bell;

    [SerializeField, Min(0f)]
    [Tooltip("How long it rings before the wall gives way.")]
    private float ringSeconds = 5f;

    private Openable _wall;
    private float _ringingSince = -1f;
    private bool _released;

    private void Awake()
    {
        _wall = GetComponent<Openable>();
    }

    private void Update()
    {
        if (_released)
            return;

        if (_ringingSince < 0f)
        {
            if (!movement.IsRunning)
                return;

            _ringingSince = Time.time;
            bell.Play();
            return;
        }

        if (Time.time - _ringingSince < ringSeconds)
            return;

        _released = true;
        _wall.SetOpen(true);
    }
}
