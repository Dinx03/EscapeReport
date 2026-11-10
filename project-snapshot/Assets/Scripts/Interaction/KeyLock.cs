using UnityEngine;

// Holds an Openable shut until the right item is used on it. E offers the bag; the
// right key turns it for good, and a wrong one earns the usual "nothing happened".
//
// A door that was never locked simply has none of these on it.
[DisallowMultipleComponent]
public class KeyLock : MonoBehaviour, ILock, IItemReceiver
{
    [SerializeField]
    private ItemData requiredKey;

    [SerializeField]
    private string lockedPrompt = "[E] ล็อกอยู่ — ลองใช้ของ";

    private bool _turned;

    public bool IsLocked => !_turned;
    public string LockedPrompt => lockedPrompt;

    public void RefuseOpening(IActor actor)
    {
        actor.OfferBag(this);
    }

    public bool TryUseItem(ItemData item)
    {
        if (item != requiredKey)
            return false;

        _turned = true;
        return true;
    }
}
