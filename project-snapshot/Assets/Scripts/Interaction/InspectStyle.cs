// How a prop wants to be looked at when F lands on it. Set it on the Inspectable, the same
// way a clock's behaviour is set by picking a ClockCondition.
public enum InspectStyle
{
    // Comes up to the eyes and stays still. Notes, tickets, coins — anything you read.
    Hold,

    // Comes up to the eyes and can be turned over with F, so the back can be looked at and
    // worked on. E still reaches whatever is on it while it is up.
    Turn,

    // Stays exactly where it is; the view goes to it instead. For anything nobody could
    // pick up — a longcase clock, a movement bolted to a wall.
    Zoom
}
