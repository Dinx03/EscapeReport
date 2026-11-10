using UnityEngine;

// Keeps a time and hands it to whatever can show one.
//
// Two lists, gathered rather than wired, and between them they are the whole design:
//
//   IClockDisplay   everything that can show a time. The movement calls Show on all of
//                   them and knows nothing else about any of them.
//   IClockPart      everything that has an opinion about whether the time may move. Each
//                   has a veto and each scales the rate.
//
// It owns no power of its own. Strip the parts off and this still shows a time; it just
// never changes. That is the whole idea: a display screwed to anything is already a clock,
// and what makes it *go* is whatever else is screwed on beside it.
//
// Condition is the fiction laid over the top: what is wrong with this particular clock,
// whatever it happens to be built from.
[DisallowMultipleComponent]
public class ClockMovement : MonoBehaviour
{
    private const float MinutesOnDial = 12f * 60f;

    [Header("Condition")]
    [SerializeField] private ClockCondition condition = ClockCondition.Running;

    [SerializeField, Min(0f)]
    [Tooltip("Rate used when the condition is Slow. 1 would keep good time.")]
    private float slowRate = 0.4f;

    [Header("Time shown when found")]
    [SerializeField, Range(0, 11)] private int startHour = 10;
    [SerializeField, Range(0, 59)] private int startMinute = 10;

    [Header("Pace")]
    [SerializeField, Min(0f)]
    [Tooltip("Clock minutes per real second at full rate. Use 0.0167 for real time.")]
    private float minutesPerSecond = 1f;

    private IClockDisplay[] _displays;
    private IClockPart[] _parts;

    // Kept as a double, and it has to be. A clock counts by adding a frame's worth of
    // minutes to however many minutes it already holds, and in a float those two numbers
    // are too far apart to add: at 4:40 the running total is 280, where the gap between one
    // float and the next is 0.00003 minutes, while a slow watch advances 0.00017 in a frame.
    // A fifth of every step was being rounded away — and by a different amount on every
    // watch, because the size of that gap depends on the time the watch is showing. Twelve
    // identical watches given identical ten second runs came back with the hands anywhere
    // from 59.3 to 61.3 degrees. In a double the gap is 0.00000000000006.
    private double _minutes;

    // Every part gets a veto. One dead part stops the clock, however healthy the rest are.
    private bool PartsAllow
    {
        get
        {
            foreach (IClockPart part in _parts)
            {
                if (!part.Allows)
                    return false;
            }

            return true;
        }
    }

    private float Rate
    {
        get
        {
            float rate = ConditionRate;
            foreach (IClockPart part in _parts)
                rate *= part.RateScale;

            return rate;
        }
    }

    // What is wrong with this clock, as a multiplier, leaving out anything that is merely
    // *stopping* it. Public because a bench test has to know how long to let this movement
    // run to cover a given stretch of dial, and the parts are deliberately not in it: one of
    // those parts is the test doing the asking, so folding them in would ask itself.
    public float ConditionRate
    {
        get
        {
            switch (condition)
            {
                case ClockCondition.Slow: return slowRate;
                case ClockCondition.Reversed: return -1f;
                case ClockCondition.Broken: return 0f;
                default: return 1f;   // Running, and Stopped once someone winds it
            }
        }
    }

    public ClockCondition Condition => condition;
    public bool IsRunning => PartsAllow && !Mathf.Approximately(Rate, 0f);

    // Minutes past twelve, seconds in the fraction. Hour and Minute round it off for
    // anyone reading the dial; this is for anything that has to put the hands back where
    // it found them.
    public float Minutes => (float)_minutes;
    public int Hour => (int)(_minutes / 60d) % 12;
    public int Minute => (int)(_minutes % 60d);

    private void Awake()
    {
        // Both lists are found, not wired. Bolt a second display on and it starts showing
        // the time; unbolt the only one and the clock keeps perfect time in private, which
        // is exactly what a movement with no hands does.
        _displays = GetComponentsInChildren<IClockDisplay>(true);

        // Inactive ones included: a part that is switched off is one the player has not
        // fitted yet, and it has to be in the list so it can start saying no.
        _parts = GetComponentsInChildren<IClockPart>(true);
        _minutes = (startHour % 12) * 60f + startMinute;

        // The parts are told how this clock was found, so a spring on one that had stopped
        // starts empty. The fiction sets the state of the mechanism, once.
        bool foundGoing = condition == ClockCondition.Running
                       || condition == ClockCondition.Slow
                       || condition == ClockCondition.Reversed;

        foreach (IClockPart part in _parts)
            part.Prime(foundGoing);

        ShowTime();
    }

    private void Update()
    {
        if (!IsRunning)
            return;

        double advance = (double)minutesPerSecond * Rate * Time.deltaTime;

        // The parts pay for movement in either direction, so a clock running backwards
        // still winds itself down.
        foreach (IClockPart part in _parts)
            part.Draw((float)System.Math.Abs(advance));

        _minutes = OnDial(_minutes + advance);
        ShowTime();
    }

    // Moves the hands without touching whatever powers them. This is a person turning them,
    // not the clock going: a broken movement can still be set to whatever you like.
    public void Advance(float minutes)
    {
        _minutes = OnDial(_minutes + minutes);
        ShowTime();
    }

    private void ShowTime()
    {
        foreach (IClockDisplay display in _displays)
            display.Show((float)_minutes);
    }

    // Mathf.Repeat in double. Twelve hours round is still twelve hours round; what it must
    // not do is drop back to float on the way past, which is the whole point of the type.
    private static double OnDial(double minutes)
    {
        return minutes - System.Math.Floor(minutes / MinutesOnDial) * MinutesOnDial;
    }
}
