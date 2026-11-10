using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Builds the shell of the house: floors, ceilings, and walls with doorways and windows cut
// through them, plus the trim that stops a wall reading as a bare plane.
//
// The plan lives here as data — five rectangles and a handful of openings — so moving a
// room means editing two numbers rather than dragging thirty boxes. Nothing in here knows
// anything about clocks or puzzles; props go in on top afterwards and are never touched by
// a rebuild.
//
//                          z = 9.6
//     +-------------------+--------------------+
//     |                   |                    |
//     |   II  ห้องช่าง      |   III ห้องนั่งเล่น    |
//     |                   |                    |
//     +---------[door]----+---------+----------+  z = 2.6
//     |                   |         [door]     |
//     |   I   หน้าร้าน      |   IV  ห้องนอน       +---+
//     |                   |                    | V |
//     +-------------------+--------------------+---+  z = -2.6
//   x = -3.1            x = 3.1            x = 10.1  x = 13.1
//
public static class HouseBuilder
{
    private const string HouseRoot = "House";
    private const float Thickness = 0.2f;
    private const float WallTop = 2.8f;
    private const float CeilingDepth = 0.2f;

    private const float SkirtHeight = 0.14f;
    private const float SkirtDepth = 0.022f;
    private const float ArchitraveWidth = 0.085f;
    private const float ArchitraveDepth = 0.020f;

    // The plan is an asset now, not an array in this file. Moving a room is an edit in the
    // Inspector; it was a code change, which is exactly the kind of thing that made the
    // "front-end people can work without a programmer" claim untrue.
    private static HousePlan Plan
    {
        get
        {
            string[] found = AssetDatabase.FindAssets("t:HousePlan");

            if (found.Length == 0)
                throw new System.InvalidOperationException(
                    "No HousePlan asset in the project. Create > Escape Room > House Plan.");

            return AssetDatabase.LoadAssetAtPath<HousePlan>(AssetDatabase.GUIDToAssetPath(found[0]));
        }
    }

    [MenuItem("Tools/Escape Room/House/Build Shell")]
    public static void BuildShell()
    {
        HousePlan plan = Plan;

        Material wall = Plaster();
        Material ceiling = Ceiling();
        Material trim = Trim();
        Material glass = WindowGlass();
        Material night = Night();

        GameObject house = GameObject.Find(HouseRoot) ?? new GameObject(HouseRoot);

        // Found through the parent rather than by path: `GameObject.Find` cannot see an
        // object that is switched off, and a shell hidden while somebody worked on the props
        // is a shell this would build a second copy of, walls standing through both.
        Transform old = house.transform.Find("Shell");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);
        var shell = new GameObject("Shell");
        shell.transform.SetParent(house.transform, false);

        foreach (HousePlan.Room room in plan.Rooms)
        {
            Transform group = BuildKit.Group(room.Name, shell.transform, Vector3.zero);

            float cx = (room.X0 + room.X1) * 0.5f;
            float cz = (room.Z0 + room.Z1) * 0.5f;
            float w = room.X1 - room.X0;
            float d = room.Z1 - room.Z0;

            GameObject floor = BuildKit.Box("Floor", group, new Vector3(cx, -0.01f, cz),
                new Vector3(w, 0.02f, d), FloorMaterial(room));
            BuildKit.Hull(floor, Vector3.zero, Vector3.one);
            BuildKit.Tile(floor, room.FloorTile);

            // A roofless room is one something else covers — the last room is under glass —
            // and capping it in plaster first would put the ceiling inside the dome.
            if (room.Roofless)
                continue;

            GameObject cap = BuildKit.Box("Ceiling", group,
                new Vector3(cx, WallTop + CeilingDepth * 0.5f, cz),
                new Vector3(w, CeilingDepth, d), ceiling);
            BuildKit.Hull(cap, Vector3.zero, Vector3.one);
            BuildKit.Tile(cap, 2.4f);
        }

        Transform walls = BuildKit.Group("Walls", shell.transform, Vector3.zero);
        foreach (HousePlan.Line line in plan.Walls)
            BuildLine(plan, walls, line, wall, trim, glass, night);

