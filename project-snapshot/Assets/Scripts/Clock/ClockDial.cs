using UnityEngine;

// Points the hands at a time it is handed: the analogue implementation of `IClockDisplay`.
//
// It has no idea whether that time runs forwards, backwards or not at all — that is the
// movement's business. Keeping the dial ignorant is what lets the same dial prefab drop
// into a wristwatch, a pocket watch or a longcase without changing a line, and it is why a
// second kind of display could be written without touching this one.
[DisallowMultipleComponent]
public class ClockDial : MonoBehaviour, IClockDisplay
{
    private const float MinutesOnDial = 12f * 60f;

    [SerializeField] private Transform hourHand;
    [SerializeField] private Transform minuteHand;

    [SerializeField]
    [Tooltip("Optional. Plenty of clocks have no second hand.")]
    private Transform secondHand;

    // Minutes past twelve. The fractional part carries the seconds.
    public void Show(float minutes)
    {
        float onTheDial = Mathf.Repeat(minutes, MinutesOnDial);
        float minuteOfHour = Mathf.Repeat(minutes, 60f);
        float seconds = minuteOfHour % 1f * 60f;

        // Positive, not negative. The face looks down +Z and the player stands on that
        // side, and from there a positive Z rotation sweeps the hand from the 12 towards
        // the 3 — which is the way a clock goes. Reading it as anticlockwise is the
        // mistake you make from the maths convention, where you are behind the dial.
        hourHand.localRotation = Quaternion.Euler(0f, 0f, onTheDial / MinutesOnDial * 360f);
        minuteHand.localRotation = Quaternion.Euler(0f, 0f, minuteOfHour / 60f * 360f);

        if (secondHand != null)
            secondHand.localRotation = Quaternion.Euler(0f, 0f, seconds / 60f * 360f);
    }
}
