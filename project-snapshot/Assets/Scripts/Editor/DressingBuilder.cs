using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Everything in the house that is neither shell, clock, nor puzzle: the fire in the grate,
// the chair in front of it, rugs, pictures, the clutter on a workbench.
//
// It rebuilds into one `Dressing` group per room and touches nothing else, the same bargain
// HouseBuilder makes with `Shell`. Puzzle props and clocks are placed elsewhere and are
// never moved from here, so a dressing pass can be run over and over while the game is
// being tuned.
public static class DressingBuilder
{
    private const string PropsRoot = "House/Props";
    private const float RailHeight = 2.35f;

    [MenuItem("Tools/Escape Room/House/Dress All Rooms")]
    public static void DressAll()
    {
        Retexture();
        Rehang();
        Showroom();
        Workshop();
        Parlour();
        Bedroom();
        Behind();

        Debug.Log("Dressing rebuilt for all five rooms.");
    }

    // ---------- shared materials ----------

    // Puts a base map on the materials that were authored as flat colour: furniture, doors,
    // clock cases, paper. They keep the colour they already have — every map here is drawn
    // white, so one piece of grain serves every shade of timber in the house and a colour
    // change stays a one-line edit.
    [MenuItem("Tools/Escape Room/House/Retexture Shared Materials")]
    public static void Retexture()
    {
        Texture2D grain = TextureKit.WoodGrain("OakGrain", Color.white, new Color(0.55f, 0.45f, 0.36f));
        Texture2D weave = TextureKit.Fabric("PlushWeave", Color.white);
        Texture2D tooth = TextureKit.Plaster("WallPlaster", Color.white);

        foreach (string timber in new[] { "Wood", "Door", "ClockCase", "ClockCaseDark", "ClockCaseLight" })
            Skin(timber, grain);

        Skin("ClockStrap", weave);
        Skin("Paper", tooth);
        Skin("NotePaper", tooth);

        // The leaflet on the showroom wall. Its own map, at the sheet's aspect, because the
        // border printed on it has to stay even and must never be tiled.
        Skin("CardPaper", TextureKit.Paper("CardPaper", 1024, 1408));

        ScaleGrainToProps();
        AssetDatabase.SaveAssets();
    }

    // A cube UVs each face 0..1, so a two-metre chimney breast and a ten-centimetre drawer
    // front wear the same single tile of grain — one stretched into wallpaper, the other
    // squeezed into noise. Anything big enough for that to show gets its map scaled to its
    // own size, the same way the walls and floors do.
    private static void ScaleGrainToProps()
    {
        const float ShowsAt = 0.55f;

        foreach (Renderer r in GameObject.Find(PropsRoot).GetComponentsInChildren<Renderer>(true))
        {
            if (r.sharedMaterial == null || r.sharedMaterial.GetTexture("_BaseMap") == null)
                continue;

            Vector3 s = r.transform.lossyScale;
            if (Mathf.Max(s.x, Mathf.Max(s.y, s.z)) < ShowsAt)
                continue;

            BuildKit.Tile(r.gameObject, 0.85f);
        }
    }

    private static void Skin(string material, Texture map)
    {
        Material mat = BuildKit.Load(material);
        mat.SetTexture("_BaseMap", map);
        EditorUtility.SetDirty(mat);
    }

    // ---------- I — the showroom ----------

    [MenuItem("Tools/Escape Room/House/Dress I Showroom")]
    public static void Showroom()
    {
        Transform g = Fresh("Room_I_Showroom");
        Material oak = Oak(), dark = DarkOak(), brass = Brass(), gilt = Gilt();

        PictureRail(g, -3f, -2.5f, 3f, 2.5f);

        // A shop counter across the corner the player walks in past. It is the first thing
        // that says this room sold clocks rather than merely held them.
        Transform counter = Group("Counter", g, new Vector3(-2.35f, 0f, 0.55f));
        Box("Front", counter, new Vector3(0f, 0.48f, 0f), new Vector3(0.62f, 0.96f, 2.40f), dark);
        Box("Top", counter, new Vector3(0.02f, 0.98f, 0f), new Vector3(0.74f, 0.05f, 2.56f), oak);
        Box("Kick", counter, new Vector3(0f, 0.05f, 0f), new Vector3(0.68f, 0.10f, 2.46f), dark);
        for (int i = 0; i < 3; i++)
        {
            float z = -0.8f + i * 0.8f;
            Box("Panel", counter, new Vector3(0.31f, 0.52f, z), new Vector3(0.02f, 0.62f, 0.62f), oak);
        }
        Hull(counter.gameObject, new Vector3(0.02f, 0.50f, 0f), new Vector3(0.74f, 1.00f, 2.56f));

        // A day book and a bell, the two things that live on a shop counter.
        Box("DayBook", counter, new Vector3(0.02f, 1.03f, 0.55f), new Vector3(0.26f, 0.05f, 0.34f), Paper());
        Dome("Bell", counter, new Vector3(0.02f, 1.04f, -0.62f), 0.11f, brass);
        Box("BellBase", counter, new Vector3(0.02f, 1.01f, -0.62f), new Vector3(0.14f, 0.02f, 0.14f), brass);

        // Boxed stock on a shelf: a dealer's back room stacked where customers can see it.
        Transform shelf = Group("StockShelf", g, new Vector3(-1.80f, 0f, 2.42f));
        shelf.localRotation = Quaternion.Euler(0f, -90f, 0f);
        for (int tier = 0; tier < 2; tier++)
        {
            float y = 1.05f + tier * 0.62f;
            Box("Board", shelf, new Vector3(-0.14f, y, 0f), new Vector3(0.28f, 0.04f, 1.90f), oak);
            Box("Bracket", shelf, new Vector3(-0.06f, y - 0.10f, -0.80f), new Vector3(0.12f, 0.16f, 0.04f), dark);
            Box("Bracket", shelf, new Vector3(-0.06f, y - 0.10f, 0.80f), new Vector3(0.12f, 0.16f, 0.04f), dark);

            for (int i = 0; i < 5; i++)
            {
                float z = -0.72f + i * 0.36f;
                float h = 0.20f + (i % 3) * 0.05f;
                Box("Case", shelf, new Vector3(-0.14f, y + 0.02f + h * 0.5f, z),
                    new Vector3(0.22f, h, 0.28f), i % 2 == 0 ? dark : oak);
                Box("Label", shelf, new Vector3(-0.25f, y + 0.02f + h * 0.5f, z),
                    new Vector3(0.005f, 0.07f, 0.12f), Paper());
            }
        }

        Rug(g, new Vector3(0.10f, 0f, 0.20f), 2.6f, 2.0f, new Color(0.36f, 0.20f, 0.16f), "RugShowroom");

        // The mat inside the street door. The door never opens and says nothing, so this is
        // what tells the player which door it is: nobody lays a welcome mat inside a room.
        //
        // It reads for someone walking towards the door, not for someone coming in through
        // it. The player is always on this side of it.
        Doormat(g, new Vector3(1.30f, 0f, -2.13f), "WELCOME");

        // Two trade prints, in the gaps left by the street door and the window. The south
        // wall of this room carries three wall clocks at chest height, a doorway to 2.15 and
        // a window from 0.95 to 2.10, so a print has to sit above the clocks and clear of
        // both openings.
        Picture(g, new Vector3(0f, 1.86f, -2.46f), 180f, 0.62f, 0.46f, gilt, 1);
        Picture(g, new Vector3(2.45f, 1.86f, -2.46f), 180f, 0.44f, 0.56f, gilt, 2);
    }

    // ---------- II — the workshop ----------

