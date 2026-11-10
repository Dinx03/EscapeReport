using UnityEngine;

// Arthur's last machine: three prisms on a gnomon, a lens, and six points of light thrown
// on the wall.
//
// The light is not simulated, and it is not meant to be. Each prism does one thing to the
// pattern and nothing else — the first turns it, the second slides it, the third opens it
// out — so a player who turns one and watches the wall learns what that one is for, which is
// the whole puzzle. Three parts of a stated shape, found by looking rather than by arithmetic.
//
// What it opens it does not know. `Solved` latches, and a lock reads it.
[DisallowMultipleComponent]
public class Horologium : MonoBehaviour
{
    [SerializeField]
    [Tooltip("In the order the light passes them: the turner, the slider, the spreader.")]
    private Prism[] prisms;

    [SerializeField]
    [Tooltip("Six points of light, and the six marks they belong on. Marks are the shape " +
             "itself; the lights are that shape put wrong by however the prisms stand.")]
    private Transform[] lights;

    [SerializeField] private Transform[] marks;

    [SerializeField]
    [Tooltip("Switched on once all three prisms are in and light is running the train.")]
    private GameObject beam;

    [SerializeField]
    [Tooltip("Where each prism has to stand. Nothing says so out loud; the wall does.")]
    private int[] answer = { 2, 3, 4 };

    [SerializeField, Min(0f)]
    [Tooltip("How far the slider moves the pattern per face, and how much the spreader " +
             "opens it out per face.")]
    private float slide = 0.09f;

    [SerializeField, Min(0f)] private float spread = 0.14f;

    [SerializeField, Min(0f)]
    [Tooltip("How close a point has to be to its mark to count, and how long every one of " +
             "them has to stay there.")]
    private float tolerance = 0.02f;

    [SerializeField, Min(0f)] private float hold = 2f;

    [SerializeField, Min(0.1f)]
    [Tooltip("How quickly a point slides to where the prisms now put it.")]
    private float glide = 7f;

    private float _held;
    private float _lift;

    private void Awake()
    {
        // How far off the screen the lights are drawn. The builder sets it; taking it from
        // the prop rather than writing it down again is also what stops the pattern being
        // flattened into the marks it is supposed to be sliding across.
        _lift = lights[0].localPosition.z;
    }

    // Every prism fitted. A socket switches its prism on, so this is the same question as
    // asking whether all three are there.
    public bool Ready
    {
        get
        {
            foreach (Prism prism in prisms)
            {
                if (!prism.isActiveAndEnabled)
                    return false;
            }

            return true;
        }
    }

    public bool Solved { get; private set; }

    // Turnable while the train is complete and the pattern has not been found. After that the
    // prisms are set: a mechanism that has done its work does not come apart again.
    public bool Turnable => Ready && !Solved;

    private void Update()
    {
        beam.SetActive(Ready);

        if (!Ready)
            return;

        bool onTheMarks = true;

        for (int i = 0; i < lights.Length; i++)
        {
            Vector3 goal = PointFor(marks[i].localPosition);
            goal.z = _lift;
            lights[i].localPosition = Vector3.Lerp(lights[i].localPosition, goal, glide * Time.deltaTime);

            // Across the screen only. The lights ride a few millimetres in front of the
            // marks, and that gap is not the player being wrong about anything.
            Vector2 apart = lights[i].localPosition - marks[i].localPosition;

            if (apart.magnitude > tolerance)
                onTheMarks = false;
        }

        if (Solved)
            return;

        // Held, not touched: the points drift into place, and a pattern that is only right
        // in passing is not the pattern.
        _held = onTheMarks ? _held + Time.deltaTime : 0f;
        Solved = _held >= hold;
    }

    // The shape as the prisms have it: turned, slid, opened out. Each prism reads as the
    // distance it stands from where it belongs, so all three at their answer leaves the
    // shape exactly as it was drawn.
    private Vector3 PointFor(Vector3 mark)
    {
        int turned = Off(0);
        int slid = Off(1);
        int opened = Off(2);

        Vector3 point = mark * (1f + opened * spread);
        point = Quaternion.Euler(0f, 0f, turned * 360f / prisms[0].Faces) * point;

        return point + new Vector3(slid * slide, 0f, 0f);
    }

    private int Off(int prism)
    {
        int faces = prisms[prism].Faces;
        return ((prisms[prism].Index - answer[prism]) % faces + faces) % faces;
    }
}
