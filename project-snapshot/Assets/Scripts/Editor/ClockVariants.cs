using UnityEditor;
using UnityEngine;

// The rest of the collection: a longcase, a mantel clock, a carriage clock, a cuckoo and a
// pocket watch.
//
// Every one of them nests the same `ClockDial.prefab` and drives it with the same
// `ClockMovement`. What differs between a grandfather clock and a pocket watch is the case
// around them, which is all this file is — one method per case, and three shared
// sub-assemblies underneath.
public static class ClockVariants
{
    private const string PrefabFolder = "Assets/Prefabs/Clocks";
    private const string DialPath = PrefabFolder + "/ClockDial.prefab";

    [MenuItem("Tools/Escape Room/Clocks/Build The Whole Collection")]
    public static void BuildAll()
    {
        BuildLongcase();
        BuildMantel();
        BuildCarriage();
        BuildCuckoo();
        BuildPocketWatch();
        BuildAlarmClock();
        BuildWristwatch();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Clock collection rebuilt into " + PrefabFolder);
    }

    // ---------- the longcase ----------

    // Floor standing, just over two metres. The trunk door is a real Openable, which is
    // what makes this the one clock a person could walk through later.
    [MenuItem("Tools/Escape Room/Clocks/Build Longcase Clock")]
    public static GameObject BuildLongcase()
    {
        Material wood = Wood(), dark = WoodDark(), brass = Brass(), glass = Glass();
        var root = new GameObject("LongcaseClock");

        const float PlinthTop = 0.34f, TrunkTop = 1.44f, HoodTop = 1.98f;
        const float TrunkWidth = 0.34f, TrunkDepth = 0.22f;
        const float HoodWidth = 0.44f, HoodDepth = 0.28f;

        Box("Plinth", root.transform, new Vector3(0f, PlinthTop * 0.5f, 0f),
            new Vector3(0.44f, PlinthTop, 0.30f), wood);
        Box("PlinthCap", root.transform, new Vector3(0f, PlinthTop + 0.015f, 0f),
            new Vector3(0.40f, 0.03f, 0.27f), dark);

        // Trunk, open at the front where the door hangs.
        float trunkMid = (PlinthTop + TrunkTop) * 0.5f;
        float trunkHeight = TrunkTop - PlinthTop;
        Box("TrunkBack", root.transform, new Vector3(0f, trunkMid, -TrunkDepth * 0.5f + 0.015f),
            new Vector3(TrunkWidth, trunkHeight, 0.03f), wood);
        Box("TrunkLeft", root.transform, new Vector3(-TrunkWidth * 0.5f + 0.02f, trunkMid, 0f),
            new Vector3(0.04f, trunkHeight, TrunkDepth), wood);
        Box("TrunkRight", root.transform, new Vector3(TrunkWidth * 0.5f - 0.02f, trunkMid, 0f),
            new Vector3(0.04f, trunkHeight, TrunkDepth), wood);

        // The weights and the pendulum, seen through the door. The weights are this clock's
        // power, so the Mainspring lives with them: take the weights out and it stops.
        Transform guts = Group("Weights", root.transform, new Vector3(0f, TrunkTop - 0.02f, -0.02f));
        var weightDrive = guts.gameObject.AddComponent<Mainspring>();
        Cylinder("WeightLeft", guts, new Vector3(-0.075f, -0.42f, 0f), 0.052f, 0.20f, brass);
        Cylinder("WeightRight", guts, new Vector3(0.075f, -0.30f, 0f), 0.052f, 0.20f, brass);
        Box("ChainLeft", guts, new Vector3(-0.075f, -0.17f, 0f), new Vector3(0.006f, 0.30f, 0.006f), brass);
        Box("ChainRight", guts, new Vector3(0.075f, -0.11f, 0f), new Vector3(0.006f, 0.18f, 0.006f), brass);

        Transform swing = Group("Pendulum", root.transform, new Vector3(0f, TrunkTop - 0.04f, -0.01f));
        Box("Rod", swing, new Vector3(0f, -0.34f, 0f), new Vector3(0.007f, 0.68f, 0.007f), brass);
        Disc("Bob", swing, new Vector3(0f, -0.70f, 0f), 0.115f, 0.012f, brass);

        // Trunk door, hinged on the left stile, and glazed rather than panelled. The
        // pendulum behind it is the tell the whole house runs on; a solid door would hide
        // the one thing the player has to be able to see.
        float doorWidth = TrunkWidth - 0.08f;
        float doorHeight = trunkHeight - 0.05f;
        var door = new GameObject("TrunkDoor");
        door.transform.SetParent(root.transform, false);
        door.transform.localPosition = new Vector3(-doorWidth * 0.5f, trunkMid, TrunkDepth * 0.5f - 0.012f);

        Box("StileLeft", door.transform, new Vector3(0.018f, 0f, 0f),
            new Vector3(0.036f, doorHeight, 0.020f), wood);
        Box("StileRight", door.transform, new Vector3(doorWidth - 0.018f, 0f, 0f),
            new Vector3(0.036f, doorHeight, 0.020f), wood);
        Box("RailTop", door.transform, new Vector3(doorWidth * 0.5f, doorHeight * 0.5f - 0.045f, 0f),
            new Vector3(doorWidth, 0.090f, 0.020f), wood);
        Box("RailBottom", door.transform, new Vector3(doorWidth * 0.5f, -doorHeight * 0.5f + 0.045f, 0f),
            new Vector3(doorWidth, 0.090f, 0.020f), wood);
        Box("Light", door.transform, new Vector3(doorWidth * 0.5f, 0f, 0.004f),
            new Vector3(doorWidth - 0.06f, doorHeight - 0.10f, 0.006f), glass);
        Sphere("Knob", door.transform, new Vector3(doorWidth - 0.03f, 0f, 0.018f), 0.024f, brass);
        BuildKit.Hull(door, new Vector3(doorWidth * 0.5f, 0f, 0f), new Vector3(doorWidth, trunkHeight, 0.05f));

        var openable = door.AddComponent<Openable>();
        var swingMotion = door.AddComponent<SwingMotion>();
        Wire(openable, "label", "ตู้นาฬิกา");
        Wire(swingMotion, "openAngle", -105f);
        Wire(swingMotion, "swingSpeed", 110f);

        // Hood.
        float hoodMid = (TrunkTop + HoodTop) * 0.5f;
        float hoodHeight = HoodTop - TrunkTop;
        Box("HoodBack", root.transform, new Vector3(0f, hoodMid, -HoodDepth * 0.5f + 0.015f),
            new Vector3(HoodWidth, hoodHeight, 0.03f), wood);
        Box("HoodLeft", root.transform, new Vector3(-HoodWidth * 0.5f + 0.025f, hoodMid, 0f),
            new Vector3(0.05f, hoodHeight, HoodDepth), wood);
        Box("HoodRight", root.transform, new Vector3(HoodWidth * 0.5f - 0.025f, hoodMid, 0f),
            new Vector3(0.05f, hoodHeight, HoodDepth), wood);
        Box("HoodTop", root.transform, new Vector3(0f, HoodTop + 0.02f, 0f),
            new Vector3(HoodWidth + 0.04f, 0.04f, HoodDepth + 0.04f), wood);
        Box("Cornice", root.transform, new Vector3(0f, TrunkTop + 0.02f, 0f),
            new Vector3(HoodWidth + 0.02f, 0.04f, HoodDepth + 0.02f), dark);

        // Swan neck, the flourish that says longcase rather than cupboard.
        Box("NeckLeft", root.transform, new Vector3(-0.12f, HoodTop + 0.09f, -0.05f),
            new Vector3(0.16f, 0.035f, 0.05f), wood).transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
        Box("NeckRight", root.transform, new Vector3(0.12f, HoodTop + 0.09f, -0.05f),
            new Vector3(0.16f, 0.035f, 0.05f), wood).transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
        Cylinder("Finial", root.transform, new Vector3(0f, HoodTop + 0.11f, -0.05f), 0.030f, 0.10f, brass);

        float dialY = (TrunkTop + HoodTop) * 0.5f + 0.02f;
        ClockDial dial = Dial(root.transform, new Vector3(0f, dialY, HoodDepth * 0.5f - 0.02f), 1.55f);
        Box("DialSurround", root.transform, new Vector3(0f, dialY, HoodDepth * 0.5f - 0.035f),
            new Vector3(HoodWidth - 0.10f, HoodWidth - 0.10f, 0.02f), dark);

        var movement = root.AddComponent<ClockMovement>();
        Wire(movement, "startHour", 0);   // noon; the field is 0-11 and a dial shows 0 as twelve
        Wire(movement, "startMinute", 0);
        Wire(movement, "minutesPerSecond", 1f / 60f);

        var pendulum = swing.gameObject.AddComponent<Pendulum>();
        Wire(pendulum, "swingDegrees", 5f);
        Wire(pendulum, "secondsPerSwing", 2f);

        // Kept small: at the case's own scale the crank arm would swing out past the hood
        // and read as a lever bolted to the side.
        ClockWinder winder = Winder(root.transform, new Vector3(0f, dialY - 0.19f, HoodDepth * 0.5f + 0.02f), 0.80f, movement, weightDrive);
        Wire(winder, "revealedDigit", 0);

        BuildKit.Hull(root, new Vector3(0f, 1.0f, -0.02f), new Vector3(HoodWidth, 2.0f, HoodDepth));
        // Two metres of oak: the view goes to it, it does not come to the view.
        Wire(root.AddComponent<Inspectable>(), "style", (int)InspectStyle.Zoom);

        return Save(root, "LongcaseClock");
    }

