using System.Collections.Generic;
using UnityEngine;

// Arthur's bench standard: turn it over and the sand runs for ten seconds.
//
// That is the whole of it. It counts nothing, compares itself to nothing and tells nobody
// when it empties — a glass is a length of time made visible, and what anyone does with
// that length is their business. `Running` is there for whoever eventually wants to know.
//
// `Turn` is called rather than pressed: in the room this prop is a `PickupItem`, and the
// glass the player turns is the one in their hand, held up beside whatever they are timing.
[DisallowMultipleComponent]
public class Sandglass : MonoBehaviour
{
    private const int Sides = 36;

    [SerializeField] private Transform frame;
    [SerializeField] private MeshFilter upperSand;
    [SerializeField] private MeshFilter lowerSand;
    [SerializeField] private Transform grains;

    [SerializeField, Min(0.05f)]
    [Tooltip("Seconds a grain takes to fall from the neck to the pile.")]
    private float grainFall = 0.3f;

    [SerializeField, Min(0f)]
    [Tooltip("How high the pile heaps up in the middle, and how deep the funnel sinks in " +
             "the bulb that is draining.")]
    private float heap = 0.009f;

    [SerializeField, Min(0.1f)]
    private float seconds = 10f;

    [SerializeField, Min(0.05f)]
    [Tooltip("How long the glass takes to come over in the hand.")]
    private float turnSeconds = 0.35f;

    [SerializeField]
    [Tooltip("The inside of one bulb, as radius against height above its own floor. The " +
             "builder samples it off the glass it turned, so the sand is that same shape " +
             "cut off at a level.")]
    private Vector2[] bulb;

    // One buffer, refilled. Sand is redrawn every frame it moves, and a new list each time
    // is a new list sixty times a second.
    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<int> _triangles = new List<int>();

    private Mesh _upperMesh;
    private Mesh _lowerMesh;
    private Renderer _upperRenderer;
    private Renderer _lowerRenderer;
    private float _height;
    private float _run;
    private float _turn;

    private bool Turning => _turn < 1f;

    public bool Running => !Turning && _run < seconds;

    private void Awake()
    {
        _height = bulb[bulb.Length - 1].y;

        // A mesh each, owned by this glass and rebuilt in place. Two glasses exist — the one
        // in the room and the one in the player's hand — and they must not share a mesh, or
        // the one on the bench drains as the one in the hand does.
        _upperMesh = new Mesh { name = "SandUpper" };
        _lowerMesh = new Mesh { name = "SandLower" };
        _upperMesh.MarkDynamic();
        _lowerMesh.MarkDynamic();
        upperSand.sharedMesh = _upperMesh;
        lowerSand.sharedMesh = _lowerMesh;
        _upperRenderer = upperSand.GetComponent<Renderer>();
        _lowerRenderer = lowerSand.GetComponent<Renderer>();

        // Run out and settled, which is how a glass nobody has touched stands.
        _run = seconds;
        _turn = 1f;
        Show();
    }

    private void Update()
    {
        if (Turning)
        {
            _turn = Mathf.Min(_turn + Time.deltaTime / turnSeconds, 1f);
            frame.localRotation = Quaternion.Euler(0f, 0f, _turn * 180f);

            // The prop is symmetric about that half turn — discs, glass and sand all land on
            // themselves — so ending it upright is the same picture as ending it upside down,
            // and upright is the one the sand can be drawn in. Nothing here has to know
            // which way up the glass is.
            if (!Turning)
            {
                frame.localRotation = Quaternion.identity;
                _run = 0f;
                Show();
            }

            return;
        }

        if (_run >= seconds)
            return;

        _run = Mathf.Min(_run + Time.deltaTime, seconds);
        Show();
    }

    private void OnDestroy()
    {
        Destroy(_upperMesh);
        Destroy(_lowerMesh);
    }

    public void Turn()
    {
        if (Turning)
            return;

        _turn = 0f;
    }

    private void Show()
    {
        float poured = _run / seconds;

        // Upside down to each other: the sand left above rests on the neck and the sand
        // below stands on the base, and both are the same bulb read from the other end.
        Fill(_upperMesh, _upperRenderer, 1f - poured, true);
        Fill(_lowerMesh, _lowerRenderer, poured, false);
        Pour(poured);
    }

