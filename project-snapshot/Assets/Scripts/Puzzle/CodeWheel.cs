using System;
using UnityEngine;

// One digit of a combination, printed on a strip that steps round behind a window: the
// mechanical implementation of `ICodeInput`.
//
// It knows its own number and nothing about whether that number is right — reading the
// inputs and deciding is `CodeLock`'s job. Same strip and same material as the number reel
// in a clock winder, so the case reads as the work of the same clockmaker.
[DisallowMultipleComponent]
public class CodeWheel : MonoBehaviour, IInteractable, ICodeInput
{
    private const int DigitsOnStrip = 10;

    [SerializeField, Range(0, 9)]
    private int startDigit;

    [SerializeField]
    private Renderer strip;

    [SerializeField, Min(0.01f)]
    [Tooltip("Seconds for the strip to step on by one digit.")]
    private float stepSeconds = 0.12f;

    private int _digit;
    private float _shown;    // strip position, counted in digits
    private float _target;
    private Material _material;

    public event Action Changed;

    public int Digit => _digit;

    public string Prompt => "[E] หมุนเลข";

    private void Awake()
    {
        _material = strip.material;
        _digit = startDigit;
        _shown = startDigit;
        _target = startDigit;

        Draw();
    }

    private void Update()
    {
        if (Mathf.Approximately(_shown, _target))
            return;

        _shown = Mathf.MoveTowards(_shown, _target, Time.deltaTime / stepSeconds);
        Draw();
    }

    public void Interact(IActor actor)
    {
        _digit = (_digit + 1) % DigitsOnStrip;

        // Always forwards, so nine rolls up into zero the short way instead of winding
        // all the way back down the strip.
        _target += 1f;

        Changed?.Invoke();
    }

    private void Draw()
    {
        var scroll = new Vector2(0f, _shown / DigitsOnStrip);
        _material.SetTextureOffset("_BaseMap", scroll);
        _material.SetTextureOffset("_EmissionMap", scroll);
    }
}
