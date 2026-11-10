using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Act four: the things that are missing off the chest.
//
// An old photograph stands on the chest of drawers showing that same chest with four
// ordinary objects on it, left to right. The chest is bare now and the four are scattered
// round the bedroom, each with a dealer's stock ticket on the back of it carrying a number.
// The wardrobe wants those numbers in the order the photograph stands them in — never the
// order they are lying about the room in, which is the whole of it.
//
// **The picture is drawn from the props, not beside them.** Each object is built first and
// then photographed: every renderer on it becomes one mark on the print, at the tone of the
// material it is made of. Change the shape of the vase and the vase in the photograph
// changes with it, the same bargain `Stars` makes with the constellation two rooms on. A
// clue that can drift away from the thing it is a clue to is not a clue.
//
// Nothing states the rule anywhere, and nothing may: the player is meant to find the four,
// turn them over, and work out for themselves that the photograph is what puts them in order.
public static class MissingThingsBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Puzzle";
    private const string PropsRoot = "House/Props";
    private const string Room = PropsRoot + "/Room_IV_Bedroom";
    private const string Group = "MissingThings";
    private const string Wardrobe = Room + "/Wardrobe/WardrobeDoor";

    // The print, in pixels, and how much of a metre one of them is.
    private const int Wide = 512, High = 384;
    private const float TableTop = 104f;
    private const float Scale = 950f;

    // What the print is made of. Everything on it is one of these two, mixed by how pale
    // the thing being photographed actually is.
    private static readonly Color Deep = new Color(0.19f, 0.14f, 0.10f);
    private static readonly Color Pale = new Color(0.82f, 0.74f, 0.57f);

    [MenuItem("Tools/Escape Room/Puzzle/Build Missing Things")]
    public static void BuildMissingThings()
    {
        BuildKit.EnsureFolder(PrefabFolder);

        // The puzzle itself: four things, left to right as the photograph stands them, and
        // the number on the back of each. Reorder these and both the picture and the
        // combination follow — there is nowhere else the answer is written down.
        var things = new (string Name, int Digit, System.Func<GameObject> Build, Vector3 At, Vector3 Facing)[]
        {
            ("Teacup",      4, Teacup,      new Vector3(6.02f, 0.615f, -2.14f), new Vector3(0f, 25f, 0f)),
            ("Candlestick", 6, Candlestick, new Vector3(5.30f, 0f,     -0.12f), new Vector3(0f, 115f, 0f)),
            ("Vase",        9, Vase,        new Vector3(8.06f, 1.300f, -2.32f), new Vector3(0f, 8f, 0f)),
            ("HandMirror",  2, HandMirror,  new Vector3(5.22f, 0.686f, -0.62f), new Vector3(-90f, 25f, 0f)),
        };

        // Four homes and four different heights: a bedside table, the floor at the foot of
        // the bed, a shelf, and the bed itself. Nothing goes in the corner by the wardrobe —
        // one lamp lights this room and a brass candlestick standing in that shadow is a
        // thing the player walks past four times without ever seeing it.

        Material ticket = Ticket();
        var portraits = new List<Mark>[things.Length];
        var prefabs = new GameObject[things.Length];

        for (int i = 0; i < things.Length; i++)
        {
            GameObject body = things[i].Build();

            // Photographed before the ticket goes on: the ticket is round the back, and a
            // camera in front of the thing cannot see it.
            portraits[i] = Photograph(body);

            Stamp(body, things[i].Digit, ticket);
            prefabs[i] = Save(body, things[i].Name + "Prop");
        }

        WritePrint(portraits);
        GameObject frame = Save(Frame(), "OldPhotograph");

        // ---------- into the room ----------

        Transform room = GameObject.Find(Room).transform;
        Transform old = room.Find(Group);
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        Transform group = BuildKit.Group(Group, room, Vector3.zero);

        // On the chest, which is bare apart from it. The picture and the empty surface it is
        // a picture of are meant to be in one glance.
        Place(frame, "OldPhotograph", group, new Vector3(7.60f, 0.690f, -2.26f), new Vector3(0f, 6f, 0f));

        var pictured = new StampedNumber[things.Length];
        for (int i = 0; i < things.Length; i++)
        {
            GameObject placed = Place(prefabs[i], things[i].Name, group, things[i].At, things[i].Facing);
            pictured[i] = placed.GetComponent<StampedNumber>();
        }

        FitLock(pictured, things);

        AssetDatabase.SaveAssets();
        Debug.Log("Missing things rebuilt: the photograph and the four objects in it.");
    }

    // ---------- the four things ----------
    //
    // All four are built standing, facing local +z, with their foot on y = 0 and the ticket
    // on the back. That is what lets one pair of angles serve every one of them — the
    // Inspectable turns them over about y and the back comes round — and it is also the
    // frame the photograph is taken in.
    //
    // They are stacks of turned pieces rather than lathed profiles on purpose: the steps are
    // what the print is drawn from, and a vase that is one smooth mesh photographs as one
    // featureless rectangle.

    private static GameObject Teacup()
    {
        Material china = China();
        var root = new GameObject("Teacup");

        BuildKit.Cylinder("Saucer", root.transform, new Vector3(0f, 0.006f, 0f), 0.130f, 0.012f, china);
        BuildKit.Cylinder("Foot", root.transform, new Vector3(0f, 0.017f, 0f), 0.038f, 0.010f, china);
        BuildKit.Cylinder("Bowl", root.transform, new Vector3(0f, 0.041f, 0f), 0.070f, 0.038f, china);
        BuildKit.Cylinder("Lip", root.transform, new Vector3(0f, 0.062f, 0f), 0.076f, 0.005f, china);
        Arc("Handle", root.transform, new Vector3(0.040f, 0.042f, 0f), 0.017f, -104f, 104f, 11, 0.005f, china);

        BuildKit.Hull(root, new Vector3(0f, 0.034f, 0f), new Vector3(0.140f, 0.072f, 0.140f));
        return root;
    }

    private static GameObject Candlestick()
    {
        Material brass = BuildKit.Load("ClockBrass");
        Material wax = BuildKit.Load("DressWax");
        var root = new GameObject("Candlestick");

        // A squat chamberstick rather than a tall one, and it is the ticket that decides
        // that. A held prop backs off by its own size, so every centimetre of candlestick
        // pushes the number on its foot further away: at a quarter of a metre tall it was
        // held at arm's length and the figure was three pixels of brass. The plinth is deep
        // for the same reason — the paper has to have somewhere flat and legible to go.
        BuildKit.Cylinder("Foot", root.transform, new Vector3(0f, 0.016f, 0f), 0.104f, 0.032f, brass);
        BuildKit.Cylinder("Step", root.transform, new Vector3(0f, 0.038f, 0f), 0.064f, 0.012f, brass);
        BuildKit.Sphere("Knop", root.transform, new Vector3(0f, 0.053f, 0f), 0.030f, brass);
        BuildKit.Cylinder("Stem", root.transform, new Vector3(0f, 0.080f, 0f), 0.018f, 0.038f, brass);
        BuildKit.Sphere("Knop", root.transform, new Vector3(0f, 0.102f, 0f), 0.026f, brass);
        BuildKit.Cylinder("Pan", root.transform, new Vector3(0f, 0.116f, 0f), 0.058f, 0.010f, brass);
        BuildKit.Cylinder("Socket", root.transform, new Vector3(0f, 0.132f, 0f), 0.030f, 0.026f, brass);
        BuildKit.Cylinder("Candle", root.transform, new Vector3(0f, 0.164f, 0f), 0.022f, 0.048f, wax);
        BuildKit.Box("Wick", root.transform, new Vector3(0f, 0.192f, 0f), new Vector3(0.003f, 0.008f, 0.003f),
            BuildKit.Load("DressSoot"));

        BuildKit.Hull(root, new Vector3(0f, 0.098f, 0f), new Vector3(0.112f, 0.200f, 0.112f));
        return root;
    }

    private static GameObject Vase()
    {
        Material glaze = BuildKit.Opaque("VaseGlaze", new Color(0.26f, 0.33f, 0.36f), 0.55f, 0f,
            AssetDatabase.LoadAssetAtPath<Texture2D>(BuildKit.TextureFolder + "/HearthStone.png"));
        var root = new GameObject("Vase");

        // The outline, sampled up the pot as ten bands. Six were enough to say "vase" and
        // read as a stack of tins; the step between one band and the next has to be smaller
        // than the eye bothers with, and at this size that is about a centimetre and a half.
        var profile = new[] { 0.029f, 0.042f, 0.048f, 0.049f, 0.046f, 0.039f, 0.030f, 0.023f, 0.021f, 0.026f };
        float band = 0.158f / profile.Length;

        for (int i = 0; i < profile.Length; i++)
            BuildKit.Cylinder("Band", root.transform, new Vector3(0f, (i + 0.5f) * band, 0f),
                profile[i] * 2f, band * 1.04f, glaze);

        BuildKit.Hull(root, new Vector3(0f, 0.078f, 0f), new Vector3(0.104f, 0.158f, 0.104f));
        return root;
    }

    private static GameObject HandMirror()
    {
        Material brass = BuildKit.Load("ClockBrass");
        Material silvering = BuildKit.Opaque("MirrorSilvering", new Color(0.84f, 0.87f, 0.89f), 0.94f, 0.85f, null);
        var root = new GameObject("HandMirror");

        // The one flat back in the set, which is where the ticket wants to be anyway.
        BuildKit.Disc("Plate", root.transform, new Vector3(0f, 0.115f, -0.004f), 0.110f, 0.008f, brass);
        BuildKit.Disc("Glass", root.transform, new Vector3(0f, 0.115f, 0.002f), 0.094f, 0.004f, silvering);
        BuildKit.Box("Neck", root.transform, new Vector3(0f, 0.055f, 0f), new Vector3(0.020f, 0.048f, 0.010f), brass);
        BuildKit.Cylinder("Handle", root.transform, new Vector3(0f, 0.028f, 0f), 0.016f, 0.052f, brass);
        BuildKit.Cylinder("Ferrule", root.transform, new Vector3(0f, 0.004f, 0f), 0.020f, 0.008f, brass);

        BuildKit.Hull(root, new Vector3(0f, 0.086f, 0f), new Vector3(0.115f, 0.176f, 0.032f));
        return root;
    }

    // The photograph itself: a small standing frame, a print behind glass, an easel leg.
    private static GameObject Frame()
    {
        Material brass = BuildKit.Load("ClockBrass");
        Material board = BuildKit.Load("ClockCaseDark");
        Material print = BuildKit.Opaque("OldPhotograph", Color.white, 0.06f, 0f,
            AssetDatabase.LoadAssetAtPath<Texture2D>(BuildKit.TextureFolder + "/OldPhotograph.png"));

        var root = new GameObject("OldPhotograph");

        // A big frame: this is the one thing in the room the player has to notice from the
        // doorway. At a postcard's size it sat on a chest of drawers nearly two metres wide
        // and read as part of the furniture.
        BuildKit.Box("Mount", root.transform, new Vector3(0f, 0.0985f, 0f), new Vector3(0.250f, 0.197f, 0.012f), board);
        BuildKit.Box("Print", root.transform, new Vector3(0f, 0.0985f, 0.007f), new Vector3(0.216f, 0.162f, 0.003f), print);

        BuildKit.Box("Bar", root.transform, new Vector3(0f, 0.1880f, 0.007f), new Vector3(0.250f, 0.018f, 0.008f), brass);
        BuildKit.Box("Bar", root.transform, new Vector3(0f, 0.0090f, 0.007f), new Vector3(0.250f, 0.018f, 0.008f), brass);
        BuildKit.Box("Bar", root.transform, new Vector3(-0.116f, 0.0985f, 0.007f), new Vector3(0.018f, 0.197f, 0.008f), brass);
        BuildKit.Box("Bar", root.transform, new Vector3(0.116f, 0.0985f, 0.007f), new Vector3(0.018f, 0.197f, 0.008f), brass);

        GameObject strut = BuildKit.Box("Strut", root.transform, new Vector3(0f, 0.075f, -0.042f),
            new Vector3(0.045f, 0.150f, 0.006f), board);
        strut.transform.localRotation = Quaternion.Euler(16f, 0f, 0f);

        BuildKit.Hull(root, new Vector3(0f, 0.099f, -0.012f), new Vector3(0.256f, 0.202f, 0.072f));

        // Held, not turned: there is nothing on the back of it, and a photograph the player
        // can flip over is a photograph they will spend a minute flipping over.
        var inspectable = root.AddComponent<Inspectable>();
        BuildKit.Wire(inspectable, "style", (int)InspectStyle.Hold);
        BuildKit.Wire(inspectable, "facing", new Vector3(0f, 180f, 0f));

        return root;
    }

    // ---------- the ticket ----------

    // A dealer's stock number, on paper, stuck to the back. Every mark on it is a printed
    // figure off the same ten-digit strip a winder's number reel runs, which is why the
    // number wants no drawing of its own.
    private static void Stamp(GameObject prop, int digit, Material paper)
    {
        // Where the back of the thing actually is at the height the ticket goes, so the
        // paper sits on the surface rather than hanging in the air behind a curved one.
        var backs = new Dictionary<string, Vector4>
        {
            { "Teacup", new Vector4(0f, 0.042f, -0.0370f, 0.024f) },
            { "Candlestick", new Vector4(0f, 0.016f, -0.0535f, 0.028f) },
            { "Vase", new Vector4(0f, 0.062f, -0.0505f, 0.028f) },
            { "HandMirror", new Vector4(0f, 0.115f, -0.0090f, 0.030f) },
        };

        Vector4 where = backs[prop.name];

        // A bare Unity quad faces its own -z and is read from that side, which is exactly
        // where the player is standing once the prop has been turned over. Anything that
        // turned it to face +z would print the figure back to front.
        GameObject face = GameObject.CreatePrimitive(PrimitiveType.Quad);
        face.name = "Ticket";
        Object.DestroyImmediate(face.GetComponent<Collider>());
        face.transform.SetParent(prop.transform, false);
        face.transform.localPosition = new Vector3(where.x, where.y, where.z);
        face.transform.localScale = Vector3.one * where.w;
        face.GetComponent<Renderer>().sharedMaterial = paper;

        var number = prop.AddComponent<StampedNumber>();
        BuildKit.Wire(number, "digit", digit);
        BuildKit.Wire(number, "ticket", face.GetComponent<Renderer>());

        // Turn, so E brings the back round. There is nothing to work once it is round there,
        // so a second press simply turns it back — which is the whole interaction and the
        // reason none of these four may carry a PickupItem.
        var inspectable = prop.AddComponent<Inspectable>();
        BuildKit.Wire(inspectable, "style", (int)InspectStyle.Turn);
        BuildKit.Wire(inspectable, "facing", new Vector3(0f, 180f, 0f));
        BuildKit.Wire(inspectable, "backFacing", Vector3.zero);
    }

    private static Material Ticket()
    {
        Material mat = BuildKit.Opaque("StockTicket", Color.white, 0.05f, 0f,
            AssetDatabase.LoadAssetAtPath<Texture2D>(BuildKit.TextureFolder + "/ClockDigits.png"));

        // Ten digits stacked up the strip; StampedNumber scrolls the one it wants into view.
        mat.SetTextureScale("_BaseMap", new Vector2(1f, 0.1f));
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------- the print ----------

    // One thing in the picture: where it stands in the prop's own frame, how pale it is, and
    // whether it is turned or square. Everything on the print is one of these.
    private struct Mark
    {
        public Rect Box;
        public float Tone;
        public bool Round;
    }

    // What a camera in front of the prop would see: every renderer flattened onto the plane
    // the photograph is taken in. The props are built at the origin unturned, so a
    // renderer's world box is already its box in the prop's own frame.
    private static List<Mark> Photograph(GameObject prop)
    {
        var marks = new List<Mark>();

        foreach (MeshRenderer r in prop.GetComponentsInChildren<MeshRenderer>())
        {
            Bounds box = r.bounds;
            string mesh = r.GetComponent<MeshFilter>().sharedMesh.name;

            marks.Add(new Mark
            {
                Box = Rect.MinMaxRect(box.min.x, box.min.y, box.max.x, box.max.y),
                Tone = r.sharedMaterial.GetColor("_BaseColor").grayscale,

                // A cylinder standing on its own y is a barrel and photographs as a
                // rectangle. One turned onto z is a disc facing the camera.
                Round = mesh == "Sphere" || (mesh == "Cylinder" && Mathf.Abs(r.transform.up.z) > 0.7f)
            });
        }

        return marks;
    }

    private static void WritePrint(List<Mark>[] portraits)
    {
        var px = new Color[Wide * High];

        // The room behind: light falling off towards the corners, the way one window and a
        // long exposure leaves it.
        for (int y = 0; y < High; y++)
        for (int x = 0; x < Wide; x++)
        {
            float up = y / (float)High;
            px[y * Wide + x] = Color.Lerp(Deep, Pale, 0.30f + up * 0.34f);
        }

        // The chest itself, seen end on: a top, a shadow under its lip, and two drawers.
        Fill(px, 0f, 0f, Wide, TableTop, Color.Lerp(Deep, Pale, 0.24f));
        Fill(px, 0f, TableTop - 7f, Wide, 7f, Color.Lerp(Deep, Pale, 0.42f));
        Fill(px, 0f, TableTop - 12f, Wide, 5f, Color.Lerp(Deep, Pale, 0.12f));

        for (int drawer = 0; drawer < 2; drawer++)
        {
            float top = TableTop - 24f - drawer * 42f;
            Fill(px, 26f, top - 34f, Wide - 52f, 34f, Color.Lerp(Deep, Pale, 0.30f));
            Fill(px, 26f, top - 36f, Wide - 52f, 2f, Color.Lerp(Deep, Pale, 0.10f));
            Disc(px, Wide * 0.5f, top - 17f, 5f, Color.Lerp(Deep, Pale, 0.46f));
        }

        float slot = Wide / (float)portraits.Length;

        for (int i = 0; i < portraits.Length; i++)
        {
            float cx = slot * (i + 0.5f);

            // Something to stand on. Without a shadow four objects float a hair above the
            // wood and the whole print reads as a cut-out.
            float widest = 0f;
            foreach (Mark mark in portraits[i])
                widest = Mathf.Max(widest, mark.Box.width);

            Disc(px, cx, TableTop - 2f, widest * Scale * 0.5f, Color.Lerp(Deep, Pale, 0.16f), 0.34f);

            foreach (Mark mark in portraits[i])
            {
                Color ink = Color.Lerp(Deep, Pale, Mathf.Clamp01(mark.Tone) * 0.78f + 0.10f);
                float x0 = cx + mark.Box.xMin * Scale;
                float y0 = TableTop + mark.Box.yMin * Scale;
                float w = mark.Box.width * Scale, h = mark.Box.height * Scale;

                if (mark.Round)
                    Disc(px, x0 + w * 0.5f, y0 + h * 0.5f, w * 0.5f, ink, h / Mathf.Max(w, 0.001f));
                else
                    Fill(px, x0, y0, w, h, ink);
            }
        }

        // Age: grain over the whole plate, and the corners going dark the way a print does
        // where the mount has kept the light off it.
        for (int y = 0; y < High; y++)
        for (int x = 0; x < Wide; x++)
        {
            int i = y * Wide + x;
            float grain = (Mathf.PerlinNoise(x * 0.19f, y * 0.19f) - 0.5f) * 0.11f;
            float edge = Mathf.Min(Mathf.Min(x, Wide - 1 - x) / 46f, Mathf.Min(y, High - 1 - y) / 40f);
            px[i] = Color.Lerp(Deep * 0.9f, px[i] * (1f + grain), Mathf.Clamp01(0.34f + edge * 0.66f));
        }

        TextureKit.Save(px, Wide, High, "OldPhotograph");
    }

    // ---------- fitting it to the room ----------

    private static GameObject Place(GameObject prefab, string name, Transform parent, Vector3 at, Vector3 facing)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.SetPositionAndRotation(at, Quaternion.Euler(facing));
        return instance;
    }

    // The wardrobe never learns what any of this is. It keeps a CodeLock, and the component
    // bolted beside it reads the four tickets in the photograph's order and tells the lock
    // what number that spells.
    private static void FitLock(StampedNumber[] pictured,
                                (string Name, int Digit, System.Func<GameObject> Build, Vector3 At, Vector3 Facing)[] things)
    {
        GameObject door = GameObject.Find(Wardrobe);
        var codeLock = door.GetComponent<CodeLock>();

        StampedCombination sum = door.GetComponent<StampedCombination>();
        if (sum == null)
            sum = door.AddComponent<StampedCombination>();

        BuildKit.Wire(sum, "asPictured", pictured);

        // Written into the lock as well, from the same array, so the Inspector says what the
        // wardrobe wants instead of showing whatever number the last puzzle left there.
        var so = new SerializedObject(codeLock);
        SerializedProperty digits = so.FindProperty("combination");
        digits.arraySize = things.Length;
        for (int i = 0; i < things.Length; i++)
            digits.GetArrayElementAtIndex(i).intValue = things[i].Digit;

        // The old line pointed at the alarm clocks. Nothing here may name the photograph:
        // noticing that it is the photograph is the puzzle.
        so.FindProperty("refusedMessage").stringValue = "ตู้ยังล็อก ต้องตั้งเลขสี่หลักให้ถูกก่อน";
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(door);
    }

    // ---------- pieces ----------

    // Part of a ring, laid out of short bars in the xy plane: a cup handle, and nothing else
    // in this room needs one.
    private static void Arc(string name, Transform parent, Vector3 centre, float radius,
                            float fromDeg, float toDeg, int segments, float thickness, Material material)
    {
        Transform hub = BuildKit.Group(name, parent, centre);

        for (int i = 0; i < segments; i++)
        {
            float a = Mathf.Lerp(fromDeg, toDeg, (i + 0.5f) / segments) * Mathf.Deg2Rad;
            float step = Mathf.Abs(toDeg - fromDeg) / segments * Mathf.Deg2Rad;

            GameObject bar = BuildKit.Box("Bar", hub,
                new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f),
                new Vector3(thickness, radius * step * 1.04f + thickness * 0.3f, thickness), material);
            bar.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 90f);
        }
    }

    private static Material China() =>
        BuildKit.Opaque("ChinaGlaze", new Color(0.90f, 0.88f, 0.84f), 0.66f, 0f, null);

    private static GameObject Save(GameObject root, string name)
    {
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + name + ".prefab");
        Object.DestroyImmediate(root);
        return asset;
    }

    // ---------- ink ----------

    private static void Fill(Color[] px, float x, float y, float w, float h, Color ink)
    {
        for (int j = Mathf.RoundToInt(y); j < y + h; j++)
        for (int i = Mathf.RoundToInt(x); i < x + w; i++)
        {
            if (i >= 0 && i < Wide && j >= 0 && j < High)
                px[j * Wide + i] = ink;
        }
    }

    private static void Disc(Color[] px, float cx, float cy, float radius, Color ink) =>
        Disc(px, cx, cy, radius, ink, 1f);

    private static void Disc(Color[] px, float cx, float cy, float radius, Color ink, float squash)
    {
        float ry = Mathf.Max(radius * squash, 0.5f);

        for (int j = Mathf.RoundToInt(cy - ry); j <= cy + ry; j++)
        for (int i = Mathf.RoundToInt(cx - radius); i <= cx + radius; i++)
        {
            if (i < 0 || i >= Wide || j < 0 || j >= High)
                continue;

            float dx = (i - cx) / radius, dy = (j - cy) / ry;
            if (dx * dx + dy * dy <= 1f)
                px[j * Wide + i] = ink;
        }
    }
}
