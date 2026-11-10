using UnityEngine;

// Goes straight into the bag on E, no menu in between.
[DisallowMultipleComponent]
public class PickupItem : MonoBehaviour, IInteractable
{
    [SerializeField]
    private ItemData item;

    public string Prompt => $"[E] เก็บ{item.DisplayName}";

    public void Interact(IActor actor)
    {
        actor.Take(item);
        actor.Say($"เก็บ{item.DisplayName}แล้ว");
        gameObject.SetActive(false);
    }
}
