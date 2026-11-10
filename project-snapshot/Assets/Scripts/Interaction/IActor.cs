// Whoever is working this thing.
//
// Every prop in the house used to take a `PlayerInteractor`, so a clock, a drawer and a
// combination wheel were each compiled against this game's player: the whole class, with
// its reach, its aim assist, its inspect view and its walking speed. What they actually
// wanted from it was three verbs.
//
// Those three are here, and nothing else. A prop that talks to this can be worked by the
// player, by something that is not a player at all, or by a test that never opens a scene —
// and the clock module stops needing to know that a player exists.
//
// It lives beside `IInteractable` rather than with the player on purpose: `Clock` and
// `Puzzle` already depend on this module, so putting it here costs no new dependency.
// Put it with the player and the problem comes straight back under a different name.
public interface IActor
{
    // Say something to whoever is working this. Where it appears is their business.
    void Say(string message);

    // Offer the bag, so they can pick something to use on `to`. What comes back arrives
    // through `IItemReceiver.TryUseItem`, not from here.
    void OfferBag(IItemReceiver to);

    // Hand them something to carry away.
    void Take(ItemData item);
}