        Debug.Log("House shell rebuilt from " + plan.name + ": " + plan.Rooms.Length + " rooms, " + plan.Walls.Length + " wall runs.");
        Selection.activeObject = shell;
    }

    // Cuts the openings out of one run and puts back what is left: full-height wall between
    // them, an apron under every window, a lintel over everything, and trim on both faces.
    private static void BuildLine(HousePlan plan, Transform parent, HousePlan.Line line, Material wall, Material trim,
                                  Material glass, Material night)
    {
        string axis = line.AlongX ? "X" : "Z";
        Transform group = BuildKit.Group("Run_" + axis + "_" + line.Fixed.ToString("0.0"), parent, Vector3.zero);

        var edges = new List<float> { line.From, line.To };
        foreach (HousePlan.Opening o in line.Openings)
        {
            edges.Add(plan.LeftOf(o));
            edges.Add(plan.RightOf(o));
        }
        edges.Sort();

        for (int i = 0; i + 1 < edges.Count; i++)
        {
            float from = edges[i], to = edges[i + 1];
            if (to - from <= 0.001f)
                continue;

            float middle = (from + to) * 0.5f;
            bool inside = false;

            foreach (HousePlan.Opening o in line.Openings)
            {
                if (middle <= plan.LeftOf(o) || middle >= plan.RightOf(o))
                    continue;

                inside = true;
                Slab(group, "Apron", line, from, to, 0f, plan.BottomOf(o), wall);
                Slab(group, "Lintel", line, from, to, plan.TopOf(o), WallTop, wall);

                if (plan.IsGlazed(o))
                {
                    Slab(group, "Glazing", line, from + 0.02f, to - 0.02f, plan.BottomOf(o) + 0.02f, plan.TopOf(o) - 0.02f, glass, 0.03f);

                    // Wide, and held close in. A sight line through the edge of the opening
                    // spreads with distance, so a small patch a long way out shows the bare
                    // skybox round its edges the moment the window is looked at from the
                    // side. Close and generous beats far and neat.
                    //
                    // The nudge is what keeps two windows on one wall from sharing a plane:
                    // it cannot span the whole run, since a run may be outdoors at one end
                    // and against the next room at the other.
                    float depth = 0.26f + Mathf.Abs(o.Centre % 0.07f);
                    Slab(group, "Night", line, from - 1.5f, to + 1.5f, plan.BottomOf(o) - 1.3f, plan.TopOf(o) + 1.0f,
                         night, 0.04f, line.Outward * depth);
                }
                break;
            }

            if (!inside)
            {
                Slab(group, "Wall", line, from, to, 0f, WallTop, wall);
                Skirting(group, line, from, to, trim);
            }
        }

        foreach (HousePlan.Opening o in line.Openings)
            Architrave(plan, group, line, o, trim);
    }

    // A moulding round the foot of the wall, on both faces. Cheap, and it is the single
    // thing that most stops a room reading as six flat planes.
    private static void Skirting(Transform parent, HousePlan.Line line, float from, float to, Material trim)
    {
        foreach (int face in new[] { -1, 1 })
        {
            float offset = face * (Thickness + SkirtDepth) * 0.5f;
            float along = (from + to) * 0.5f;
            float length = to - from;

            Vector3 centre = line.AlongX
                ? new Vector3(along, SkirtHeight * 0.5f, line.Fixed + offset)
                : new Vector3(line.Fixed + offset, SkirtHeight * 0.5f, along);
            Vector3 size = line.AlongX
                ? new Vector3(length, SkirtHeight, SkirtDepth)
                : new Vector3(SkirtDepth, SkirtHeight, length);

            BuildKit.Box("Skirting", parent, centre, size, trim);
        }
    }

    // A frame round each opening, on both faces: two uprights and a head.
    private static void Architrave(HousePlan plan, Transform parent, HousePlan.Line line, HousePlan.Opening o, Material trim)
    {
        foreach (int face in new[] { -1, 1 })
        {
            float offset = face * (Thickness + ArchitraveDepth) * 0.5f;
            float half = ArchitraveWidth * 0.5f;

            foreach (int side in new[] { -1, 1 })
            {
                float along = o.Centre + side * (plan.WidthOf(o) * 0.5f + half);
                float height = plan.TopOf(o) - plan.BottomOf(o) + ArchitraveWidth;
                float mid = (plan.BottomOf(o) + plan.TopOf(o)) * 0.5f;

                Vector3 c = line.AlongX
                    ? new Vector3(along, mid, line.Fixed + offset)
                    : new Vector3(line.Fixed + offset, mid, along);
                Vector3 s = line.AlongX
                    ? new Vector3(ArchitraveWidth, height, ArchitraveDepth)
                    : new Vector3(ArchitraveDepth, height, ArchitraveWidth);

                BuildKit.Box("Architrave", parent, c, s, trim);
            }

            float width = plan.WidthOf(o) + ArchitraveWidth * 2f;
            Vector3 hc = line.AlongX
                ? new Vector3(o.Centre, plan.TopOf(o) + half, line.Fixed + offset)
                : new Vector3(line.Fixed + offset, plan.TopOf(o) + half, o.Centre);
            Vector3 hs = line.AlongX
                ? new Vector3(width, ArchitraveWidth, ArchitraveDepth)
                : new Vector3(ArchitraveDepth, ArchitraveWidth, width);

            BuildKit.Box("ArchitraveHead", parent, hc, hs, trim);

            if (plan.BottomOf(o) <= 0.001f)
                continue;

            // Windows get a sill on the inside as well.
            Vector3 sc = line.AlongX
                ? new Vector3(o.Centre, plan.BottomOf(o) - half, line.Fixed + offset)
                : new Vector3(line.Fixed + offset, plan.BottomOf(o) - half, o.Centre);
            Vector3 ss = line.AlongX
                ? new Vector3(width, ArchitraveWidth, ArchitraveDepth * 2.4f)
                : new Vector3(ArchitraveDepth * 2.4f, ArchitraveWidth, width);

            BuildKit.Box("Sill", parent, sc, ss, trim);
        }
    }

    private static void Slab(Transform parent, string name, HousePlan.Line line, float from, float to,
                             float bottom, float top, Material material,
                             float thickness = Thickness, float push = 0f)
    {
        float length = to - from;
        float height = top - bottom;

        // An opening that reaches the ceiling has no lintel, and two adjacent openings can
        // leave a span with nothing in it.
        if (length <= 0.001f || height <= 0.001f)
            return;

        float along = (from + to) * 0.5f;
        Vector3 centre = line.AlongX
            ? new Vector3(along, bottom + height * 0.5f, line.Fixed + push)
            : new Vector3(line.Fixed + push, bottom + height * 0.5f, along);
        Vector3 size = line.AlongX
            ? new Vector3(length, height, thickness)
            : new Vector3(thickness, height, length);

        GameObject go = BuildKit.Box(name, parent, centre, size, material);

        // Glass and the view beyond it are scenery; only real wall stops the player.
        if (material.name.Contains("Glass") || material.name.Contains("Night"))
            return;

        BuildKit.Tile(go, 1.6f);
        BuildKit.Hull(go, Vector3.zero, Vector3.one);
    }

    // A panelled door leaf, hinged at its own origin so it can be dropped straight into a
    // doorway and given a SwingMotion. Built along local +x, the way every other hinged
    // thing in the project is.
    [MenuItem("Tools/Escape Room/House/Build Door Leaf")]
    public static GameObject BuildDoorLeaf()
    {
        BuildKit.EnsureFolder("Assets/Prefabs/House");
        Material wood = BuildKit.Load("Door") ?? BuildKit.Load("Wood");
        Material brass = BuildKit.Load("ClockBrass");

        // Sized off the plan, not written down again. The leaf was 1.06 in a 1.10 opening
        // and hinged on one edge of it, which put the whole 40 mm in one slot down the
        // closing edge — the strip of daylight this comment used to warn about, moved
        // sideways. It leaves a real door's reveal now, the same on both sides and at the
        // head, and the plan is the one place a doorway's size is stated.
        const float Reveal = 0.006f;
        const float T = 0.045f;
        float W = Plan.DoorWidth - Reveal * 2f;
        float H = Plan.DoorTop - Reveal;

        // Everything but the hinges is set in from the hinge edge by that reveal. The
        // barrels stay on the edge itself, standing proud of the frame, which is where the
        // barrel of a real hinge is.
        float mid = Reveal + W * 0.5f;
        float latch = Reveal + W - 0.10f;

        var root = new GameObject("DoorLeaf");

        BuildKit.Box("Leaf", root.transform, new Vector3(mid, H * 0.5f, 0f),
            new Vector3(W, H, T), wood);

        // Two sunken panels, which is all it takes to stop a door reading as a plank.
        foreach (float y in new[] { H * 0.30f, H * 0.72f })
        foreach (int face in new[] { -1, 1 })
            BuildKit.Box("Panel", root.transform,
                new Vector3(mid, y, face * (T * 0.5f + 0.004f)),
                new Vector3(W - 0.22f, H * 0.30f, 0.008f), wood);

        BuildKit.Box("Rail", root.transform, new Vector3(mid, H * 0.51f, 0f),
            new Vector3(W, 0.10f, T + 0.010f), wood);

        foreach (int face in new[] { -1, 1 })
        {
            BuildKit.Sphere("Knob", root.transform,
                new Vector3(latch, 1.02f, face * (T * 0.5f + 0.022f)), 0.058f, brass);
            BuildKit.Box("Plate", root.transform,
                new Vector3(latch, 1.02f, face * (T * 0.5f + 0.004f)),
                new Vector3(0.075f, 0.16f, 0.008f), brass);

            // The escutcheon under the knob. Every locked door in this house is opened by
            // something the player is carrying, and a keyhole is where you look for that.
            BuildKit.Box("Escutcheon", root.transform,
                new Vector3(latch, 0.90f, face * (T * 0.5f + 0.004f)),
                new Vector3(0.036f, 0.048f, 0.006f), brass);
        }

        // Hinges, on the edge the door actually turns about. Their absence is the quiet
        // reason a door built out of one box reads as a hatch: nothing says which side is
        // fixed until it swings, and by then the player has already tried the wrong edge.
        foreach (float y in new[] { 0.28f, H * 0.5f, H - 0.28f })
        {
            BuildKit.Box("HingeLeaf", root.transform, new Vector3(0.045f, y, 0f),
                new Vector3(0.09f, 0.12f, T + 0.004f), brass);

            GameObject barrel = BuildKit.Cylinder("HingeBarrel", root.transform,
                new Vector3(0f, y, 0f), 0.026f, 0.13f, brass);
            barrel.transform.localRotation = Quaternion.identity;
        }

        // Fat enough for the crosshair to find without being twice the thickness of the
        // leaf: at 0.09 the collider alone put four centimetres of door inside the wall
        // every time one swung open.
        BuildKit.Hull(root, new Vector3(mid, H * 0.5f, 0f), new Vector3(W, H, 0.055f));

        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/House/DoorLeaf.prefab");
        Object.DestroyImmediate(root);
        return asset;
    }

    // The street door, at the front of the showroom, and the one door in the house that is
    // only ever scenery. It carries no Openable, no motion and no lock: the way out is the
    // door at the far end of room five, and a front door that answers E is a front door the
    // player will spend the game coming back to try.
    //
    // It is glazed, which is what tells it apart from the four panelled doors inside the
    // house — and what it shows through the glass is the same painted night every window in
    // the house shows. That plane is part of the prefab rather than cut by the shell,
    // because the plan calls this a doorway and only a window gets a night of its own. It
    // hangs a little further out than the window's beside it so the two never share a plane.
    [MenuItem("Tools/Escape Room/House/Build Shop Door")]
    public static GameObject BuildShopDoor()
    {
        BuildKit.EnsureFolder("Assets/Prefabs/House");
        Material wood = BuildKit.Load("Door") ?? BuildKit.Load("Wood");
        Material brass = BuildKit.Load("ClockBrass");
        Material glass = WindowGlass();
        Material night = Night();

        const float Reveal = 0.006f;
        const float T = 0.045f;
        const float Stile = 0.115f;
        float W = Plan.DoorWidth - Reveal * 2f;
        float H = Plan.DoorTop - Reveal;

        float mid = Reveal + W * 0.5f;
        float inner = W - Stile * 2f;      // across, between the stiles
        float latch = Reveal + W - 0.10f;

        var root = new GameObject("ShopDoor");

        // The frame: two stiles the full height, and three rails across between them.
        foreach (int side in new[] { -1, 1 })
            BuildKit.Box("Stile", root.transform,
                new Vector3(mid + side * (W - Stile) * 0.5f, H * 0.5f, 0f),
                new Vector3(Stile, H, T), wood);

        BuildKit.Box("RailTop", root.transform, new Vector3(mid, H - 0.055f, 0f),
            new Vector3(inner, 0.110f, T), wood);
        BuildKit.Box("RailLock", root.transform, new Vector3(mid, 0.960f, 0f),
            new Vector3(inner, 0.200f, T), wood);
        BuildKit.Box("RailBottom", root.transform, new Vector3(mid, 0.115f, 0f),
            new Vector3(inner, 0.230f, T), wood);

        // The light, in four panes. A single sheet reads as a hole; the bars are what say
        // this is a shop's street door rather than a patio one.
        float lightBottom = 1.060f, lightTop = H - 0.110f;
        float lightMid = (lightBottom + lightTop) * 0.5f;
        float lightHeight = lightTop - lightBottom;

        BuildKit.Box("Glazing", root.transform, new Vector3(mid, lightMid, 0f),
            new Vector3(inner, lightHeight, 0.008f), glass);
        BuildKit.Box("Bar", root.transform, new Vector3(mid, lightMid, 0f),
            new Vector3(0.028f, lightHeight, T - 0.004f), wood);
        BuildKit.Box("Bar", root.transform, new Vector3(mid, lightMid, 0f),
            new Vector3(inner, 0.028f, T - 0.004f), wood);

        // The board below the lock rail, with a sunken panel on each face.
        BuildKit.Box("Board", root.transform, new Vector3(mid, 0.545f, 0f),
            new Vector3(inner, 0.630f, 0.030f), wood);

        foreach (int face in new[] { -1, 1 })
        foreach (int side in new[] { -1, 1 })
            BuildKit.Box("Panel", root.transform,
                new Vector3(mid + side * inner * 0.24f, 0.545f, face * 0.019f),
                new Vector3(inner * 0.40f, 0.470f, 0.008f), wood);

        foreach (int face in new[] { -1, 1 })
        {
            BuildKit.Sphere("Knob", root.transform,
                new Vector3(latch, 1.02f, face * (T * 0.5f + 0.022f)), 0.058f, brass);

            // A letterplate on the lock rail. Nothing posts anything through it; it is one
            // more thing that only ever goes on the door a house is entered by.
            BuildKit.Box("Letterplate", root.transform,
                new Vector3(mid, 0.960f, face * (T * 0.5f + 0.004f)),
                new Vector3(0.300f, 0.055f, 0.008f), brass);
        }

        foreach (float y in new[] { 0.28f, H * 0.5f, H - 0.28f })
        {
            BuildKit.Box("HingeLeaf", root.transform, new Vector3(0.045f, y, 0f),
                new Vector3(0.09f, 0.12f, T + 0.004f), brass);
            BuildKit.Cylinder("HingeBarrel", root.transform, new Vector3(0f, y, 0f), 0.026f, 0.13f, brass);
        }

        // Outdoors, seen through the light. Wide and tall well past the opening, because a
        // sight line through the edge of a doorway spreads with distance and a neat little
        // patch shows the bare skybox round its corners the moment the door is looked at
        // from the side.
        BuildKit.Box("Beyond", root.transform, new Vector3(mid, 1.20f, -0.34f),
            new Vector3(3.5f, 4.4f, 0.04f), night);

        BuildKit.Hull(root, new Vector3(mid, H * 0.5f, 0f), new Vector3(W, H, 0.055f));

        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/House/ShopDoor.prefab");
        Object.DestroyImmediate(root);
        return asset;
    }

    private static Material FloorMaterial(HousePlan.Room room)
    {
        string leaf = room.Name.Substring(room.Name.LastIndexOf('_') + 1);
        Texture2D map = room.Boards
            ? TextureKit.Floorboards("Floor_" + leaf + "_Map", Color.white, new Color(0.62f, 0.58f, 0.54f))
            : TextureKit.Carpet("Floor_" + leaf + "_Map", Color.white);

        // White in the map, colour in the material: one drawing serves five floors and the
        // room keeps its own shade, which is what tells the player they have crossed over.
        return BuildKit.Opaque("Floor_" + leaf, room.Floor, room.Boards ? 0.16f : 0.05f, 0f, map);
    }

    private static Material Plaster() =>
        BuildKit.Opaque("Wall", new Color(0.78f, 0.75f, 0.69f), 0.06f, 0f,
                        TextureKit.Plaster("WallPlaster", Color.white));

    private static Material Ceiling() =>
        BuildKit.Opaque("Ceiling", new Color(0.83f, 0.82f, 0.80f), 0.04f, 0f,
                        TextureKit.Plaster("WallPlaster", Color.white));

    private static Material Trim() =>
        BuildKit.Opaque("HouseTrim", new Color(0.86f, 0.84f, 0.78f), 0.18f, 0f, null);

    // Nearly clear, and smooth enough to catch the room lamp. Glass with any body to it
    // lights up like a painted panel and the night behind it never gets a look in.
    private static Material WindowGlass() =>
        BuildKit.Glass("WindowGlass", new Color(0.38f, 0.44f, 0.52f, 0.10f));

    // The sky emits rather than reflects: it is meant to be the same night whatever the
    // lamp in the room is doing, and an unlit plane out in the dark reads as a black hole.
    private static Material Night()
    {
        Texture2D sky = TextureKit.NightSky("NightSky");
        Material mat = BuildKit.Opaque("NightBeyond", Color.black, 0f, 0f, sky);

        mat.EnableKeyword("_EMISSION");
        mat.SetTexture("_EmissionMap", sky);
        mat.SetColor("_EmissionColor", Color.white);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
