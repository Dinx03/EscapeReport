using System.IO;
using UnityEditor;
using UnityEngine;

// Builds the props Puzzle 1 needs: the glass case the code opens, the key inside it, and
// the note that states the rule.
//
// Same bargain as ClockBuilder — the geometry lives here so it can be rebuilt, which
// means editing it by hand in the Editor is thrown away on the next rebuild.
public static class PuzzleBuilder
{
    private const string TextureFolder = "Assets/Textures";
    private const string MaterialFolder = "Assets/Materials";
    private const string PrefabFolder = "Assets/Prefabs/Puzzle";
    private const string MeshFolder = "Assets/Meshes";
    private const string CasePath = PrefabFolder + "/DisplayCase.prefab";
    private const string KeyPath = PrefabFolder + "/BrassKeyProp.prefab";

    // Cabinet, in metres. The front faces +z.
    private const float CaseWidth = 0.80f;
    private const float CaseDepth = 0.36f;
    private const float PlinthTop = 0.62f;
    private const float BodyTop = 1.52f;
    private const float Panel = 0.03f;

    private static float HalfWidth => CaseWidth * 0.5f;
    private static float HalfDepth => CaseDepth * 0.5f;
    private static float BodyMidY => (PlinthTop + BodyTop) * 0.5f;
    private static float BodyHeight => BodyTop - PlinthTop;

    [MenuItem("Tools/Escape Room/Puzzle/Build All")]
    public static void BuildAll()
    {
        BuildKey();
        BuildDisplayCase();
        WriteNoteTexture();
        AssetDatabase.Refresh();
        Debug.Log("Puzzle props rebuilt: display case, brass key, note page.");
    }

    [MenuItem("Tools/Escape Room/Puzzle/Build Display Case")]
    public static GameObject BuildDisplayCase()
    {
        EnsureFolder(PrefabFolder);

        Material wood = Wood();
        Material dark = WoodDark();
        Material brass = Brass();
        Material glass = Glass();
        Material reelMat = LoadMaterial("ClockReel");

        var root = new GameObject("DisplayCase");

        // Carcass. The plinth is solid; the upper box is open at the front for the door.
        Box("Plinth", root.transform, new Vector3(0f, PlinthTop * 0.5f, 0f),
            new Vector3(CaseWidth, PlinthTop, CaseDepth), wood, true);

        Box("Back", root.transform, new Vector3(0f, BodyMidY, -HalfDepth + Panel * 0.5f),
            new Vector3(CaseWidth, BodyHeight, Panel), wood, true);

        Box("SideLeft", root.transform, new Vector3(-HalfWidth + Panel * 0.5f, BodyMidY, 0f),
            new Vector3(Panel, BodyHeight, CaseDepth), wood, true);

        Box("SideRight", root.transform, new Vector3(HalfWidth - Panel * 0.5f, BodyMidY, 0f),
            new Vector3(Panel, BodyHeight, CaseDepth), wood, true);

        Box("Cornice", root.transform, new Vector3(0f, BodyTop + 0.03f, 0f),
            new Vector3(CaseWidth + 0.06f, 0.06f, CaseDepth + 0.06f), wood, true);

        Box("Shelf", root.transform, new Vector3(0f, PlinthTop + 0.02f, 0f),
            new Vector3(CaseWidth - Panel * 2f, 0.02f, CaseDepth - Panel), dark, false);

        // The door hangs on the left stile, so its own origin sits there.
        float doorWidth = CaseWidth - Panel * 2f;
        var door = new GameObject("Door");
        door.transform.SetParent(root.transform, false);
        door.transform.localPosition = new Vector3(-HalfWidth + Panel, BodyMidY, HalfDepth - 0.012f);

        Box("Glass", door.transform, new Vector3(doorWidth * 0.5f, 0f, 0f),
            new Vector3(doorWidth - 0.05f, BodyHeight - 0.06f, 0.008f), glass, false);

        float railY = (BodyHeight - 0.02f) * 0.5f;
        Box("RailTop", door.transform, new Vector3(doorWidth * 0.5f, railY, 0f),
            new Vector3(doorWidth, 0.02f, 0.016f), brass, false);
        Box("RailBottom", door.transform, new Vector3(doorWidth * 0.5f, -railY, 0f),
            new Vector3(doorWidth, 0.02f, 0.016f), brass, false);
        Box("StileHinge", door.transform, new Vector3(0.01f, 0f, 0f),
            new Vector3(0.02f, BodyHeight, 0.016f), brass, false);
        Box("StileLatch", door.transform, new Vector3(doorWidth - 0.01f, 0f, 0f),
            new Vector3(0.02f, BodyHeight, 0.016f), brass, false);
        Sphere("Knob", door.transform, new Vector3(doorWidth - 0.04f, 0f, 0.02f), 0.035f, brass);

        var doorCollider = door.AddComponent<BoxCollider>();
        doorCollider.center = new Vector3(doorWidth * 0.5f, 0f, 0f);
        doorCollider.size = new Vector3(doorWidth, BodyHeight, 0.05f);

        var openable = door.AddComponent<Openable>();
        var swing = door.AddComponent<SwingMotion>();
        var codeLock = door.AddComponent<CodeLock>();

        // The wheels live on the plinth, where the door cannot carry them away as it opens.
        var plate = new GameObject("Dialplate");
        plate.transform.SetParent(root.transform, false);
        plate.transform.localPosition = new Vector3(0f, 0.40f, HalfDepth);

        Box("Plate", plate.transform, new Vector3(0f, 0f, 0.006f),
            new Vector3(0.60f, 0.17f, 0.012f), brass, false);

        var wheels = new CodeWheel[4];
        const float Spacing = 0.135f;

        for (int i = 0; i < wheels.Length; i++)
        {
            // Descending, not ascending. The plate sits on the case's +z face, so someone
            // standing in front of it looks down local -z — and in that view local +x is
            // on their left. Counting up would print the combination backwards.
            float x = (1.5f - i) * Spacing;

            var wheel = new GameObject("Wheel" + (i + 1));
            wheel.transform.SetParent(plate.transform, false);
            wheel.transform.localPosition = new Vector3(x, 0f, 0.012f);

            Box("Recess", wheel.transform, new Vector3(0f, 0f, 0.004f),
                new Vector3(0.095f, 0.115f, 0.008f), dark, false);

            GameObject reel = Quad("Reel", wheel.transform, new Vector3(0f, 0f, 0.010f), 0.075f, reelMat);

            var hit = wheel.AddComponent<BoxCollider>();
            hit.center = new Vector3(0f, 0f, 0.01f);
            hit.size = new Vector3(0.12f, 0.14f, 0.05f);

            var code = wheel.AddComponent<CodeWheel>();
            Wire(code, "strip", reel.GetComponent<Renderer>());
            wheels[i] = code;
        }

        Wire(openable, "label", "ตู้กระจก");
        Wire(swing, "openAngle", -100f);   // hinged on the left, so it swings out that way
        Wire(swing, "swingSpeed", 150f);

        var lockSo = new SerializedObject(codeLock);
        SerializedProperty list = lockSo.FindProperty("wheels");
        list.arraySize = wheels.Length;
        for (int i = 0; i < wheels.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = wheels[i];

        int[] answer = { 4, 7, 1, 9 };
        SerializedProperty digits = lockSo.FindProperty("combination");
        digits.arraySize = answer.Length;
        for (int i = 0; i < answer.Length; i++)
            digits.GetArrayElementAtIndex(i).intValue = answer[i];

        lockSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, CasePath);
        Object.DestroyImmediate(root);
        return asset;
    }

    [MenuItem("Tools/Escape Room/Puzzle/Build Brass Key")]
    public static GameObject BuildKey()
    {
        EnsureFolder(PrefabFolder);
        Material brass = Brass();

        var root = new GameObject("BrassKeyProp");

        // A warded key: bow, shaft, and two teeth on the bit.
        Torus("Bow", root.transform, new Vector3(0f, 0f, -0.032f), 0.042f, 0.008f, brass);
        Box("Shaft", root.transform, new Vector3(0f, 0f, 0.012f),
            new Vector3(0.008f, 0.008f, 0.070f), brass, false);
        Box("Collar", root.transform, new Vector3(0f, 0f, -0.008f),
            new Vector3(0.016f, 0.016f, 0.008f), brass, false);
        Box("BitA", root.transform, new Vector3(0f, -0.014f, 0.034f),
            new Vector3(0.007f, 0.022f, 0.010f), brass, false);
        Box("BitB", root.transform, new Vector3(0f, -0.011f, 0.050f),
            new Vector3(0.007f, 0.016f, 0.008f), brass, false);

        var hit = root.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, -0.005f, 0.008f);
        hit.size = new Vector3(0.05f, 0.055f, 0.115f);

        var key = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Scenes/Items/BrassKey.asset");
        var pickup = root.AddComponent<PickupItem>();
        Wire(pickup, "item", key);

