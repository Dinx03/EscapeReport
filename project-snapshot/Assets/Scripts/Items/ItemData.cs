using UnityEngine;

[CreateAssetMenu(menuName = "Escape Room/Item", fileName = "Item")]
public class ItemData : ScriptableObject
{
    [SerializeField]
    private string displayName = "ของ";

    [SerializeField]
    [Tooltip("Off for something carried and worked in the hand rather than given to " +
             "anything. The bag is only ever opened because a lock or a socket asked what " +
             "you want to use on it, and an item that answers nothing has no business in " +
             "that list — it is one more wrong choice to scroll past on every door.")]
    private bool usedOnThings = true;

    public string DisplayName => displayName;
    public bool UsedOnThings => usedOnThings;
}
