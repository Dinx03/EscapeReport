using UnityEngine;

// The fourth hand on an alarm clock. It points at the time the bell is set for and stays
// there while the others move, which makes it the one hand worth reading on a shelf full
// of clocks that all show the same hour.
//
// It knows nothing about the movement. A clock can be stopped, wound down or plain broken
// and still be set for six.
[DisallowMultipleComponent]
public class AlarmHand : MonoBehaviour
{
    private const float MinutesOnDial = 12f * 60f;

    [SerializeField] private Transform hand;

    [SerializeField, Range(0, 11)] private int setForHour = 6;
    [SerializeField, Range(0, 59)] private int setForMinute;

    [SerializeField]
    [Tooltip("The lever that shows the bell is live. It is meant to be readable from across " +
             "the room, because whether an alarm counts is the first thing to sort out.")]
    private Transform armedLever;

    [SerializeField] private bool armed = true;

    [SerializeField]
    [Tooltip("How far the lever throws when the alarm is set.")]
    private float armedAngle = -38f;

    public bool Armed => armed;
    public int Hour => setForHour;
    public int Minute => setForMinute;

    private void Awake()
    {
        float minutes = setForHour % 12 * 60f + setForMinute;

        // Positive, like every other hand in the project: the face looks down +Z and the
        // player stands on that side.
        hand.localRotation = Quaternion.Euler(0f, 0f, minutes / MinutesOnDial * 360f);
        armedLever.localRotation = Quaternion.Euler(0f, 0f, armed ? armedAngle : 0f);
    }
}
