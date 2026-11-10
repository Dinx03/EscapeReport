using UnityEngine;

// A place on a clock where a missing part goes.
//
// Use the right part on it and the part switches on — from that moment the movement finds
// it and asks it like any other. Until then the socket is empty, the part is not there, and
// the clock does not run. Fitting a clock together is the same act as bolting an IClockPart
// to it; this only puts that act in the player's hands.
[DisallowMultipleComponent]
public class ClockPartSocket : MonoBehaviour, IInteractable, IItemReceiver
{
    [SerializeField]
    private ItemData part;

    [SerializeField]
    [Tooltip("Switched on once the part is fitted. Put the IClockPart itself in here, along " +
             "with whatever should become visible with it.")]
    private GameObject[] fitted;

    [SerializeField] private string emptyPrompt = "[E] ยังขาดชิ้นส่วน — ลองใช้ของ";
    [SerializeField] private string fittedPrompt = "[E] ใส่แล้ว";

    private bool _fitted;

    public bool IsFitted => _fitted;
    public string Prompt => _fitted ? fittedPrompt : emptyPrompt;

    private void Awake()
    {
        // Whatever goes here starts absent, whatever the prefab was saved with.
        foreach (GameObject piece in fitted)
            piece.SetActive(false);
    }

    public void Interact(IActor actor)
    {
        if (_fitted)
            return;

        actor.OfferBag(this);
    }

    public bool TryUseItem(ItemData item)
    {
        if (_fitted || item != part)
            return false;

        _fitted = true;
        foreach (GameObject piece in fitted)
            piece.SetActive(true);

        return true;
    }
}
