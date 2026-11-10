using UnityEngine;

// The card the game opens on: whose house this is, and which key does what.
//
// It only ever hands control back. The player starts unable to move because
// PlayerModeController's own `startingMode` says Frozen, not because anything here reached
// in and switched MouseLook off — that remains the mode controller's job alone.
[DisallowMultipleComponent]
public class TitleScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private PlayerModeController modes;

    private void Awake()
    {
        panel.SetActive(true);
    }

    public void Begin()
    {
        panel.SetActive(false);
        modes.SwitchTo(PlayerMode.Exploring);
    }
}
