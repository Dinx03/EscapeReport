using UnityEngine;

// A second implementation of `IClockDisplay`, and the reason the interface exists.
//
// It shows the same minutes as a `ClockDial` and shares not one line with it: four digits
// scrolling a printed strip past four windows, instead of two hands turning. Neither knows
// the other exists, and `ClockMovement` was not touched to accept it — it gathers every
// display bolted to it and calls `Show` on all of them.
//
// Put one of these on the same movement as a dial and both read the same time, because both
// are being told the same number. That is the whole claim of the architecture, standing on
// a bench where it can be looked at.
[DisallowMultipleComponent]
public class DigitalReadout : MonoBehaviour, IClockDisplay
{
    private const int DigitsOnStrip = 10;
    private const int Blank = -1;

    // Not a digit and not a blank: what every window holds before the first time anything is
    // shown. Starting them at Blank instead made the first Show for a blank window a no-op,
    // and the leading zero the strip happens to rest on stayed on the glass.
    private const int Unset = -2;

    [SerializeField]
    [Tooltip("Four quads showing a digit strip: tens of hours, hours, tens of minutes, " +
             "minutes. Each is scrolled by its own material offset.")]
    private Renderer[] digits;

    [SerializeField]
    [Tooltip("Twelve-hour, the way every other face in this house reads. Off gives 0 to 23.")]
    private bool twelveHour = true;

    private Material[] _faces;
    private readonly int[] _showing = { Unset, Unset, Unset, Unset };

    private void Awake()
    {
        // An instanced material each, or scrolling one digit scrolls all four.
        _faces = new Material[digits.Length];
        for (int i = 0; i < digits.Length; i++)
            _faces[i] = digits[i].material;
    }

    public void Show(float minutes)
    {
        int hour = Mathf.FloorToInt(minutes / 60f) % 12;
        if (twelveHour && hour == 0)
            hour = 12;

        int minute = Mathf.FloorToInt(Mathf.Repeat(minutes, 60f));

        // A leading zero on an hour is what a station board does and a clock never does.
        SetDigit(0, hour >= 10 ? hour / 10 : Blank);
        SetDigit(1, hour % 10);
        SetDigit(2, minute / 10);
        SetDigit(3, minute % 10);
    }

    // Only when it changes: this runs every frame on every readout in the scene, and a
    // material write that lands on the same value is a write nobody asked for.
    private void SetDigit(int place, int value)
    {
        if (_showing[place] == value)
            return;

        _showing[place] = value;
        _faces[place].SetTextureOffset("_BaseMap", new Vector2(0f, value < 0 ? 0f : value / (float)DigitsOnStrip));
        _faces[place].SetTextureOffset("_EmissionMap", new Vector2(0f, value < 0 ? 0f : value / (float)DigitsOnStrip));

        // A blank is a dark window, not a nought. Hiding the renderer is cheaper and more
        // honest than printing a blank frame onto the strip.
        digits[place].enabled = value >= 0;
    }
}
