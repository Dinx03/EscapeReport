using UnityEngine;

// A catch with nothing on this side to work it.
//
// Every other lock in the house is a question put to the player — a key, a code, a time, a
// pattern of stars — and answering it is what opens the thing. This one is never put to them
// at all. It is let go from the far side, by whatever machinery is fitted beside it, and no
// press will ever shift it.
//
// `Openable.SetOpen` moves a thing regardless of its locks, which is how that machinery
// springs a catch the player cannot reach. Once the thing has opened this stands down for
// good, so E behaves like any other door afterwards: someone who shuts a secret wall behind
// them is not sealed in by it.
[DisallowMultipleComponent]
[RequireComponent(typeof(Openable))]
public class SealedLock : MonoBehaviour, ILock
{
    [SerializeField]
    private string lockedPrompt = "[E] ไม่มีที่ให้จับ";

    [SerializeField]
    private string refusedMessage = "ผลักเท่าไรก็ไม่ขยับ";

    private Openable _openable;

    // Looked up on first use rather than in Awake, for the same reason Openable looks up its
    // own locks that way: the prompt is read in the editor too, where Awake has never run.
    private Openable Fitting => _openable ?? (_openable = GetComponent<Openable>());

    private bool _released;

    public bool IsLocked
    {
        get
        {
            // Read when asked and latching, the way RunningLock reads its movement. Once the
            // machinery has opened this, the catch is off and stays off.
            if (!_released && Fitting.IsOpen)
                _released = true;

            return !_released;
        }
    }

    public string LockedPrompt => lockedPrompt;

    public void RefuseOpening(IActor actor)
    {
        actor.Say(refusedMessage);
    }
}
