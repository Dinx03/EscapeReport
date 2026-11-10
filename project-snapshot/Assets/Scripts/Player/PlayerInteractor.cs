using UnityEngine;
using UnityEngine.InputSystem;

// Watches whatever is under the crosshair and hands E and F to it. It never asks
// what kind of thing it found, so new props need no changes here.
//
// It is also this game's `IActor`: the three things a prop is allowed to ask of whoever is
// working it. A prop is handed the interface, never this class, which is what keeps the
// clock and puzzle modules from knowing a player exists at all.
[DisallowMultipleComponent]
[RequireComponent(typeof(InputReader))]
[RequireComponent(typeof(Inventory))]
public class PlayerInteractor : MonoBehaviour, IActor
{
    [Header("Reach")]
    [SerializeField] private Camera eyes;

    [SerializeField, Min(0f)]
    private float reach = 3f;

    [SerializeField, Min(0f)]
    [Tooltip("A fat ray rather than a thin one, so small props are easier to aim at than they deserve.")]
    private float aimAssist = 0.05f;

    [SerializeField]
    [Tooltip("Everything solid, not just interactables. Walls have to stop the ray or " +
             "you can take a key out of a closed drawer from across the room.")]
    private LayerMask blockingLayers;

    [Header("Links")]
    [SerializeField] private PlayerHud hud;
    [SerializeField] private InspectView inspectView;
    [SerializeField] private InventoryUI inventoryUI;

    private InputReader _input;
    private IInteractable _interactable;
    private IInspectable _inspectable;

    public Inventory Inventory { get; private set; }
    public PlayerHud Hud => hud;

    private void Awake()
    {
        _input = GetComponent<InputReader>();
        Inventory = GetComponent<Inventory>();
    }

    private void OnEnable()
    {
        var map = _input.Controls.Player;
        map.Interact.performed += OnInteract;
        map.Inspect.performed += OnInspect;
    }

    private void Update()
    {
        Ray ray = new Ray(eyes.transform.position, eyes.transform.forward);

        // Nearest surface wins, then we ask whether it happens to be interesting.
        // A wall that answers "nothing here" is the point, not a wasted cast.
        if (Physics.SphereCast(ray, aimAssist, out RaycastHit hit, reach, blockingLayers, QueryTriggerInteraction.Ignore))
        {
            _interactable = hit.collider.GetComponentInParent<IInteractable>();
            _inspectable = hit.collider.GetComponentInParent<IInspectable>();

            // Some things are only worked once they are in your hands. The crosshair does
            // not offer those, so F comes first and InspectView hands them the press.
            if (_interactable is IInHandOnly)
                _interactable = null;
        }
        else
        {
            _interactable = null;
            _inspectable = null;
        }

        hud.ShowPrompt(BuildPrompt());
    }

    private void OnDisable()
    {
        var map = _input.Controls.Player;
        map.Interact.performed -= OnInteract;
        map.Inspect.performed -= OnInspect;

        // The crosshair is stale the moment we stop looking for targets.
        _interactable = null;
        _inspectable = null;
        hud.ShowPrompt(null);
    }

    // Lets the inspect view press E on whatever the crosshair has found without handing the
    // player back the whole exploring map.
    public void PressInteract()
    {
        if (_interactable != null)
            _interactable.Interact(this);
    }

    // ---------- IActor ----------
    //
    // Three one-line forwards. That they are this thin is the point: everything a prop
    // needs from a player fits here, and the rest of this class is nobody else's business.

    public void Say(string message) => hud.ShowMessage(message);

    public void OfferBag(IItemReceiver to) => inventoryUI.Open(to);

    public void Take(ItemData item) => Inventory.Add(item);

    private string BuildPrompt()
    {
        if (_interactable != null && _inspectable != null)
            return _interactable.Prompt + "    [F] ดูใกล้ๆ";

        if (_interactable != null)
            return _interactable.Prompt;

        if (_inspectable != null)
            return "[F] ดูใกล้ๆ";

        return null;
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        _interactable?.Interact(this);
    }

    private void OnInspect(InputAction.CallbackContext ctx)
    {
        if (_inspectable != null)
            inspectView.Show(_inspectable);
    }
}
