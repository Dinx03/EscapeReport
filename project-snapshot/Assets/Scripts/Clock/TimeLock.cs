using UnityEngine;

// Holds an Openable shut until a particular clock reads a particular time.
//
// It sits alongside KeyLock and CodeLock and needs nothing new from Openable: a door that
// wants both a key and the right time is just both components on the same object.
[DisallowMultipleComponent]
public class TimeLock : MonoBehaviour, ILock
{
    [SerializeField] private ClockMovement movement;

    [Header("The time it is waiting for")]
    [SerializeField, Range(0, 11)] private int hour;
    [SerializeField, Range(0, 59)] private int minute;

    [SerializeField] private string lockedPrompt = "[E] ล็อกอยู่ — ตั้งเวลาให้ถูก";
    [SerializeField] private string refusedMessage = "ยังไม่ขยับ ลองตั้งเข็มให้ตรงเวลาที่ต้องการ";

    private bool _sprung;

    public bool IsLocked
    {
        get
        {
            // Checked when someone asks rather than every frame, so the catch does not
            // depend on a frame having run since the hands moved.
            //
            // It latches: once the clock has been right the catch stays off, or a movement
            // that is still going would lock the door again a minute after it was solved.
            if (!_sprung && movement.Hour == hour && movement.Minute == minute)
                _sprung = true;

            return !_sprung;
        }
    }

    public string LockedPrompt => lockedPrompt;

    public void RefuseOpening(IActor actor)
    {
        actor.Say(refusedMessage);
    }
}
