using UnityEngine;

// One prism on Arthur's gnomon: a thing with so many faces, which turns to that many
// positions and no others. A three-sided one has three, a six-sided one six, and where it
// stops is the whole of what it tells the light train below it.
//
// It will not turn until the train is complete, and it will not turn again once the pattern
// has been found. Both of those are the rig's business, not the prism's, so it asks.
[DisallowMultipleComponent]
public class Prism : MonoBehaviour, IInteractable
{
    [SerializeField, Range(3, 12)]
    [Tooltip("Faces, and so how many positions it turns to: a third of a circle each for a " +
             "three-sided one, a sixth for a six-sided one.")]
    private int faces = 3;

    [SerializeField, Min(1f)]
    [Tooltip("Degrees a second while it comes round to its next face.")]
    private float turnSpeed = 300f;

    [SerializeField] private string prompt = "[E] หมุนปริซึม";
    [SerializeField] private string lockedPrompt = "[E] ยังหมุนไม่ได้";

    private Horologium _rig;
    private float _goal;
    private int _index;

    public int Faces => faces;

    // Which face is forward, counted from the one it was fitted in.
    public int Index => _index;

    public string Prompt
    {
        get
        {
            // Once the pattern is found the prisms are set, and a line still offering to turn
            // one is a line telling the player the machine is not finished when it is.
            if (_rig.Solved)
                return null;

            return _rig.Turnable ? prompt : lockedPrompt;
        }
    }

    private void Awake()
    {
        _rig = GetComponentInParent<Horologium>();
    }

    private void Update()
    {
        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation, Quaternion.Euler(0f, _goal, 0f), turnSpeed * Time.deltaTime);
    }

    public void Interact(IActor actor)
    {
        if (!_rig.Turnable)
            return;

        _index = (_index + 1) % faces;
        _goal = _index * 360f / faces;
    }
}
