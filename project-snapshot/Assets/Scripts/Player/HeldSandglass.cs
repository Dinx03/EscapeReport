using UnityEngine;

// The glass in the player's other hand, held up beside whatever they are timing.
//
// It is its own instance parked in front of the camera and switched off, not the prop out
// of the workshop: by the time anyone is timing anything, the workshop's glass is in the
// bag. Carrying it is what puts this one within reach, which is why the bag is the only
// thing this asks about.
[DisallowMultipleComponent]
public class HeldSandglass : MonoBehaviour
{
    [SerializeField] private Sandglass glass;
    [SerializeField] private ItemData item;
    [SerializeField] private Inventory bag;

    [SerializeField]
    [Tooltip("Layer drawn only by the overlay camera, as on InspectView.")]
    private string overlayLayer = "InspectedProp";

    [SerializeField, Range(0, 7)]
    private int overlayRenderingLayer = 1;

    public bool Carried
    {
        get
        {
            foreach (ItemData carried in bag.Items)
            {
                if (carried == item)
                    return true;
            }

            return false;
        }
    }

    public bool Raised => glass.gameObject.activeSelf;

    private void Awake()
    {
        // Put on the overlay layer here rather than in the scene: this copy is a prefab
        // instance, and a rebuilt sandglass brings its new parts in wearing the room's
        // layer. Setting it once a frame late is what stops half a cage rendering into the
        // room and the other half into the player's hand.
        int layer = LayerMask.NameToLayer(overlayLayer);

        foreach (Transform part in glass.GetComponentsInChildren<Transform>(true))
            part.gameObject.layer = layer;

        foreach (Renderer r in glass.GetComponentsInChildren<Renderer>(true))
            r.renderingLayerMask = 1u << overlayRenderingLayer;
    }

    private void Start()
    {
        Lower();
    }

    public void Raise()
    {
        glass.gameObject.SetActive(true);
    }

    public void Lower()
    {
        glass.gameObject.SetActive(false);
    }

    // Turned by whoever is running the test, at the same moment the movement is started.
    public void Turn()
    {
        glass.Turn();
    }
}
