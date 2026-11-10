// A part bolted to a movement that has a say in whether it runs.
//
// The movement asks every part on it before it moves a hand, the same way an Openable asks
// every ILock. Take the spring off a clock and nothing runs it down; seize its pendulum and
// it stops. A movement with no parts at all is a dial showing a time and nothing more,
// which is exactly what a clock face nailed to a wall should be.
public interface IClockPart
{
    // Will this part let the movement run right now.
    bool Allows { get; }

    // And how fast it lets it go. 1 is true time; a part that only gates returns 1.
    float RateScale { get; }

    // Told once, at the start, whether this clock was found going. A spring that was
    // running has something left in it; one that was not is empty.
    void Prime(bool foundRunning);

    // The movement reports what it used, so anything with a reserve can run down.
    void Draw(float minutes);
}