    // ---------- the mantel clock ----------

    // Sits on a shelf. Small enough that its origin is its base, so it can be dropped onto
    // any surface at that surface's height.
    [MenuItem("Tools/Escape Room/Clocks/Build Mantel Clock")]
    public static GameObject BuildMantel()
    {
        Material wood = Wood(), dark = WoodDark(), brass = Brass();
        var root = new GameObject("MantelClock");

        const float Width = 0.24f, Height = 0.25f, Depth = 0.13f;

        Box("Body", root.transform, new Vector3(0f, Height * 0.5f, 0f),
            new Vector3(Width, Height, Depth), wood);
        // Stepped arch, three slabs rather than a curve. At this size nobody counts steps.
        Box("Arch1", root.transform, new Vector3(0f, Height + 0.012f, 0f), new Vector3(0.19f, 0.024f, Depth), wood);
        Box("Arch2", root.transform, new Vector3(0f, Height + 0.032f, 0f), new Vector3(0.14f, 0.020f, Depth - 0.01f), wood);
        Box("Arch3", root.transform, new Vector3(0f, Height + 0.048f, 0f), new Vector3(0.08f, 0.014f, Depth - 0.02f), wood);
        Box("Plinth", root.transform, new Vector3(0f, 0.012f, 0f), new Vector3(Width + 0.03f, 0.024f, Depth + 0.02f), dark);

        foreach (int sx in new[] { -1, 1 })
        foreach (int sz in new[] { -1, 1 })
            Cylinder("Foot", root.transform, new Vector3(sx * 0.10f, 0.008f, sz * 0.05f), 0.020f, 0.016f, brass);

        ClockDial dial = Dial(root.transform, new Vector3(0f, 0.145f, Depth * 0.5f + 0.002f), 0.62f);
        Disc("Bezel", root.transform, new Vector3(0f, 0.145f, Depth * 0.5f - 0.004f), 0.125f, 0.008f, brass);

        var movement = root.AddComponent<ClockMovement>();
        Wire(movement, "startHour", 10);
        Wire(movement, "startMinute", 10);
        Wire(movement, "minutesPerSecond", 1f / 60f);

        // On the back, where a mantel clock is really wound. You pick this one up to reach it.
        ClockWinder winder = Winder(root.transform, new Vector3(0f, 0.115f, -Depth * 0.5f - 0.020f),
                                    0.62f, movement, null, true);
        Wire(winder, "revealedDigit", 0);

        BuildKit.Hull(root, new Vector3(0f, Height * 0.5f, 0f), new Vector3(Width + 0.03f, Height + 0.07f, Depth + 0.02f));
        Wire(root.AddComponent<Inspectable>(), "style", (int)InspectStyle.Turn);

        return Save(root, "MantelClock");
    }

