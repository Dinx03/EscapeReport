using System.IO;
using UnityEditor;
using UnityEngine;

// Builds the clock prefabs out of primitives, so the props stay reproducible from source
// instead of becoming binaries nobody can diff.
//
//   Tools > Escape Room > Clocks > Build Dial Prefab
//   Tools > Escape Room > Clocks > Build Wall Clock Prefab   (rebuilds the dial first)
//
// The dial is its own prefab on purpose. A wristwatch, a pocket watch and a longcase all
// want the same hands and the same ClockDial script; only the case around them differs.
public static class ClockBuilder
{
    private const string TextureFolder = "Assets/Textures";
    private const string MaterialFolder = "Assets/Materials";
    private const string PrefabFolder = "Assets/Prefabs/Clocks";
    private const string WallClockPath = PrefabFolder + "/WallClock.prefab";
    private const string DialPath = PrefabFolder + "/ClockDial.prefab";

    private const float DialRadius = 0.082f;

    // Wall clock case, local to the prefab origin which sits flat against the wall.
    private const float StandOff = 0.09f;
    private const float CaseWidth = 0.22f;
    private const float CaseDepth = 0.10f;
    private const float CaseBottom = 0.09f;
    private const float CaseTop = 0.61f;
    private const float DialY = 0.505f;

    private static float Back => StandOff;
    private static float Front => StandOff + CaseDepth;

    [MenuItem("Tools/Escape Room/Clocks/Build Dial Prefab")]
    public static void BuildDialMenu()
    {
        Selection.activeObject = BuildDial();
    }

