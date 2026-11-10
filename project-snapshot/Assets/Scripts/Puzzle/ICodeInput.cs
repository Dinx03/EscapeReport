using System;

// One place a player can put a digit.
//
// The same shape of seam as `ILock` and `IClockDisplay`: `CodeLock` reads a row of these
// and compares them with the combination, and has never been told what one looks like. A
// wheel that steps round behind a window is the implementation this house uses; a keypad, a
// row of push-buttons, a dial with a pointer or five books pulled part way off a shelf are
// each a new class here and no change at all to the lock.
public interface ICodeInput
{
    // Raised whenever the player changes it. The lock checks on every turn rather than
    // waiting for a submit button, so this is also the whole of the "try it" interaction.
    event Action Changed;

    int Digit { get; }
}
