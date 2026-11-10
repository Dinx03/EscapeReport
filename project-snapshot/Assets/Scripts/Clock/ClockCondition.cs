// What a given clock's movement is doing. The house is full of clocks and most of them
// are wrong in some particular way, so this is the first thing to set when placing one.
public enum ClockCondition
{
    Running,    // keeps good time
    Slow,       // loses time, at the rate set on the movement
    Reversed,   // runs backwards
    Stopped,    // wound down, but a turn of the key starts it
    Broken      // nothing will start it, no matter how long you crank
}
