using UnityEngine;

// Anything the crosshair can rest on. Whoever holds the crosshair never asks what kind of
// thing it found — it calls Interact and lets the object decide what that means. Adding a
// new kind of prop means writing one of these, not touching the player.
//
// The parameter is `IActor` and not the player: a prop is told what it may ask of whoever
// is working it, and nothing else about them. That one word is what lets this module — and
// the clock and puzzle modules built on it — compile without a player existing at all.
public interface IInteractable
{
    string Prompt { get; }
    void Interact(IActor actor);
}

// Something that wants an item used on it: a lock, a socket, an empty frame.
// Returning false is what puts "ไม่มีอะไรเกิดขึ้น" on screen.
public interface IItemReceiver
{
    bool TryUseItem(ItemData item);
}

// Something F can pull up close for a proper look. Whether the prop travels to the eyes or
// the view travels to the prop is the prop's own business; the player never chooses.
public interface IInspectable
{
    InspectStyle Style { get; }

    // Hold and Turn: the part that leaves the world, and how it is turned once it is up.
    // "Face the player" means something different for a note on a wall than for a coin
    // lying on a table, so each prop states its own.
    Transform InspectModel { get; }
    Quaternion InspectRotation { get; }
    Quaternion InspectBackRotation { get; }

    // Zoom: what is actually worth looking at — a dial, a keyhole, a window with a number
    // in it. Left empty, the whole prop is framed.
    Transform Focus { get; }

    // Zoom: where the view should stand. Left empty, the view works one out from the size
    // of the thing.
    Transform Viewpoint { get; }
}

// An IInteractable that only answers once the prop is up in the player's hands. The
// crosshair passes over one of these where it stands — a movement is started on the bench,
// not by pointing at it across the room — so InspectView is the only thing that ever
// presses it, and F has to come before E.
public interface IInHandOnly
{
}

// A prop that is judged against a length of time, and so is worked with the sandglass in the
// other hand: F offers the glass beside it, and the E that starts the prop turns the glass on
// the same press.
//
// Every other prop in the house is held in one hand and wants nothing in the other. Without
// this the offer was made on all of them — a prism held up to be counted came with a line
// about raising an hourglass, which is an instruction to do something that means nothing here.
public interface ITimedTest
{
    // Put down part way through. Whoever was holding it says so on the way out, and what
    // "abandoned" means is the prop's own business — a run has to end somewhere, and the
    // hand that was holding it is the only thing that knows it has let go.
    void Abandon();
}

// Something with words on it. A page drawn into a texture is a picture of writing and
// nothing more — no font engine draws Thai into a Color[] — so what it says lives here as
// text, where it can be read out on screen, changed without redrawing the paper, and one
// day translated.
//
// Heading and body rather than one string: the heading is the part the prop itself shows,
// and the body is the part only the reader gets.
public interface IReadable
{
    string Heading { get; }
    string Body { get; }
}

// Something that can hold an Openable shut: a keyhole, a combination, a bolt on the far
// side. An Openable asks every lock fitted to it before it moves, so a case wanting both
// a key and a code needs no new class — just both components on the one object.
public interface ILock
{
    bool IsLocked { get; }

    // What the crosshair says while it is still shut.
    string LockedPrompt { get; }

    // What E does while it is still shut: offer the bag, or simply say no.
    void RefuseOpening(IActor actor);
}