    [MenuItem("Tools/Escape Room/House/Dress II Workshop")]
    public static void Workshop()
    {
        Transform g = Fresh("Room_II_Workshop");
        Material oak = Oak(), dark = DarkOak(), brass = Brass(), iron = Iron(), glass = BuildKit.Load("CaseGlass");

        PictureRail(g, -3f, 2.7f, 3f, 9.5f);

        // A bank of little drawers, the one piece of furniture every watchmaker owns. Its
        // colliders go on the carcass boards, never round the volume: the rule that has
        // already cost this project three unreachable keys.
        Transform cab = Group("PartsCabinet", g, new Vector3(2.82f, 0f, 8.40f));
        const float CabW = 0.34f, CabH = 1.45f, CabD = 1.30f;
        Box("Back", cab, new Vector3(CabW * 0.5f - 0.01f, CabH * 0.5f, 0f), new Vector3(0.03f, CabH, CabD), dark);
        Box("Top", cab, new Vector3(0f, CabH - 0.015f, 0f), new Vector3(CabW, 0.04f, CabD + 0.03f), oak);
        Box("Bottom", cab, new Vector3(0f, 0.30f, 0f), new Vector3(CabW, 0.04f, CabD), dark);
        Box("Plinth", cab, new Vector3(0f, 0.14f, 0f), new Vector3(CabW - 0.04f, 0.28f, CabD - 0.04f), dark);
        foreach (int side in new[] { -1, 1 })
            Box("Side", cab, new Vector3(0f, 0.75f, side * CabD * 0.5f), new Vector3(CabW, 0.94f, 0.03f), dark);

        for (int row = 0; row < 6; row++)
        for (int col = 0; col < 4; col++)
        {
            float y = 0.38f + row * 0.15f;
            float z = -0.48f + col * 0.32f;
            Box("Drawer", cab, new Vector3(-CabW * 0.5f + 0.03f, y, z), new Vector3(0.03f, 0.13f, 0.28f), oak);
            Box("Pull", cab, new Vector3(-CabW * 0.5f + 0.01f, y, z), new Vector3(0.02f, 0.02f, 0.07f), brass);
        }
        Hull(cab.gameObject, new Vector3(0.04f, 0.75f, 0f), new Vector3(CabW, 1.45f, CabD));

        // The bench itself is placed elsewhere; what it was missing was everything a person
        // leaves on one.
        Transform bench = Group("BenchClutter", g, new Vector3(-2.30f, 0.94f, 6.00f));

        Transform lamp = Group("Lamp", bench, new Vector3(-0.34f, 0f, 1.28f));
        Cyl("Base", lamp, new Vector3(0f, 0.015f, 0f), 0.16f, 0.03f, iron);
        Cyl("Stem", lamp, new Vector3(0f, 0.22f, 0f), 0.022f, 0.44f, iron);
        Box("Arm", lamp, new Vector3(0.10f, 0.43f, 0f), new Vector3(0.22f, 0.02f, 0.02f), iron);
        GameObject shade = Cone("Shade", lamp, new Vector3(0.20f, 0.39f, 0f), 0.20f, 0.14f, iron);
        shade.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);

        // Jars of parts. Glass over a dark heap of something reads as "spares" instantly.
        for (int i = 0; i < 4; i++)
        {
            float z = -1.24f + i * 0.28f;
            Transform jar = Group("Jar", bench, new Vector3(-0.30f, 0f, z));
            Cyl("Body", jar, new Vector3(0f, 0.055f, 0f), 0.085f, 0.11f, glass);
            Cyl("Contents", jar, new Vector3(0f, 0.022f, 0f), 0.075f, 0.045f, i % 2 == 0 ? brass : iron);
            Cyl("Lid", jar, new Vector3(0f, 0.116f, 0f), 0.088f, 0.014f, brass);
        }

        // Files and tweezers, laid out the way a bench worker lines them up.
        for (int i = 0; i < 6; i++)
        {
            float z = -0.55f + i * 0.13f;
            Box("Tool", bench, new Vector3(0.30f, 0.008f, z), new Vector3(0.20f, 0.012f, 0.016f), iron);
            Box("Handle", bench, new Vector3(0.16f, 0.012f, z), new Vector3(0.09f, 0.022f, 0.024f), dark);
        }

        Cyl("OilCan", bench, new Vector3(0.28f, 0.05f, 0.95f), 0.09f, 0.10f, brass);
        Cyl("Spout", bench, new Vector3(0.28f, 0.14f, 0.95f), 0.012f, 0.11f, brass);

        Box("Loupe", bench, new Vector3(-0.10f, 0.02f, 0.62f), new Vector3(0.05f, 0.04f, 0.05f), dark);
        Box("Rag", bench, new Vector3(0.10f, 0.006f, -1.05f), new Vector3(0.26f, 0.012f, 0.20f), Cloth());

