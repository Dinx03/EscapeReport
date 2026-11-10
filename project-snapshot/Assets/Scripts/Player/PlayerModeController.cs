using UnityEngine;

public enum PlayerMode
{
    Exploring,
    Inspecting,
    Zoomed,          // standing still, looking closely at something too big to pick up
    InventoryOpen,
    Frozen          // the game is over and the player is a spectator
}

// The single owner of what the player can do right now: which action map is live,
// which components run, and where the cursor is. Nothing else should touch those
// three things, or they drift apart and the player ends up walking around with the
// inventory open.
[DisallowMultipleComponent]
[RequireComponent(typeof(InputReader))]
[RequireComponent(typeof(MouseLook))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInteractor))]
public class PlayerModeController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("What the player may do on the first frame. Frozen, when a title card is up.")]
    private PlayerMode startingMode = PlayerMode.Exploring;

    private InputReader _input;
    private MouseLook _look;
    private PlayerMovement _movement;
    private PlayerInteractor _interactor;

    private void Awake()
    {
        _input = GetComponent<InputReader>();
        _look = GetComponent<MouseLook>();
        _movement = GetComponent<PlayerMovement>();
        _interactor = GetComponent<PlayerInteractor>();
    }

    // Start rather than Awake, so InputReader has built the controls first.
    private void Start()
    {
        SwitchTo(startingMode);
    }

    // Give the mouse back when the player goes away, including on leaving play mode.
    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SwitchTo(PlayerMode mode)
    {
        // Clear every map before enabling one. Enabling without clearing is how two
        // maps end up live at once, which is the bug the maps exist to prevent.
        PlayerControls controls = _input.Controls;
        controls.Disable();

        switch (mode)
        {
            case PlayerMode.Exploring:
                controls.Player.Enable();
                break;
            case PlayerMode.Inspecting:
            case PlayerMode.Zoomed:
                controls.Inspect.Enable();
                break;
            case PlayerMode.InventoryOpen:
                controls.Inventory.Enable();
                break;
            case PlayerMode.Frozen:
                break;
        }

        bool walking = mode == PlayerMode.Exploring;
        _look.enabled = walking;
        _movement.enabled = walking;

        // Zoomed in, the crosshair stays live: the prompt still answers and E still reaches
        // whatever is under it. Only walking and looking are taken away.
        _interactor.enabled = walking || mode == PlayerMode.Zoomed;

        // Play is keyboard-driven throughout, so the cursor only comes back once the
        // game has handed control to a menu.
        bool playing = mode != PlayerMode.Frozen;
        Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !playing;
    }
}
