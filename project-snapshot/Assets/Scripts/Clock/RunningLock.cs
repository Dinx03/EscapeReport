using UnityEngine;

// Holds an Openable shut until a particular clock is going.
//
// Not until it shows a time — TimeLock does that — but until it is actually running, which
// means every part it needs is fitted and has something left in it. Sits alongside KeyLock,
// CodeLock and TimeLock and asks nothing new of Openable.
[DisallowMultipleComponent]
public class RunningLock : MonoBehaviour, ILock
{
    [SerializeField] private ClockMovement movement;

    [SerializeField] private string lockedPrompt = "[E] ไม่ขยับ — นาฬิกายังไม่เดิน";
    [SerializeField] private string refusedMessage = "ผนังนิ่งสนิท ตราบใดที่นาฬิกายังไม่เดิน";

    private bool _sprung;

    public bool IsLocked
    {
        get
        {
            // Checked when asked, and latching. Once the clock has run the catch is off for
            // good, so a spring winding down later does not shut the player back in.
            if (!_sprung && movement.IsRunning)
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
