// Anything that can show a time.
//
// This is the seam the whole project is built to demonstrate. A `ClockMovement` keeps a
// number of minutes and hands it to every display bolted to it; it has never been told what
// a display *is*. A pair of hands on a dial is one. A digital readout is another. A row of
// flaps, a shadow on a gnomon, a voice that says the hour out loud — each is a new class
// implementing this, and not one line of the movement changes to accept it.
//
// The consequence is the thing worth saying out loud: **a display attached to an object
// turns that object into a clock**. Whether it *goes* is a separate question, answered by
// the `IClockPart`s attached beside it.
public interface IClockDisplay
{
    // Minutes past twelve, 0 to 720. The fractional part carries the seconds.
    //
    // A display is handed the time and nothing else. It is not told whether that time is
    // running forwards, backwards, slowly or not at all, because a real dial cannot tell
    // either — and a display that cannot tell is a display that works on any clock.
    void Show(float minutes);
}
