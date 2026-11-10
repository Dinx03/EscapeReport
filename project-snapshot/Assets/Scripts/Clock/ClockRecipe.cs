using UnityEngine;

// A kind of clock, written down as data.
//
// The project has claimed all along that the kind of a clock is the sum of its parts and
// never a subclass. Until this existed you could not actually *write* that sum: a new kind
// of clock meant a new ninety-line method in an editor script, which is the one place the
// architecture contradicted itself.
//
// A recipe is a case and a list of things bolted to it. Note that displays and parts are
// not separate lists — from here they are the same thing, "something you screw on", and
// `ClockMovement` sorts out which is which by asking what interfaces they implement. That
// is the whole idea, sitting in an Inspector where a designer can edit it.
[CreateAssetMenu(menuName = "Escape Room/Clock Recipe", fileName = "ClockRecipe")]
public class ClockRecipe : ScriptableObject
{
    // One thing screwed to the case: a dial, a digital readout, a mainspring, a pendulum,
    // or something nobody has written yet.
    [System.Serializable]
    public struct Fitting
    {
        public GameObject Prefab;
        public Vector3 At;
        public Vector3 Turn;

        [Min(0.01f)]
        public float Scale;
    }

    [SerializeField]
    [Tooltip("The empty case. Geometry only — no clock components on it.")]
    private GameObject caseModel;

    [SerializeField]
    [Tooltip("Everything bolted to the case, in any order. A display starts showing the " +
             "time; a part starts having an opinion about whether it may run.")]
    private Fitting[] fittings;

    [Header("How this one was found")]
    [SerializeField] private ClockCondition condition = ClockCondition.Running;
    [SerializeField, Range(0, 11)] private int startHour = 10;
    [SerializeField, Range(0, 59)] private int startMinute = 10;

    [SerializeField, Min(0f)]
    [Tooltip("Clock minutes per real second. 0.0167 is real time.")]
    private float minutesPerSecond = 1f / 60f;

    public GameObject CaseModel => caseModel;
    public Fitting[] Fittings => fittings;
    public ClockCondition Condition => condition;
    public int StartHour => startHour;
    public int StartMinute => startMinute;
    public float MinutesPerSecond => minutesPerSecond;
}
