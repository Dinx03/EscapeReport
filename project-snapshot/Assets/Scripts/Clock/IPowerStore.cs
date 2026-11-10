using System;

// Where a clock keeps the energy that drives it.
//
// Three separate jobs meet here and none of them needs to know the others:
//
//   filling    a crank, a pulled chain, a key, a hand on a lever
//   storing    a wound spring, a raised weight, a cell
//   drawing    the movement, taking a little for every minute it turns
//
// It is named for the role and not for the mechanism on purpose. Called `IMainspring` it
// would be a spring wearing an interface; called this, a weight line and a battery are as
// welcome as a spring, and `ClockStriker` can listen for "this was filled" without ever
// learning what filled it — which is exactly how the cuckoos traded their cranks for chains
// without the striker noticing.
public interface IPowerStore
{
    // Raised when the store is filled, by whatever filled it.
    event Action Filled;

    // 0 to 1. Anything that wants to show how much is left reads this: the height of a
    // weight on its chain, a power-reserve hand, a gauge.
    float Charge { get; }

    void Fill();
}
