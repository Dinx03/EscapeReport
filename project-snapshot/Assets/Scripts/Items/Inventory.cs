using System.Collections.Generic;
using UnityEngine;

// What the player is carrying. The bag is small enough that the UI rebuilds itself every
// time it opens, so there is nothing here to subscribe to.
[DisallowMultipleComponent]
public class Inventory : MonoBehaviour
{
    private readonly List<ItemData> _items = new List<ItemData>();

    public IReadOnlyList<ItemData> Items => _items;

    // Escape room items are one-of-a-kind, so a second copy is a level design mistake
    // rather than something to stack.
    public void Add(ItemData item)
    {
        if (_items.Contains(item))
            return;

        _items.Add(item);
    }

    public void Remove(ItemData item)
    {
        _items.Remove(item);
    }
}
