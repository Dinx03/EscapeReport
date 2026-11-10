using UnityEngine;

// The bird behind the little door. It rides out while the door is open and goes back in when
// it shuts, which is the whole of what a cuckoo does.
//
// It follows a door on another object, which is why it is not `SlideMotion`: that moves the
// thing it is bolted to according to the catch bolted beside it, and a bird bolted to the
// door would swing aside with the door instead of coming out through it. Same three lines of
// arithmetic, a different thing to read them off.
[DisallowMultipleComponent]
public class CuckooBird : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The little door. The bird is out for exactly as long as it is open.")]
    private Openable door;

    [SerializeField]
    [Tooltip("How far out it rides, in its own local space. Straight forward, out of the case.")]
    private Vector3 outOffset = new Vector3(0f, 0f, 0.052f);

    [SerializeField, Min(0.01f)]
    [Tooltip("Metres a second. Fast enough to be all the way out before the door shuts again.")]
    private float speed = 0.32f;

    private Vector3 _inside;

    private void Awake()
    {
        _inside = transform.localPosition;
    }

    private void Update()
    {
        Vector3 target = door.IsOpen ? _inside + outOffset : _inside;

        transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * Time.deltaTime);
    }

    // Where it started is state, and a bird left hanging out of its hole is what a stale
    // position looks like on this prop.
    private void OnDisable()
    {
        transform.localPosition = _inside;
    }
}