    // ---------- the carriage clock ----------

    // A brass box with glass on every side and a handle to carry it by. No winder: this one
    // is a curiosity to pick up and look at, not a thing to crank.
    [MenuItem("Tools/Escape Room/Clocks/Build Carriage Clock")]
    public static GameObject BuildCarriage()
    {
        Material brass = Brass(), glass = Glass();
        var root = new GameObject("CarriageClock");

        const float Width = 0.095f, Height = 0.125f, Depth = 0.075f;
        float hw = Width * 0.5f, hh = Height * 0.5f, hd = Depth * 0.5f;

        Box("Base", root.transform, new Vector3(0f, 0.008f, 0f), new Vector3(Width + 0.014f, 0.016f, Depth + 0.014f), brass);
        Box("Top", root.transform, new Vector3(0f, Height - 0.008f, 0f), new Vector3(Width + 0.014f, 0.016f, Depth + 0.014f), brass);

        // Four corner posts, then glass filling the gaps.
        foreach (int sx in new[] { -1, 1 })
        foreach (int sz in new[] { -1, 1 })
            Box("Post", root.transform, new Vector3(sx * hw, hh, sz * hd), new Vector3(0.010f, Height, 0.010f), brass);

        Box("GlassLeft", root.transform, new Vector3(-hw, hh, 0f), new Vector3(0.004f, Height - 0.02f, Depth - 0.008f), glass);
        Box("GlassRight", root.transform, new Vector3(hw, hh, 0f), new Vector3(0.004f, Height - 0.02f, Depth - 0.008f), glass);
        Box("GlassBack", root.transform, new Vector3(0f, hh, -hd), new Vector3(Width - 0.008f, Height - 0.02f, 0.004f), glass);
        Box("GlassFront", root.transform, new Vector3(0f, hh, hd), new Vector3(Width - 0.008f, Height - 0.02f, 0.004f), glass);

        Ring("Handle", root.transform, new Vector3(0f, Height + 0.026f, 0f), 0.060f, 0.006f, brass)
            .localRotation = Quaternion.Euler(0f, 90f, 0f);

        ClockDial dial = Dial(root.transform, new Vector3(0f, hh + 0.004f, hd + 0.004f), 0.46f);

        var movement = root.AddComponent<ClockMovement>();
        Wire(movement, "startHour", 4);
        Wire(movement, "startMinute", 35);
        Wire(movement, "minutesPerSecond", 1f / 60f);

        // It has a spring even though nothing here winds it. Without one there is nothing to
        // run down, and a clock the fiction calls Stopped would quietly keep perfect time.
        root.AddComponent<Mainspring>();

        BuildKit.Hull(root, new Vector3(0f, hh, 0f), new Vector3(Width + 0.02f, Height + 0.05f, Depth + 0.02f));
        var inspectable = root.AddComponent<Inspectable>();
        Wire(inspectable, "style", (int)InspectStyle.Turn);

        return Save(root, "CarriageClock");
    }

    // ---------- the cuckoo ----------

