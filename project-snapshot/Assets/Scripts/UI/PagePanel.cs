using UnityEngine;
using UnityEngine.UI;

// The panel that says what a page of writing says, for as long as the page is up.
//
// Its own job, and not PlayerHud's: the prompt line and the message are one line each and
// answer what you just did, while this is a block of text that stays put and answers what
// you are looking at.
[DisallowMultipleComponent]
public class PagePanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text headingLabel;
    [SerializeField] private Text bodyLabel;

    private void Start()
    {
        Hide();
    }

    public void Show(IReadable page)
    {
        headingLabel.text = page.Heading;
        bodyLabel.text = page.Body;
        panel.SetActive(true);
    }

    public void Hide()
    {
        // Leaving play tears the HUD down before InspectView, whose OnDisable puts every
        // page away on the way out. A destroyed object is not a null field, and Unity's own
        // == is the only thing that knows the difference.
        if (panel != null)
            panel.SetActive(false);
    }
}
