using UnityEngine;

// Builds and owns the one PlayerControls instance. Which map is live is decided by
// PlayerModeController, not here — this only creates the asset and reads from it.
//
// Runs early: several components subscribe to actions in their own OnEnable, and
// there is no other guarantee that Controls exists by then.
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class InputReader : MonoBehaviour
{
    public PlayerControls Controls { get; private set; }

    // A disabled action reads as default, so these fall to zero on their own
    // whenever the mode controller switches away from the Player map.
    public Vector2 Move => Controls.Player.Move.ReadValue<Vector2>();
    public Vector2 Look => Controls.Player.Look.ReadValue<Vector2>();

    private void Awake() => Controls = new PlayerControls();
    private void OnDestroy() => Controls.Dispose();
}