    // The thread through the neck, as a dozen grains on a loop rather than a rod of sand.
    // Each falls from the neck to whatever the pile has risen to, so the drop shortens as the
    // glass fills — and they cost nothing, which a solid stream through a transparent bulb
    // could not say for itself.
    private void Pour(float poured)
    {
        bool falling = poured > 0f && poured < 1f;
        grains.gameObject.SetActive(falling);

        if (!falling)
            return;

        float top = lowerSand.transform.localPosition.y + _height;
        float pile = lowerSand.transform.localPosition.y + _height * poured;
        int count = grains.childCount;

        for (int i = 0; i < count; i++)
        {
            Transform grain = grains.GetChild(i);

            // Staggered by index, so they come down as a thread rather than in ranks.
            float fallen = Mathf.Repeat(_run / grainFall + i / (float)count, 1f);
            Vector3 at = grain.localPosition;
            grain.localPosition = new Vector3(at.x, Mathf.Lerp(top, pile, fallen), at.z);
        }
    }

    // Sand is the inside of the bulb, cut off level with however much of it is left. Cutting
    // rather than squashing is the whole of it: a shape scaled about its floor keeps the
    // width it had when it was full, and puts the top of a half-empty pile out through the
    // side of the glass.
    private void Fill(Mesh mesh, Renderer target, float amount, bool inverted)
    {
        target.enabled = amount > 0.0015f;

        if (!target.enabled)
            return;

        float level = _height * Mathf.Clamp01(amount);

        _vertices.Clear();
        _triangles.Clear();

        int rings = 0;

        for (int i = 0; i < bulb.Length && bulb[i].y < level; i++)
        {
            AddRing(RadiusAt(bulb[i].y, inverted), bulb[i].y);
            rings++;
        }

        AddRing(RadiusAt(level, inverted), level);
        rings++;

        int stride = Sides + 1;

        for (int r = 0; r < rings - 1; r++)
        {
            for (int s = 0; s < Sides; s++)
            {
                int a = r * stride + s;
                int b = a + 1;
                int c = a + stride;
                int d = c + 1;

                // Rings run from +Z towards +X, which is clockwise seen from above, so the
                // outward face is a-b-c and not a-c-b. Get it the other way round and the
                // sand is lit from the inside and reads as a hole in the glass.
                _triangles.Add(a); _triangles.Add(b); _triangles.Add(c);
                _triangles.Add(b); _triangles.Add(d); _triangles.Add(c);
            }
        }

        // Sand does not lie flat: it heaps where it lands and funnels where it leaves, and
        // both are the same cap with the middle pushed the other way.
        float peak = heap * Mathf.Clamp01(amount) * (inverted ? -1f : 1f);

        AddCap(RadiusAt(0f, inverted), 0f, false, 0f);
        AddCap(RadiusAt(level, inverted), level, true, peak);

        mesh.Clear();
        mesh.SetVertices(_vertices);
        mesh.SetTriangles(_triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void AddRing(float radius, float y)
    {
        for (int s = 0; s <= Sides; s++)
        {
            float angle = s / (float)Sides * Mathf.PI * 2f;
            _vertices.Add(new Vector3(Mathf.Sin(angle) * radius, y, Mathf.Cos(angle) * radius));
        }
    }

    // The floor of the pile and its surface. Sand is solid, and an open end is a pile you can
    // see the inside of. `peak` lifts the middle of the surface into a heap, or sinks it into
    // a funnel.
    private void AddCap(float radius, float y, bool up, float peak)
    {
        int centre = _vertices.Count;
        _vertices.Add(new Vector3(0f, y + peak, 0f));

        for (int s = 0; s <= Sides; s++)
        {
            float angle = s / (float)Sides * Mathf.PI * 2f;
            _vertices.Add(new Vector3(Mathf.Sin(angle) * radius, y, Mathf.Cos(angle) * radius));
        }

        for (int s = 0; s < Sides; s++)
        {
            int a = centre + 1 + s;

            if (up)
            {
                _triangles.Add(centre); _triangles.Add(a + 1); _triangles.Add(a);
            }
            else
            {
                _triangles.Add(centre); _triangles.Add(a); _triangles.Add(a + 1);
            }
        }
    }

    // The bulb read from its floor up, or from its neck down for the half that is draining.
    private float RadiusAt(float y, bool inverted)
    {
        float at = Mathf.Clamp(inverted ? _height - y : y, 0f, _height);
        float step = _height / (bulb.Length - 1);
        int i = Mathf.Clamp(Mathf.FloorToInt(at / step), 0, bulb.Length - 2);

        return Mathf.Lerp(bulb[i].x, bulb[i + 1].x, Mathf.InverseLerp(bulb[i].y, bulb[i + 1].y, at));
    }
}
