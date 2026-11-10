using UnityEngine;
using UnityEngine.InputSystem;

// Shows a prop up close and puts everything back on Q.
//
// Three ways of doing that, chosen by the prop rather than by the player:
//
//   Hold   the prop flies to an anchor in front of the eyes and stays there
//   Turn   the same, and E works in two beats: the first press turns it over, and after
//          that E reaches whatever is round the back — a winder, or nothing, in which case
//          it just keeps turning over
//   Zoom   the prop does not move at all; the camera goes to it
//
// Hold and Turn borrow the prop out of the world — BorrowedProp handles that, including
// putting it back. Zoom borrows nothing, because a longcase clock lifted to your face is
// not a closer look, it is a bug.
[DisallowMultipleComponent]
public class InspectView : MonoBehaviour
{
    [SerializeField] private Transform anchor;
    [SerializeField] private Camera eyes;
    [SerializeField] private InputReader input;
    [SerializeField] private PlayerModeController modes;
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PagePanel page;
    [SerializeField] private HeldSandglass glass;

    [SerializeField]
    [Tooltip("Layer drawn only by the overlay camera. Nothing else should live on it.")]
    private string overlayLayer = "InspectedProp";

    [SerializeField, Range(0, 7)]
    [Tooltip("Rendering layer the inspect lights use. Room lights sit on 0, so moving a " +
             "prop here takes it out of the room's lighting entirely.")]
    private int overlayRenderingLayer = 1;

    [SerializeField, Min(0.1f)] private float followSpeed = 10f;
    [SerializeField, Min(0.1f)] private float zoomSpeed = 5f;

    [SerializeField, Min(0.1f)]
    [Tooltip("How far the view stands back from a zoomed prop, as a multiple of its size.")]
    private float zoomBackOff = 1.9f;

    [SerializeField, Min(0.1f)]
    [Tooltip("How close a held prop comes, as a multiple of its size. A pocket watch has " +
             "to come nearer than a sheet of paper to be equally readable.")]
    private float holdCloseness = 5.5f;

    [SerializeField, Min(0.05f)]
    private float nearestHold = 0.16f;

    [SerializeField, Min(0.2f)]
    [Tooltip("The far end of that scale. A wall clock is two feet tall and needs more room " +
             "than the anchor allows, or it arrives with its ends off the screen.")]
    private float farthestHold = 0.95f;

    private IInspectable _target;
    private BorrowedProp _borrowed;
    private Quaternion _facing;
    private bool _showingBack;
    private IInteractable _work;
    private float _holdDistance;
    private Vector3 _holdPivot;
    private bool _returning;
    private int _overlayLayer;

    private Vector3 _cameraHome;
    private Quaternion _cameraHomeRotation;
    private Vector3 _cameraGoal;
    private Quaternion _cameraGoalRotation;

    private bool Zooming => _target != null && _target.Style == InspectStyle.Zoom;

    private Vector3 HoldPoint =>
        eyes.transform.position + (anchor.position - eyes.transform.position).normalized * _holdDistance;

    // What E would do if it were pressed right now, said out loud on the prompt line, and
    // what F would do while there is a glass in the bag to hold up beside it.
    private string HeldPrompt
    {
        get
        {
            if (_target == null || _target.Style == InspectStyle.Zoom)
                return null;

            string line = WorkPrompt;

            // Only beside something that is actually being timed. The glass is act two's
            // tool, not a second hand the player carries everywhere.
            if (!glass.Carried || glass.Raised || !(_work is ITimedTest))
                return line;

            return line == null ? "[F] ชูนาฬิกาทราย" : line + "    [F] ชูนาฬิกาทราย";
        }
    }

    // What E would do to the prop itself. Turn adds a beat before any of it: a clock arrives
    // face on, and the first press is what gets you round to the back of it.
    private string WorkPrompt
    {
        get
        {
            if (_target.Style == InspectStyle.Turn && !_showingBack)
                return "[E] พลิกดูด้านหลัง";

            if (_work != null)
                return _work.Prompt;

            return _target.Style == InspectStyle.Turn ? "[E] พลิกกลับ" : null;
        }
    }

    private void Awake()
    {
        _overlayLayer = LayerMask.NameToLayer(overlayLayer);
    }

    private void OnEnable()
    {
        var map = input.Controls.Inspect;
        map.Cancel.performed += OnCancel;
        map.Interact.performed += OnInteract;
        map.Inspect.performed += OnRaiseGlass;
    }

