using UnityEngine;

// Turns on its hinge to follow an Openable. Doors, cupboard fronts, the lid of a case —
// anything hinged rather than sliding.
[DisallowMultipleComponent]
[RequireComponent(typeof(Openable))]
public class SwingMotion : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Negative to hang the hinge on the other side.")]
    private float openAngle = 95f;

    [SerializeField, Min(1f)]
    private float swingSpeed = 180f;   // degrees per second

    private Openable _openable;
    private float _shutYaw;

    private void Awake()
    {
        _openable = GetComponent<Openable>();
        _shutYaw = transform.localEulerAngles.y;
    }

    private void Update()
    {
        float target = _openable.IsOpen ? _shutYaw + openAngle : _shutYaw;
        float yaw = Mathf.MoveTowardsAngle(transform.localEulerAngles.y, target, swingSpeed * Time.deltaTime);

        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }
}