    // Wall mounted like the Vienna regulator, origin flat against the wall. The little door
    // above the dial is an Openable, so whatever opens it — the hour, a lever, a puzzle —
    // is decided in the scene rather than here.
    [MenuItem("Tools/Escape Room/Clocks/Build Cuckoo Clock")]
    public static GameObject BuildCuckoo()
    {
        Material wood = Wood(), light = WoodLight(), dark = WoodDark(), brass = Brass();
        var root = new GameObject("CuckooClock");

        const float Width = 0.26f, BodyTop = 0.36f, Depth = 0.14f;

        // The doorway, and a real hole for it.
        //
        // The case used to be one solid block with a dark plate stuck on the front where the
        // door was, which is fine for a door that only ever swings — but a bird has to come
        // out of somewhere. So the front is built round the opening the way a wall is built
        // round a window: a carcass behind, and four pieces of face with a hole between them
        // for the bird to sit in.
        const float DoorW = 0.070f, DoorH = 0.075f;
        const float Carcass = 0.085f;                 // the block behind the recess
        float doorY = BodyTop - 0.045f;               // the middle of the opening
        float faceMid = (Carcass + Depth) * 0.5f;
        float faceDeep = Depth - Carcass;

        Box("Body", root.transform, new Vector3(0f, BodyTop * 0.5f + 0.02f, Carcass * 0.5f),
            new Vector3(Width, BodyTop, Carcass), wood);

        // The back of the recess, dark, so the hole reads as a hole rather than as more case.
        Box("Recess", root.transform, new Vector3(0f, doorY, Carcass + 0.002f),
            new Vector3(DoorW, DoorH, 0.004f), dark);

        foreach (int side in new[] { -1, 1 })
        {
            // Face above and below the opening, then the two cheeks beside it.
            float outer = side < 0 ? 0.02f : BodyTop + 0.02f;
            float inner = doorY + side * DoorH * 0.5f;
            Box("Face", root.transform, new Vector3(0f, (outer + inner) * 0.5f, faceMid),
                new Vector3(Width, Mathf.Abs(inner - outer), faceDeep), wood);

            float cheek = (side * Width * 0.5f + side * DoorW * 0.5f) * 0.5f;
            Box("Face", root.transform, new Vector3(cheek, doorY, faceMid),
                new Vector3(Width * 0.5f - DoorW * 0.5f, DoorH, faceDeep), wood);
        }

        // Gable: two rafters leaning together over a ridge. Steep and short — a shallow
        // wide one stops reading as a roof and starts reading as a bird.
        const float Rafter = 0.21f, Pitch = 35f;
        float halfSpan = Rafter * Mathf.Cos(Pitch * Mathf.Deg2Rad) * 0.5f;
        float eaves = BodyTop + 0.03f;
        float rise = Rafter * Mathf.Sin(Pitch * Mathf.Deg2Rad) * 0.5f;

        foreach (int side in new[] { -1, 1 })
        {
            GameObject rafter = Box("Roof", root.transform,
                new Vector3(side * halfSpan, eaves + rise, Depth * 0.5f + 0.015f),
                new Vector3(Rafter, 0.024f, Depth + 0.09f), light);
            // Minus side: a positive Z turn lifts the right-hand end, so the rafter on the
            // left has to turn positively to reach up to the ridge. Getting this backwards
            // builds a V instead of a roof.
            rafter.transform.localRotation = Quaternion.Euler(0f, 0f, -side * Pitch);
        }
        Box("Ridge", root.transform, new Vector3(0f, eaves + rise * 2f, Depth * 0.5f + 0.015f),
            new Vector3(0.046f, 0.022f, Depth + 0.09f), light);

        // A little carved trim, so the front is not one flat brown slab.
        Box("TrimTop", root.transform, new Vector3(0f, BodyTop + 0.015f, Depth + 0.004f),
            new Vector3(Width - 0.01f, 0.020f, 0.016f), dark);
        Box("TrimBottom", root.transform, new Vector3(0f, 0.030f, Depth + 0.004f),
            new Vector3(Width - 0.01f, 0.020f, 0.016f), dark);
        foreach (int side in new[] { -1, 1 })
            Box("TrimSide", root.transform, new Vector3(side * (Width * 0.5f - 0.012f), BodyTop * 0.5f + 0.02f, Depth + 0.003f),
                new Vector3(0.018f, BodyTop - 0.02f, 0.012f), dark);

        // The cuckoo's door, hinged on the left.
        var door = new GameObject("CuckooDoor");
        door.transform.SetParent(root.transform, false);
        door.transform.localPosition = new Vector3(-0.035f, BodyTop - 0.045f, Depth + 0.002f);
        Box("Leaf", door.transform, new Vector3(0.035f, 0f, 0f), new Vector3(0.070f, 0.075f, 0.006f), light);

        // A carved surround, as four bars round the opening rather than one plate over it —
        // a plate would be the thing the bird has to push through on its way out.
        foreach (int side in new[] { -1, 1 })
        {
            Box("Frame", root.transform, new Vector3(0f, doorY + side * (DoorH * 0.5f + 0.005f), Depth - 0.003f),
                new Vector3(DoorW + 0.020f, 0.010f, 0.008f), dark);
            Box("Frame", root.transform, new Vector3(side * (DoorW * 0.5f + 0.005f), doorY, Depth - 0.003f),
                new Vector3(0.010f, DoorH + 0.020f, 0.008f), dark);
        }

        BuildKit.Hull(door, new Vector3(0.035f, 0f, 0f), new Vector3(0.070f, 0.075f, 0.03f));

        var openable = door.AddComponent<Openable>();
        var swingMotion = door.AddComponent<SwingMotion>();
        Wire(openable, "label", "ช่องนกกาเหว่า");
        Wire(swingMotion, "openAngle", -95f);

        // Snappy, because the recording leaves under three tenths of a second between the
        // bird going back in and the next call. At 320 the door was still shutting when it
        // had to open again, and the whole thing read as a shuffle.
        Wire(swingMotion, "swingSpeed", 620f);

        // The bird. It was a peg of dowel sitting behind a solid front, which no player ever
        // saw; it is carved now, and it comes out.
        //
        // Sized to the hole rather than to a bird: 34 mm across the wings in a 70 mm doorway,
        // so it rides through the middle of it without clipping the frame.
        Transform bird = Group("Bird", root.transform, new Vector3(0f, doorY, Carcass + 0.030f));

        // The head has to be plainly smaller than the body and set forward of it, or the two
        // spheres read as a snowman. The beak is what settles it either way: long, thin and
        // out in front of everything, in brass rather than the dark the eyes are — a dark
        // beak at this size reads as an open mouth.
        GameObject breast = Sphere("Body", bird, new Vector3(0f, -0.004f, 0f), 0.030f, light);
        breast.transform.localScale = new Vector3(0.026f, 0.024f, 0.044f);

        Sphere("Head", bird, new Vector3(0f, 0.011f, 0.019f), 0.017f, light);
        Box("Beak", bird, new Vector3(0f, 0.010f, 0.033f), new Vector3(0.004f, 0.004f, 0.016f), brass);

        foreach (int side in new[] { -1, 1 })
        {
            Sphere("Eye", bird, new Vector3(side * 0.006f, 0.014f, 0.026f), 0.0035f, dark);

            // Folded along the body, not held out from it. A wing square to the flank is a
            // plank; a few degrees of sweep and it reads as feathers laid along the back.
            GameObject wing = Box("Wing", bird, new Vector3(side * 0.0125f, -0.003f, -0.002f),
                new Vector3(0.003f, 0.012f, 0.026f), light);
            wing.transform.localRotation = Quaternion.Euler(6f, side * 7f, 0f);
        }

        GameObject tail = Box("Tail", bird, new Vector3(0f, 0.004f, -0.026f),
            new Vector3(0.011f, 0.004f, 0.026f), light);
        tail.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);