        // A stool, so the bench reads as somewhere a person sat for years.
        Transform stool = Group("Stool", g, new Vector3(-1.55f, 0f, 6.10f));
        Cyl("Seat", stool, new Vector3(0f, 0.60f, 0f), 0.34f, 0.05f, oak);
        for (int i = 0; i < 3; i++)
        {
            float a = i / 3f * Mathf.PI * 2f;
            GameObject leg = Cyl("Leg", stool, new Vector3(Mathf.Cos(a) * 0.12f, 0.30f, Mathf.Sin(a) * 0.12f), 0.035f, 0.60f, dark);
            leg.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 7f, 0f, -Mathf.Cos(a) * 7f);
        }
        Hull(stool.gameObject, new Vector3(0f, 0.31f, 0f), new Vector3(0.36f, 0.62f, 0.36f));

        Rug(g, new Vector3(-1.30f, 0f, 6.00f), 1.5f, 2.4f, new Color(0.28f, 0.26f, 0.22f), "RugWorkshop");

        // The vice, and the movement put into it. Act II's movement was standing on a post
        // in the middle of the floor, which read as a parking meter rather than as a job
        // left half done on the bench.
        // The jaws grip a stem and the dial stands above them. Clamping the movement itself
        // put a jaw across the bottom half of the face, and act two wants 4:40 — an hour
        // hand at four and a minute hand at eight, both of them in the half that was hidden.
        Transform vice = Group("Vice", g, new Vector3(ViceAt.x, 0.94f, ViceAt.z));
        Box("Plate", vice, new Vector3(0f, 0.015f, 0f), new Vector3(0.24f, 0.03f, 0.22f), iron);
        Box("FixedJaw", vice, new Vector3(-0.05f, 0.09f, 0f), new Vector3(0.05f, 0.12f, 0.20f), iron);
        Box("LooseJaw", vice, new Vector3(0.05f, 0.09f, 0f), new Vector3(0.05f, 0.12f, 0.20f), iron);
        Box("Stem", vice, new Vector3(0f, 0.13f, 0f), new Vector3(0.030f, 0.20f, 0.09f), iron);
        GameObject screw = Cyl("Screw", vice, new Vector3(0.13f, 0.09f, 0f), 0.03f, 0.14f, brass);
        screw.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        GameObject tommy = Cyl("Tommy", vice, new Vector3(0.20f, 0.09f, 0f), 0.016f, 0.16f, brass);
        tommy.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        SeatMovementInVice();
    }

    // ---------- III — the parlour ----------

    [MenuItem("Tools/Escape Room/House/Dress III Parlour")]
    public static void Parlour()
    {
        Transform g = Fresh("Room_III_Parlour");
        Hollow("Room_III_Parlour", "Firebox");
        Material oak = Oak(), dark = DarkOak(), brass = Brass(), iron = Iron(), gilt = Gilt();
        Material soot = Soot(), stone = Hearth(), plush = Plush();

        PictureRail(g, 3.2f, 2.7f, 10f, 9.5f);

        // ----- the fire -----
        //
        // The firebox was a black hole in a brown slab. What makes a fireplace is what is
        // inside it: a sooted back, iron bars, something burning between them.
        Transform fire = Group("Fireplace", g, new Vector3(6.60f, 0f, 9.20f));

        // The recess is lined rather than filled. The legacy `Firebox` was one solid box in
        // the opening, so everything built inside it — grate, logs, embers — was sealed in
        // and invisible; Hollow() takes it out before this runs.
        Box("Back", fire, new Vector3(0f, 0.44f, 0.16f), new Vector3(1.06f, 0.86f, 0.03f), soot);
        foreach (int side in new[] { -1, 1 })
            Box("Cheek", fire, new Vector3(side * 0.52f, 0.44f, 0f), new Vector3(0.03f, 0.86f, 0.32f), soot);
        Box("Throat", fire, new Vector3(0f, 0.85f, 0f), new Vector3(1.06f, 0.04f, 0.32f), soot);
        Box("Floor", fire, new Vector3(0f, 0.015f, 0f), new Vector3(1.06f, 0.03f, 0.32f), soot);

        // Surround: two pilasters and a lintel standing proud of the breast, which is what
        // turns a hole in a wall into a chimneypiece.
        foreach (int side in new[] { -1, 1 })
        {
            Box("Pilaster", fire, new Vector3(side * 0.70f, 0.50f, -0.10f), new Vector3(0.22f, 1.00f, 0.14f), dark);
            Box("Capital", fire, new Vector3(side * 0.70f, 1.01f, -0.11f), new Vector3(0.28f, 0.06f, 0.18f), dark);
        }
        Box("Lintel", fire, new Vector3(0f, 0.94f, -0.10f), new Vector3(1.62f, 0.16f, 0.14f), dark);

        Box("Hearth", fire, new Vector3(0f, 0.02f, -0.38f), new Vector3(1.66f, 0.05f, 0.68f), stone);

        // Grate: bars across the front, an ash pan under them.
        Box("AshPan", fire, new Vector3(0f, 0.06f, 0.02f), new Vector3(0.86f, 0.05f, 0.30f), iron);
        for (int i = 0; i < 6; i++)
            Box("Bar", fire, new Vector3(0f, 0.13f + i * 0.052f, -0.11f), new Vector3(0.84f, 0.018f, 0.018f), iron);
        foreach (int side in new[] { -1, 1 })
            Box("GrateEnd", fire, new Vector3(side * 0.43f, 0.22f, 0f), new Vector3(0.02f, 0.30f, 0.28f), iron);

        // Logs, and the embers under them. The embers emit rather than reflect: an unlit
        // box in a dark recess is not a fire however orange it is painted.
        Material ember = Ember();
        for (int i = 0; i < 3; i++)
        {
            float y = 0.20f + (i == 2 ? 0.09f : 0f);
            float z = 0.02f + (i - 1) * 0.09f;
            GameObject log = Cyl("Log", fire, new Vector3((i - 1) * 0.06f, y, z), 0.10f, 0.62f, Charred());
            log.transform.localRotation = Quaternion.Euler(0f, 0f, 90f + (i - 1) * 6f);
        }
        Box("Embers", fire, new Vector3(0f, 0.13f, 0.05f), new Vector3(0.62f, 0.05f, 0.20f), ember);

        // A fender: iron, with only its top rail in brass. A whole one in brass sits right
        // where the firelight is strongest and reads as a bar of neon.
        Box("Fender", fire, new Vector3(0f, 0.05f, -0.26f), new Vector3(1.28f, 0.10f, 0.03f), iron);
        Box("FenderRail", fire, new Vector3(0f, 0.105f, -0.26f), new Vector3(1.28f, 0.02f, 0.04f), Pewter());
        foreach (int side in new[] { -1, 1 })
            Box("FenderEnd", fire, new Vector3(side * 0.63f, 0.05f, -0.18f), new Vector3(0.03f, 0.10f, 0.18f), iron);

        Transform tools = Group("FireIrons", fire, new Vector3(0.95f, 0f, -0.30f));
        Box("Stand", tools, new Vector3(0f, 0.02f, 0f), new Vector3(0.14f, 0.04f, 0.14f), iron);
        Cyl("Post", tools, new Vector3(0f, 0.32f, 0f), 0.024f, 0.62f, iron);
        Box("Poker", tools, new Vector3(-0.05f, 0.32f, 0.02f), new Vector3(0.014f, 0.60f, 0.014f), iron);
        Box("Brush", tools, new Vector3(0.05f, 0.32f, -0.02f), new Vector3(0.014f, 0.60f, 0.014f), iron);
        Box("Bristles", tools, new Vector3(0.05f, 0.06f, -0.02f), new Vector3(0.05f, 0.10f, 0.05f), Cloth());

        Transform basket = Group("LogBasket", g, new Vector3(5.45f, 0f, 9.10f));
        Cyl("Body", basket, new Vector3(0f, 0.16f, 0f), 0.46f, 0.32f, Wicker());
        for (int i = 0; i < 5; i++)
        {
            GameObject spare = Cyl("Log", basket, new Vector3(-0.10f + i * 0.05f, 0.34f, (i % 2) * 0.08f - 0.04f), 0.09f, 0.44f, Charred());
            spare.transform.localRotation = Quaternion.Euler(90f, i * 24f, 0f);
        }
        Hull(basket.gameObject, new Vector3(0f, 0.17f, 0f), new Vector3(0.48f, 0.34f, 0.48f));

        // ----- the chair -----
        //
        // Rebuilt facing the fire. The old one had its back on the fire side, which put the
        // one comfortable seat in the house pointed at a blank wall.
        Transform chair = Group("Armchair", g, new Vector3(5.20f, 0f, 7.70f));
        chair.localRotation = Quaternion.Euler(0f, 42f, 0f);
        Box("Skirt", chair, new Vector3(0f, 0.19f, 0f), new Vector3(0.92f, 0.26f, 0.88f), plush);
        Box("Seat", chair, new Vector3(0f, 0.40f, 0.02f), new Vector3(0.82f, 0.20f, 0.80f), plush);
        Box("Cushion", chair, new Vector3(0f, 0.52f, 0.04f), new Vector3(0.74f, 0.12f, 0.70f), plush);
        Box("Back", chair, new Vector3(0f, 0.68f, -0.40f), new Vector3(0.92f, 0.78f, 0.18f), plush);
        GameObject crown = Cyl("Crown", chair, new Vector3(0f, 1.06f, -0.40f), 0.19f, 0.92f, plush);
        crown.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        foreach (int side in new[] { -1, 1 })
        {
            Box("Arm", chair, new Vector3(side * 0.41f, 0.56f, 0.04f), new Vector3(0.16f, 0.24f, 0.80f), plush);
            GameObject roll = Cyl("ArmRoll", chair, new Vector3(side * 0.41f, 0.68f, 0.04f), 0.16f, 0.80f, plush);
            roll.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Box("Wing", chair, new Vector3(side * 0.41f, 0.88f, -0.28f), new Vector3(0.13f, 0.34f, 0.34f), plush);
        }

        foreach (int sx in new[] { -1, 1 })
        foreach (int sz in new[] { -1, 1 })
            Cyl("Foot", chair, new Vector3(sx * 0.36f, 0.04f, sz * 0.34f), 0.08f, 0.08f, dark);

        Hull(chair.gameObject, new Vector3(0f, 0.55f, -0.08f), new Vector3(0.98f, 1.10f, 1.00f));

        Rug(g, new Vector3(6.30f, 0f, 7.70f), 3.2f, 2.2f, new Color(0.36f, 0.17f, 0.15f), "RugParlour");

        // ----- walls -----
        Picture(g, new Vector3(4.40f, 1.82f, 2.82f), 180f, 0.70f, 0.52f, gilt, 3);
        Picture(g, new Vector3(8.70f, 1.82f, 2.82f), 180f, 0.50f, 0.64f, gilt, 4);

        // No looking glass over this mantel. The space belongs to the middle cuckoo, which
        // is where the parlour puzzle wants the player's eye anyway.

        // ----- light -----
        Fire(new Vector3(6.60f, 0.42f, 9.18f));
    }

    // ---------- IV — the bedroom ----------

    [MenuItem("Tools/Escape Room/House/Dress IV Bedroom")]
    public static void Bedroom()
    {
        Transform g = Fresh("Room_IV_Bedroom");
        Material oak = Oak(), dark = DarkOak(), linen = Linen(), drape = Drape();

        PictureRail(g, 3.2f, -2.5f, 10f, 2.5f);

        // Bedding. A mattress on a frame is a slab; a pillow and a turned-down quilt is a
        // bed somebody got out of.
        Transform bed = Group("Bedding", g, new Vector3(4.90f, 0f, -1.35f));
        Box("Quilt", bed, new Vector3(0f, 0.63f, 0.18f), new Vector3(1.48f, 0.10f, 1.62f), drape);
        Box("Turndown", bed, new Vector3(0f, 0.66f, 0.98f), new Vector3(1.48f, 0.05f, 0.30f), linen);
        Box("Sheet", bed, new Vector3(0f, 0.62f, -0.72f), new Vector3(1.44f, 0.03f, 0.60f), linen);
        foreach (int side in new[] { -1, 1 })
        {
            Box("Pillow", bed, new Vector3(side * 0.36f, 0.68f, -0.86f), new Vector3(0.62f, 0.13f, 0.36f), linen);
            Box("PillowLip", bed, new Vector3(side * 0.36f, 0.63f, -0.68f), new Vector3(0.58f, 0.07f, 0.10f), linen);
        }

        // Curtains at the window on the south wall, drawn back.
        Curtains(g, new Vector3(6.60f, 0f, -2.46f), 0f, drape);

        // A shelf over the chest. The alarm clocks are moved onto it and the chest top by
        // ArrangeAlarms; twelve of them lying on bare floorboards read as spillage.
        Transform shelf = Group("AlarmShelf", g, new Vector3(ShelfX, 0f, -2.44f));
        Box("Board", shelf, new Vector3(0f, 1.28f, 0.10f), new Vector3(1.90f, 0.04f, 0.26f), oak);
        Box("Board", shelf, new Vector3(0f, 1.70f, 0.10f), new Vector3(1.90f, 0.04f, 0.26f), oak);
        foreach (int side in new[] { -1, 1 })
        {
            Box("Bracket", shelf, new Vector3(side * 0.80f, 1.20f, 0.06f), new Vector3(0.04f, 0.14f, 0.14f), dark);
            Box("Bracket", shelf, new Vector3(side * 0.80f, 1.62f, 0.06f), new Vector3(0.04f, 0.14f, 0.14f), dark);
        }

        // A lamp by the bed. Every light in this room was blue-white, which is correct for
        // moonlight and wrong for a room a person sleeps in.
        Transform lamp = Group("BedsideLamp", g, new Vector3(BedsideX, 0.605f, BedsideZ + 0.13f));
        Cyl("Foot", lamp, new Vector3(0f, 0.012f, 0f), 0.13f, 0.024f, Brass());
        Cyl("Stem", lamp, new Vector3(0f, 0.13f, 0f), 0.028f, 0.24f, Brass());
        GameObject shade = Cyl("Shade", lamp, new Vector3(0f, 0.30f, 0f), 0.22f, 0.16f, Linen());
        shade.transform.localScale = new Vector3(0.22f, 0.08f, 0.22f);

        var bulbGo = new GameObject("Bulb");
        bulbGo.transform.SetParent(lamp, false);
        bulbGo.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        var bulb = bulbGo.AddComponent<Light>();
        bulb.type = LightType.Point;
        bulb.range = 4.2f;
        bulb.intensity = 1.5f;
        bulb.color = new Color(1f, 0.83f, 0.60f);
        bulb.shadows = LightShadows.None;

        ArrangeAlarms();

        Rug(g, new Vector3(6.90f, 0f, 0.60f), 2.4f, 1.8f, new Color(0.34f, 0.30f, 0.42f), "RugBedroom");

        Picture(g, new Vector3(9.90f, 1.80f, -0.40f), 90f, 0.52f, 0.66f, Gilt(), 5);
    }

    // ---------- V — behind the case ----------

    [MenuItem("Tools/Escape Room/House/Dress V Behind")]
    public static void Behind()
    {
        Transform g = Fresh("Room_V_Behind");
        Material dark = DarkOak(), iron = Iron(), paper = Paper();

        // No picture rail here. This room was never finished, and that is the point of it.

        // The room grew east to nine metres and its light did not: bulb, crate and candles
        // all stayed by the door, and the machine and the screen — the whole of act five —
        // stood at the dark end of it. Three floor stands carry a flame apiece down there.
        // Not lamps: nothing in this room is wired, and the moon is not enough to read a
        // constellation by.
        Stand(g, new Vector3(13.30f, 0f, -3.70f));
        Stand(g, new Vector3(16.40f, 0f, -3.70f));
        Stand(g, new Vector3(17.20f, 0f, -0.30f));

        // A crate the man sat on while he wrote on the walls, and the candles he did it by. Clear
        // of the panel: it stood a metre inside the way in, and the player's own capsule caught
        // its corner coming through — the doorway rule, one room further along.
        Transform crate = Group("Crate", g, new Vector3(12.10f, 0f, 0.05f));
        Box("Body", crate, new Vector3(0f, 0.24f, 0f), new Vector3(0.52f, 0.48f, 0.46f), dark);
        foreach (int side in new[] { -1, 1 })
            Box("Batten", crate, new Vector3(side * 0.27f, 0.24f, 0f), new Vector3(0.03f, 0.44f, 0.42f), Oak());
        Hull(crate.gameObject, new Vector3(0f, 0.24f, 0f), new Vector3(0.54f, 0.48f, 0.48f));

        for (int i = 0; i < 3; i++)
        {
            var at = new Vector3(12.10f + (i - 1) * 0.16f, 0.48f, 0.05f + (i % 2) * 0.12f - 0.06f);
            Candle(g, at, 0.16f - i * 0.03f);
        }
        Candle(g, new Vector3(13.20f, 0f, 2.05f), 0.22f);

        // Pages torn out and dropped. They say nothing the walls do not, but a room nobody
        // has swept is what makes the writing on the walls read as years rather than a prop.
        //
        // They lie on the far side of the crate from the prisms. Three small coloured things
        // on a floor strewn with pale paper are three things nobody finds.
        for (int i = 0; i < 9; i++)
        {
            float a = i * 61.7f;
            var sheet = Box("Page", g,
                new Vector3(12.95f + Mathf.Cos(a) * 0.85f, 0.003f, 0.30f + Mathf.Sin(a * 1.7f) * 0.75f),
                new Vector3(0.21f, 0.004f, 0.29f), paper);
            sheet.transform.localRotation = Quaternion.Euler(0f, a * 3f, 0f);
        }

        Box("Bucket", g, new Vector3(13.30f, 0.14f, -0.35f), new Vector3(0.26f, 0.28f, 0.26f), iron);
    }

    // ---------- the two things dressing moves rather than builds ----------

    // Where the movement is clamped. The vice is dressing and the movement is a puzzle
    // prop, so the one number they both need lives here rather than in either of them.
    private static readonly Vector3 ViceAt = new Vector3(-2.32f, 0f, 6.42f);

    // The alarm shelf. It began at 7.60, directly over the window, so two boards and eight
    // clocks stood between the room and the only night sky in it.
    private const float ShelfX = 8.90f;

    // The bedside table, beside the head of the bed and under the window. It began on the
    // far side, where the bed's own headboard then ran a hand's breadth into the opening;
    // moving the bed clear of the glass left the table stranded past the foot instead, which
    // is not what a bedside table is.
    private const float BedsideX = 6.20f;
    private const float BedsideZ = -2.05f;

    // Takes the movement off its floor post and puts it in the vice, with its two setting
    // knobs on the bench beside it.
    //
    // Deleting the post takes the only colliders on the prop with it, so a hull goes back on
    // the root: a clock nobody can put the crosshair on is a clock nobody can set, and this
    // one is the whole of act two.
    private static void SeatMovementInVice()
    {
        Transform movement = GameObject.Find(PropsRoot + "/Room_II_Workshop/BenchMovement").transform;

        foreach (string part in new[] { "Post", "Foot" })
        {
            Transform found = movement.Find(part);
            if (found != null)
                Undo.DestroyObjectImmediate(found.gameObject);
        }

        // The dial's own radius is 0.135, so its foot has to clear the top of the jaws
        // (0.94 + 0.15) with room to spare or the vice eats the bottom of the face again.
        Undo.RecordObject(movement, "seat movement");
        movement.SetPositionAndRotation(new Vector3(ViceAt.x, 1.15f, ViceAt.z), Quaternion.Euler(0f, 90f, 0f));
        movement.Find("Dial").localPosition = new Vector3(0f, 0.16f, 0f);

        if (movement.GetComponent<Collider>() == null)
            Undo.AddComponent<BoxCollider>(movement.gameObject);

        var hull = movement.GetComponent<BoxCollider>();
        hull.center = new Vector3(0f, 0.16f, 0f);
        hull.size = new Vector3(0.30f, 0.30f, 0.09f);

        FitTimingMachine(movement);

        // The knobs are placed in world on purpose. Parented to the movement they inherit
        // its scale and its origin, which is how they once ended up buried under the floor.
        //
        // **They belong on the arbor, not on the bench.** They used to sit a hand's width in
        // front of the movement, a third of a metre below it and a quarter of a metre out to
        // either side, facing along the bench rather than at the player — so they read as two
        // brass things somebody had left lying about rather than as the controls of the clock
        // between them. They go on the front plate now, either side of the arbor, where the
        // winding squares of a real movement are.
        //
        // The numbers come off the dial rather than being typed a second time: the movement
        // is seated at 1.15 with its dial 0.16 above that, and it faces +x because it is
        // turned a quarter into the vice.
        Vector3 arbor = new Vector3(ViceAt.x, 1.15f + 0.16f, ViceAt.z);
        const float Proud = 0.016f;   // out through the face, which stands 0.010 off the arbor
        const float Apart = 0.058f;   // either side of it, inside a dial of radius 0.135

        // **Above the arbor, not below it**, which is where a real movement would put them
        // and where this one cannot. The answer act two wants is 4:40: the hour hand then
        // lies 40 degrees off straight down to the right and the minute hand 60 degrees off
        // it to the left — which is almost exactly where two knobs set below the arbor sit.
        // They would have covered the one reading the whole puzzle turns on. Up here the
        // hands only cross them on the way round, while the player is pressing rather than
        // reading.
        const float Above = 0.046f;

        Transform knobs = GameObject.Find(PropsRoot + "/Room_II_Workshop/SettingKnobs").transform;

        foreach (var knob in new (string Name, float Side)[] { ("HourKnob", 1f), ("MinuteKnob", -1f) })
        {
            Transform t = knobs.Find(knob.Name);
            Undo.RecordObject(t, "knobs");

            // Turned to face the player, which is the same quarter turn the movement took.
            t.SetPositionAndRotation(arbor + new Vector3(Proud, Above, knob.Side * Apart),
                                     Quaternion.Euler(0f, 90f, 0f));

            // Sized here too, because the size only makes sense against the dial they are
            // now on. At 70 mm across on a 270 mm face they were two brass lids covering the
            // five and the seven; a winding square is a third of that.
            Undo.RecordObject(t.Find("Boss"), "knobs");
            Undo.RecordObject(t.Find("Grip"), "knobs");
            t.Find("Boss").localScale = new Vector3(0.046f, 0.015f, 0.046f);
            t.Find("Grip").localScale = new Vector3(0.011f, 0.038f, 0.011f);

            // Snug, and standing in front of the face. At 0.13 across the boxes reached back
            // through the dial, and the crosshair then took a knob whenever it was pointed
            // anywhere near the bottom of it — which is a dial nobody can pull up close.
            var hit = t.GetComponent<BoxCollider>();
            Undo.RecordObject(hit, "knobs");
            hit.center = new Vector3(0f, 0f, 0.014f);
            hit.size = new Vector3(0.062f, 0.062f, 0.052f);
        }
    }

    // Where everything hangs.
    //
    // The rule the whole pass exists for: **nothing is fixed to a window**. A clock cannot
    // be nailed to glass, and a rack of pocket watches across an opening reads as a mistake
    // however neatly it is spaced. Openings are 1.30 wide, sill 0.95, head 2.10, and the
    // gaps between them are what the walls of this house actually offer.
    //
    // It moves props rather than building them, so it runs before the rooms are dressed and
    // the dressing can then be placed around the result.
    private static void Rehang()
    {
        // ---- I: seven wall clocks. The south wall has two windows in it and only one gap
        // wide enough to hang anything, so most of them go on the side walls.
        var showroom = new (string Name, Vector3 At, float Yaw)[]
        {
            ("Clock_1_Study",   new Vector3(-2.99f, 1.05f, -2.05f),  90f),
            ("Clock_2_Vienna",  new Vector3(-2.99f, 1.12f, -1.25f),  90f),
            ("Clock_3_Parlour", new Vector3( 2.99f, 1.00f, -1.95f), 270f),
            ("Clock_4_Kitchen", new Vector3( 2.99f, 0.98f, -1.15f), 270f),
            ("Clock_5_Hall",    new Vector3( 2.99f, 1.09f, -0.35f), 270f),
            ("Clock_6_Cellar",  new Vector3( 2.99f, 1.02f,  1.45f), 270f),
            ("Clock_7_Landing", new Vector3(-0.20f, 1.10f, -2.49f),   0f),
        };
        Transform clocks = GameObject.Find(PropsRoot + "/Room_I_Showroom/Clocks").transform;
        foreach (var c in showroom)
            Place(clocks.Find(c.Name), c.At, c.Yaw);

        // ---- II: the twelve pocket watches. They were a six-wide grid centred on the north
        // wall, and the middle two columns hung straight across the window. Four columns of
        // three, to the right of the opening, keeps them together — which the puzzle needs,
        // since eleven of them agree and one does not.
        // The children run watch, hook, watch, hook: a pair shares a place on the grid, and
        // the count only moves on once the hook that goes with a watch has been hung.
        Transform watches = GameObject.Find(PropsRoot + "/Room_II_Workshop/WatchWall").transform;
        int hung = 0;

        for (int i = 0; i < watches.childCount; i++)
        {
            Transform part = watches.GetChild(i);
            bool hook = part.name.StartsWith("Hook");

            float x = 0.35f + (hung % 4) * 0.80f;
            float y = 1.15f + (hung / 4) * 0.35f;
            Place(part, new Vector3(x, hook ? y + 0.14f : y, hook ? 9.44f : 9.40f), 180f);

            if (hook) hung++;
        }

        // The bench keeps its place under the window — a clockmaker works by daylight — but
        // its back board and tool shelf drop below the head of the opening, so the glass
        // shows above the bench instead of being boarded over.
        Resize(PropsRoot + "/Room_II_Workshop/BenchBack", new Vector3(-2.86f, 1.06f, 6.00f), new Vector3(0.06f, 0.24f, 3.40f));
        Place(GameObject.Find(PropsRoot + "/Room_II_Workshop/ToolShelf").transform, new Vector3(-2.72f, 1.16f, 6.00f), 0f);

        Transform workshop = GameObject.Find(PropsRoot + "/Room_II_Workshop").transform;
        for (int i = 0; i < workshop.childCount; i++)
        {
            Transform tray = workshop.GetChild(i);
            if (tray.name != "Tray") continue;
            Place(tray, new Vector3(-2.70f, 1.23f, tray.position.z), 0f);
        }

        // The repair ticket comes off the back board, which is now too low to read from, and
        // goes on the wall beside the window where it is at eye height.
        Place(GameObject.Find(PropsRoot + "/Room_II_Workshop/BenchTicket").transform,
              new Vector3(-2.98f, 1.52f, 4.30f), 90f);

        // ---- IV: the bed slides west so its headboard stops clear of the window, and the
        // bedside table crosses to the other side, under the sill where it belongs.
        foreach (string part in new[] { "BedBase", "Mattress", "Headboard" })
        {
            Transform t = GameObject.Find(PropsRoot + "/Room_IV_Bedroom/" + part).transform;
            Place(t, new Vector3(4.90f, t.position.y, t.position.z), t.eulerAngles.y);
        }
        foreach (string part in new[] { "BedsideTop", "BedsideBody" })
        {
            Transform t = GameObject.Find(PropsRoot + "/Room_IV_Bedroom/" + part).transform;
            Place(t, new Vector3(BedsideX, t.position.y, BedsideZ), t.eulerAngles.y);
        }
        Place(GameObject.Find(PropsRoot + "/Room_IV_Bedroom/Wristwatch").transform,
              new Vector3(BedsideX + 0.13f, 0.62f, BedsideZ - 0.14f), 24f);

        Reswing();
    }

    // Which way a door goes.
    //
    // The wardrobe's opened backwards: hinged on its front-left stile but swinging the wrong
    // way about it, so the leaf went through the bedroom wall and stood a hundred millimetres
    // inside it. Nothing said so, because a door that has swung is a door you have stopped
    // looking at — you are looking at what it uncovered.
    //
    // A hinge stile always bites a little as it turns; the numbers here are the angle at
    // which each leaf bites least while still standing properly open.
    private static void Reswing()
    {
        var swings = new (string Path, float Angle)[]
        {
            (PropsRoot + "/Room_IV_Bedroom/Wardrobe/WardrobeDoor", 90f),
            ("House/Props/SecretPanel/LongcaseClock/TrunkDoor",   -90f),
            ("House/Doors/Door_II_to_III",                         95f),
            ("House/Doors/Door_III_to_IV",                        -95f),
            ("House/Doors/Door_Out",                               95f),
        };

        foreach (var swing in swings)
        {
            var motion = GameObject.Find(swing.Path).GetComponent<SwingMotion>();
            var so = new SerializedObject(motion);
            so.FindProperty("openAngle").floatValue = swing.Angle;
            so.ApplyModifiedProperties();
        }
    }

    private static void Place(Transform prop, Vector3 at, float yaw)
    {
        Undo.RecordObject(prop, "rehang");
        prop.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
    }

    private static void Resize(string path, Vector3 at, Vector3 size)
    {
        Transform prop = GameObject.Find(path).transform;
        Undo.RecordObject(prop, "rehang");
        prop.position = at;
        prop.localScale = size;
    }

    // A bench timing machine, plugged into the movement in the vice.
    //
    // It exists to be looked at rather than used: it is a **second display on a movement
    // that already has one**, and the two agree because both are handed the same number by
    // the same `ClockMovement`. Nothing in the movement was changed to accept it, nothing in
    // `ClockDial` knows it is there, and it shares no code with either — it is four quads
    // scrolling a printed strip where the dial has two hands turning.
    //
    // It is a child of the movement because that is what "plugged into" means here:
    // `ClockMovement` gathers the displays under it, so a display is fitted by parenting it
    // and unfitted by taking it away.
    private static void FitTimingMachine(Transform movement)
    {
        Transform old = movement.Find("TimingMachine");
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);

        Material iron = Iron(), brass = Brass();
        Material strip = BuildKit.Load("ClockReel");

        // The movement stands on the bench turned a quarter, so its local +z is the way the
        // player looks and its local +x runs along the bench. Everything here is in that
        // frame, which is why the offsets read as they do.
        Transform box = Group("TimingMachine", movement, new Vector3(0.47f, -0.21f, 0.02f));

        Box("Case", box, new Vector3(0f, 0.055f, 0f), new Vector3(0.26f, 0.11f, 0.16f), iron);
        Box("Bezel", box, new Vector3(0f, 0.062f, 0.081f), new Vector3(0.21f, 0.062f, 0.006f), brass);
        Box("Window", box, new Vector3(0f, 0.062f, 0.084f), new Vector3(0.185f, 0.046f, 0.004f), iron);

        foreach (int side in new[] { -1, 1 })
            Cyl("Foot", box, new Vector3(side * 0.10f, 0.006f, 0f), 0.026f, 0.012f, brass);

        // Four windows: tens of hours, hours, tens of minutes, minutes.
        var faces = new Renderer[4];
        for (int i = 0; i < 4; i++)
        {
            // Negated: the movement stands turned a quarter, so its local +x runs away from
            // the player's right. Laid out in local order the four digits read backwards.
            float x = -((i - 1.5f) * 0.042f + (i < 2 ? -0.008f : 0.008f));
            faces[i] = Quad("Digit", box, new Vector3(x, 0.062f, 0.087f), 0.040f, strip).GetComponent<Renderer>();
        }

        foreach (float y in new[] { 0.052f, 0.072f })
            Box("Colon", box, new Vector3(0f, y, 0.087f), new Vector3(0.005f, 0.005f, 0.002f), brass);

        var readout = box.gameObject.AddComponent<DigitalReadout>();
        Wire(readout, "digits", faces);
        Wire(readout, "twelveHour", true);
    }

    // Twelve alarm clocks, laid out on the surfaces of the room rather than dropped on the
    // floor. Five of them were lying on bare boards, which reads as a bug; and the ninth
    // hour — one of the four the puzzle wants — was on top of the wardrobe at 2.14, above
    // the player's eyes, where its alarm hand could not be read at all.
    private static void ArrangeAlarms()
    {
        Transform alarms = GameObject.Find(PropsRoot + "/Room_IV_Bedroom/Alarms").transform;

        // All twelve on the two boards, and nothing on the chest, the bedside table or the
        // bed. The chest is the table in Arthur's photograph and has to be found bare: the
        // whole of act four is noticing what is not standing on it any more. A clock left
        // there is one more thing the player has to rule out.
        //
        // Six to a board at 0.29, which leaves the left end of the lower one clear for the
        // vase. They are identical clocks set to the same hour, so a shelf of them reads as
        // one man's habit and no one of them stands out as worth reading.
        var places = new (string Name, Vector3 At, float Yaw)[]
        {
            ("Alarm_01", new Vector3(ShelfX - 0.62f, 1.305f, -2.34f),   8f),
            ("Alarm_02", new Vector3(ShelfX - 0.33f, 1.305f, -2.34f), 354f),
            ("Alarm_03", new Vector3(ShelfX - 0.04f, 1.305f, -2.34f),  11f),
            ("Alarm_04", new Vector3(ShelfX + 0.25f, 1.305f, -2.34f), 350f),
            ("Alarm_05", new Vector3(ShelfX + 0.54f, 1.305f, -2.34f),   5f),
            ("Alarm_06", new Vector3(ShelfX + 0.83f, 1.305f, -2.34f), 347f),
            ("Alarm_07", new Vector3(ShelfX - 0.75f, 1.725f, -2.34f), 352f),
            ("Alarm_08", new Vector3(ShelfX - 0.45f, 1.725f, -2.34f),   6f),
            ("Alarm_09", new Vector3(ShelfX - 0.15f, 1.725f, -2.34f), 344f),
            ("Alarm_10", new Vector3(ShelfX + 0.15f, 1.725f, -2.34f),  13f),
            ("Alarm_11", new Vector3(ShelfX + 0.45f, 1.725f, -2.34f), 357f),
            ("Alarm_12", new Vector3(ShelfX + 0.75f, 1.725f, -2.34f), 349f),
        };

        foreach (var place in places)
        {
            Transform clock = alarms.Find(place.Name);
            Undo.RecordObject(clock, "arrange alarms");
            clock.SetPositionAndRotation(place.At, Quaternion.Euler(0f, place.Yaw, 0f));
        }
    }

    // ---------- shared pieces ----------

    // A moulding round the top of the walls. It clears every door and window head in the
    // house, so unlike the skirting it never has to be cut round an opening.
    private static void PictureRail(Transform parent, float x0, float z0, float x1, float z1)
    {
        Transform rail = Group("PictureRail", parent, Vector3.zero);
        Material trim = BuildKit.Load("HouseTrim");

        float cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f;

        foreach (int side in new[] { -1, 1 })
        {
            Box("Rail", rail, new Vector3(cx, RailHeight, side < 0 ? z0 + 0.02f : z1 - 0.02f),
                new Vector3(x1 - x0, 0.05f, 0.035f), trim);
            Box("Rail", rail, new Vector3(side < 0 ? x0 + 0.02f : x1 - 0.02f, RailHeight, cz),
                new Vector3(0.035f, 0.05f, z1 - z0), trim);
        }
    }

    private static void Rug(Transform parent, Vector3 at, float width, float depth, Color colour, string map)
    {
        Texture2D weave = TextureKit.Carpet(map, Color.white);
        Material mat = BuildKit.Opaque(map, colour, 0.04f, 0f, weave);

        GameObject rug = Box("Rug", parent, at + Vector3.up * 0.006f, new Vector3(width, 0.012f, depth), mat);
        BuildKit.Tile(rug, 0.55f);

        // A border in the same cloth, a shade darker. Two rectangles is all a rug needs to
        // stop reading as a stain on the floor.
        Material edge = BuildKit.Opaque(map + "Edge", colour * 0.62f, 0.04f, 0f, weave);
        GameObject band = Box("Border", parent, at + Vector3.up * 0.004f,
            new Vector3(width + 0.13f, 0.008f, depth + 0.13f), edge);
        BuildKit.Tile(band, 0.55f);
    }

    // A coir mat with a word on it.
    //
    // The word is type, not paint: the same live `Text` in the same face the shop's care
    // leaflet and the HUD prompts use, so it can be changed or translated without anything
    // being redrawn. A `Color[]` has no font engine, and a mat is the one prop in the house
    // whose whole job is a word.
    //
    // Three things about the canvas. It is read from its own -z, so it carries a half turn
    // like a bare Unity quad. It is scaled well under one to one, because at one to one a
    // 60 mm word asks the font for 60 pixels and is then magnified a thousandfold onto the
    // floor. And the number it asks the font for has a ceiling:
    //
    // **Legacy dynamic text stops growing at fontSize 500.** Ask for 900 and the glyphs come
    // out at 500, silently — the leaflet never met this because its heading is 190. So the
    // size of the word is set by the units the canvas is drawn in, not by the number typed
    // into fontSize: four units to the millimetre here against the leaflet's ten, which is
    // what puts a 500 unit word across three quarters of a 780 mm mat. At ten units to the
    // millimetre WELCOME comes out an inch wide whatever number is typed.
    private static void Doormat(Transform parent, Vector3 at, string word)
    {
        const float Wide = 0.78f, Deep = 0.48f;
        const float PerMillimetre = 0.00025f;
        const int Largest = 500;

        Texture2D coir = TextureKit.Fabric("MatCoir", Color.white);
        Material pile = BuildKit.Opaque("Doormat", new Color(0.30f, 0.22f, 0.14f), 0.04f, 0f, coir);
        Material edge = BuildKit.Opaque("DoormatEdge", new Color(0.16f, 0.12f, 0.08f), 0.04f, 0f, coir);

        Transform hub = Group("Doormat", parent, at);

        GameObject cloth = Box("Pile", hub, new Vector3(0f, 0.010f, 0f), new Vector3(Wide, 0.020f, Deep), pile);
        BuildKit.Tile(cloth, 0.14f);

        GameObject band = Box("Border", hub, new Vector3(0f, 0.008f, 0f),
            new Vector3(Wide + 0.06f, 0.016f, Deep + 0.06f), edge);
        BuildKit.Tile(band, 0.14f);

        // Face up, with the word's own up pointing at the door, which is how it falls for a
        // player walking towards it.
        var face = new GameObject("Face", typeof(RectTransform), typeof(Canvas));
        face.transform.SetParent(hub, false);
        face.transform.localPosition = new Vector3(0f, 0.021f, 0f);
        face.transform.localRotation = Quaternion.Euler(90f, 0f, 180f);
        face.transform.localScale = Vector3.one * PerMillimetre;

        // Said out loud rather than left to a default: a canvas with no canvas above it can
        // land in screen space, and a screen-space doormat is one printed across the view.
        face.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ((RectTransform)face.transform).sizeDelta = new Vector2(Wide, Deep) / PerMillimetre;

        var label = new GameObject("Word", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        label.transform.SetParent(face.transform, false);
        var rect = (RectTransform)label.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var type = label.GetComponent<Text>();
        type.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/LeelawadeeUI.ttf");
        type.text = word;
        type.fontSize = Largest;
        type.fontStyle = FontStyle.Bold;
        type.alignment = TextAnchor.MiddleCenter;
        type.horizontalOverflow = HorizontalWrapMode.Overflow;
        type.verticalOverflow = VerticalWrapMode.Overflow;
        type.color = new Color(0.84f, 0.80f, 0.70f);
    }

    private static void Picture(Transform parent, Vector3 at, float facing, float width, float height,
                                Material frame, int print)
    {
        Transform hub = Group("Picture", parent, at);
        hub.localRotation = Quaternion.Euler(0f, facing, 0f);

        // Four rails round an opening, not one slab. A solid frame with the print stacked
        // behind it shows the viewer nothing but a rectangle of gilt, which is exactly what
        // the first pass hung on the showroom wall.
        //
        // A picture looks down its own **-z**, so `facing` turns its back to the viewer:
        // for a wall the player stands to the +z side of, that is 180. Getting it the other
        // way round hides the print behind its own backing board again.
        const float Rail = 0.055f, Depth = 0.05f;

        Box("Backing", hub, new Vector3(0f, 0f, 0.020f), new Vector3(width, height, 0.012f), frame);
        Box("Canvas", hub, new Vector3(0f, 0f, -0.008f), new Vector3(width - Rail * 2f, height - Rail * 2f, 0.008f),
            BuildKit.Opaque("Print_" + print, Color.white, 0.04f, 0f, TextureKit.Mezzotint("Print" + print, print)));

        foreach (int side in new[] { -1, 1 })
        {
            Box("Rail", hub, new Vector3(0f, side * (height - Rail) * 0.5f, 0f),
                new Vector3(width, Rail, Depth), frame);
            Box("Rail", hub, new Vector3(side * (width - Rail) * 0.5f, 0f, 0f),
                new Vector3(Rail, height - Rail * 2f, Depth), frame);
        }
    }

    private static void Curtains(Transform parent, Vector3 at, float facing, Material cloth)
    {
        Transform hub = Group("Curtains", parent, at);
        hub.localRotation = Quaternion.Euler(0f, facing, 0f);

        Box("Pole", hub, new Vector3(0f, 2.24f, 0.06f), new Vector3(1.90f, 0.035f, 0.035f), Brass());
        foreach (int side in new[] { -1, 1 })
        {
            BuildKit.Sphere("Finial", hub, new Vector3(side * 0.95f, 2.24f, 0.06f), 0.07f, Brass());

            // Gathered, not flat: three folds of different depth reads as hanging cloth.
            for (int f = 0; f < 3; f++)
                Box("Fold", hub, new Vector3(side * (0.62f + f * 0.09f), 1.52f, 0.10f + f * 0.015f),
                    new Vector3(0.10f, 1.34f, 0.07f), cloth);
        }
    }

    // A wrought stand with a candle on top of it, for lighting a room that has no wiring in
    // it. The flame ends up at 1.2 m, which is where a flame has to be to put light on a
    // wall rather than on the floor round its own foot.
    private static void Stand(Transform parent, Vector3 at)
    {
        Transform hub = Group("Stand", parent, at);
        Material iron = Iron();

        Cyl("Foot", hub, new Vector3(0f, 0.015f, 0f), 0.30f, 0.03f, iron);
        Cyl("Post", hub, new Vector3(0f, 0.60f, 0f), 0.035f, 1.14f, iron);
        Cyl("Pan", hub, new Vector3(0f, 1.18f, 0f), 0.17f, 0.02f, iron);
        Hull(hub.gameObject, new Vector3(0f, 0.60f, 0f), new Vector3(0.30f, 1.20f, 0.30f));

        Candle(hub, new Vector3(0f, 1.19f, 0f), 0.18f);
    }

    private static void Candle(Transform parent, Vector3 at, float height)
    {
        Transform hub = Group("Candle", parent, at);

        Cyl("Dish", hub, new Vector3(0f, 0.008f, 0f), 0.10f, 0.016f, Brass());
        Cyl("Wax", hub, new Vector3(0f, 0.016f + height * 0.5f, 0f), 0.036f, height, Wax());
        Cyl("Wick", hub, new Vector3(0f, 0.016f + height + 0.008f, 0f), 0.004f, 0.016f, Iron());

        var flameGo = new GameObject("Flame");
        flameGo.transform.SetParent(hub, false);
        flameGo.transform.localPosition = new Vector3(0f, 0.016f + height + 0.030f, 0f);
        GameObject bead = BuildKit.Sphere("Bead", flameGo.transform, Vector3.zero, 0.026f, Ember());
        bead.transform.localScale = new Vector3(0.020f, 0.042f, 0.020f);

        var light = flameGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 2.6f;
        light.intensity = 1.15f;
        light.color = new Color(1f, 0.76f, 0.44f);
        light.shadows = LightShadows.None;
        flameGo.AddComponent<Flicker>();
    }

    // Moves the parlour's fire light into the grate and gives it a flame's unsteadiness. It
    // was authored in the middle of the room, two metres from anything burning.
    private static void Fire(Vector3 at)
    {
        GameObject go = GameObject.Find("Lighting/Light_III_Fire");
        Undo.RecordObject(go.transform, "fire light");
        Undo.RecordObject(go.GetComponent<Light>(), "fire light");

        go.transform.position = at;

        var light = go.GetComponent<Light>();
        light.range = 5.5f;
        light.intensity = 1.35f;
        light.color = new Color(1f, 0.61f, 0.29f);

        if (go.GetComponent<Flicker>() == null)
            Undo.AddComponent<Flicker>(go);
    }

    // ---------- plumbing ----------

    // Takes out a legacy prop that fills a space the dressing needs to build inside. A
    // container that is solid all through is the same mistake as a drawer whose collider
    // wraps its own contents, and it hides geometry rather than merely blocking it.
    private static void Hollow(string room, string prop)
    {
        Transform found = GameObject.Find(PropsRoot + "/" + room).transform.Find(prop);
        if (found != null)
            Undo.DestroyObjectImmediate(found.gameObject);
    }

    // Clears this room's Dressing group and hands back an empty one, so the pass can be run
    // as many times as it takes without the room silently doubling.
    private static Transform Fresh(string room)
    {
        GameObject group = GameObject.Find(PropsRoot + "/" + room);

        // The showroom grew up as loose props at the top of Props and never got a group of
        // its own. Making one here is cheaper than a migration, and Tidy Showroom moves the
        // rest of it in.
        if (group == null)
        {
            group = new GameObject(room);
            group.transform.SetParent(GameObject.Find(PropsRoot).transform, false);
        }

        Transform props = group.transform;

        Transform old = props.Find("Dressing");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        return BuildKit.Group("Dressing", props, Vector3.zero);
    }

    private static Transform Group(string name, Transform parent, Vector3 at) =>
        BuildKit.Group(name, parent, at);

    private static GameObject Box(string name, Transform parent, Vector3 at, Vector3 size, Material m) =>
        BuildKit.Box(name, parent, at, size, m);

    private static GameObject Cyl(string name, Transform parent, Vector3 at, float diameter, float height, Material m) =>
        BuildKit.Cylinder(name, parent, at, diameter, height, m);

    private static GameObject Quad(string name, Transform parent, Vector3 at, float size, Material m) =>
        BuildKit.Quad(name, parent, at, size, m);

    private static void Wire(Object target, string property, object value) =>
        BuildKit.Wire(target, property, value);

    private static GameObject Dome(string name, Transform parent, Vector3 at, float diameter, Material m)
    {
        GameObject go = BuildKit.Sphere(name, parent, at, diameter, m);
        go.transform.localScale = new Vector3(diameter, diameter * 0.62f, diameter);
        return go;
    }

    // A cylinder squeezed to a point at one end is not worth a mesh of its own; scaling the
    // top ring is not possible on a primitive, so a shallow cone is faked by a short taper.
    private static GameObject Cone(string name, Transform parent, Vector3 at, float diameter, float height, Material m)
    {
        GameObject go = BuildKit.Cylinder(name, parent, at, diameter, height, m);
        go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter * 0.72f);
        return go;
    }

    private static void Hull(GameObject go, Vector3 centre, Vector3 size) => BuildKit.Hull(go, centre, size);

    // ---------- materials ----------

    private static Material Oak() =>
        BuildKit.Opaque("DressOak", new Color(0.52f, 0.36f, 0.22f), 0.22f, 0f,
                        TextureKit.WoodGrain("OakGrain", Color.white, new Color(0.55f, 0.45f, 0.36f)));

    private static Material DarkOak() =>
        BuildKit.Opaque("DressOakDark", new Color(0.26f, 0.17f, 0.11f), 0.24f, 0f,
                        TextureKit.WoodGrain("OakGrain", Color.white, new Color(0.55f, 0.45f, 0.36f)));

    private static Material Brass() => BuildKit.Load("ClockBrass");
    private static Material Paper() => BuildKit.Load("Paper") ?? BuildKit.Load("NotePaper");

    // Brass with the shine taken off it. Anything properly polished sitting half a metre
    // from the grate reads as a strip light rather than as metal.
    private static Material Pewter() =>
        BuildKit.Opaque("DressPewter", new Color(0.34f, 0.28f, 0.17f), 0.20f, 0.55f, null);

    private static Material Iron() =>
        BuildKit.Opaque("DressIron", new Color(0.13f, 0.13f, 0.14f), 0.34f, 0.85f, null);

    private static Material Gilt() =>
        BuildKit.Opaque("DressGilt", new Color(0.55f, 0.43f, 0.20f), 0.62f, 0.75f, null);

    private static Material Soot() =>
        BuildKit.Opaque("DressSoot", new Color(0.42f, 0.30f, 0.26f), 0.05f, 0f,
                        TextureKit.Brick("FireBrick", Color.white, new Color(0.72f, 0.70f, 0.66f)));

    private static Material Hearth() =>
        BuildKit.Opaque("DressHearth", new Color(0.21f, 0.20f, 0.19f), 0.03f, 0f,
                        TextureKit.Stone("HearthStone", Color.white));

    private static Material Charred() =>
        BuildKit.Opaque("DressCharred", new Color(0.16f, 0.12f, 0.10f), 0.08f, 0f,
                        TextureKit.WoodGrain("CharredGrain", Color.white, new Color(0.40f, 0.34f, 0.30f)));

    private static Material Wicker() =>
        BuildKit.Opaque("DressWicker", new Color(0.48f, 0.36f, 0.20f), 0.14f, 0f,
                        TextureKit.Fabric("WickerWeave", Color.white));

    private static Material Plush() =>
        BuildKit.Opaque("DressPlush", new Color(0.44f, 0.22f, 0.20f), 0.10f, 0f,
                        TextureKit.Fabric("PlushWeave", Color.white));

    private static Material Cloth() =>
        BuildKit.Opaque("DressCloth", new Color(0.34f, 0.33f, 0.30f), 0.06f, 0f,
                        TextureKit.Fabric("PlushWeave", Color.white));

    private static Material Linen() =>
        BuildKit.Opaque("DressLinen", new Color(0.78f, 0.76f, 0.71f), 0.07f, 0f,
                        TextureKit.Fabric("PlushWeave", Color.white));

    private static Material Drape() =>
        BuildKit.Opaque("DressDrape", new Color(0.22f, 0.21f, 0.30f), 0.09f, 0f,
                        TextureKit.Fabric("PlushWeave", Color.white));

    private static Material Wax() =>
        BuildKit.Opaque("DressWax", new Color(0.87f, 0.83f, 0.72f), 0.16f, 0f, null);


    private static Material Ember()
    {
        Material mat = BuildKit.Opaque("DressEmber", new Color(0.30f, 0.09f, 0.03f), 0.10f, 0f, null);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1.05f, 0.34f, 0.08f));
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
