using UnityEngine;

// The house, written down as data.
//
// It was five rectangles and a list of wall runs living as C# arrays inside `HouseBuilder`,
// which meant moving a room was a code change and a designer could not do it at all. The
// numbers are the same numbers; they have only stopped being source.
//
// The plan is deliberately not clever. There is no room graph, no doorway solver, no
// autolayout — a wall run is a straight line with holes in it, and the holes are placed by
// hand, because a five-room house is small enough that a person laying it out by eye beats
// anything that has to be configured.
[CreateAssetMenu(menuName = "Escape Room/House Plan", fileName = "HousePlan")]
public class HousePlan : ScriptableObject
{
    // A hole in a wall. A doorway reaches the floor; a window leaves a sill under it, and
    // gets glass and a painted night behind it.
    public enum Hole
    {
        Door,
        Window
    }

    [System.Serializable]
    public struct Opening
    {
        public Hole Kind;

        [Tooltip("Along the run: an x for a wall that runs east-west, a z for one that runs north-south.")]
        public float Centre;
    }

    [System.Serializable]
    public struct Room
    {
        public string Name;
        public float X0, Z0, X1, Z1;

        [Tooltip("The floor's own shade. Every room having its own is the cheapest way to " +
                 "tell the player they have crossed into somewhere new.")]
        public Color Floor;

        [Tooltip("Boards, or carpet.")]
        public bool Boards;

        [Min(0.1f)]
        [Tooltip("Metres per tile of the floor map.")]
        public float FloorTile;

        [Tooltip("No ceiling slab. For a room that is roofed by something else — a dome, a " +
                 "skylight — and would otherwise be capped in plaster first.")]
        public bool Roofless;
    }

    [System.Serializable]
    public struct Line
    {
        [Tooltip("Runs east-west. Off means it runs north-south.")]
        public bool AlongX;

        [Tooltip("The coordinate the run sits at: a z for an east-west wall, an x for the other.")]
        public float Fixed;

        public float From, To;

        [Tooltip("Which way is out of doors: -1, 0 for an internal wall, or +1. Get it wrong " +
                 "and the painted night sky lands in the middle of a room.")]
        public float Outward;

        public Opening[] Openings;
    }

    [SerializeField] private Room[] rooms;
    [SerializeField] private Line[] walls;

    [Header("Sizes every opening shares")]
    [SerializeField, Min(0.1f)] private float doorWidth = 1.10f;
    [SerializeField, Min(0.1f)] private float doorTop = 2.15f;
    [SerializeField, Min(0.1f)] private float windowWidth = 1.30f;
    [SerializeField, Min(0f)] private float windowSill = 0.95f;
    [SerializeField, Min(0.1f)] private float windowHead = 2.10f;

    public Room[] Rooms => rooms;
    public Line[] Walls => walls;

    public float DoorWidth => doorWidth;
    public float DoorTop => doorTop;
    public float WindowWidth => windowWidth;
    public float WindowSill => windowSill;
    public float WindowHead => windowHead;

    // Every opening of a kind is the same size, so the size lives here rather than on each
    // hole. A window that had to be its own width would be a different kind of window, and
    // this house does not have one.
    public float WidthOf(Opening opening) => opening.Kind == Hole.Door ? doorWidth : windowWidth;
    public float BottomOf(Opening opening) => opening.Kind == Hole.Door ? 0f : windowSill;
    public float TopOf(Opening opening) => opening.Kind == Hole.Door ? doorTop : windowHead;

    public float LeftOf(Opening opening) => opening.Centre - WidthOf(opening) * 0.5f;
    public float RightOf(Opening opening) => opening.Centre + WidthOf(opening) * 0.5f;

    // Only a window gets glass and a painted night behind it.
    public bool IsGlazed(Opening opening) => opening.Kind == Hole.Window;
}
