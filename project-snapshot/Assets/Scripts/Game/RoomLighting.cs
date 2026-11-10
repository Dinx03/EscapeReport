using UnityEngine;

public enum RoomBrightness
{
    Bright,
    Dim,
    Dark
}

// How lit the room itself is. Kept separate from the lights that shine on a prop being
// inspected: those sit on their own rendering layer, so turning the room dark for
// atmosphere never makes a clue unreadable.
[DisallowMultipleComponent]
public class RoomLighting : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Room lights only. Leave the inspect lights out — they are meant to be immune to this.")]
    private Light[] roomLights;

    [SerializeField]
    private RoomBrightness startingBrightness = RoomBrightness.Bright;

    [Header("Bright")]
    [SerializeField] private Level bright = new Level(1f, new Color(0.160f, 0.160f, 0.190f), 0.90f);

    [Header("Dim")]
    [SerializeField] private Level dim = new Level(0.45f, new Color(0.070f, 0.070f, 0.090f), 0.30f);

    [Header("Dark")]
    [SerializeField] private Level dark = new Level(0.12f, new Color(0.020f, 0.020f, 0.030f), 0.06f);

    private float[] _authoredIntensities;

    public RoomBrightness Current { get; private set; }

    private void Awake()
    {
        // Remember what the lights were dialled to in the scene, so every level is a
        // proportion of the look that was authored rather than a number typed twice.
        _authoredIntensities = new float[roomLights.Length];
        for (int i = 0; i < roomLights.Length; i++)
            _authoredIntensities[i] = roomLights[i].intensity;
    }

    private void Start()
    {
        SetBrightness(startingBrightness);
    }

    public void SetBrightness(RoomBrightness brightness)
    {
        Level level = LevelFor(brightness);

        for (int i = 0; i < roomLights.Length; i++)
            roomLights[i].intensity = _authoredIntensities[i] * level.LightScale;

        RenderSettings.ambientLight = level.Ambient;

        // The skybox reflection lights every surface on its own and ignores the ambient
        // colour completely. Leave it alone and turning the lights off changes almost
        // nothing — the room just sits there lit by the sky it cannot see.
        RenderSettings.reflectionIntensity = level.Reflection;

        Current = brightness;
    }

    // Handy for a light switch: one press walks Bright -> Dim -> Dark -> Bright.
    public RoomBrightness Cycle()
    {
        SetBrightness(Current == RoomBrightness.Dark ? RoomBrightness.Bright : Current + 1);
        return Current;
    }

    private Level LevelFor(RoomBrightness brightness)
    {
        switch (brightness)
        {
            case RoomBrightness.Dim: return dim;
            case RoomBrightness.Dark: return dark;
            default: return bright;
        }
    }

    [System.Serializable]
    private struct Level
    {
        [SerializeField, Min(0f)] private float lightScale;
        [SerializeField] private Color ambient;
        [SerializeField, Range(0f, 1f)] private float reflection;

        public Level(float lightScale, Color ambient, float reflection)
        {
            this.lightScale = lightScale;
            this.ambient = ambient;
            this.reflection = reflection;
        }

        public float LightScale => lightScale;
        public Color Ambient => ambient;
        public float Reflection => reflection;
    }
}