        var rider = bird.gameObject.AddComponent<CuckooBird>();
        Wire(rider, "door", openable);
        Wire(rider, "speed", 0.46f);   // out and back inside the door's own swing

        ClockDial dial = Dial(root.transform, new Vector3(0f, 0.185f, Depth + 0.004f), 0.72f);

        // Weights on chains, the pine cones every cuckoo clock hangs. They are the power
        // and the gauge at once, so they hang below the case where they can be seen and
        // reached, not tucked up inside it, where a chain is only a hidden stick.
        Transform chains = Group("Weights", root.transform, Vector3.zero);
        var cones = new Transform[2];

        for (int i = 0; i < 2; i++)
        {
            float x = (i == 0 ? -1 : 1) * 0.060f;

            // The chain stays put and the cone rides up it, so hauling on it does not drag
            // the head of the chain out of the case.
            Box("Chain", chains, new Vector3(x, -0.140f, Depth * 0.5f),
                new Vector3(0.004f, 0.320f, 0.004f), brass);

            cones[i] = Sphere("Cone", chains, new Vector3(x, -0.250f, Depth * 0.5f), 0.042f, brass).transform;
            cones[i].localScale = new Vector3(0.042f, 0.070f, 0.042f);
        }

        Transform swing = Group("Pendulum", root.transform, new Vector3(0f, 0.06f, Depth - 0.01f));
        Box("Rod", swing, new Vector3(0f, -0.075f, 0f), new Vector3(0.005f, 0.150f, 0.005f), brass);
        Box("Leaf", swing, new Vector3(0f, -0.165f, 0f), new Vector3(0.055f, 0.055f, 0.004f), light);

        var movement = root.AddComponent<ClockMovement>();
        Wire(movement, "startHour", 7);
        Wire(movement, "startMinute", 55);
        Wire(movement, "minutesPerSecond", 1f / 60f);

        var pendulum = swing.gameObject.AddComponent<Pendulum>();
        Wire(pendulum, "swingDegrees", 11f);
        Wire(pendulum, "secondsPerSwing", 1.2f);

        // No crank and no key anywhere on it: a cuckoo is wound by hauling its chains. The
        // striker takes its cue from the spring being filled rather than from any one
        // handle, so a pulled chain sets the bird off exactly as a crank would.
        var weightDrive = chains.gameObject.AddComponent<Mainspring>();

        var pull = chains.gameObject.AddComponent<WeightChain>();
        Wire(pull, "movement", movement);
        Wire(pull, "weights", cones);
        Wire(pull, "travel", 0.160f);

        BuildKit.Hull(chains.gameObject, new Vector3(0f, -0.150f, Depth * 0.5f), new Vector3(0.20f, 0.34f, 0.09f));

