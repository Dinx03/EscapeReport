using System;
using UnityEngine;

// Something with two states: shut and open.
//
// It holds which state it is in, asks any locks fitted to it for permission, and
// announces the change. It moves nothing — a SwingMotion or SlideMotion alongside it
// reads IsOpen and does the moving. Splitting it that way is what lets a drawer, a
// cupboard door and the glass lid of a case all be the same component.
[DisallowMultipleComponent]
public class Openable : MonoBehaviour, IInteractable
{
    [SerializeField]
    private string label = "ประตู";

    private ILock[] _locks;

    // Looked up on first use rather than in Awake. The prompt is read in the editor too,
    // where Awake has never run, and a lock list that only exists in play mode throws there.
    private ILock[] Locks => _locks ?? (_locks = GetComponents<ILock>());

    // The first lock still holding it shut, or null when it is free to move.
    private ILock BlockingLock
    {
        get
        {
            foreach (ILock fitted in Locks)
            {
                if (fitted.IsLocked)
                    return fitted;
            }

            return null;
        }
    }

    public event Action Opened;

    public bool IsOpen { get; private set; }

    public string Prompt
    {
        get
        {
            ILock blocking = BlockingLock;
            if (blocking != null)
                return blocking.LockedPrompt;

            return IsOpen ? $"[E] ปิด{label}" : $"[E] เปิด{label}";
        }
    }

    public void Interact(IActor actor)
    {
        ILock blocking = BlockingLock;

        if (blocking != null)
        {
            blocking.RefuseOpening(actor);
            return;
        }

        SetOpen(!IsOpen);
    }

    // Moves it regardless of any lock, which is how a lock springs its own catch once
    // the player has satisfied it. Anything the player does goes through Interact.
    public void SetOpen(bool open)
    {
        if (open == IsOpen)
            return;

        IsOpen = open;

        if (open)
            Opened?.Invoke();
    }
}
