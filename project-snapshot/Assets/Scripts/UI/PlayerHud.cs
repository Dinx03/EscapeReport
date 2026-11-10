using UnityEngine;
using UnityEngine.UI;

// Crosshair, the line telling you what E would do, and the short-lived message that
// answers what just happened.
[DisallowMultipleComponent]
public class PlayerHud : MonoBehaviour
{
    [SerializeField] private Text promptLabel;
    [SerializeField] private Text messageLabel;
    [SerializeField] private Graphic crosshair;

    [SerializeField, Min(0.5f)]
    private float messageSeconds = 2f;

    [SerializeField]
    [Tooltip("Where the prompt drops to while a prop is up at the eyes. Held props are " +
             "drawn in the middle of the screen and a line under the crosshair lands on " +
             "the face of whatever is being looked at.")]
    private float handPromptY = -452f;

    private float _promptY;
    private float _messageClearsAt;

    private void Awake()
    {
        _promptY = promptLabel.rectTransform.anchoredPosition.y;
    }

    private void Start()
    {
        ShowPrompt(null);
        messageLabel.text = string.Empty;
    }

    private void Update()
    {
        if (messageLabel.text.Length > 0 && Time.time >= _messageClearsAt)
            messageLabel.text = string.Empty;
    }

    public void ShowPrompt(string prompt)
    {
        bool hasTarget = !string.IsNullOrEmpty(prompt);
        Write(prompt, _promptY);

        // The dot growing solid is the only cue that something is in range, so it
        // has to keep working even when the prompt line is empty.
        crosshair.color = hasTarget ? Color.white : new Color(1f, 1f, 1f, 0.35f);
    }

    // The same line, said from the foot of the screen. What it is talking about is up at the
    // player's eyes filling the middle of it, and a prompt across a clock face is a prompt
    // over the one thing the player is trying to read.
    public void ShowHandPrompt(string prompt)
    {
        Write(prompt, handPromptY);
    }

    public void ShowMessage(string message)
    {
        messageLabel.text = message;
        _messageClearsAt = Time.time + messageSeconds;
    }

    private void Write(string prompt, float y)
    {
        // Leaving play tears the HUD down before InspectView, whose OnDisable clears the
        // line on the way out. A destroyed object is not a null field, and Unity's own ==
        // is the only thing that knows the difference.
        if (promptLabel == null)
            return;

        promptLabel.text = string.IsNullOrEmpty(prompt) ? string.Empty : prompt;
        promptLabel.rectTransform.anchoredPosition =
            new Vector2(promptLabel.rectTransform.anchoredPosition.x, y);
    }
}