        // Every cuckoo strikes; that is most of what makes it one. The parlour puzzle reads
        // the count, but a clock does not have to be in a puzzle to have a bird.
        //
        // The recording is what the striker keeps time by, so the voice lives with it.
        //
        // The three numbers under it are measured off the clip, and they are stated here
        // rather than left to the component's defaults because getting one of them wrong is
        // silent: `firstCall` was set to 1.84 once, which is the *second* call, and the bird
        // then kept perfect time one call behind the sound all the way down.
        //
        // Each call is a loud "cuck" with a softer "oo" a quarter second after it. The loud
        // one is the beat: 1.035, 1.855, 2.680, 3.465, 4.270, 5.075, 5.895, 6.685, 7.475,
        // 8.275, 9.085 — eleven of them, 0.805 apart, and then a flourish that is not a call.
        var voice = root.AddComponent<AudioSource>();
        voice.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound/clock-cuckoo.mp3");
        voice.playOnAwake = false;
        voice.loop = false;
        voice.spatialBlend = 0f;
        voice.volume = 1f;
        voice.priority = 0;
        voice.dopplerLevel = 0f;

        var striker = root.AddComponent<ClockStriker>();
        Wire(striker, "movement", movement);
        Wire(striker, "door", openable);
        Wire(striker, "firstCall", 1.035f);
        Wire(striker, "callSpacing", 0.805f);
        Wire(striker, "callsInClip", 11);

        BuildKit.Hull(root, new Vector3(0f, 0.20f, Depth * 0.5f), new Vector3(Width, 0.44f, Depth));
        // Hangs on a wall with weights and a pendulum below it; lifting it makes no sense.
        Wire(root.AddComponent<Inspectable>(), "style", (int)InspectStyle.Zoom);

