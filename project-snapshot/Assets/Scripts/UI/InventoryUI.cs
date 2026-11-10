using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The bag, opened against one particular thing in the world. It only ever appears
// because something asked "which item are you using on me?", so it always closes
// with an answer, even when the answer is that nothing happened.
[DisallowMultipleComponent]
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform listRoot;
    [SerializeField] private Text rowTemplate;
    [SerializeField] private Text emptyLabel;

    [Header("Links")]
    [SerializeField] private InputReader input;
    [SerializeField] private PlayerModeController modes;
    [SerializeField] private Inventory inventory;
    [SerializeField] private PlayerHud hud;

    private readonly List<Text> _rows = new List<Text>();

    // What is actually on the list, which is not everything in the bag: the sandglass is
    // carried and worked in the hand and is offered to nothing. Kept beside the rows rather
    // than read back out of the bag, because the two stopped lining up the moment anything
    // was left out of one of them.
    private readonly List<ItemData> _offered = new List<ItemData>();

    private IItemReceiver _receiver;
    private int _selected;

    private void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
        panel.SetActive(false);
    }

    private void OnEnable()
    {
        var map = input.Controls.Inventory;
        map.Navigate.performed += OnNavigate;
        map.Submit.performed += OnSubmit;
        map.Cancel.performed += OnCancel;
    }

    private void OnDisable()
    {
        var map = input.Controls.Inventory;
        map.Navigate.performed -= OnNavigate;
        map.Submit.performed -= OnSubmit;
        map.Cancel.performed -= OnCancel;
    }

    public void Open(IItemReceiver receiver)
    {
        _receiver = receiver;
        _selected = 0;

        Rebuild();
        panel.SetActive(true);
        modes.SwitchTo(PlayerMode.InventoryOpen);
    }

    public void Close()
    {
        panel.SetActive(false);
        _receiver = null;
        modes.SwitchTo(PlayerMode.Exploring);
    }

    private void Rebuild()
    {
        foreach (Text row in _rows)
            Destroy(row.gameObject);
        _rows.Clear();
        _offered.Clear();

        foreach (ItemData item in inventory.Items)
        {
            if (!item.UsedOnThings)
                continue;

            _offered.Add(item);

            Text row = Instantiate(rowTemplate, listRoot);
            row.text = item.DisplayName;
            row.gameObject.SetActive(true);
            _rows.Add(row);
        }

        emptyLabel.gameObject.SetActive(_rows.Count == 0);
        Highlight();
    }

    private void Highlight()
    {
        for (int i = 0; i < _rows.Count; i++)
            _rows[i].text = (i == _selected ? "> " : "   ") + _offered[i].DisplayName;
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (_rows.Count == 0)
            return;

        float y = ctx.ReadValue<Vector2>().y;
        if (Mathf.Abs(y) < 0.5f)
            return;

        _selected = (_selected + (y > 0f ? -1 : 1) + _rows.Count) % _rows.Count;
        Highlight();
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (_rows.Count == 0)
        {
            hud.ShowMessage("ไม่มีของในกระเป๋า");
            Close();
            return;
        }

        ItemData chosen = _offered[_selected];

        if (_receiver.TryUseItem(chosen))
        {
            inventory.Remove(chosen);
            hud.ShowMessage($"ใช้{chosen.DisplayName}แล้ว");
        }
        else
        {
            hud.ShowMessage("ไม่มีอะไรเกิดขึ้น");
        }

        Close();
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        Close();
    }
}
