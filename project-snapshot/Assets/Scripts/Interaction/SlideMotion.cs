using UnityEngine;

// Slides straight out to follow an Openable. Drawers, and anything else on runners.
[DisallowMultipleComponent]
[RequireComponent(typeof(Openable))]
public class SlideMotion : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Which way it travels and how far, in its own local space. A hatch that " +
             "lifts is the same component with this pointing up.")]
    private Vector3 openOffset = new Vector3(0f, 0f, 0.35f);

    [SerializeField, Min(0.01f)]
    private float slideSpeed = 1.2f;   // metres per second

    private Openable _openable;
    private Vector3 _shutPosition;

    private void Awake()
    {
        _openable = GetComponent<Openable>();
        _shutPosition = transform.localPosition;
    }

    private void Update()
    {
        Vector3 target = _openable.IsOpen ? _shutPosition + openOffset : _shutPosition;

        transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, slideSpeed * Time.deltaTime);
    }
}