        return Save(root, "CuckooClock");
    }

    // ---------- the pocket watch ----------

    // Small enough to pick up, which is the point: it is the one clock the player can carry
    // from room to room and hold against another.
    [MenuItem("Tools/Escape Room/Clocks/Build Pocket Watch")]
    public static GameObject BuildPocketWatch()
    {
        Material silver = Silver(), glass = Glass();
        var root = new GameObject("PocketWatch");

        const float Diameter = 0.052f;

        Disc("Case", root.transform, new Vector3(0f, 0f, -0.004f), Diameter + 0.006f, 0.011f, silver);
        Disc("Bezel", root.transform, new Vector3(0f, 0f, 0.004f), Diameter + 0.004f, 0.004f, silver);
        Disc("Crystal", root.transform, new Vector3(0f, 0f, 0.007f), Diameter - 0.002f, 0.002f, glass);

        Cylinder("Stem", root.transform, new Vector3(0f, Diameter * 0.5f + 0.006f, 0f), 0.010f, 0.014f, silver);
        Ring("Bow", root.transform, new Vector3(0f, Diameter * 0.5f + 0.022f, 0f), 0.020f, 0.004f, silver);

        ClockDial dial = Dial(root.transform, new Vector3(0f, 0f, 0.005f), 0.29f);

        var movement = root.AddComponent<ClockMovement>();
        Wire(movement, "startHour", 1);
        Wire(movement, "startMinute", 45);
        Wire(movement, "minutesPerSecond", 1f / 60f);

        root.AddComponent<Mainspring>();

        BuildKit.Hull(root, new Vector3(0f, 0.008f, 0f), new Vector3(0.07f, 0.10f, 0.035f));
        Wire(root.AddComponent<Inspectable>(), "style", (int)InspectStyle.Turn);

        return Save(root, "PocketWatch");
    }

    // ---------- the alarm clock ----------

    // Twin bells, a hammer between them, and a fourth hand nobody looks at until they have
    // to. Meant to be scattered by the dozen: the case is deliberately plain so a shelf of
    // them reads as one man's habit rather than as a collection.
    [MenuItem("Tools/Escape Room/Clocks/Build Alarm Clock")]
    public static GameObject BuildAlarmClock()
    {
        Material steel = Silver(), brass = Brass(), dark = WoodDark(), red = AlarmRed();
        var root = new GameObject("AlarmClock");

        const float Diameter = 0.098f;
        float centre = Diameter * 0.5f + 0.018f;   // clear of the feet

        Disc("Case", root.transform, new Vector3(0f, centre, -0.006f), Diameter, 0.042f, steel);
        Disc("Rim", root.transform, new Vector3(0f, centre, 0.014f), Diameter + 0.006f, 0.008f, brass);

        foreach (int side in new[] { -1, 1 })
        {
            Sphere("Bell", root.transform, new Vector3(side * 0.040f, centre + 0.048f, -0.006f), 0.042f, brass);
            Box("BellStem", root.transform, new Vector3(side * 0.040f, centre + 0.030f, -0.006f),
                new Vector3(0.008f, 0.020f, 0.008f), steel);
            // Splayed feet, so it sits like a clock rather than rolls like a coin.
            GameObject foot = Box("Foot", root.transform, new Vector3(side * 0.030f, 0.010f, -0.006f),
                new Vector3(0.010f, 0.030f, 0.010f), steel);
            foot.transform.localRotation = Quaternion.Euler(0f, 0f, side * 22f);
        }

        Box("Hammer", root.transform, new Vector3(0f, centre + 0.056f, -0.006f),
            new Vector3(0.022f, 0.009f, 0.009f), steel);
        Cylinder("Backstop", root.transform, new Vector3(0f, 0.014f, -0.030f), 0.012f, 0.028f, steel);

        ClockDial dial = Dial(root.transform, new Vector3(0f, centre, 0.016f), 0.52f);

        // The alarm hand rides just behind the others on the same centre, in a colour that
        // separates it at a glance.
        Transform alarmPivot = BuildKit.HandPivot("AlarmHand", root.transform, 0.0f,
            new Vector3(0.006f, 0.030f, 0.003f), red);
        alarmPivot.localPosition = new Vector3(0f, centre, 0.013f);

        Transform lever = Group("ArmedLever", root.transform, new Vector3(0.040f, centre - 0.036f, 0.012f));
        Box("Flag", lever, new Vector3(0f, 0.012f, 0f), new Vector3(0.008f, 0.024f, 0.005f), red);
        Disc("Boss", lever, Vector3.zero, 0.014f, 0.006f, brass);

        var movement = root.AddComponent<ClockMovement>();
        Wire(movement, "startHour", 6);
        Wire(movement, "startMinute", 40);
        Wire(movement, "minutesPerSecond", 1f / 60f);

        var alarm = root.AddComponent<AlarmHand>();
        Wire(alarm, "hand", alarmPivot);
        Wire(alarm, "armedLever", lever);
        Wire(alarm, "setForHour", 6);
        Wire(alarm, "setForMinute", 0);
        Wire(alarm, "armed", true);

        root.AddComponent<Mainspring>();

        BuildKit.Hull(root, new Vector3(0f, centre, -0.004f), new Vector3(Diameter + 0.05f, Diameter + 0.10f, 0.06f));
        Wire(root.AddComponent<Inspectable>(), "style", (int)InspectStyle.Turn);

        return Save(root, "AlarmClock");
    }

    // ---------- the wristwatch ----------

    // The smallest clock in the house and the only one that belonged to a person rather
    // than to a room. Modelled lying face up with the strap splayed, the way a watch looks
    // when someone has taken it off and put it down.
    [MenuItem("Tools/Escape Room/Clocks/Build Wristwatch")]
    public static GameObject BuildWristwatch()
    {
        Material steel = Silver(), glass = Glass(), leather = Leather();
        var root = new GameObject("Wristwatch");

        const float Case = 0.036f;

        Disc("Caseback", root.transform, new Vector3(0f, 0f, -0.005f), Case, 0.009f, steel);
        Disc("Bezel", root.transform, new Vector3(0f, 0f, 0.003f), Case + 0.003f, 0.004f, steel);
        Disc("Crystal", root.transform, new Vector3(0f, 0f, 0.006f), Case - 0.004f, 0.002f, glass);

        // Crown at three o'clock, with its little grip ring.
        Cylinder("Crown", root.transform, new Vector3(Case * 0.5f + 0.004f, 0f, -0.001f), 0.007f, 0.006f, steel)
            .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        foreach (int end in new[] { -1, 1 })
        {
            foreach (int side in new[] { -1, 1 })
                Box("Lug", root.transform, new Vector3(side * 0.012f, end * (Case * 0.5f + 0.002f), -0.002f),
                    new Vector3(0.006f, 0.008f, 0.008f), steel);

            // Four tapering segments, each leaning a little further over than the last, so
            // the strap falls away instead of sticking out like a plank.
            for (int i = 0; i < 4; i++)
            {
                float t = i / 3f;
                float width = Mathf.Lerp(0.021f, 0.014f, t);
                GameObject seg = Box("Strap", root.transform,
                    new Vector3(0f, end * (Case * 0.5f + 0.012f + i * 0.019f), -0.004f - t * t * 0.008f),
                    new Vector3(width, 0.020f, 0.004f), leather);
                seg.transform.localRotation = Quaternion.Euler(-end * t * 26f, 0f, 0f);
            }
        }

        Box("Buckle", root.transform, new Vector3(0f, -(Case * 0.5f + 0.090f), -0.016f),
            new Vector3(0.017f, 0.014f, 0.003f), steel);

        ClockDial dial = Dial(root.transform, new Vector3(0f, 0f, 0.004f), 0.20f);

        var movement = root.AddComponent<ClockMovement>();
        Wire(movement, "startHour", 11);
        Wire(movement, "startMinute", 57);
        Wire(movement, "minutesPerSecond", 1f / 60f);
        // His own watch, and the one clock in the house that stopped for a reason.
        Wire(movement, "condition", (int)ClockCondition.Broken);

        root.AddComponent<Mainspring>();

        BuildKit.Hull(root, new Vector3(0f, 0f, -0.004f), new Vector3(0.07f, 0.22f, 0.04f));
        var inspectable = root.AddComponent<Inspectable>();
        Wire(inspectable, "style", (int)InspectStyle.Turn);

        return Save(root, "Wristwatch");
    }

    // ---------- shared sub-assemblies ----------

    // Nests the one dial prefab. Every clock in the house shows the time through this, which
    // is the whole reason it is a prefab and not a lump of geometry inside each case.
    private static ClockDial Dial(Transform parent, Vector3 localPos, float scale)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialPath);
        var dial = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        dial.transform.SetParent(parent, false);
        dial.transform.localPosition = localPos;
        dial.transform.localScale = Vector3.one * scale;
        return dial.GetComponent<ClockDial>();
    }

    // The crank, its window and its number reel, at whatever size the case can carry. The
    // collider is deliberately proud of everything so the crosshair finds the handle first.
    // The winding assembly: plate, number window, crank.
    //
    // `onBack` mirrors the whole thing through the case, which is where a winder belongs on
    // anything you can take down and turn over. It stays on the front only for clocks that
    // are fixed where they stand — a longcase bolted to a wall has no reachable back, and a
    // crank the player can never touch is not a mechanism, it is scenery.
    private static ClockWinder Winder(Transform parent, Vector3 localPos, float scale,
                                      ClockMovement movement, Mainspring spring = null,
                                      bool onBack = false)
    {
        Material dark = WoodDark(), brass = Brass(), reelMat = BuildKit.Load("ClockReel");
        float face = onBack ? -1f : 1f;

        Transform hub = Group("Winder", parent, localPos);
        hub.localScale = Vector3.one * scale;

        Box("Plate", hub, new Vector3(0f, 0.022f, 0.008f * face), new Vector3(0.098f, 0.120f, 0.014f), dark);

        Transform window = Group("Window", hub, new Vector3(0f, 0.062f, 0.016f * face));
        Box("Recess", window, Vector3.zero, new Vector3(0.046f, 0.034f, 0.005f), dark);
        GameObject reel = Quad("Reel", window, new Vector3(0f, 0f, 0.005f * face), 0.030f, reelMat);
        // The built-in quad already faces -Z, so a back-mounted reel needs no turning and a
        // front-mounted one needs half a turn. Quad() does the front case for us.
        if (onBack)
            reel.transform.localRotation = Quaternion.identity;

        GameObject shutter = Box("Shutter", window, new Vector3(0f, 0f, 0.009f * face),
            new Vector3(0.050f, 0.036f, 0.004f), brass);

        Transform crank = Group("Crank", hub, new Vector3(0f, 0f, 0.014f * face));
        crank.localRotation = Quaternion.Euler(0f, 0f, 118f);
        Disc("Hub", crank, Vector3.zero, 0.050f, 0.024f, brass);
        Disc("Collar", crank, new Vector3(0f, 0f, 0.014f * face), 0.030f, 0.010f, brass);
        Box("Arm", crank, new Vector3(0f, 0.088f, 0.010f * face), new Vector3(0.018f, 0.170f, 0.011f), brass);
        Disc("Handle", crank, new Vector3(0f, 0.168f, 0.018f * face), 0.017f, 0.030f, brass);

        BuildKit.Hull(hub.gameObject, new Vector3(0f, 0.02f, 0.030f * face), new Vector3(0.34f, 0.30f, 0.090f));

        // The spring lives in the winding assembly, which is where it lives in a real clock —
        // unless the caller has one already, as a longcase does, where the power is the
        // falling weights and the crank only hauls them back up.
        if (spring == null)
            spring = hub.gameObject.AddComponent<Mainspring>();

        var winder = hub.gameObject.AddComponent<ClockWinder>();
        Wire(winder, "movement", movement);
        Wire(winder, "crank", crank);
        Wire(winder, "shutter", shutter.transform);
        Wire(winder, "numberReel", reel.GetComponent<Renderer>());
        return winder;
    }

    private static GameObject Save(GameObject root, string name)
    {
        BuildKit.EnsureFolder(PrefabFolder);
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + name + ".prefab");
        Object.DestroyImmediate(root);
        return asset;
    }

    // ---------- shorthand ----------

    private static Material Wood() => BuildKit.Load("ClockCase");
    private static Material WoodDark() => BuildKit.Load("ClockCaseDark");
    private static Material Brass() => BuildKit.Load("ClockBrass");
    private static Material Glass() => BuildKit.Glass("CaseGlass", new Color(0.72f, 0.82f, 0.80f, 0.20f));
    private static Material WoodLight() => BuildKit.Opaque("ClockCaseLight", new Color(0.58f, 0.40f, 0.24f), 0.25f, 0f, null);
    private static Material Silver() => BuildKit.Opaque("ClockSilver", new Color(0.78f, 0.79f, 0.81f), 0.80f, 0.9f, null);
    private static Material AlarmRed() => BuildKit.Opaque("ClockAlarmHand", new Color(0.62f, 0.13f, 0.10f), 0.35f, 0f, null);
    private static Material Leather() => BuildKit.Opaque("ClockStrap", new Color(0.26f, 0.16f, 0.11f), 0.18f, 0f, null);

    private static Transform Group(string n, Transform p, Vector3 v) => BuildKit.Group(n, p, v);
    private static GameObject Box(string n, Transform p, Vector3 v, Vector3 s, Material m) => BuildKit.Box(n, p, v, s, m);
    private static GameObject Disc(string n, Transform p, Vector3 v, float d, float t, Material m) => BuildKit.Disc(n, p, v, d, t, m);
    private static GameObject Cylinder(string n, Transform p, Vector3 v, float d, float h, Material m) => BuildKit.Cylinder(n, p, v, d, h, m);
    private static GameObject Sphere(string n, Transform p, Vector3 v, float d, Material m) => BuildKit.Sphere(n, p, v, d, m);
    private static GameObject Quad(string n, Transform p, Vector3 v, float s, Material m) => BuildKit.Quad(n, p, v, s, m);
    private static Transform Ring(string n, Transform p, Vector3 v, float d, float t, Material m) => BuildKit.Ring(n, p, v, d, t, m);
    private static void Wire(Object t, string prop, object v) => BuildKit.Wire(t, prop, v);
}