        var inspectable = root.AddComponent<Inspectable>();
        Wire(inspectable, "facing", new Vector3(0f, 90f, 0f));

        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, KeyPath);
        Object.DestroyImmediate(root);
        return asset;
    }

    // The shop's care leaflet, and the page the whole of Act I hangs on.
    //
    // The rule is pictures, because a picture states it in any language and the player never
    // has to wonder whether a translation lost something. The prose above it is not drawn as
    // words at all: a texture has no font engine, so it gets the *shape* of handwriting —
    // and what the writing says is read out by the HUD instead, off the `WrittenPage` bolted
    // to the note. The heading is the one thing on this page a player can read, and it is
    // not here either: `Face` on the note prop prints it live, so it can be translated.
    //
    // The top strip is therefore deliberately empty. Draw into it and the heading lands on
    // top of whatever was put there.
    [MenuItem("Tools/Escape Room/Puzzle/Write Note Texture")]
    public static void WriteNoteTexture()
    {
        const int W = 512, H = 704;
        var ink = new Color(0.16f, 0.12f, 0.09f);
        var faint = new Color(0.45f, 0.38f, 0.30f);
        var paper = new Color(0.87f, 0.83f, 0.71f);

        var px = new Color[W * H];
        for (int i = 0; i < px.Length; i++)
            px[i] = paper;

        Frame(px, W, H, 26f, 26f, W - 52f, H - 52f, 3f, faint);

        // ---- heading strip: y 606..676 stays blank, see above ----

        Fill(px, W, H, 70f, 592f, W - 140f, 3f, faint);

        // ---- prose strip: y 477..583 stays blank too, and for the same reason ----

        Fill(px, W, H, 70f, 470f, W - 140f, 2f, faint);

        // Rule one: a clock whose pendulum still swings counts.
        ClockIcon(px, W, H, 120f, 422f, 44f, 40f, 200f, true, ink);
        Tick(px, W, H, 360f, 422f, 42f, ink);

        // Rule two: a dead one does not, however loudly it shows a number.
        ClockIcon(px, W, H, 120f, 252f, 44f, 150f, 300f, false, ink);
        Cross(px, W, H, 360f, 252f, 36f, ink);

        Fill(px, W, H, 70f, 120f, W - 140f, 3f, faint);

        // Then: read the survivors in the order their dials point to. Bare dials here —
        // drawing pendulums on them too would look like a third rule.
        Dial(px, W, H, 96f, 64f, 30f, 60f, 90f, ink);
        Arrow(px, W, H, 146f, 64f, 246f, 64f, ink);
        Dial(px, W, H, 296f, 64f, 30f, 300f, 180f, ink);

        for (int i = 0; i < 4; i++)
            Frame(px, W, H, 366f + i * 30f, 42f, 24f, 44f, 3f, ink);

        WritePng(px, W, H, "NotePage.png");
    }

    private static void Dial(Color[] px, int w, int h, float cx, float cy, float radius,
                             float hourDegrees, float minuteDegrees, Color ink)
    {
        RingAt(px, w, h, cx, cy, radius - 5f, radius, ink);
        Hand(px, w, h, cx, cy, hourDegrees, radius * 0.52f, 6f, ink);
        Hand(px, w, h, cx, cy, minuteDegrees, radius * 0.78f, 4f, ink);
        DiscAt(px, w, h, cx, cy, 5f, ink);
    }

    // The two pieces missing from the longcase clock in the bedroom. Small enough to carry,
    // and shaped so it is obvious at a glance where each one belongs.
    [MenuItem("Tools/Escape Room/Puzzle/Build Clock Parts")]
    public static void BuildClockParts()
    {
        EnsureFolder(PrefabFolder);
        Material brass = Brass();

        var bob = new GameObject("PendulumBobProp");
        BuildKit.Disc("Bob", bob.transform, Vector3.zero, 0.115f, 0.014f, brass);
        BuildKit.Box("Rod", bob.transform, new Vector3(0f, 0.10f, 0f), new Vector3(0.008f, 0.20f, 0.008f), brass);
        BuildKit.Box("Hook", bob.transform, new Vector3(0f, 0.205f, 0f), new Vector3(0.020f, 0.016f, 0.010f), brass);
        BuildKit.Hull(bob, new Vector3(0f, 0.08f, 0f), new Vector3(0.14f, 0.30f, 0.06f));
        FitPickup(bob, "PendulumBob", new Vector3(0f, 180f, 0f));
        SavePart(bob, "PendulumBobProp");

        var weights = new GameObject("ClockWeightsProp");
        foreach (int side in new[] { -1, 1 })
        {
            BuildKit.Cylinder("Weight", weights.transform, new Vector3(side * 0.045f, 0f, 0f), 0.052f, 0.20f, brass);
            BuildKit.Box("Cap", weights.transform, new Vector3(side * 0.045f, 0.105f, 0f),
                new Vector3(0.020f, 0.012f, 0.020f), brass);
        }
        BuildKit.Hull(weights, Vector3.zero, new Vector3(0.16f, 0.26f, 0.08f));
        FitPickup(weights, "ClockWeights", new Vector3(0f, 180f, 0f));
        SavePart(weights, "ClockWeightsProp");

        AssetDatabase.SaveAssets();
        Debug.Log("Clock parts rebuilt: pendulum bob and weights.");
    }

    // Arthur's bench standard: a ten-second glass, the thing a clockmaker checks a movement
    // against when the movement is the only other clock in the room.
    //
    // It is built as a prop and nothing else. It keeps no time, counts nothing and is wired
    // to no puzzle — what it measures, and what that opens, is Act II's business and is not
    // decided here.
    // Arthur's bench standard: a ten-second glass, the thing a clockmaker checks a movement
    // against when the movement is the only other clock in the room.
    //
    // The glass is the whole shape — no cage, no posts — and it is **turned** rather than
    // built up out of primitives. A stack of cylinders shows a step at every joint down the
    // silhouette, and on this prop the silhouette is the object.
    [MenuItem("Tools/Escape Room/Puzzle/Build Sandglass")]
    public static GameObject BuildSandglass()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(MeshFolder);

        Material wood = BuildKit.Opaque("SandglassAsh", new Color(0.80f, 0.68f, 0.52f), 0.25f, 0f,
            AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/OakGrain.png"));
        Material glass = SandglassGlass();
        Material sand = BuildKit.Opaque("Sand", new Color(0.26f, 0.23f, 0.20f), 0.32f, 0f, null);

        // 18 cm from foot to cap, standing on its own base at y = 0 the way anything set
        // down on a table has to.
        const float Height = 0.180f;
        const float CapDiameter = 0.140f;
        const float CapDepth = 0.018f;
        const float Waist = 0.090f;
        const float Neck = 0.0085f;
        const float Rim = 0.050f;
        const float PostReach = 0.060f;   // just clear of the widest part of the glass
        const float Skin = 0.005f;   // how far inside the glass the sand is drawn

        const float Floor = CapDepth;
        const float Ceiling = Height - CapDepth;
        const float Reach = Waist - Floor;

        var root = new GameObject("Sandglass");

        // Everything turns except the root: the hull, the crosshair's target, stays where the
        // glass stands while the glass itself comes over.
        var frame = new GameObject("Frame").transform;
        frame.SetParent(root.transform, false);

        // Cylinder, not Disc: a Disc is stood on its Z axis for a clock face, and a cap
        // built that way is a coin balanced on its edge.
        BuildKit.Cylinder("Base", frame, new Vector3(0f, CapDepth * 0.5f, 0f), CapDiameter, CapDepth, wood);
        BuildKit.Cylinder("Cap", frame, new Vector3(0f, Height - CapDepth * 0.5f, 0f), CapDiameter, CapDepth, wood);

        Mesh body = Lathe("SandglassGlass", Profile(Floor, Ceiling, Waist, Reach, Neck, Rim, 0f));
        Shape("Glass", frame, new Vector3(0f, Floor, 0f), Quaternion.identity, body, glass);

        // Two turned posts, one either side, the way the old glasses were framed. They map
        // onto each other under the half turn — and each is mirrored about the waist — so
        // the flip still ends where it started.
        foreach (int side in new[] { -1, 1 })
        {
            var post = new GameObject("Post").transform;
            post.SetParent(frame, false);
            post.localPosition = new Vector3(side * PostReach, Waist, 0f);

            foreach (int end in new[] { -1, 1 })
            {
                float at = end * 0.006f;
                BuildKit.Cylinder("Shaft", post, new Vector3(0f, at + end * 0.007f, 0f), 0.009f, 0.026f, wood);
                BuildKit.Sphere("Bead", post, new Vector3(0f, at + end * 0.022f, 0f), 0.016f, wood);
                BuildKit.Cylinder("Shaft", post, new Vector3(0f, at + end * 0.040f, 0f), 0.011f, 0.024f, wood);
                BuildKit.Sphere("Bead", post, new Vector3(0f, at + end * 0.056f, 0f), 0.020f, wood);
                BuildKit.Cylinder("Collar", post, new Vector3(0f, at + end * 0.066f, 0f), 0.014f, 0.012f, wood);
            }
        }

        // Two empty bodies of sand, one per bulb. `Sandglass` turns them itself, every frame
        // the level moves, so the surface follows the glass exactly and slides rather than
        // stepping — which is what all of this is for.
        GameObject lowerSand = Shape("SandLower", frame, new Vector3(0f, Floor + 0.002f, 0f),
            Quaternion.identity, null, sand);

        GameObject upperSand = Shape("SandUpper", frame, new Vector3(0f, Waist + 0.002f, 0f),
            Quaternion.identity, null, sand);

        // What falls through the neck is grains, not a rod of sand: a dozen of them on a
        // loop, which costs less than the cylinder it replaces and is what a running glass
        // actually looks like. The scatter is authored here, so every grain keeps its own
        // line down and `Sandglass` only has to move them.
        var grains = new GameObject("Grains").transform;
        grains.SetParent(frame, false);
        var scatter = new System.Random(10);

        for (int i = 0; i < 14; i++)
        {
            float angle = (float)scatter.NextDouble() * Mathf.PI * 2f;
            float spread = Mathf.Sqrt((float)scatter.NextDouble()) * 0.0032f;
            var at = new Vector3(Mathf.Sin(angle) * spread, Waist, Mathf.Cos(angle) * spread);
            BuildKit.Box("Grain", grains, at, Vector3.one * 0.0022f, sand);
        }

        // The frame is pivoted at the waist, not at the foot. Everything above was laid out
        // from the base, which is how a thing that stands on a table is measured — but a
        // glass turned about its foot swings round in an arc like a signpost, instead of
        // coming over in the hand. Shifting it here keeps both: the numbers stay measured
        // from the base and the turn happens about the middle.
        frame.localPosition = new Vector3(0f, Waist, 0f);

        foreach (Transform part in frame)
            part.localPosition -= new Vector3(0f, Waist, 0f);

        // Except the grains: `Sandglass` drops them down the frame's own axis, so their
        // holder has to sit on it.
        grains.localPosition = Vector3.zero;

        BuildKit.Hull(root, new Vector3(0f, Height * 0.5f, 0f), new Vector3(CapDiameter, Height, CapDiameter));

        var glassPiece = root.AddComponent<Sandglass>();
        Wire(glassPiece, "frame", frame);
        Wire(glassPiece, "upperSand", upperSand.GetComponent<MeshFilter>());
        Wire(glassPiece, "lowerSand", lowerSand.GetComponent<MeshFilter>());
        Wire(glassPiece, "grains", grains);

        // The shape of one bulb's inside, handed over as data. The glass is turned from the
        // same curve a line above, so the sand cannot drift out of step with the vessel it
        // is supposed to be sitting in.
        var so = new SerializedObject(glassPiece);
        SerializedProperty shape = so.FindProperty("bulb");
        Vector2[] inside = Profile(Floor + 0.002f, Waist - 0.002f, Waist, Reach, Neck, Rim, Skin);
        shape.arraySize = inside.Length;

        for (int i = 0; i < inside.Length; i++)
            shape.GetArrayElementAtIndex(i).vector2Value = inside[i];

        so.ApplyModifiedPropertiesWithoutUndo();

        // It goes in the bag, because the glass has to be somewhere the player can hold it
        // up beside a watch on the far side of the room. Turning it is `HeldSandglass`'s
        // business by then, which is why nothing here answers E.
        FitPickup(root, "Sandglass", new Vector3(0f, 180f, 0f));

        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/Sandglass.prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("Sandglass rebuilt.");
        return asset;
    }

    // The outline, sampled from one end to the other, as radius against height. The pinch
    // is a power curve: most of the width is picked up in the first third off the neck and
    // the rest runs nearly straight, which is what reads as an hourglass rather than as two
    // balls stuck together.
    //
    // Points come back with y measured from `fromY`, so a mesh turned from them stands on
    // its own origin and can be scaled about it.
    private static Vector2[] Profile(float fromY, float toY, float waist, float reach,
                                     float neck, float rim, float inset)
    {
        const int Samples = 44;
        var points = new Vector2[Samples + 1];

        for (int i = 0; i <= Samples; i++)
        {
            float y = Mathf.Lerp(fromY, toY, i / (float)Samples);
            points[i] = new Vector2(Flare(y, waist, reach, neck, rim) - inset, y - fromY);
        }

        return points;
    }

    // How wide the glass is at a given height: fast off the neck, then nearly straight. The
    // power is the whole of the shape — an hourglass rather than two balls on a stick.
    private static float Flare(float y, float waist, float reach, float neck, float rim)
    {
        float t = Mathf.Abs(y - waist) / reach;

        // The power is the pinch; the second term pulls the very top back in, so the bulb
        // has a shoulder and meets the disc rather than flaring into it.
        float radius = (neck + (rim - neck) * Mathf.Pow(t, 0.42f)) * (1f - 0.10f * Mathf.Pow(t, 8f));

        return Mathf.Max(radius, 0.0006f);
    }

    // Turns a profile about Y into a mesh asset. Copied over the old one rather than written
    // fresh, so prefabs pointing at it keep pointing at it.
    private static Mesh Lathe(string name, Vector2[] profile)
    {
        const int Sides = 40;

        int rings = profile.Length;
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s <= Sides; s++)
            {
                float angle = s / (float)Sides * Mathf.PI * 2f;
                vertices.Add(new Vector3(Mathf.Sin(angle) * profile[r].x, profile[r].y, Mathf.Cos(angle) * profile[r].x));
            }
        }

        int stride = Sides + 1;

        for (int r = 0; r < rings - 1; r++)
        {
            for (int s = 0; s < Sides; s++)
            {
                int a = r * stride + s;
                int b = a + 1;
                int c = a + stride;
                int d = c + 1;

                // Rings run from +Z towards +X, which is clockwise seen from above, so this
                // is the winding that faces out.
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(b); triangles.Add(d); triangles.Add(c);
            }
        }

        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        string path = MeshFolder + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        EditorUtility.CopySerialized(mesh, existing);
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(mesh);
        return existing;
    }

    // The compass rose struck into the coin on the showroom table.
    //
    // The first mark on it was a plain black arrow, and it read as a sticker: near-black on
    // gold is the hardest contrast in the house, and nothing else here is marked that way. A
    // rose says the same thing in the shop's own metal — eight points, each split into a pale
    // half and a dark one, with the bearing's point longer than the rest and a gold boss at
    // the middle. Struck rather than printed, which is what a coin is.
    //
    // **It rebuilds the face and nothing else.** The coin's root, its collider, its
    // Inspectable and the bearing on the needle are all left where they were found, so this
    // can be run again after the bearing has been pointed at a different wall.
    [MenuItem("Tools/Escape Room/Puzzle/Build Coin Face")]
    public static void BuildCoinFace()
    {
        EnsureFolder(MeshFolder);

        Transform coin = GameObject.Find("House/Props/Room_I_Showroom/Table/RustyCoin").transform;
        Transform was = coin.Find("Needle");

        // Lifted off before anything is destroyed: which wall this points at is a decision
        // about the room, not about the pattern.
        Vector3 bearing = Vector3.right;
        if (was != null)
        {
            bearing = new SerializedObject(was.GetComponent<CompassNeedle>())
                .FindProperty("pointsTowards").vector3Value;
            Undo.DestroyObjectImmediate(was.gameObject);
        }

        // Two metals rather than one, because the halves are what make a rose read as struck
        // rather than drawn. Both are gold: the pale one catches the light the dark one has
        // turned away from, which is the whole trick and needs no texture to do it.
        // Only half metal. A fully metallic surface has no colour of its own — everything it
        // shows is reflection — and with no probe in this room the rose came out grey tin on
        // a gold coin. At 0.5 the gold in the base colour still carries.
        Material pale = BuildKit.Opaque("CoinRosePale", new Color(1.00f, 0.84f, 0.46f), 0.66f, 0.50f, null);
        Material deep = BuildKit.Opaque("CoinRoseDeep", new Color(0.46f, 0.31f, 0.11f), 0.30f, 0.45f, null);

        // The bearing's own point is the long one. The other three cardinals are shorter and
        // the four diagonals shorter again, which is what stops eight equal spikes reading as
        // a star and nothing more.
        var tips = new[] { 0.0340f, 0.0155f, 0.0250f, 0.0155f, 0.0250f, 0.0155f, 0.0250f, 0.0155f };
        const float Waist = 0.0062f;

        var needle = new GameObject("Needle");
        Undo.RegisterCreatedObjectUndo(needle, "coin face");
        needle.transform.SetParent(coin, false);
        needle.transform.localPosition = new Vector3(0f, 0.0068f, 0f);
        needle.layer = coin.gameObject.layer;

        Shape("Pale", needle.transform, Vector3.zero, Quaternion.identity,
              RoseHalf("CoinRosePale", tips, Waist, true), pale).layer = coin.gameObject.layer;
        Shape("Deep", needle.transform, Vector3.zero, Quaternion.identity,
              RoseHalf("CoinRoseDeep", tips, Waist, false), deep).layer = coin.gameObject.layer;

        // The boss at the middle, which is the one part of the face that is plain gold.
        BuildKit.Cylinder("Boss", needle.transform, new Vector3(0f, 0.0009f, 0f), 0.0135f, 0.0022f, pale)
            .layer = coin.gameObject.layer;

        // A moon over the side the coin is *not* pointing at, so which of the eight points
        // carries the bearing stops being a matter of measuring them: the long one is the one
        // in the clear. It sits behind the rose's centre, on -z, with its horns opening
        // towards the point that matters.
        Shape("Moon", needle.transform, new Vector3(0f, 0.0006f, 0f), Quaternion.identity,
              Crescent("CoinMoon", new Vector3(0f, 0f, -0.0175f), 0.0125f, 0.0105f, 0.0042f), deep)
            .layer = coin.gameObject.layer;

        BuildKit.Wire(Undo.AddComponent<CompassNeedle>(needle), "pointsTowards", bearing);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(coin.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Coin face rebuilt. It still points " + bearing.ToString("F2") + ".");
    }

    // Half a compass rose: the triangle on one side of each point, all eight in one mesh.
    // Two of these wound the same way and drawn in two metals are the whole rose.
    private static Mesh RoseHalf(string name, float[] tips, float waist, bool leading)
    {
        float step = 360f / tips.Length;
        var vertices = new System.Collections.Generic.List<Vector3> { Vector3.zero };
        var triangles = new System.Collections.Generic.List<int>();

        for (int point = 0; point < tips.Length; point++)
        {
            float toTip = point * step;
            float toWaist = toTip + (leading ? step * 0.5f : -step * 0.5f);

            vertices.Add(Flat(toTip, tips[point]));
            vertices.Add(Flat(toWaist, waist));

            int tip = vertices.Count - 2, edge = vertices.Count - 1;

            // Wound to face up: the centre, then the vertex at the *lesser* angle, then the
            // greater. Rings here run from +z towards +x, and the other order gives a rose
            // you can only see from underneath the table — which is what it did.
            triangles.Add(0);
            triangles.Add(leading ? tip : edge);
            triangles.Add(leading ? edge : tip);
        }

        return Save(name, vertices, triangles);
    }

    // A crescent: one disc with a second one taken out of it, offset by `bite` along +z. The
    // strip runs between the two rims and stops where the bite has eaten through, which is
    // where the horns come to their points on their own.
    private static Mesh Crescent(string name, Vector3 at, float outer, float inner, float bite)
    {
        const int Steps = 26;

        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        float horn = Mathf.Acos(Mathf.Clamp((outer - inner) / bite, -1f, 1f)) * Mathf.Rad2Deg;

        for (int i = 0; i <= Steps; i++)
        {
            float a = Mathf.Lerp(horn, 360f - horn, i / (float)Steps) * Mathf.Deg2Rad;
            var along = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));

            vertices.Add(at + along * outer);
            vertices.Add(at + new Vector3(0f, 0f, bite) + along * inner);
        }

        for (int i = 0; i < Steps; i++)
        {
            int rim = i * 2, next = rim + 2;
            triangles.Add(rim); triangles.Add(next); triangles.Add(rim + 1);
            triangles.Add(rim + 1); triangles.Add(next); triangles.Add(next + 1);
        }

        return Save(name, vertices, triangles);
    }

    private static Vector3 Flat(float degrees, float radius) =>
        new Vector3(Mathf.Sin(degrees * Mathf.Deg2Rad) * radius, 0f, Mathf.Cos(degrees * Mathf.Deg2Rad) * radius);

    private static Mesh Save(string name, System.Collections.Generic.List<Vector3> vertices,
                             System.Collections.Generic.List<int> triangles)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Written out fresh rather than copied over whatever is already there.
        // `EditorUtility.CopySerialized` updates the file but leaves the mesh that is already
        // loaded holding its old buffers, and a rose copied over that way drew nothing at all
        // — a renderer reporting `isVisible`, a valid submesh, twenty-four indices, and not
        // one pixel. Everything that points at these is rebuilt in the same pass, so there is
        // no reference for the delete to break.
        string path = MeshFolder + "/" + name + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static GameObject Shape(string name, Transform parent, Vector3 localPos,
                                    Quaternion localRot, Mesh mesh, Material material)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    // Act V: Arthur's last machine. A gnomon carrying three prisms, a lens at the front of
    // it, and a screen on the wall with six marks on it.
    //
    // The two halves are built as separate prefabs because they stand three metres apart,
    // and what joins them — which light belongs on which mark — is wired in the scene.
    [MenuItem("Tools/Escape Room/Puzzle/Build Horologium")]
    public static void BuildHorologium()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(MeshFolder);

        Material stone = BuildKit.Opaque("SundialStone", new Color(0.46f, 0.45f, 0.43f), 0.15f, 0f,
            AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/HearthStone.png"));
        Material iron = BuildKit.Opaque("SundialIron", new Color(0.17f, 0.16f, 0.15f), 0.45f, 0.75f, null);
        Material brass = Brass();
        Material glass = SandglassGlass();

        Material star = Glow("StarLight", new Color(0.98f, 0.95f, 0.84f), 2.4f);
        Material beamMat = Glow("StarBeam", new Color(0.80f, 0.86f, 0.98f), 0.6f);
        Material screenMat = BuildKit.Opaque("StarScreen", new Color(0.09f, 0.10f, 0.13f), 0.05f, 0f, null);

        // The marks carry a little light of their own. They are the target, and a target
        // painted dark grey on a dark screen in a room lit by one moon is a target nobody
        // can see: the player was being asked to land six points on six things they had to
        // guess the position of.
        Material markMat = Glow("StarMark", new Color(0.42f, 0.50f, 0.62f), 0.35f);

        // Three stones cut from the same night: deep blue, near solid, with a scatter of
        // stars caught inside each. Nearly opaque rather than glassy — what a prism does to
        // the light is the puzzle's business, and a clear one only ever read as a window.
        // They are three shades of the one colour, so they still tell apart on the gnomon
        // without any of them being the odd one out.
        Material[] gems =
        {
            Gem("PrismGemThree", new Color(0.018f, 0.030f, 0.130f)),
            Gem("PrismGemFour", new Color(0.014f, 0.052f, 0.140f)),
            Gem("PrismGemSix", new Color(0.044f, 0.024f, 0.145f))
        };

        // The stars inside them. One material, because a star is a star.
        Material spark = Glow("PrismStar", new Color(0.92f, 0.95f, 1f), 3.4f);

        // ---------- the sundial ----------

        // An armillary: a stand of wrought legs, a pair of standing rings, an hour band lying
        // square to the axis, and the axis itself through the middle of all of it. The three
        // prisms ride that axis, which is why they are at the heart of the thing rather than
        // stacked up a post.
        // The centre of the rings, off the floor. It is eye height on purpose: the three
        // prisms ride the axis through this point, and at chest height the player had to
        // stand back and look down 30 degrees to put the crosshair on the lowest of them.
        const float Heart = 1.44f;
        const float Ring = 0.52f;

        // The dome, declared here because the moonbeam has to reach it. It is the roof of
        // the room: it stands on the top of the walls, and the moon rides inside it.
        const float DomeFoot = 2.80f;
        const float DomeRadius = 5.10f;
        const float DomeRise = 3.30f;
        const float Tilt = -38f;       // the axis, leaning the way a gnomon leans
        var dial = new GameObject("Sundial");

        BuildKit.Cylinder("Slab", dial.transform, new Vector3(0f, 0.05f, 0f), 0.66f, 0.10f, stone);

        // Four legs curving in from the slab to a collar under the rings.
        for (int i = 0; i < 4; i++)
        {
            var leg = new GameObject("Leg").transform;
            leg.SetParent(dial.transform, false);
            leg.localRotation = Quaternion.Euler(0f, i * 90f + 45f, 0f);

            for (int s = 0; s < 6; s++)
            {
                float t0 = s / 6f, t1 = (s + 1) / 6f;
                Rod("Bend", leg, Leg(t0), Leg(t1), 0.022f, iron);
            }
        }

        BuildKit.Cylinder("Collar", dial.transform, new Vector3(0f, Heart - Ring - 0.02f, 0f), 0.11f, 0.05f, iron);

        // Two standing rings crossing at right angles, and the hour band square to the axis.
        Hoop("Meridian", dial.transform, new Vector3(0f, Heart, 0f), Quaternion.Euler(0f, 90f, 0f),
            Ring, 0f, 360f, 48, 0.024f, iron);

        Hoop("Colure", dial.transform, new Vector3(0f, Heart, 0f), Quaternion.identity,
            Ring, 0f, 360f, 48, 0.024f, iron);

        Quaternion square = Quaternion.Euler(Tilt, 0f, 0f) * Quaternion.Euler(90f, 0f, 0f);
        Band("HourBand", dial.transform, new Vector3(0f, Heart, 0f), square, 0.40f, 0.15f, -110f, 110f, 28, brass);

        // The hours, cut into the band the way they are on Arthur's own.
        for (int i = 0; i <= 8; i++)
        {
            float at = Mathf.Lerp(-100f, 100f, i / 8f);
            Band("Hour", dial.transform, new Vector3(0f, Heart, 0f), square, 0.40f, 0.012f, at - 0.8f, at + 0.8f, 1, iron);
        }

        // The axis, out both ends the way a gnomon runs through an armillary.
        var spindle = new GameObject("Spindle").transform;
        spindle.SetParent(dial.transform, false);
        spindle.localPosition = new Vector3(0f, Heart, 0f);
        spindle.localRotation = Quaternion.Euler(Tilt, 0f, 0f);
        BuildKit.Cylinder("Rod", spindle, Vector3.zero, 0.022f, 1.44f, brass);

        // Solid at the foot only. A hull round the whole machine is a hull round the three
        // sockets inside it, and the crosshair would find the armillary every time and the
        // prism sockets never — which is a puzzle whose pieces cannot be fitted.
        var slabHit = dial.AddComponent<BoxCollider>();
        slabHit.center = new Vector3(0f, 0.06f, 0f);
        slabHit.size = new Vector3(0.66f, 0.12f, 0.66f);

        var stemHit = dial.AddComponent<BoxCollider>();
        stemHit.center = new Vector3(0f, 0.42f, 0f);
        stemHit.size = new Vector3(0.20f, 0.72f, 0.20f);

        int[] faces = { 3, 4, 6 };
        string[] named = { "Three", "Four", "Six" };

        // A hand's width apart up the axis. At 0.20 the three hit boxes were close enough
        // that the fringe of one lay over the next, and which prism a press reached
        // depended on a degree of aim.
        float[] station = { -0.27f, 0f, 0.27f };
        var fitted = new GameObject[faces.Length];

        for (int i = 0; i < faces.Length; i++)
        {
            var socket = new GameObject("Socket" + named[i]);
            socket.transform.SetParent(spindle, false);
            socket.transform.localPosition = new Vector3(0f, station[i], 0f);

            BuildKit.Cylinder("Seat", socket.transform, new Vector3(0f, -0.062f, 0f), 0.075f, 0.016f, brass);

            // The seat is what the crosshair finds while the socket is empty, and it is kept
            // down at the seat on purpose: a socket collider big enough to cover the prism is
            // a socket that answers instead of the prism for ever after, and the player is
            // left holding three prisms they can fit and never turn.
            var hit = socket.AddComponent<BoxCollider>();
            hit.center = new Vector3(0f, -0.062f, 0f);
            hit.size = new Vector3(0.16f, 0.05f, 0.16f);

            // The prism itself, switched off until the player fits it — collider and all,
            // which is what makes it the thing under the crosshair from then on.
            Mesh cut = Facets("Prism" + named[i], faces[i], 0.086f, 0.106f);
            GameObject prism = Shape("Prism" + named[i], socket.transform, new Vector3(0f, -0.053f, 0f),
                Quaternion.identity, cut, gems[i]);
            Sparks(prism.transform, faces[i], 0.086f, 0.106f, spark, 7000 + faces[i]);

            // Generous, and deliberately larger than the seat under it: the fringe of a
            // press belongs to the prism rather than to a socket whose only line, once it
            // is full, is that it is full.
            var body = prism.AddComponent<BoxCollider>();
            body.center = new Vector3(0f, 0.053f, 0f);
            body.size = new Vector3(0.24f, 0.15f, 0.24f);

            var turning = prism.AddComponent<Prism>();
            Wire(turning, "faces", faces[i]);

            EnsureItem("Prism" + named[i], "ปริซึม " + faces[i] + " เหลี่ยม");
            var slot = socket.AddComponent<ClockPartSocket>();
            Wire(slot, "part", AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Scenes/Items/Prism" + named[i] + ".asset"));
            Wire(slot, "emptyPrompt", "[E] ช่องปริซึม " + faces[i] + " เหลี่ยม — ลองใช้ของ");
            Wire(slot, "fittedPrompt", "[E] ใส่แล้ว");

            var slotSo = new SerializedObject(slot);
            SerializedProperty pieces = slotSo.FindProperty("fitted");
            pieces.arraySize = 1;
            pieces.GetArrayElementAtIndex(0).objectReferenceValue = prism;
            slotSo.ApplyModifiedPropertiesWithoutUndo();

            fitted[i] = prism;
        }

        // The lens on the front of the ring, and the beam it throws at the wall. Both point
        // down the dial's own -Z, so aiming the machine is a matter of turning the whole
        // thing. The moonbeam comes the other way — straight down out of the dome.
        var lens = new GameObject("Lens");
        lens.transform.SetParent(dial.transform, false);
        lens.transform.localPosition = new Vector3(0f, Heart, -Ring - 0.03f);
        BuildKit.Disc("Mount", lens.transform, Vector3.zero, 0.16f, 0.022f, brass);
        BuildKit.Disc("Glass", lens.transform, new Vector3(0f, 0f, -0.008f), 0.13f, 0.008f, glass);

        var beams = new GameObject("Beams");
        beams.transform.SetParent(dial.transform, false);

        // Long enough to reach the wall. It stopped two thirds of the way there, which reads
        // as a machine throwing light at nothing in particular.
        var beam = BuildKit.Cylinder("Beam", beams.transform, new Vector3(0f, Heart, -Ring - 1.58f), 0.022f, 3.10f, beamMat);
        beam.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // Down from the moon to the top of the rings, and no further. Carried on into the
        // middle of the machine it stands in front of the prisms — a bright column through
        // the three things the player is there to see and press.
        const float BeamFoot = Heart + Ring + 0.04f;
        const float BeamHead = DomeFoot + DomeRise * 0.80f - 0.10f;
        BuildKit.Cylinder("Moonbeam", beams.transform, new Vector3(0f, (BeamFoot + BeamHead) * 0.5f, 0f),
            0.04f, BeamHead - BeamFoot, beamMat);

        var rig = dial.AddComponent<Horologium>();
        var rigSo = new SerializedObject(rig);
        SerializedProperty list = rigSo.FindProperty("prisms");
        list.arraySize = fitted.Length;
        for (int i = 0; i < fitted.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = fitted[i].GetComponent<Prism>();
        rigSo.FindProperty("beam").objectReferenceValue = beams;
        rigSo.FindProperty("slide").floatValue = 0.055f;
        rigSo.FindProperty("spread").floatValue = 0.10f;
        rigSo.FindProperty("tolerance").floatValue = 0.018f;
        rigSo.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(dial, PrefabFolder + "/Sundial.prefab");
        Object.DestroyImmediate(dial);

        // ---------- the screen ----------

        Vector2[] shape = Stars();

        var screen = new GameObject("StarScreen");
        Box("Panel", screen.transform, new Vector3(0f, 0f, 0.01f), new Vector3(1.10f, 0.96f, 0.02f), screenMat, false);

        for (int i = 0; i < shape.Length; i++)
        {
            var mark = new GameObject("Mark" + (i + 1));
            mark.transform.SetParent(screen.transform, false);
            mark.transform.localPosition = new Vector3(shape[i].x, shape[i].y, 0f);
            BuildKit.Disc("Ring", mark.transform, Vector3.zero, 0.058f, 0.004f, markMat);

            var light = new GameObject("Light" + (i + 1));
            light.transform.SetParent(screen.transform, false);
            light.transform.localPosition = new Vector3(shape[i].x, shape[i].y, -0.004f);
            BuildKit.Disc("Spot", light.transform, Vector3.zero, 0.034f, 0.004f, star);
        }

        PrefabUtility.SaveAsPrefabAsset(screen, PrefabFolder + "/StarScreen.prefab");
        Object.DestroyImmediate(screen);

        // ---------- the chart ----------

        // Arthur's own drawing of the constellation, framed on the wall beside the screen.
        // The six marks are the answer, but a ring of dark paint on a dark screen states it
        // in a whisper: the chart says the same thing out loud, with the stars joined up in
        // the order they are joined up in a sky atlas.
        //
        // It is drawn from the same array the marks are, so the answer on the wall cannot
        // drift away from the answer on the screen. That is the whole reason `Stars` exists.
        Material chartInk = Glow("StarChartInk", new Color(0.96f, 0.94f, 0.86f), 0.9f);
        Material chartLine = BuildKit.Opaque("StarChartLine", new Color(0.46f, 0.52f, 0.64f), 0.3f, 0.2f, null);
        Material chartPlate = BuildKit.Opaque("StarChartPlate", new Color(0.10f, 0.11f, 0.16f), 0.1f, 0f, null);

        const float Chart = 1.35f;    // the shape is a hand across; the chart is drawn larger

        var atlas = new GameObject("StarChart");
        Box("Frame", atlas.transform, new Vector3(0f, 0f, 0.012f), new Vector3(0.70f, 0.62f, 0.024f), brass, false);
        Box("Plate", atlas.transform, new Vector3(0f, 0f, -0.004f), new Vector3(0.64f, 0.56f, 0.014f), chartPlate, false);

        var drawing = new GameObject("Drawing").transform;
        drawing.SetParent(atlas.transform, false);
        drawing.localPosition = new Vector3(0f, -0.03f, -0.014f);

        for (int i = 0; i < shape.Length; i++)
        {
            if (i > 0)
                Rod("Join", drawing, shape[i - 1] * Chart, shape[i] * Chart, 0.006f, chartLine);

            // Brighter for the stars a real chart would print bigger. The two ends of the
            // hook are what the eye lands on first, and that is how the shape is recognised.
            float size = i == 0 || i == shape.Length - 1 ? 0.030f : 0.022f;
            BuildKit.Disc("Star" + (i + 1), drawing, new Vector3(shape[i].x * Chart, shape[i].y * Chart, -0.004f),
                size, 0.004f, chartInk);
        }

        // A row of holes down the side, the way a chart torn from a bound atlas has.
        for (int i = 0; i < 5; i++)
            BuildKit.Disc("Punch", atlas.transform, new Vector3(-0.295f, 0.20f - i * 0.10f, -0.010f), 0.006f, 0.004f, chartLine);

        BuildKit.Hull(atlas, new Vector3(0f, 0f, 0.01f), new Vector3(0.70f, 0.62f, 0.05f));

        // Fixed to a wall, so it is looked at where it hangs rather than lifted off it —
        // and what it says in words comes up on the HUD, the same way the shop's leaflet does.
        var reading = atlas.AddComponent<Inspectable>();
        Wire(reading, "style", (int)InspectStyle.Zoom);
        Wire(reading, "facing", new Vector3(0f, 180f, 0f));

        var written = atlas.AddComponent<WrittenPage>();
        Wire(written, "heading", "กลุ่มดาวโฮโรโลจิอุม — นาฬิกาลูกตุ้ม");
        Wire(written, "body",
            "หกดวง เรียงเป็นตะขอยาว นี่คือรูปที่ถูกต้อง\n" +
            "แสงจันทร์ผ่านปริซึมทั้งสามแล้วตกลงบนฉาก\n" +
            "หมุนปริซึมจนจุดแสงทั้งหกทับรอยทั้งหกบนฉาก\n" +
            "แล้วค้างไว้ กลไกจะล็อกตัวมันเอง\n\n" +
            "ปริซึมแต่ละแท่งทำอย่างเดียว หมุนแล้วดูที่ฉากก็รู้ว่าอันไหนทำอะไร");

        PrefabUtility.SaveAsPrefabAsset(atlas, PrefabFolder + "/StarChart.prefab");
        Object.DestroyImmediate(atlas);

        // ---------- the dome ----------

        // Where the moonlight comes in. Drawn on both faces, because a glass dome is a thing
        // you stand under and look up through: cull the far side and there is nothing there.
        Material domeGlass = BuildKit.Glass("MoonDomeGlass", new Color(0.74f, 0.82f, 0.94f, 0.16f));
        domeGlass.SetFloat("_Cull", 0f);
        EditorUtility.SetDirty(domeGlass);

        // Roof-sized: it is not a lantern hung from a ceiling, it is what the last room has
        // instead of one. Springs from the top of the walls and covers the floor.
        var quarter = new Vector2[25];

        for (int i = 0; i < quarter.Length; i++)
        {
            float a = i / (float)(quarter.Length - 1) * Mathf.PI * 0.5f;
            quarter[i] = new Vector2(Mathf.Cos(a) * DomeRadius, Mathf.Sin(a) * DomeRise);
        }

        var dome = new GameObject("MoonDome");
        Shape("Shell", dome.transform, Vector3.zero, Quaternion.identity, Lathe("MoonDome", quarter), domeGlass);
        Hoop("Rim", dome.transform, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), DomeRadius, 0f, 360f, 64, 0.09f, brass);

        // The ribs follow the same curve as the glass: circles squashed to the dome's rise.
        for (int i = 0; i < 6; i++)
        {
            Transform rib = Hoop("Rib", dome.transform, Vector3.zero, Quaternion.Euler(0f, i * 30f, 0f),
                DomeRadius, 0f, 180f, 26, 0.055f, brass);
            rib.localScale = new Vector3(1f, DomeRise / DomeRadius, 1f);
        }

        // A hoop part way up, the way a glasshouse dome is braced.
        Hoop("Purlin", dome.transform, new Vector3(0f, DomeRise * 0.62f, 0f), Quaternion.Euler(90f, 0f, 0f),
            DomeRadius * 0.79f, 0f, 360f, 56, 0.045f, brass);

        // The moon itself, seen through it. It is a disc and not a light: what lights this
        // room is the room's own lamps, and this only has to read as where the beam is from.
        BuildKit.Disc("Moon", dome.transform, new Vector3(0f, DomeRise * 0.80f, 0f), 1.30f, 0.02f,
            Glow("MoonFace", new Color(0.90f, 0.93f, 1f), 1.6f))
            .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        PrefabUtility.SaveAsPrefabAsset(dome, PrefabFolder + "/MoonDome.prefab");
        Object.DestroyImmediate(dome);

        // ---------- the three prisms, lying about the room ----------

        for (int i = 0; i < faces.Length; i++)
        {
            var loose = new GameObject("Prism" + named[i] + "Prop");
            Mesh cut = Facets("Prism" + named[i], faces[i], 0.086f, 0.106f);

            // Facets are built up from y = 0, so the body goes at the prop's own origin and
            // the origin is the foot of it. Lifted off that, the prop floats a finger's
            // width over whatever it is stood on.
            GameObject body = Shape("Body", loose.transform, Vector3.zero, Quaternion.identity, cut, gems[i]);
            Sparks(body.transform, faces[i], 0.086f, 0.106f, spark, 7000 + faces[i]);
            BuildKit.Hull(loose, new Vector3(0f, 0.053f, 0f), new Vector3(0.20f, 0.13f, 0.20f));
            // Held cut-face towards the eye, not stood upright. How many sides a prism has is
            // the one thing the player has to read off it — which socket it will go in — and
            // an upright prism shows its sides edge-on and its face not at all. Tipped 62
            // degrees the face is most of what you see, and the twenty off the half turn
            // leaves one side face showing under it, so it reads as a solid and not a card.
            FitPickup(loose, "Prism" + named[i], new Vector3(62f, 200f, 0f));
            PrefabUtility.SaveAsPrefabAsset(loose, PrefabFolder + "/Prism" + named[i] + "Prop.prefab");
            Object.DestroyImmediate(loose);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Horologium rebuilt: sundial, screen and three prisms.");
    }

    // One leg of the stand, as a curve from the edge of the slab in to the collar under the
    // rings: out at the foot, upright at the top, the way wrought iron is bent.
    private static Vector3 Leg(float t)
    {
        float lift = Mathf.SmoothStep(0f, 1f, t);
        return new Vector3(Mathf.Lerp(0.30f, 0.07f, lift * lift), Mathf.Lerp(0.06f, 0.94f, lift), 0f);
    }

    // One length of iron between two points. Cylinders are built standing on their own Y, so
    // the piece has to be turned onto the line rather than placed along it.
    private static GameObject Rod(string name, Transform parent, Vector3 from, Vector3 to,
                                  float thickness, Material material)
    {
        Vector3 along = to - from;
        GameObject go = Primitive(PrimitiveType.Cylinder, name, parent, (from + to) * 0.5f, material);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, along.normalized);
        go.transform.localScale = new Vector3(thickness, along.magnitude * 0.5f, thickness);
        return go;
    }

    // A ring, or a part of one, laid out of short rods in the plane `turn` puts it in.
    private static Transform Hoop(string name, Transform parent, Vector3 centre, Quaternion turn,
                                  float radius, float fromDeg, float toDeg, int segments,
                                  float thickness, Material material)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent, false);
        group.localPosition = centre;
        group.localRotation = turn;

        for (int i = 0; i < segments; i++)
        {
            float a0 = Mathf.Lerp(fromDeg, toDeg, i / (float)segments) * Mathf.Deg2Rad;
            float a1 = Mathf.Lerp(fromDeg, toDeg, (i + 1) / (float)segments) * Mathf.Deg2Rad;
            Rod("Arc", group, new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * radius,
                new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * radius, thickness, material);
        }

        return group;
    }

    // The hour band: plates laid end to end round an arc, each turned to lie along it. Wide
    // enough to carry the hours and thin enough to read as sheet, which is what it is.
    private static Transform Band(string name, Transform parent, Vector3 centre, Quaternion turn,
                                  float radius, float width, float fromDeg, float toDeg, int segments,
                                  Material material)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent, false);
        group.localPosition = centre;
        group.localRotation = turn;

        float step = Mathf.Abs(toDeg - fromDeg) / segments * Mathf.Deg2Rad;

        for (int i = 0; i < segments; i++)
        {
            float mid = Mathf.Lerp(fromDeg, toDeg, (i + 0.5f) / segments) * Mathf.Deg2Rad;
            var at = new Vector3(Mathf.Cos(mid), Mathf.Sin(mid), 0f) * radius;
            GameObject plate = Box("Plate", group, at,
                new Vector3(radius * step * 1.06f, 0.008f, width), material, false);
            plate.transform.localRotation = Quaternion.Euler(0f, 0f, mid * Mathf.Rad2Deg + 90f);
        }

        return group;
    }

    // A prism: a polygon pulled up into a solid. Flat faces, and as many of them as the
    // puzzle says — which is also how many positions it turns to.
    private static Mesh Facets(string name, int sides, float radius, float height)
    {
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        for (int s = 0; s < sides; s++)
        {
            float a0 = s / (float)sides * Mathf.PI * 2f;
            float a1 = (s + 1) / (float)sides * Mathf.PI * 2f;
            var lo0 = new Vector3(Mathf.Sin(a0) * radius, 0f, Mathf.Cos(a0) * radius);
            var lo1 = new Vector3(Mathf.Sin(a1) * radius, 0f, Mathf.Cos(a1) * radius);
            var hi0 = lo0 + Vector3.up * height;
            var hi1 = lo1 + Vector3.up * height;

            int at = vertices.Count;
            vertices.Add(lo0); vertices.Add(lo1); vertices.Add(hi0); vertices.Add(hi1);
            triangles.Add(at); triangles.Add(at + 1); triangles.Add(at + 2);
            triangles.Add(at + 1); triangles.Add(at + 3); triangles.Add(at + 2);
        }

        AddFace(vertices, triangles, sides, radius, height, true);
        AddFace(vertices, triangles, sides, radius, 0f, false);

        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        string path = MeshFolder + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        EditorUtility.CopySerialized(mesh, existing);
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(mesh);
        return existing;
    }

    private static void AddFace(System.Collections.Generic.List<Vector3> vertices,
                                System.Collections.Generic.List<int> triangles,
                                int sides, float radius, float y, bool up)
    {
        int centre = vertices.Count;
        vertices.Add(new Vector3(0f, y, 0f));

        for (int s = 0; s <= sides; s++)
        {
            float a = s / (float)sides * Mathf.PI * 2f;
            vertices.Add(new Vector3(Mathf.Sin(a) * radius, y, Mathf.Cos(a) * radius));
        }

        for (int s = 0; s < sides; s++)
        {
            int a = centre + 1 + s;

            if (up)
            {
                triangles.Add(centre); triangles.Add(a + 1); triangles.Add(a);
            }
            else
            {
                triangles.Add(centre); triangles.Add(a); triangles.Add(a + 1);
            }
        }
    }

    // Something that reads as light in a dark room: unlit surfaces stay flat whatever is
    // near them, and emission is what makes a spot on a wall a spot of light.
    private static Material Glow(string name, Color colour, float strength)
    {
        Material mat = BuildKit.Opaque(name, colour, 0.1f, 0f, null);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetColor("_EmissionColor", colour * strength);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Cut stone with a night sky in it. Deep blue and only just translucent — enough to see
    // the stars set inside and no more — polished, and lit faintly from within so that a
    // room with one moon in it does not swallow it whole.
    private static Material Gem(string name, Color colour)
    {
        Material mat = BuildKit.Glass(name, new Color(colour.r, colour.g, colour.b, 0.94f));

        // Low. A polished face takes a broad highlight off the nearest candle, and a broad
        // highlight across a small flat face is most of that face: at 0.88 three midnight
        // stones read as steel blue from a stride away.
        mat.SetFloat("_Smoothness", 0.34f);
        mat.SetFloat("_Metallic", 0f);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        // Barely any. Enough that the stone is not a black hole in a dark room, and no more:
        // at half strength the emission lifted a midnight blue to pale lavender, which is a
        // long way from the colour asked for.
        mat.SetColor("_EmissionColor", colour * 0.22f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // The stars caught inside a prism: little bright specks, set well in from the faces so
    // they read as suspended in the stone rather than printed on it. Seeded, so a prism is
    // the same prism every rebuild, and drawn inside the inscribed circle — a three-sided
    // stone is much narrower than its own radius, and a speck placed by that radius pokes
    // out through a face.
    private static void Sparks(Transform prism, int sides, float radius, float height, Material material, int seed)
    {
        var was = Random.state;
        Random.InitState(seed);

        float inner = radius * Mathf.Cos(Mathf.PI / sides) * 0.72f;

        // Small, and round. A cube of a centimetre seen through a face at arm's length is a
        // white square with corners on it, which reads as a chip in the stone rather than as
        // a star; a handful of little spheres reads as a sky.
        for (int i = 0; i < 22; i++)
        {
            var at = new Vector3(Random.Range(-inner, inner), Random.Range(height * 0.12f, height * 0.88f),
                                 Random.Range(-inner, inner));

            if (new Vector2(at.x, at.z).magnitude > inner)
                at = new Vector3(at.x * 0.55f, at.y, at.z * 0.55f);

            BuildKit.Sphere("Star", prism, at, Random.Range(0.0026f, 0.0062f), material);
        }

        Random.state = was;
    }

    // Horologium, the pendulum clock: six stars in a long hook. This is the shape the marks
    // are cut in, the shape the chart on the wall is drawn in, and — with every prism at its
    // answer — the shape the lights land in. One array, so those three cannot disagree.
    //
    // Kept small: the spreader can open the pattern half as far again and the slider push it
    // a hand's width sideways, and all of that has to stay on a screen a metre across.
    private static Vector2[] Stars()
    {
        return new[]
        {
            new Vector2(0.19f, -0.15f), new Vector2(0.11f, -0.06f), new Vector2(0.03f, 0.01f),
            new Vector2(-0.05f, 0.07f), new Vector2(-0.12f, 0.12f), new Vector2(-0.17f, 0.21f)
        };
    }

    // The item asset is part of the prop: a PickupItem with nothing behind it puts "[E] เก็บ"
    // on the crosshair and throws when it is pressed.
    private static void EnsureItem(string file, string displayName)
    {
        string path = "Assets/Scenes/Items/" + file + ".asset";

        if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null)
            return;

        var item = ScriptableObject.CreateInstance<ItemData>();
        Wire(item, "displayName", displayName);
        AssetDatabase.CreateAsset(item, path);
    }

    private static void FitPickup(GameObject go, string itemFile, Vector3 facing)
    {
        var item = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Scenes/Items/" + itemFile + ".asset");
        Wire(go.AddComponent<PickupItem>(), "item", item);

        var inspectable = go.AddComponent<Inspectable>();
        Wire(inspectable, "style", (int)InspectStyle.Hold);
        Wire(inspectable, "facing", facing);
    }

    private static void SavePart(GameObject root, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + name + ".prefab");
        Object.DestroyImmediate(root);
    }

    // The card pinned over the workbench. Same trick as the note: it states the rule in
    // pictures, so it needs no words and no translation.
    [MenuItem("Tools/Escape Room/Puzzle/Write Bench Ticket")]
    public static void WriteTicketTexture()
    {
        const int W = 384, H = 480;
        var ink = new Color(0.17f, 0.14f, 0.11f);
        var faint = new Color(0.48f, 0.42f, 0.34f);
        var paper = new Color(0.90f, 0.86f, 0.74f);

        var px = new Color[W * H];
        for (int i = 0; i < px.Length; i++)
            px[i] = paper;

        Frame(px, W, H, 18f, 18f, W - 36f, H - 36f, 3f, faint);

        // Three that agree, and one that does not. The odd one is ringed.
        for (int i = 0; i < 3; i++)
            Dial(px, W, H, 66f + i * 78f, 372f, 30f, 222f, 150f, ink);

        Dial(px, W, H, 300f, 372f, 30f, 140f, 240f, ink);
        RingAt(px, W, H, 300f, 372f, 41f, 44f, ink);

        Fill(px, W, H, 48f, 292f, W - 96f, 3f, faint);

        // Take that one, and set the movement on the bench to it.
        Dial(px, W, H, 118f, 190f, 46f, 140f, 240f, ink);
        RingAt(px, W, H, 118f, 190f, 58f, 61f, ink);
        Arrow(px, W, H, 186f, 190f, 250f, 190f, ink);
        RingAt(px, W, H, 300f, 190f, 44f, 47f, ink);
        Line(px, W, H, 300f, 190f, 300f, 224f, 5f, ink);

        // Four ruled lines at the foot, the way a docket has somewhere to write.
        for (int i = 0; i < 4; i++)
            Fill(px, W, H, 64f, 60f + i * 22f, W - 128f, 2f, faint);

        WritePng(px, W, H, "BenchTicket.png");
    }

    // The walls of the room behind the clock. One time, written over and over by someone
    // who had stopped meaning it and could not stop doing it. It is the only place the
    // final answer is stated outright, and by then the player has walked past it four times
    // in four other rooms without noticing.
    [MenuItem("Tools/Escape Room/Puzzle/Write Wall Scrawl")]
    public static void WriteWallScrawl()
    {
        const int W = 512, H = 512;
        const int Hour = 11, Minute = 57;
        var ink = new Color(0.30f, 0.26f, 0.22f);
        var deep = new Color(0.36f, 0.31f, 0.26f);
        var plaster = new Color(0.17f, 0.16f, 0.15f);

        var px = new Color[W * H];
        for (int i = 0; i < px.Length; i++)
            px[i] = plaster;

        // A fixed seed, so the wall is the same wall every time it is rebuilt.
        var rng = new System.Random(1157);

        for (int i = 0; i < 46; i++)
        {
            float x = (float)rng.NextDouble() * W;
            float y = (float)rng.NextDouble() * H;
            float scale = 0.45f + (float)rng.NextDouble() * 0.75f;
            Color c = rng.Next(4) == 0 ? deep : ink;

            DrawNumber(px, W, H, Hour, x - 34f * scale, y, c);
            DrawNumber(px, W, H, Minute, x + 34f * scale, y, c);
            Fill(px, W, H, x - 3f, y - 2f, 5f, 5f, c);
            Fill(px, W, H, x - 3f, y + 12f, 5f, 5f, c);
        }

        for (int i = 0; i < 9; i++)
        {
            float x = (float)rng.NextDouble() * W;
            float y = (float)rng.NextDouble() * H;
            Dial(px, W, H, x, y, 26f + (float)rng.NextDouble() * 16f,
                 (Hour + Minute / 60f) / 12f * 360f, Minute / 60f * 360f, deep);
        }

        WritePng(px, W, H, "WallScrawl.png");
    }

    private static void ClockIcon(Color[] px, int w, int h, float cx, float cy, float radius,
                                  float hourDegrees, float minuteDegrees, bool swinging, Color ink)
    {
        Dial(px, w, h, cx, cy, radius, hourDegrees, minuteDegrees, ink);

        // The pendulum below is the whole tell, so it is drawn larger than life.
        float top = cy - radius - 4f;
        const float Length = 58f;

        if (!swinging)
        {
            Line(px, w, h, cx, top, cx, top - Length, 4f, ink);
            DiscAt(px, w, h, cx, top - Length, 12f, ink);
            return;
        }

        // A wide fan, so it reads as motion rather than as three separate pendulums.
        foreach (float lean in new[] { -42f, 0f, 42f })
        {
            float rad = lean * Mathf.Deg2Rad;
            float bx = cx + Mathf.Sin(rad) * Length;
            float by = top - Mathf.Cos(rad) * Length;
            bool centre = Mathf.Approximately(lean, 0f);

            Line(px, w, h, cx, top, bx, by, centre ? 4f : 2f, ink);
            DiscAt(px, w, h, bx, by, centre ? 12f : 7f, ink);
        }
    }

    // Seven-segment numerals, the same shapes the number reels use, so a figure scrawled on
    // a wall and a figure in a clock window read as the same hand.
    private static void DrawNumber(Color[] px, int w, int h, int value, float cx, float cy, Color ink)
    {
        const float DigitW = 22f, DigitH = 34f, Gap = 5f;
        string text = value.ToString();
        float x = cx - (text.Length * DigitW + (text.Length - 1) * Gap) * 0.5f;

        foreach (char c in text)
        {
            DrawDigit(px, w, h, c - '0', x, cy - DigitH * 0.5f, DigitW, DigitH, ink);
            x += DigitW + Gap;
        }
    }

    private static void DrawDigit(Color[] px, int w, int h, int digit, float ox, float oy, float dw, float dh, Color ink)
    {
        string[] segments = { "abcdef", "bc", "abged", "abgcd", "fgbc", "afgcd", "afgcde", "abc", "abcdefg", "abcfgd" };
        string on = segments[digit];
        float t = Mathf.Max(3f, dw / 5f);

        if (on.Contains("a")) Fill(px, w, h, ox, oy + dh - t, dw, t, ink);
        if (on.Contains("g")) Fill(px, w, h, ox, oy + dh * 0.5f - t * 0.5f, dw, t, ink);
        if (on.Contains("d")) Fill(px, w, h, ox, oy, dw, t, ink);
        if (on.Contains("f")) Fill(px, w, h, ox, oy + dh * 0.5f, t, dh * 0.5f, ink);
        if (on.Contains("b")) Fill(px, w, h, ox + dw - t, oy + dh * 0.5f, t, dh * 0.5f, ink);
        if (on.Contains("e")) Fill(px, w, h, ox, oy, t, dh * 0.5f, ink);
        if (on.Contains("c")) Fill(px, w, h, ox + dw - t, oy, t, dh * 0.5f, ink);
    }

    private static void Hand(Color[] px, int w, int h, float cx, float cy, float degrees, float length, float thickness, Color ink)
    {
        float rad = degrees * Mathf.Deg2Rad;
        Line(px, w, h, cx, cy, cx + Mathf.Sin(rad) * length, cy + Mathf.Cos(rad) * length, thickness, ink);
    }

    private static void Tick(Color[] px, int w, int h, float cx, float cy, float size, Color ink)
    {
        Line(px, w, h, cx - size, cy, cx - size * 0.25f, cy - size * 0.7f, 10f, ink);
        Line(px, w, h, cx - size * 0.25f, cy - size * 0.7f, cx + size, cy + size * 0.8f, 10f, ink);
    }

    private static void Cross(Color[] px, int w, int h, float cx, float cy, float size, Color ink)
    {
        Line(px, w, h, cx - size, cy - size, cx + size, cy + size, 10f, ink);
        Line(px, w, h, cx - size, cy + size, cx + size, cy - size, 10f, ink);
    }

    private static void Arrow(Color[] px, int w, int h, float x0, float y0, float x1, float y1, Color ink)
    {
        Line(px, w, h, x0, y0, x1, y1, 4f, ink);
        Line(px, w, h, x1, y1, x1 - 16f, y1 + 11f, 4f, ink);
        Line(px, w, h, x1, y1, x1 - 16f, y1 - 11f, 4f, ink);
    }

    private static void Line(Color[] px, int w, int h, float x0, float y0, float x1, float y1, float thickness, Color ink)
    {
        float dx = x1 - x0, dy = y1 - y0;
        int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) * 2f) + 1;

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            DiscAt(px, w, h, x0 + dx * t, y0 + dy * t, thickness * 0.5f, ink);
        }
    }

    private static void Frame(Color[] px, int w, int h, float x, float y, float bw, float bh, float t, Color ink)
    {
        Fill(px, w, h, x, y, bw, t, ink);
        Fill(px, w, h, x, y + bh - t, bw, t, ink);
        Fill(px, w, h, x, y, t, bh, ink);
        Fill(px, w, h, x + bw - t, y, t, bh, ink);
    }

    private static void DiscAt(Color[] px, int w, int h, float cx, float cy, float radius, Color ink)
    {
        RingAt(px, w, h, cx, cy, 0f, radius, ink);
    }

    private static void RingAt(Color[] px, int w, int h, float cx, float cy, float inner, float outer, Color ink)
    {
        int r = Mathf.CeilToInt(outer) + 1;

        for (int y = -r; y <= r; y++)
        for (int x = -r; x <= r; x++)
        {
            float d = Mathf.Sqrt(x * x + y * y);
            if (d >= inner && d <= outer)
                Set(px, w, h, cx + x, cy + y, ink);
        }
    }

    private static void Fill(Color[] px, int w, int h, float x0, float y0, float bw, float bh, Color colour)
    {
        for (int y = Mathf.RoundToInt(y0); y < Mathf.RoundToInt(y0 + bh); y++)
        for (int x = Mathf.RoundToInt(x0); x < Mathf.RoundToInt(x0 + bw); x++)
            Set(px, w, h, x, y, colour);
    }

    private static void Set(Color[] px, int w, int h, float x, float y, Color colour)
    {
        int ix = Mathf.RoundToInt(x), iy = Mathf.RoundToInt(y);
        if (ix >= 0 && ix < w && iy >= 0 && iy < h)
            px[iy * w + ix] = colour;
    }

    private static void WritePng(Color[] px, int width, int height, string fileName)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "Textures/" + fileName), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static Material LoadMaterial(string name) =>
        AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + name + ".mat");

    private static Material Wood() => LoadMaterial("ClockCase");
    private static Material WoodDark() => LoadMaterial("ClockCaseDark");
    private static Material Brass() => LoadMaterial("ClockBrass");

    // The sandglass has glass of its own: thinner than the display case's and drawn on both
    // faces, so the far wall of a bulb shows through the near one the way it does in a real
    // glass. The case in the showroom keeps the plain one — a shared material would put that
    // second pass on every pane in the house.
    private static Material SandglassGlass()
    {
        Material mat = BuildKit.Glass("SandglassGlass", new Color(0.76f, 0.84f, 0.83f, 0.20f));
        mat.SetFloat("_Smoothness", 0.9f);

        // Front faces only. Drawn on both, the inside of the far wall catches the light and
        // the bulb fills with a white glare that the sand disappears into.
        mat.SetFloat("_Cull", 2f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material Glass()
    {
        string path = MaterialFolder + "/CaseGlass.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }

        // URP wants the surface mode, the blend factors and the keyword set together.
        // Any one of them on its own leaves the glass stubbornly opaque.
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_Smoothness", 0.95f);
        mat.SetFloat("_Metallic", 0f);
        mat.SetColor("_BaseColor", new Color(0.72f, 0.82f, 0.80f, 0.20f));
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Material material, bool solid)
    {
        GameObject go = Primitive(PrimitiveType.Cube, name, parent, localPos, material);
        go.transform.localScale = size;

        if (!solid)
            Object.DestroyImmediate(go.GetComponent<Collider>());

        return go;
    }

    private static GameObject Quad(string name, Transform parent, Vector3 localPos, float size, Material material)
    {
        GameObject go = Primitive(PrimitiveType.Quad, name, parent, localPos, material);
        go.transform.localScale = new Vector3(size, size, 1f);
        // A bare quad faces into its parent, which leaves it invisible in a recess. Half a
        // turn about Y is a rotation rather than a mirror, so the digits still read
        // the right way round.
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    private static GameObject Sphere(string name, Transform parent, Vector3 localPos, float diameter, Material material)
    {
        GameObject go = Primitive(PrimitiveType.Sphere, name, parent, localPos, material);
        go.transform.localScale = Vector3.one * diameter;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    // Ten short bars round a circle. Cheaper than a real torus and reads the same at the
    // size a key is actually seen.
    private static void Torus(string name, Transform parent, Vector3 localPos, float diameter, float thickness, Material material)
    {
        var hub = new GameObject(name);
        hub.transform.SetParent(parent, false);
        hub.transform.localPosition = localPos;

        const int Segments = 10;
        float radius = diameter * 0.5f;
        float side = Mathf.Tan(Mathf.PI / Segments) * radius * 2f + thickness * 0.5f;

        for (int i = 0; i < Segments; i++)
        {
            float a = i / (float)Segments * Mathf.PI * 2f;
            GameObject bar = Box("Seg" + i, hub.transform,
                new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f),
                new Vector3(thickness, side, thickness), material, false);
            bar.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
        }
    }

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private static void Wire(Object target, string property, object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(property);

        if (value is float f) p.floatValue = f;
        else if (value is int i) p.intValue = i;
        else if (value is string s) p.stringValue = s;
        else if (value is Vector3 v) p.vector3Value = v;
        else p.objectReferenceValue = (Object)value;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        int slash = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    // Act III's key hangs in a birdcage. The room is three cuckoos and a count of their
    // calls, and the thing the count opens is a cage with a bird on top of it — which says
    // what the code is about without a word of it being written down anywhere.
    //
    // The cage is the lock's body: the code wheels are let into the drum it stands on, and
    // the door in the bars is what they open. Nothing else in the house is shaped like this,
    // so a player who has counted anything at all knows where to bring the number.
    private const float CageRadius = 0.145f;
    private const float CageFloor = 0.104f;
    private const float CageStraight = 0.37f;     // where the bars stop going up
    private const float CageApex = 0.46f;
    private const float DoorFrom = -50f;          // the door is the front hundred degrees
    private const float DoorTo = 50f;
    private const int Bars = 18;

    [MenuItem("Tools/Escape Room/Puzzle/Build Strike Cage")]
    public static void BuildStrikeCage()
    {
        EnsureFolder(MeshFolder);

        var box = GameObject.Find("House/Props/Room_III_Parlour/StrikeBox");
        Transform door = box.transform.Find("BoxDoor");
        Transform plate = box.transform.Find("Dialplate");
        Transform key = box.transform.Find("BedroomKeyProp");

        // Everything else is geometry and goes. These three carry wiring that cannot be
        // rebuilt from here — the combination, the wheels' printed strips, the key's own
        // item — so they are moved rather than made again.
        var scrap = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in box.transform)
            if (child != door && child != plate && child != key)
                scrap.Add(child.gameObject);
        foreach (GameObject go in scrap)
            Undo.DestroyObjectImmediate(go);

        Material brass = Brass();
        Material dark = WoodDark();

        Drum("Drum", box.transform, 0f, 0.09f, 0.150f, dark);
        Drum("Lip", box.transform, 0.09f, CageFloor, 0.163f, dark);

        var cage = Shape("Bars", box.transform, Vector3.zero, Quaternion.identity,
                         CageMesh("StrikeCage"), brass);
        cage.layer = box.layer;
        cage.AddComponent<MeshCollider>();

        // A perch, because a cage has one — and because the key hangs off the middle of it,
        // up where a standing player's line into the open door actually goes. Lying on the
        // floor of the cage it was behind three bars from every angle anyone could stand at.
        Rod("Perch", box.transform, new Vector3(-0.12f, 0.30f, 0f), new Vector3(0.12f, 0.30f, 0f),
            0.008f, brass);

        Drum("Finial", box.transform, CageApex - 0.004f, CageApex + 0.026f, 0.018f, brass);
        Shape("Bird", box.transform, new Vector3(0f, CageApex + 0.062f, 0f),
              Quaternion.Euler(90f, 0f, 0f),
              BirdPlate("StrikeCageBird", Bird(), 0.004f, 0.045f, 0.044f), brass)
            .layer = box.layer;

        RestKey(key);
        SetDoor(door, brass);
        SetPlate(plate, dark);

        // The drum and nothing above it. A hull round the cage is a hull round the key, and
        // the fourth container in this house to be built that way before anyone noticed.
        var hull = box.GetComponent<BoxCollider>();
        Undo.RecordObject(hull, "Build Strike Cage");
        hull.center = new Vector3(0f, CageFloor * 0.5f, 0f);
        hull.size = new Vector3(0.32f, CageFloor, 0.32f);

        // It is a cage now, and three prompts still called it a box.
        var openable = door.GetComponent<Openable>();
        var codeLock = door.GetComponent<CodeLock>();
        Wire(openable, "label", "กรงนก");
        Wire(codeLock, "lockedPrompt", "[E] กรงนกล็อก — ตั้งเลขสามหลัก");
        Wire(codeLock, "refusedMessage", "กรงยังล็อก ลองดึงโซ่ถ่วงนาฬิกาแล้วนับเสียงนกดู");
    }

    // The fixed part of the cage: the bars, the hoops that tie them, and the dome they meet
    // in. The hundred degrees the door fills carry no fixed bar below the dome, and the two
    // lower hoops run round the back as arcs rather than through the doorway.
    private static Mesh CageMesh(string name)
    {
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();
        const float Wire = 0.0035f;

        for (int i = 0; i < Bars; i++)
        {
            float degrees = i * 360f / Bars;
            bool doorway = Mathf.Abs(Mathf.DeltaAngle(degrees, 0f)) < (DoorTo - DoorFrom) * 0.5f;
            Sweep(vertices, triangles, BarPath(degrees, doorway), Beside(degrees), Wire, 5);
        }

        Hoop(vertices, triangles, 0.108f, 0f, 360f, Wire);
        Hoop(vertices, triangles, 0.170f, DoorTo, 360f + DoorFrom, Wire);
        Hoop(vertices, triangles, 0.270f, DoorTo, 360f + DoorFrom, Wire);
        Hoop(vertices, triangles, CageStraight, 0f, 360f, Wire);

        return Save(name, vertices, triangles);
    }

    // One bar: up the side, then over the dome to the point they all meet at. A bar standing
    // in the doorway is dome only — there is nothing to hold below it but the door.
    private static Vector3[] BarPath(float degrees, bool doorway)
    {
        var path = new System.Collections.Generic.List<Vector3>();
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

        if (!doorway)
            path.Add(new Vector3(sin * CageRadius, CageFloor, cos * CageRadius));

        const int Over = 7;
        for (int i = 0; i <= Over; i++)
        {
            float turn = i / (float)Over * Mathf.PI * 0.5f;
            float radius = CageRadius * Mathf.Cos(turn);
            float y = CageStraight + (CageApex - CageStraight) * Mathf.Sin(turn);
            path.Add(new Vector3(sin * radius, y, cos * radius));
        }

        return path.ToArray();
    }

    private static void Hoop(System.Collections.Generic.List<Vector3> vertices,
                             System.Collections.Generic.List<int> triangles,
                             float y, float from, float to, float wire)
    {
        int steps = Mathf.Max(3, Mathf.RoundToInt(Mathf.Abs(to - from) / 8f));
        var path = new Vector3[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float degrees = Mathf.Lerp(from, to, i / (float)steps) * Mathf.Deg2Rad;
            path[i] = new Vector3(Mathf.Sin(degrees) * CageRadius, y, Mathf.Cos(degrees) * CageRadius);
        }

        Sweep(vertices, triangles, path, Vector3.up, wire, 5);
    }

    // The door is the front of the cage, hinged on one edge of the opening and carrying its
    // own bars and the arcs that frame them. Built in its own frame, so the swing is the
    // whole of what moves it.
    private static void SetDoor(Transform door, Material brass)
    {
        var scrap = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in door)
            scrap.Add(child.gameObject);
        foreach (GameObject go in scrap)
            Undo.DestroyObjectImmediate(go);

        Undo.RecordObject(door, "Build Strike Cage");
        door.localPosition = new Vector3(Mathf.Sin(DoorFrom * Mathf.Deg2Rad) * CageRadius, 0f,
                                         Mathf.Cos(DoorFrom * Mathf.Deg2Rad) * CageRadius);
        door.localRotation = Quaternion.identity;

        const float Low = 0.115f;
        const float High = 0.355f;
        const float Wire = 0.0035f;
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        for (int i = 0; i <= 4; i++)
        {
            float degrees = Mathf.Lerp(DoorFrom, DoorTo, i / 4f);
            Sweep(vertices, triangles,
                  new[] { Hinged(degrees, Low), Hinged(degrees, High) }, Beside(degrees), Wire, 5);
        }

        DoorArc(vertices, triangles, Low, Wire);
        DoorArc(vertices, triangles, High, Wire);

        Shape("Leaf", door, Vector3.zero, Quaternion.identity, Save("StrikeCageDoor", vertices, triangles), brass)
            .layer = door.gameObject.layer;

        // The whole front of the cage answers as the door, and it stands a little proud of the
        // bars so the crosshair reaches it first. Sized to the doorway alone it was almost
        // unaimable: a standing player looks *down* into a cage on a table, so a crosshair on
        // the middle of it lands on the dome, and the dome is bars with no prompt on them. The
        // hit box runs up to the spring of the dome for that reason and not because the door
        // does. It still stops short of the wheels below, which have their own.
        // It also has to stop *above* the wheels, and higher than the doorway does. A glance
        // down at a wheel crosses the front of the cage a hand above the wheel itself, so a
        // hit box carried down to the sill of the door caught every one of those glances and
        // the code became unturnable — the door answering for the lock it is fitted to.
        const float Bottom = 0.17f;
        const float Top = 0.40f;
        var hit = door.GetComponent<BoxCollider>();
        Vector3 far = Hinged(DoorTo, 0f);
        hit.center = new Vector3(far.x * 0.5f, (Bottom + Top) * 0.5f, 0.025f);
        hit.size = new Vector3(Mathf.Abs(far.x) + 0.03f, Top - Bottom, 0.09f);
    }

    private static void DoorArc(System.Collections.Generic.List<Vector3> vertices,
                                System.Collections.Generic.List<int> triangles, float y, float wire)
    {
        var path = new Vector3[13];
        for (int i = 0; i < path.Length; i++)
            path[i] = Hinged(Mathf.Lerp(DoorFrom, DoorTo, i / (float)(path.Length - 1)), y);

        Sweep(vertices, triangles, path, Vector3.up, wire, 5);
    }

    // A point on the cage wall, given from the hinge rather than from the middle.
    private static Vector3 Hinged(float degrees, float y)
    {
        return new Vector3((Mathf.Sin(degrees * Mathf.Deg2Rad) - Mathf.Sin(DoorFrom * Mathf.Deg2Rad)) * CageRadius,
                           y,
                           (Mathf.Cos(degrees * Mathf.Deg2Rad) - Mathf.Cos(DoorFrom * Mathf.Deg2Rad)) * CageRadius);
    }

    // Level, and square to the wall of the cage: the direction a bar at this bearing is not
    // bent in, which is what a swept section needs to know to keep its shape.
    private static Vector3 Beside(float degrees) =>
        new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0f, -Mathf.Sin(degrees * Mathf.Deg2Rad));

    // On the floor of the cage, lying the way it was found lying in the box.
    //
    // Hung off the perch first, which looked better and measured worse. Two ways worse: the
    // bow of this key is a ring square to its shaft, so a key hanging by it turns the ring
    // edge on and reads as a wire; and the crosshair fared *worse* the higher it went, because
    // the aim assist is a sphere 60 mm across and a key up at the top of the doorway clips the
    // arc over it. Down on the floor the sightline through the open door has 100 mm of air
    // above and below it. Measured through `PlayerInteractor`'s own reach, aim assist and
    // blocking layers, from where a player would really be standing — not judged by eye.
    private static void RestKey(Transform key)
    {
        Undo.RecordObject(key, "Build Strike Cage");
        key.localPosition = new Vector3(0f, 0.118f, 0f);
        key.localRotation = Quaternion.identity;
    }

    // The wheels move to the front of the drum and keep everything else about themselves.
    private static void SetPlate(Transform plate, Material dark)
    {
        // Stood well proud of the drum, and that is a hit box decision before it is a pretty
        // one. The door answers for the whole front of the cage, which puts its box in the air
        // above the wheels — and a glance down at a wheel passes through that air. Sixty
        // millimetres of aim assist then clips the corner of it and the door answers instead,
        // so the wheels have to stand clear in front by more than that radius. The block
        // behind them is what carries them out there without leaving a gap to see through.
        Undo.RecordObject(plate, "Build Strike Cage");
        plate.localPosition = new Vector3(0f, 0.048f, 0.168f);

        Transform face = plate.Find("Plate");
        if (face != null)
            Undo.DestroyObjectImmediate(face.gameObject);

        Box("Plate", plate, new Vector3(0f, 0f, -0.018f), new Vector3(0.26f, 0.082f, 0.048f), Brass(), false)
            .transform.SetAsFirstSibling();
    }

    // A turned drum: the base the cage stands on, and the finial on top of it.
    private static GameObject Drum(string name, Transform parent, float from, float to, float radius, Material material)
    {
        GameObject go = Primitive(PrimitiveType.Cylinder, name, parent, new Vector3(0f, (from + to) * 0.5f, 0f), material);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.localScale = new Vector3(radius * 2f, (to - from) * 0.5f, radius * 2f);
        go.layer = parent.gameObject.layer;
        Undo.RegisterCreatedObjectUndo(go, "Build Strike Cage");
        return go;
    }

    // A round section carried along a path. The section is built square to the path at every
    // point, from `side` — a direction the path is never parallel to — so one function serves
    // both a bar bending over the dome and a hoop lying flat.
    private static void Sweep(System.Collections.Generic.List<Vector3> vertices,
                              System.Collections.Generic.List<int> triangles,
                              Vector3[] path, Vector3 side, float radius, int sides)
    {
        int first = vertices.Count;

        for (int i = 0; i < path.Length; i++)
        {
            Vector3 along = (i == 0 ? path[1] - path[0] : path[i] - path[i - 1]).normalized;
            Vector3 out1 = Vector3.Cross(along, side).normalized;
            Vector3 out2 = Vector3.Cross(out1, along).normalized;

            for (int k = 0; k < sides; k++)
            {
                float turn = k / (float)sides * Mathf.PI * 2f;
                vertices.Add(path[i] + (out1 * Mathf.Cos(turn) + out2 * Mathf.Sin(turn)) * radius);
            }
        }

        for (int i = 0; i < path.Length - 1; i++)
            for (int k = 0; k < sides; k++)
            {
                int here = first + i * sides + k;
                int next = first + i * sides + (k + 1) % sides;

                triangles.Add(here); triangles.Add(here + sides); triangles.Add(next);
                triangles.Add(next); triangles.Add(here + sides); triangles.Add(next + sides);
            }
    }

    // The bird, as the outline it is cut from — the one drawing of it there is, the same
    // bargain `Stars` makes with the constellation. It is authored in its own frame, a plump
    // perched bird facing right with its tail cocked, and scaled onto whatever carries it; a
    // shape written down in millimetres is a shape nobody dares adjust.
    //
    // Two of these points are pushed *inward*, one behind the head and one under the throat.
    // The nick between them is the neck, and it is what makes the shape read as a bird rather
    // than as a blob — worth more than the beak, which is three points and reads as one.
    private static Vector2[] Bird()
    {
        return new[]
        {
            new Vector2( 1.00f,  0.34f),   // beak
            new Vector2( 0.74f,  0.48f),
            new Vector2( 0.62f,  0.64f),   // crown
            new Vector2( 0.40f,  0.72f),
            new Vector2( 0.20f,  0.66f),   // nape
            new Vector2( 0.10f,  0.44f),   // the nick behind the head — this is the neck
            new Vector2(-0.16f,  0.50f),   // back
            new Vector2(-0.74f,  0.94f),   // tail, cocked up behind
            new Vector2(-0.96f,  0.72f),
            new Vector2(-0.42f,  0.20f),
            new Vector2(-0.56f, -0.06f),   // rump
            new Vector2(-0.44f, -0.40f),
            new Vector2(-0.12f, -0.60f),   // belly
            new Vector2( 0.10f, -0.62f),   // legs
            new Vector2( 0.44f, -0.44f),
            new Vector2( 0.62f, -0.14f),   // breast
            new Vector2( 0.52f,  0.10f)    // throat, and the other side of the neck
        };
    }

    // The bird as a piece of brass: a face, a back, and an edge round it with a thickness.
    //
    // The face is a fan from the middle, which works because the outline is star-shaped about
    // that point — every ray out of the centre leaves the bird exactly once, so no triangle in
    // the fan can fold back over another. That is a property of the drawing above, and the
    // one thing to check before redrawing it.
    private static Mesh BirdPlate(string name, Vector2[] drawing, float thickness, float across, float up)
    {
        // Into the frame of whatever carries it. The cage stands with its door towards the
        // player, so it is looked at down local -z and local -x is the viewer's right: the
        // bird is turned to face that way and stand upright to whoever is looking at it.
        var outline = new Vector2[drawing.Length];
        for (int i = 0; i < drawing.Length; i++)
            outline[i] = new Vector2(-drawing[i].x * across, -drawing[i].y * up);

        float hy = thickness * 0.5f;
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        Fan(vertices, triangles, outline, hy, true);
        Fan(vertices, triangles, outline, -hy, false);
        Edge(vertices, triangles, outline, hy);

        return Save(name, vertices, triangles);
    }

    // One flat side of the plate, drawn out from its middle.
    private static void Fan(System.Collections.Generic.List<Vector3> vertices,
                            System.Collections.Generic.List<int> triangles,
                            Vector2[] outline, float y, bool up)
    {
        int middle = vertices.Count;
        vertices.Add(new Vector3(0f, y, 0f));
        foreach (Vector2 p in outline)
            vertices.Add(new Vector3(p.x, y, p.y));

        for (int i = 0; i < outline.Length; i++)
        {
            int here = middle + 1 + i;
            int next = middle + 1 + (i + 1) % outline.Length;

            triangles.Add(middle);
            triangles.Add(up ? next : here);
            triangles.Add(up ? here : next);
        }
    }

    // The edge of the plate: one strip of wall per step round the outline.
    private static void Edge(System.Collections.Generic.List<Vector3> vertices,
                             System.Collections.Generic.List<int> triangles,
                             Vector2[] outline, float hy)
    {
        for (int i = 0; i < outline.Length; i++)
        {
            Vector2 a = outline[i];
            Vector2 b = outline[(i + 1) % outline.Length];

            int at = vertices.Count;
            vertices.Add(new Vector3(a.x, hy, a.y));
            vertices.Add(new Vector3(a.x, -hy, a.y));
            vertices.Add(new Vector3(b.x, hy, b.y));
            vertices.Add(new Vector3(b.x, -hy, b.y));

            triangles.Add(at); triangles.Add(at + 2); triangles.Add(at + 1);
            triangles.Add(at + 1); triangles.Add(at + 2); triangles.Add(at + 3);
        }
    }
}
