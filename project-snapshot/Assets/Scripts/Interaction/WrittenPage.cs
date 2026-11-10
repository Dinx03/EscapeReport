using UnityEngine;

// What a piece of paper says. Bolt it beside an Inspectable and pulling the paper up close
// puts the words on screen; take it off and the same paper is a page of scrawl nobody can
// read, which is exactly what it looks like.
[DisallowMultipleComponent]
public class WrittenPage : MonoBehaviour, IReadable
{
    [SerializeField]
    [Tooltip("The one line the paper itself shows in a hand the player can read.")]
    private string heading;

    [SerializeField, TextArea(3, 10)]
    [Tooltip("Everything the scrawl underneath says. Line breaks are kept, because Thai " +
             "has no spaces to wrap at and a machine break lands mid-word.")]
    private string body;

    public string Heading => heading;
    public string Body => body;
}
