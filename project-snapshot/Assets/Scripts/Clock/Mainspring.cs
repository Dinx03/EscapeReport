using System;
using UnityEngine;

// The wound power in a clock: one implementation of `IPowerStore`, and one `IClockPart`
// among however many are bolted on beside it.
//
// It is a component rather than a field on the movement because plenty of clocks have no
// store at all — a dial screwed to a wall runs on nothing. Those simply do not have one of
// these, and never wind down. That is the difference between a clock that can stop and a
// clock that cannot, expressed by adding a component rather than by setting a flag.
[DisallowMultipleComponent]
public class Mainspring : MonoBehaviour, IClockPart, IPowerStore
{
    [SerializeField, Min(0f)]
    [Tooltip("Clock minutes gained each time it is filled.")]
    private float turnsWorth = 90f;

    [SerializeField, Min(0f)]
    private float capacity = 720f;

    private float _left;

    // Anything that fills this says so, and a striker listens. What the player actually
    // turned or pulled to do it is none of the striker's business.
    public event Action Filled;

    public bool Allows => _left > 0f && isActiveAndEnabled;
    public float RateScale => 1f;
    public float Charge => capacity > 0f ? _left / capacity : 0f;

    public void Prime(bool foundRunning)
    {
        // Only the clocks that were still going when the player arrived have anything left
        // in them. The rest are found dead, however healthy their movement is.
        _left = foundRunning ? capacity : 0f;
    }

    public void Draw(float minutes)
    {
        _left = Mathf.Max(0f, _left - minutes);
    }

    public void Fill()
    {
        _left = Mathf.Min(_left + turnsWorth, capacity);
        Filled?.Invoke();
    }
}
