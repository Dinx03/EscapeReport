using UnityEngine;

// Marks a prop as something F can pull up close, and says how. Sits alongside whatever else
// the prop does, so a note can be both readable and pocketable without a special class.
//
// Everything that makes one prop present differently from another is InspectStyle on this
// component — the same bargain ClockCondition makes for clocks. A new way of looking at
// something should mean a new case in that enum, not a new class here.
[DisallowMultipleComponent]
public class Inspectable : MonoBehaviour, IInspectable
{
    [SerializeField]
    private InspectStyle style = InspectStyle.Hold;

    [Header("Hold and Turn")]
    [SerializeField]
    [Tooltip("Leave empty to lift this whole object. Set it to lift only a part.")]
    private Transform model;

    [SerializeField]
    [Tooltip("How the prop is turned once it is up close. The anchor faces the same way " +
             "the camera does, so a prop whose front is its local +Z needs 180 here — " +
             "leave this at zero and it arrives showing its back.")]
    private Vector3 facing = new Vector3(0f, 180f, 0f);

    [SerializeField]
    [Tooltip("Turn style only: the other side, reached by pressing F again.")]
    private Vector3 backFacing;

    [Header("Zoom")]
    [SerializeField]
    [Tooltip("The part worth looking at — the dial, the window with the number. Leave empty " +
             "to frame the whole prop.")]
    private Transform focus;

    [SerializeField]
    [Tooltip("Where the view stands. Leave empty and one is worked out from the size of " +
             "the prop and the way it faces.")]
    private Transform viewpoint;

    public InspectStyle Style => style;
    public Transform InspectModel => model != null ? model : transform;
    public Quaternion InspectRotation => Quaternion.Euler(facing);
    public Quaternion InspectBackRotation => Quaternion.Euler(backFacing);
    public Transform Focus => focus;
    public Transform Viewpoint => viewpoint;
}
