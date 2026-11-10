using UnityEngine;

// A number inked on a ticket and stuck to the back of an ordinary thing, the way a dealer
// marks his stock. It knows its own digit and shows it, and nothing whatever about what the
// digit is for.
//
// Which things are worth numbering, and what order they are read in, is StampedCombination's
// business — the same split as CodeWheel and CodeLock one room earlier, where the wheel
// knows its number and not whether the number is right.
[DisallowMultipleComponent]
public class StampedNumber : MonoBehaviour
{
    private const int DigitsOnStrip = 10;

    [SerializeField, Range(0, 9)]
    private int digit;

    [SerializeField]
    [Tooltip("The ticket the number is printed on. Same strip of digits a winder's number " +
             "reel runs, so every figure in the house is written in the one hand.")]
    private Renderer ticket;

    public int Digit => digit;

    private void Awake()
    {
        Draw();
    }

    // Kept in step in the Editor as well. A digit typed into this field and a different one
    // printed on the paper is the one way this puzzle can lie to the player, and nobody
    // would find out until they had tried the wrong number on the wardrobe.
    //
    // The ticket really is missing for a moment — OnValidate runs the instant the component
    // is added, before anything has been wired to it.
    private void OnValidate()
    {
        if (ticket != null)
            Draw();
    }

    private void Draw()
    {
        var block = new MaterialPropertyBlock();
        ticket.GetPropertyBlock(block);
        block.SetVector("_BaseMap_ST", new Vector4(1f, 1f / DigitsOnStrip, 0f, digit / (float)DigitsOnStrip));
        ticket.SetPropertyBlock(block);
    }
}
