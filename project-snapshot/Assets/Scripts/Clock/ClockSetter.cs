using UnityEngine;

// Hands you can turn. E moves them on a few minutes at a time, the way you would nudge a
// clock that has drifted.
//
// It only ever moves them forwards. A clock face has no reverse gear you can find by feel,
// and letting the player overshoot and come round again is kinder than making them hunt
// for a second control.
[DisallowMultipleComponent]
public class ClockSetter : MonoBehaviour, IInteractable
{
    [SerializeField] private ClockMovement movement;

    [SerializeField, Min(1)]
    [Tooltip("Minutes per press. Five is a compromise: fine enough to hit any time on the " +
             "dial, coarse enough that setting one is not forty presses.")]
    private int minutesPerPress = 5;

    [SerializeField] private string prompt = "[E] หมุนเข็ม";

    public string Prompt => prompt;

    public void Interact(IActor actor)
    {
        movement.Advance(minutesPerPress);
    }
}