    private void LateUpdate()
    {
        if (Zooming)
        {
            Transform pivot = eyes.transform.parent;
            Vector3 goal = _returning ? pivot.TransformPoint(_cameraHome) : _cameraGoal;
            Quaternion facing = _returning ? pivot.rotation * _cameraHomeRotation : _cameraGoalRotation;

            float z = zoomSpeed * Time.deltaTime;
            eyes.transform.SetPositionAndRotation(
                Vector3.Lerp(eyes.transform.position, goal, z),
                Quaternion.Slerp(eyes.transform.rotation, facing, z));

            if (_returning && Vector3.Distance(eyes.transform.position, goal) < 0.01f)
                Park();

            return;
        }

        if (_borrowed == null)
            return;

        Transform prop = _borrowed.Prop;
        Quaternion goalRotation = _returning
            ? _borrowed.HomeRotation
            : anchor.rotation * (_showingBack ? _target.InspectBackRotation : _facing);

        // The offset turns with the prop, so the middle of it stays on the hold point
        // however it is facing.
        Vector3 goalPosition = _returning
            ? _borrowed.HomePosition
            : HoldPoint - goalRotation * _holdPivot;

        float t = followSpeed * Time.deltaTime;
        prop.SetPositionAndRotation(
            Vector3.Lerp(prop.position, goalPosition, t),
            Quaternion.Slerp(prop.rotation, goalRotation, t));

        // The prompt line belongs to the interactor, and the interactor is switched off
        // while something is being held, so say here what E would do next.
        interactor.Hud.ShowHandPrompt(_returning ? null : HeldPrompt);

        if (_returning && Vector3.Distance(prop.position, goalPosition) < 0.005f)
            Park();
    }

    private void OnDisable()
    {
        var map = input.Controls.Inspect;
        map.Cancel.performed -= OnCancel;
        map.Interact.performed -= OnInteract;
        map.Inspect.performed -= OnRaiseGlass;

        Park();   // never leave a prop stranded in the air, or the camera stranded in a wall
    }

    public void Show(IInspectable target)
    {
        // Whatever was up is nowhere near where it belongs, and reading its position now
        // would record that spot as home. Put the old one back first, or repeated F and Q
        // walk every prop across the room a step at a time.
        Park();

        _target = target;
        _showingBack = false;
        _returning = false;

        // Whatever the paper is drawn with, what it says is text. A prop with none is every
        // other prop in the house, which is why this asks rather than assumes.
        IReadable written = target.InspectModel.GetComponentInChildren<IReadable>();
        if (written != null)
            page.Show(written);

        if (target.Style == InspectStyle.Zoom)
        {
            AimCameraAt(target);
            modes.SwitchTo(PlayerMode.Zoomed);
            return;
        }

        _facing = target.InspectRotation;
        _holdDistance = HoldDistanceFor(target.InspectModel);
        _holdPivot = CentreOffsetOf(target.InspectModel);

        // What is round the back decides what the second press of E means.
        _work = target.InspectModel.GetComponentInChildren<IInteractable>();

        // Turn earns its E by having a back to reach: a winder is round there and nothing
        // else is. Hold has no such beat, so only things meant to be worked in the hand
        // answer — above all not a `PickupItem`, which pockets the prop while it is still up
        // at the player's eyes and leaves the view holding an object that is not there.
        if (target.Style != InspectStyle.Turn && !(_work is IInHandOnly))
            _work = null;
        _borrowed = new BorrowedProp(target.InspectModel, _overlayLayer, 1u << overlayRenderingLayer);
        modes.SwitchTo(PlayerMode.Inspecting);
    }

    // Distance follows size. Held at one fixed arm's length a pocket watch is a dot and a
    // wall clock spills off the edges of the screen; scaling to the prop makes a watch, a
    // sheet of paper and a two-foot clock all readable without authoring a number on each.
    private float HoldDistanceFor(Transform prop)
    {
        return Mathf.Clamp(Size(prop) * holdCloseness, nearestHold, farthestHold);
    }

    // Where the hold point sits relative to the prop, so it is the middle of the thing that
    // comes up to the eyes rather than its origin. A wall clock hangs from a point at the
    // back of its case and would otherwise arrive with its head off the top of the screen.
    private static Vector3 CentreOffsetOf(Transform prop)
    {
        Vector3 local = prop.InverseTransformPoint(BoundsOf(prop).center);
        return Vector3.Scale(local, prop.lossyScale);
    }

