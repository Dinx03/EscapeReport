using UnityEngine;

// A light that will not sit still: firelight, a candle, a bulb on a bad connection.
//
// Two waves of different speeds rather than random noise, because pure noise reads as a
// fault in the game and a slow beat under a fast one reads as a flame. The light keeps
// whatever intensity it was authored with and this rides on top, so brightening a fire is
// still a job for the Light.
[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public class Flicker : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)]
    private float depth = 0.22f;

    [SerializeField, Min(0.01f)]
    private float beatsPerSecond = 1.7f;

    [SerializeField, Min(0f)]
    [Tooltip("How far the source wanders. A flame is never in quite the same place twice.")]
    private float sway = 0.02f;

    private Light _light;
    private float _rest;
    private Vector3 _home;
    private float _phase;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _rest = _light.intensity;
        _home = transform.localPosition;

        // Two candles lit from the same script should not pulse in step.
        _phase = Random.value * 100f;
    }

    private void Update()
    {
        float t = Time.time * beatsPerSecond + _phase;
        float wave = Mathf.Sin(t) * 0.6f + Mathf.Sin(t * 2.7f + 1.3f) * 0.4f;

        _light.intensity = _rest * (1f + wave * depth);
        transform.localPosition = _home + new Vector3(Mathf.Sin(t * 1.9f), Mathf.Sin(t * 2.3f), 0f) * sway;
    }

    private void OnDisable()
    {
        // Accumulated wobble has to be put back, or a light switched off mid-beat comes
        // back dimmer than it was authored.
        _light.intensity = _rest;
        transform.localPosition = _home;
    }
}