    [MenuItem("Tools/Escape Room/Clocks/Build Wall Clock Prefab")]
    public static void BuildWallClock()
    {
        GameObject dialPrefab = BuildDial();

        Material wood = Wood();
        Material woodDark = WoodDark();
        Material brass = Brass();
        Material reelMat = Reel();

        var root = new GameObject("WallClock");
        int interactable = LayerMask.NameToLayer("Interactable");

        // ---- carcass ----
        Transform carcass = Group("Case", root.transform, Vector3.zero);
        float caseMidY = (CaseBottom + CaseTop) * 0.5f;
        float caseHeight = CaseTop - CaseBottom;

        Box("BackPanel", carcass, new Vector3(0f, caseMidY, Back - 0.006f),
            new Vector3(CaseWidth, caseHeight, 0.012f), woodDark);
        Box("SideLeft", carcass, new Vector3(-CaseWidth * 0.5f + 0.009f, caseMidY, Back + CaseDepth * 0.5f),
            new Vector3(0.018f, caseHeight, CaseDepth), wood);
        Box("SideRight", carcass, new Vector3(CaseWidth * 0.5f - 0.009f, caseMidY, Back + CaseDepth * 0.5f),
            new Vector3(0.018f, caseHeight, CaseDepth), wood);
        Cylinder("ColumnLeft", carcass, new Vector3(-CaseWidth * 0.5f + 0.012f, caseMidY, Front - 0.014f),
            0.022f, caseHeight, wood);
        Cylinder("ColumnRight", carcass, new Vector3(CaseWidth * 0.5f - 0.012f, caseMidY, Front - 0.014f),
            0.022f, caseHeight, wood);

        // ---- crown ----
        Transform crown = Group("Crown", root.transform, Vector3.zero);
        Box("Cornice", crown, new Vector3(0f, CaseTop + 0.011f, Back + CaseDepth * 0.5f),
            new Vector3(CaseWidth + 0.045f, 0.022f, CaseDepth + 0.030f), wood);
        Box("Pediment", crown, new Vector3(0f, CaseTop + 0.048f, Back + CaseDepth * 0.5f),
            new Vector3(CaseWidth + 0.010f, 0.052f, CaseDepth), wood);
        // A squat ellipse, not a circle — a full disc reads as a dome sitting on the case.
        Ellipse("Arch", crown, new Vector3(0f, CaseTop + 0.040f, Back + CaseDepth * 0.5f - 0.004f),
            CaseWidth * 0.94f, 0.105f, CaseDepth - 0.006f, wood);
        Disc("Rosette", crown, new Vector3(0f, CaseTop + 0.052f, Front - 0.004f), 0.042f, 0.010f, wood);

        // ---- base ----
        Transform plinth = Group("Base", root.transform, Vector3.zero);
        Box("Shelf", plinth, new Vector3(0f, CaseBottom - 0.011f, Back + CaseDepth * 0.5f),
            new Vector3(CaseWidth + 0.055f, 0.022f, CaseDepth + 0.036f), wood);
        Box("Apron", plinth, new Vector3(0f, CaseBottom - 0.036f, Back + CaseDepth * 0.5f),
            new Vector3(CaseWidth - 0.030f, 0.030f, CaseDepth - 0.010f), wood);
        Sphere("Finial", plinth, new Vector3(0f, CaseBottom - 0.058f, Back + CaseDepth * 0.5f), 0.022f, wood);

        // ---- dial, dropped in as a nested prefab ----
        var dial = (GameObject)PrefabUtility.InstantiatePrefab(dialPrefab);
        dial.transform.SetParent(root.transform, false);
        dial.transform.localPosition = new Vector3(0f, DialY, Front - 0.012f);

        // ---- pendulum, hung behind the dial ----
        Transform swing = Group("Pendulum", root.transform,
            new Vector3(0f, DialY - 0.02f, Back + CaseDepth * 0.45f));
        Box("Rod", swing, new Vector3(0f, -0.135f, 0f), new Vector3(0.005f, 0.270f, 0.005f), brass);
        Disc("Bob", swing, new Vector3(0f, -0.285f, 0f), 0.070f, 0.008f, brass);

        // ---- crank on the back, where a winder belongs ----
        // It spent a while on the front, because a clock hanging flat on a wall puts its own
        // case between the player and the handle and nothing can reach it. The answer was
        // never to move the crank; it was to let the player take the clock down. F lifts this
        // one and turns it round, and E winds whatever is facing them — so the winder can go
        // back where a clockmaker would have put it.
        var winder = new GameObject("Winder");
        winder.transform.SetParent(root.transform, false);
        winder.transform.localPosition = new Vector3(0f, DialY - 0.16f, Back - 0.012f);
        winder.layer = interactable;

        Box("Plate", winder.transform, new Vector3(0f, 0.022f, -0.008f),
            new Vector3(0.098f, 0.120f, 0.014f), woodDark);

        // The window stays still — bolting it to the handle would spin the digit too.
        Transform window = Group("Window", winder.transform, new Vector3(0f, 0.062f, -0.016f));
        Box("Recess", window, Vector3.zero, new Vector3(0.046f, 0.034f, 0.005f), woodDark);
        // The built-in quad already faces -Z, which is out of the back of the case, so this
        // one needs no turning at all.
        GameObject reel = Quad("Reel", window, new Vector3(0f, 0f, -0.005f), 0.030f, reelMat);
        reel.transform.localRotation = Quaternion.identity;
        GameObject shutter = Box("Shutter", window, new Vector3(0f, 0f, -0.009f),
            new Vector3(0.050f, 0.036f, 0.004f), brass);

        Transform crank = Group("Crank", winder.transform, new Vector3(0f, 0f, -0.014f));
        // Posed swung out to the side rather than straight up, so the handle clears the
        // case and the player can see there is something to turn.
        crank.localRotation = Quaternion.Euler(0f, 0f, 118f);
        Disc("Hub", crank, Vector3.zero, 0.050f, 0.024f, brass);
        Disc("Collar", crank, new Vector3(0f, 0f, -0.014f), 0.030f, 0.010f, brass);
        Box("Arm", crank, new Vector3(0f, 0.088f, -0.010f), new Vector3(0.018f, 0.170f, 0.011f), brass);
        Disc("Handle", crank, new Vector3(0f, 0.168f, -0.018f), 0.017f, 0.030f, brass);

        // Tucked in behind the case, so the crosshair cannot find it while the clock is on
        // the wall. Reaching it is the whole reason to pick the clock up.
        var winderCollider = winder.AddComponent<BoxCollider>();
        winderCollider.center = new Vector3(0f, 0.02f, -0.020f);
        winderCollider.size = new Vector3(0.24f, 0.26f, 0.050f);

        // One collider for the case, hugging it rather than the cornice. It stops short of
        // the crank in front and leaves the dial above free for a closer look.
        var hull = new GameObject("Hull");
        hull.transform.SetParent(root.transform, false);
        hull.transform.localPosition = new Vector3(0f, 0.36f, Back + CaseDepth * 0.5f);
        hull.layer = interactable;
        hull.AddComponent<BoxCollider>().size = new Vector3(CaseWidth + 0.005f, 0.70f, CaseDepth + 0.02f);

        // ---- components ----
        var movement = root.AddComponent<ClockMovement>();

        var pendulum = swing.gameObject.AddComponent<Pendulum>();

        // Small enough to lift off its hook, and it has to be lifted: the winder is round the
        // back. F takes it down and turns it over, E winds it, Q hangs it up again.
        var inspectable = root.AddComponent<Inspectable>();
        var inspectableSo = new SerializedObject(inspectable);
        inspectableSo.FindProperty("style").enumValueIndex = (int)InspectStyle.Turn;
        inspectableSo.ApplyModifiedProperties();

        // The spring lives in the winding assembly. The movement finds it by looking through
        // its own children for IClockParts, so nothing has to be wired back the other way.
        var spring = winder.AddComponent<Mainspring>();

        var clockWinder = winder.AddComponent<ClockWinder>();
        Wire(clockWinder, "movement", movement);
        Wire(clockWinder, "crank", crank);
        Wire(clockWinder, "shutter", shutter.transform);
        Wire(clockWinder, "numberReel", reel.GetComponent<Renderer>());

        PrefabUtility.SaveAsPrefabAsset(root, WallClockPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();

        Debug.Log("Wall clock prefab written to " + WallClockPath);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(WallClockPath);
    }

    // Bezel, face, hands and hub, with its origin at the centre of the bezel and the face
    // looking down +Z. Drop it into any case and wire one reference.
    public static GameObject BuildDial()
    {
        EnsureFolder(TextureFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(PrefabFolder);

        WriteDialTexture();
        WriteDigitStripTexture();
        AssetDatabase.Refresh();

        Material brass = Brass();
        Material hand = Hand();
        Material face = Face();

        var root = new GameObject("ClockDial");

        Disc("Bezel", root.transform, Vector3.zero, DialRadius * 2f + 0.014f, 0.012f, brass);
        Quad("Face", root.transform, new Vector3(0f, 0f, 0.007f), DialRadius * 2f, face);

        Transform hands = Group("Hands", root.transform, new Vector3(0f, 0f, 0.009f));
        Transform hour = HandPivot("HourHand", hands, 0f, new Vector3(0.008f, 0.048f, 0.004f), hand);
        Transform minute = HandPivot("MinuteHand", hands, 0.003f, new Vector3(0.006f, 0.068f, 0.003f), hand);
        Transform second = HandPivot("SecondHand", hands, 0.006f, new Vector3(0.002f, 0.072f, 0.002f), brass);
        Disc("Hub", hands, new Vector3(0f, 0f, 0.008f), 0.014f, 0.005f, brass);

        var dial = root.AddComponent<ClockDial>();
        Wire(dial, "hourHand", hour);
        Wire(dial, "minuteHand", minute);
        Wire(dial, "secondHand", second);

        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, DialPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();

        Debug.Log("Dial prefab written to " + DialPath);
        return asset;
    }

    private static void Wire(Object target, string property, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    // ---------- materials ----------

    private static Material Wood() => MakeMaterial("ClockCase", new Color(0.33f, 0.18f, 0.10f), 0.35f, 0f, null);
    private static Material WoodDark() => MakeMaterial("ClockCaseDark", new Color(0.17f, 0.10f, 0.06f), 0.20f, 0f, null);
    private static Material Brass() => MakeMaterial("ClockBrass", new Color(0.83f, 0.66f, 0.26f), 0.70f, 0.9f, null);
    private static Material Hand() => MakeMaterial("ClockHand", new Color(0.08f, 0.07f, 0.07f), 0.35f, 0.2f, null);

    private static Material Face()
    {
        Material mat = MakeMaterial("ClockFace", Color.white, 0.08f, 0f, ImportDial());

        // Everything outside the painted circle is cut away, so the round bezel shows
        // through instead of the square corners of the quad the dial is drawn on.
        mat.SetFloat("_AlphaClip", 1f);
        mat.SetFloat("_Cutoff", 0.5f);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material Reel()
    {
        Texture strip = ImportDigitStrip();
        Material mat = MakeMaterial("ClockReel", Color.white, 0.05f, 0f, strip);
        mat.SetTextureScale("_BaseMap", new Vector2(1f, 1f / 10f));
        mat.SetTextureOffset("_BaseMap", Vector2.zero);

        // The window faces the wall and sits in the clock's own shadow, so the reel lights
        // itself. Emission carries the same strip, which keeps the ink dark and the plate
        // bright rather than washing the digit out.
        mat.SetTexture("_EmissionMap", strip);
        mat.SetTextureScale("_EmissionMap", new Vector2(1f, 1f / 10f));
        mat.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.85f) * 1.4f);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material MakeMaterial(string name, Color colour, float smoothness, float metallic, Texture baseMap)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", colour);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        if (baseMap != null)
            mat.SetTexture("_BaseMap", baseMap);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------- primitives ----------

    private static Transform Group(string name, Transform parent, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    private static Transform HandPivot(string name, Transform parent, float z, Vector3 size, Material material)
    {
        var pivot = new GameObject(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = new Vector3(0f, 0f, z);
        Box("Blade", pivot.transform, new Vector3(0f, size.y * 0.5f, 0f), size, material);
        return pivot.transform;
    }

    private static GameObject Disc(string name, Transform parent, Vector3 localPos, float diameter, float depth, Material material)
    {
        var go = Primitive(PrimitiveType.Cylinder, name, parent, localPos, material);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // stand the cylinder on its Z axis
        go.transform.localScale = new Vector3(diameter, depth * 0.5f, diameter);
        return go;
    }

    private static GameObject Ellipse(string name, Transform parent, Vector3 localPos, float width, float height, float depth, Material material)
    {
        var go = Primitive(PrimitiveType.Cylinder, name, parent, localPos, material);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = new Vector3(width, depth * 0.5f, height);
        return go;
    }

    private static GameObject Cylinder(string name, Transform parent, Vector3 localPos, float diameter, float height, Material material)
    {
        var go = Primitive(PrimitiveType.Cylinder, name, parent, localPos, material);
        go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
        return go;
    }

    private static GameObject Sphere(string name, Transform parent, Vector3 localPos, float diameter, Material material)
    {
        var go = Primitive(PrimitiveType.Sphere, name, parent, localPos, material);
        go.transform.localScale = Vector3.one * diameter;
        return go;
    }

    private static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Material material)
    {
        var go = Primitive(PrimitiveType.Cube, name, parent, localPos, material);
        go.transform.localScale = size;
        return go;
    }

    private static GameObject Quad(string name, Transform parent, Vector3 localPos, float size, Material material)
    {
        var go = Primitive(PrimitiveType.Quad, name, parent, localPos, material);
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // the built-in quad faces -Z
        go.transform.localScale = Vector3.one * size;
        return go;
    }

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    // ---------- textures ----------

    private static Texture ImportDial()
    {
        string path = TextureFolder + "/ClockFace.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture>(path);
    }

    private static Texture ImportDigitStrip()
    {
        string path = TextureFolder + "/ClockDigits.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Repeat;   // the reel runs past 9 and back to 0
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture>(path);
    }

    // Ten digits stacked bottom to top, so a V offset of d/10 parks digit d in the window.
    private static void WriteDigitStripTexture()
    {
        const int W = 96, Cell = 96, H = Cell * 10;
        var px = new Color[W * H];
        var plate = new Color(0.90f, 0.87f, 0.79f);
        var ink = new Color(0.08f, 0.07f, 0.07f);

        for (int i = 0; i < px.Length; i++) px[i] = plate;

        for (int digit = 0; digit < 10; digit++)
        {
            float baseY = digit * Cell;

            // A hairline between cells sells it as a reel rather than a flat sprite.
            Fill(px, W, H, 0f, baseY, W, 2f, new Color(0.70f, 0.66f, 0.58f));
            DrawDigit(px, W, H, digit, W * 0.5f - 24f, baseY + Cell * 0.5f - 30f, 48f, 60f, ink);
        }

        WritePng(px, W, H, "ClockDigits.png");
    }

    private static void WriteDialTexture()
    {
        const int S = 512;
        var px = new Color[S * S];
        var white = new Color(0.96f, 0.95f, 0.92f);
        var ink = new Color(0.09f, 0.08f, 0.08f);
        var clear = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float dx = (x - S * 0.5f) / (S * 0.5f);
            float dy = (y - S * 0.5f) / (S * 0.5f);
            px[y * S + x] = dx * dx + dy * dy <= 1f ? white : clear;
        }

        Ring(px, S, 0.60f, 0.62f, ink);
        Ring(px, S, 0.86f, 0.875f, ink);

        for (int i = 0; i < 60; i++)
        {
            bool onTheHour = i % 5 == 0;
            float angle = i / 60f * Mathf.PI * 2f;
            float inner = onTheHour ? 0.78f : 0.83f;
            float halfWidth = onTheHour ? 5.5f : 2f;
            float sin = Mathf.Sin(angle), cos = Mathf.Cos(angle);

            for (float r = inner; r <= 0.86f; r += 0.0015f)
            for (float t = -halfWidth; t <= halfWidth; t += 0.5f)
                Set(px, S, S, S * 0.5f + sin * r * S * 0.5f + cos * t, S * 0.5f + cos * r * S * 0.5f - sin * t, ink);
        }

        for (int hour = 1; hour <= 12; hour++)
        {
            float angle = hour / 12f * Mathf.PI * 2f;
            DrawNumber(px, S, S, hour,
                S * 0.5f + Mathf.Sin(angle) * 0.71f * S * 0.5f,
                S * 0.5f + Mathf.Cos(angle) * 0.71f * S * 0.5f, ink);
        }

        WritePng(px, S, S, "ClockFace.png");
    }

    private static void WritePng(Color[] px, int width, int height, string fileName)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "Textures/" + fileName), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void DrawNumber(Color[] px, int width, int height, int value, float cx, float cy, Color ink)
    {
        const float DigitW = 30f, DigitH = 50f, Gap = 7f;
        string text = value.ToString();
        float x = cx - (text.Length * DigitW + (text.Length - 1) * Gap) * 0.5f;

        foreach (char c in text)
        {
            DrawDigit(px, width, height, c - '0', x, cy - DigitH * 0.5f, DigitW, DigitH, ink);
            x += DigitW + Gap;
        }
    }

    private static void DrawDigit(Color[] px, int width, int height, int digit, float ox, float oy, float w, float h, Color ink)
    {
        // a b c d e f g, the usual seven-segment layout.
        string[] segments = { "abcdef", "bc", "abged", "abgcd", "fgbc", "afgcd", "afgcde", "abc", "abcdefg", "abcfgd" };
        string on = segments[digit];
        float t = Mathf.Max(5f, w / 4.5f);

        if (on.Contains("a")) Fill(px, width, height, ox, oy + h - t, w, t, ink);
        if (on.Contains("g")) Fill(px, width, height, ox, oy + h * 0.5f - t * 0.5f, w, t, ink);
        if (on.Contains("d")) Fill(px, width, height, ox, oy, w, t, ink);
        if (on.Contains("f")) Fill(px, width, height, ox, oy + h * 0.5f, t, h * 0.5f, ink);
        if (on.Contains("b")) Fill(px, width, height, ox + w - t, oy + h * 0.5f, t, h * 0.5f, ink);
        if (on.Contains("e")) Fill(px, width, height, ox, oy, t, h * 0.5f, ink);
        if (on.Contains("c")) Fill(px, width, height, ox + w - t, oy, t, h * 0.5f, ink);
    }

    private static void Fill(Color[] px, int width, int height, float x0, float y0, float w, float h, Color colour)
    {
        for (int y = Mathf.RoundToInt(y0); y < Mathf.RoundToInt(y0 + h); y++)
        for (int x = Mathf.RoundToInt(x0); x < Mathf.RoundToInt(x0 + w); x++)
            Set(px, width, height, x, y, colour);
    }

    private static void Ring(Color[] px, int size, float innerRadius, float outerRadius, Color colour)
    {
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - size * 0.5f) / (size * 0.5f);
            float dy = (y - size * 0.5f) / (size * 0.5f);
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r >= innerRadius && r <= outerRadius)
                px[y * size + x] = colour;
        }
    }

    private static void Set(Color[] px, int width, int height, float x, float y, Color colour)
    {
        int ix = Mathf.RoundToInt(x), iy = Mathf.RoundToInt(y);
        if (ix >= 0 && ix < width && iy >= 0 && iy < height)
            px[iy * width + ix] = colour;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }
}