    private static float Size(Transform prop)
    {
        Bounds bounds = BoundsOf(prop);
        return Mathf.Max(bounds.extents.x, bounds.extents.y);
    }

    private static Bounds BoundsOf(Transform prop)
    {
        var bounds = new Bounds(prop.position, Vector3.one * 0.02f);
        foreach (Renderer r in prop.GetComponentsInChildren<Renderer>())
            bounds.Encapsulate(r.bounds);

        return bounds;
    }

    // Where to stand to see something, given how big it is and where the player already is.
    //
    // It closes in along the line the player is already looking down and **never backs
    // away**. Walking right up to a clock and then pressing F used to shove the camera to a
    // fixed point further out — behind the player's own shoulders, so the clock appeared to
    // be standing behind them.
    private void AimCameraAt(IInspectable target)
    {
        _cameraHome = eyes.transform.localPosition;
        _cameraHomeRotation = eyes.transform.localRotation;

        if (target.Viewpoint != null)
        {
            _cameraGoal = target.Viewpoint.position;
            _cameraGoalRotation = target.Viewpoint.rotation;
            return;
        }

        Transform prop = target.InspectModel;
        Transform focus = target.Focus != null ? target.Focus : prop;

        var bounds = new Bounds(focus.position, Vector3.one * 0.1f);
        foreach (Renderer r in focus.GetComponentsInChildren<Renderer>())
            bounds.Encapsulate(r.bounds);

        float framing = Mathf.Max(bounds.extents.x, bounds.extents.y) * zoomBackOff + 0.30f;
        Vector3 back = eyes.transform.position - bounds.center;

        // Standing inside the thing, there is no line to close along; fall back to its face.
        if (back.sqrMagnitude < 0.0025f)
            back = prop.forward;

        _cameraGoal = bounds.center + back.normalized * Mathf.Min(framing, back.magnitude);
        _cameraGoalRotation = Quaternion.LookRotation(bounds.center - _cameraGoal, Vector3.up);
    }

    // E in two beats: turn it over, then work whatever is round the back.
    //
    // A clock arrives face on, because that is how you recognise which clock it is. The
    // first press turns it round. Only then is the winder facing the player, and only then
    // does E reach it. On something with nothing to work, E just keeps turning it over.
    //
    // Zoomed in, none of this applies: the crosshair is still live and the interactor
    // answers as usual.
    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (_target == null || _returning)
            return;

        if (Zooming)
        {
            interactor.PressInteract();
            return;
        }

        // Turn's first beat: the prop comes round before anything on its back is in reach.
        if (_target.Style == InspectStyle.Turn && !_showingBack)
        {
            _showingBack = true;
            return;
        }

        if (_work == null)
        {
            if (_target.Style == InspectStyle.Turn)
                _showingBack = false;

            return;
        }

        _work.Interact(interactor);

        // Both on the one press. A test where the mechanism and the glass are started a
        // fumble apart is a test that measures the fumble.
        if (glass.Raised)
            glass.Turn();
    }

    // F, with something already up: the glass comes up beside it. Q puts both away.
    private void OnRaiseGlass(InputAction.CallbackContext ctx)
    {
        if (_target == null || _returning || Zooming || glass.Raised || !glass.Carried)
            return;

        // The prompt line already says so, and the two have to agree: a press that raises a
        // glass nobody was offered is how a player learns to distrust the prompt.
        if (!(_work is ITimedTest))
            return;

        glass.Raise();
    }

    // Q hands control back at once, but the prop — or the view — takes its own time getting
    // home. Park finishes the job when it arrives.
    private void OnCancel(InputAction.CallbackContext ctx)
    {
        // Q ends a test, and ends it on this frame rather than when the prop lands: the
        // ticking would otherwise follow the watch back to the wall and go on into the next
        // room. Whether the thing being put down has a run to end is its own business, asked
        // through the same interface that offered it the glass in the first place.
        if (_work is ITimedTest test)
            test.Abandon();

        _returning = true;
        modes.SwitchTo(PlayerMode.Exploring);
    }

    private void Park()
    {
        interactor.Hud.ShowPrompt(null);
        page.Hide();
        glass.Lower();

        if (Zooming)
        {
            eyes.transform.localPosition = _cameraHome;
            eyes.transform.localRotation = _cameraHomeRotation;
        }
        else if (_borrowed != null)
        {
            _borrowed.Return();
        }

        _borrowed = null;
        _target = null;
        _work = null;
        _showingBack = false;
        _returning = false;
    }
}
