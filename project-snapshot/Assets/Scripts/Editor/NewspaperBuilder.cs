using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// The paper on the showroom table, next to the coin.
//
// Same bargain as the shop's care leaflet, one prop along: **words are UI and the sheet is a
// material.** The masthead, the date and the headline are live `Text` in the same face the
// HUD uses, so they can be rewritten or translated without anything being redrawn; the
// columns underneath are drawn into the map as the *shape* of type, because a column of six
// point news set on a prop is a grey smudge whichever way it is done. What the columns
// actually say is on the `WrittenPage`, and the HUD reads it out when the paper is picked up.
//
// **Rebuilding does not eat what you wrote.** Everything on this prop that is words — the
// three printed lines and both halves of the `WrittenPage` — is lifted off the old instance
// before it is destroyed and put back on the new one. Run this as often as you like to
// change the paper; the writing is yours.
//
// It sits under the room and not under `Dressing`, so `Dress All Rooms` cannot take it away.
public static class NewspaperBuilder
{
    private const string PrefabPath = "Assets/Prefabs/House/Newspaper.prefab";
    private const string Room = "House/Props/Room_I_Showroom";

    // The sheet, in millimetres, and everything on it laid out against these numbers. The map
    // and the type are drawn from the one table, so a band left blank on the paper is blank
    // exactly where the words land in it.
    private const float SheetW = 300f, SheetH = 400f;
    private const float Margin = 14f;

    private const float MastheadTop = 16f, MastheadBottom = 50f;
    private const float DatelineTop = 61f, DatelineBottom = 71f;
    private const float HeadlineTop = 78f, HeadlineBottom = 110f;
    private const float PhotoTop = 118f, PhotoBottom = 184f;
    private const float CaptionTop = 187f;
    private const float BodyTop = 198f;
    private const float BodyBottom = SheetH - Margin;

    private const int Columns = 3;
    private const float Gutter = 6f;
    private const float LineHeight = 4.2f;   // one line of body type, baseline to baseline

    // How thick a line of that type prints. Set to the full height of a line it comes out as
    // brickwork: two thirds of the column is ink, where real news is nearer a quarter and
    // reads as grey from any distance at all.
    private const float LineWeight = 1.5f;

    // Square pixels: 768 × 1024 over 300 × 400 mm is 2.56 either way.
    private const int MapW = 768, MapH = 1024;
    private const float Px = MapW / SheetW;

    // Ten units to the millimetre on the canvas, the same as the leaflet. The largest type
    // here is a 34 mm masthead, which asks the font for 270 — under the 500 a dynamic font
    // will actually grow to, so this sheet can keep the finer scale.
    private const float PerMillimetre = 0.0001f;

    private static float ColumnWidth => (SheetW - Margin * 2f - Gutter * (Columns - 1)) / Columns;
    private static float ColumnLeft(int column) => Margin + column * (ColumnWidth + Gutter);

    [MenuItem("Tools/Escape Room/House/Build Newspaper")]
    public static void BuildNewspaper()
    {
        BuildKit.EnsureFolder("Assets/Prefabs/House");

        // Lift the writing off whatever is already in the room before anything is destroyed.
        Transform room = GameObject.Find(Room).transform;
        Transform old = room.Find("Newspaper");

        string masthead = Read(old, "Face/Masthead", "ข่าวรายวัน");
        string dateline = Read(old, "Face/Dateline", "ฉบับที่ ๔๗ · วันพฤหัสบดี");
        string headline = Read(old, "Face/Headline", "ร้านนาฬิกาปิดเงียบ\nเจ้าของหายไปพร้อมเสียงระฆัง");

        string heading = "ร้านนาฬิกาปิดเงียบ เจ้าของหายไปพร้อมเสียงระฆัง";
        string body = "(ยังไม่ได้เขียน — พิมพ์เนื้อข่าวลงใน WrittenPage ของพร็อพนี้)";

        if (old != null)
        {
            var was = old.GetComponent<WrittenPage>();
            heading = was.Heading;
            body = was.Body;
            Object.DestroyImmediate(old.gameObject);
        }

        WriteNewsprint();
        GameObject prefab = BuildSheet();

        var paper = (GameObject)PrefabUtility.InstantiatePrefab(prefab, room);
        paper.name = "Newspaper";

        // On the table beside the coin, squared up to nothing in particular, the way a paper
        // is put down rather than laid out.
        //
        // Turned to face away from the room, not towards it: the masthead is at the sheet's
        // own +z because that is what brings it up the screen when the paper is lifted, so on
        // the table that end has to point at the far side for anyone standing on this one to
        // read it. Which way it lies and which way it reads in the hand are separate questions,
        // because Inspectable turns a held prop to an angle of its own and forgets the rest.
        paper.transform.SetPositionAndRotation(new Vector3(1.18f, 0.770f, 0.22f), Quaternion.Euler(0f, 188f, 0f));

        Write(paper.transform, "Face/Masthead", masthead);
        Write(paper.transform, "Face/Dateline", dateline);
        Write(paper.transform, "Face/Headline", headline);

        var page = paper.GetComponent<WrittenPage>();
        BuildKit.Wire(page, "heading", heading);
        BuildKit.Wire(page, "body", body);

        EditorUtility.SetDirty(paper);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(paper.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Newspaper rebuilt on the showroom table. The writing on it was kept.");
    }

    // ---------- the prop ----------

    private static GameObject BuildSheet()
    {
        Material print = BuildKit.Opaque("Newsprint", new Color(0.82f, 0.77f, 0.63f), 0.04f, 0f,
            AssetDatabase.LoadAssetAtPath<Texture2D>(BuildKit.TextureFolder + "/Newsprint.png"));

        // The pages underneath: the same paper without the printing, a shade browner where a
        // stack of newsprint goes at the edges.
        Material inner = BuildKit.Opaque("NewsprintInner", new Color(0.74f, 0.69f, 0.56f), 0.04f, 0f,
            AssetDatabase.LoadAssetAtPath<Texture2D>(BuildKit.TextureFolder + "/WallPlaster.png"));

        float w = SheetW / 1000f, h = SheetH / 1000f;
        var root = new GameObject("Newspaper");

        // Three sheets, each a little out of square with the one under it. One slab is a card;
        // it is the pages that do not quite line up that read as newsprint somebody folded.
        foreach (var leaf in new[] { new Vector3(-3.4f, 0.0015f, 0.004f), new Vector3(2.1f, 0.0045f, -0.003f) })
        {
            GameObject under = BuildKit.Box("Under", root.transform, new Vector3(leaf.z, leaf.y, 0f),
                new Vector3(w + 0.004f, 0.003f, h + 0.005f), inner);
            under.transform.localRotation = Quaternion.Euler(0f, leaf.x, 0f);
        }

        BuildKit.Box("Front", root.transform, new Vector3(0f, 0.0075f, 0f), new Vector3(w, 0.003f, h), print);

        Face(root.transform);

        // Flat and low, so a downward glance finds the paper and not the table through it.
        BuildKit.Hull(root, new Vector3(0f, 0.006f, 0f), new Vector3(w + 0.01f, 0.022f, h + 0.01f));

        // Lifted to the eyes, printed side towards them: the sheet lies face up, so the turn
        // that brings +y round to the camera is a quarter back about x. Held, not turned —
        // the back of a newspaper is the rest of the newspaper, and there is nothing on it.
        var inspectable = root.AddComponent<Inspectable>();
        BuildKit.Wire(inspectable, "style", (int)InspectStyle.Hold);
        BuildKit.Wire(inspectable, "facing", new Vector3(270f, 0f, 0f));

        root.AddComponent<WrittenPage>();

        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return asset;
    }

    // The three lines that are type rather than picture. A canvas is read from its own -z,
    // so this is turned a quarter about x to lie face up on the sheet with the masthead at
    // the far end — the same half-turn problem the leaflet has, one axis over.
    private static void Face(Transform parent)
    {
        var face = new GameObject("Face", typeof(RectTransform), typeof(Canvas));
        face.transform.SetParent(parent, false);
        face.transform.localPosition = new Vector3(0f, 0.0095f, 0f);
        face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        face.transform.localScale = Vector3.one * PerMillimetre;

        // Said out loud. A Canvas with no Canvas above it defaults to screen space, and a
        // screen-space masthead is one printed across the player's whole view.
        face.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ((RectTransform)face.transform).sizeDelta = new Vector2(SheetW, SheetH) * 10f;

        var ink = new Color(0.13f, 0.11f, 0.09f);
        Line(face.transform, "Masthead", MastheadTop, MastheadBottom, 272, FontStyle.Bold, ink, 1f);
        Line(face.transform, "Dateline", DatelineTop, DatelineBottom, 62, FontStyle.Normal, ink * 1.6f, 1f);
        Line(face.transform, "Headline", HeadlineTop, HeadlineBottom, 128, FontStyle.Bold, ink, 1.05f);
    }

    private static void Line(Transform face, string name, float top, float bottom, int size,
                             FontStyle weight, Color ink, float leading)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(face, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(-Margin * 2f * 10f, (bottom - top) * 10f);
        rect.anchoredPosition = new Vector2(0f, -top * 10f);

        var type = go.GetComponent<Text>();
        type.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/LeelawadeeUI.ttf");
        type.fontSize = size;
        type.fontStyle = weight;
        type.alignment = TextAnchor.UpperCenter;
        type.lineSpacing = leading;
        type.horizontalOverflow = HorizontalWrapMode.Wrap;
        type.verticalOverflow = VerticalWrapMode.Overflow;
        type.color = ink;
        type.text = name;
    }

    // ---------- the map ----------

    // Everything on the front page that is not a word: the rules, the columns of type, the
    // halftone, the crease down the middle and the age. Drawn white, so the yellow of old
    // newsprint is the material's business and one drawing would serve a fresh paper too.
    private static void WriteNewsprint()
    {
        var px = new Color[MapW * MapH];
        var ink = new Color(0.16f, 0.15f, 0.14f);
        var faint = new Color(0.45f, 0.43f, 0.41f);

        // Body type is not black. A line of six point news is mostly the paper between the
        // letters, and printing it black gives a page of black stripes rather than columns.
        var body = new Color(0.34f, 0.32f, 0.30f);

        for (int i = 0; i < px.Length; i++)
            px[i] = Color.white;

        // Masthead rules: one heavy, one hair, the way a paper closes off its own name.
        Bar(px, Margin, MastheadBottom + 4f, SheetW - Margin * 2f, 2.0f, ink);
        Bar(px, Margin, MastheadBottom + 8f, SheetW - Margin * 2f, 0.7f, ink);
        Bar(px, Margin, DatelineBottom + 3f, SheetW - Margin * 2f, 0.7f, ink);
        Bar(px, Margin, HeadlineBottom + 4f, SheetW - Margin * 2f, 0.7f, ink);

        Halftone(px, Margin, PhotoTop, ColumnWidth * 2f + Gutter, PhotoBottom - PhotoTop, ink);

        // The caption under it, in the same shape-of-type the columns are.
        Typeset(px, Margin, CaptionTop, ColumnWidth * 2f + Gutter, 2, 1, faint, 4001);

        // Three columns. The third runs the full depth beside the picture; the other two
        // start under it.
        for (int column = 0; column < Columns; column++)
        {
            float top = column == Columns - 1 ? PhotoTop : BodyTop;
            int lines = Mathf.FloorToInt((BodyBottom - top) / LineHeight);
            Typeset(px, ColumnLeft(column), top, ColumnWidth, lines, 14, body, 700 + column * 37);
        }

        // Column rules, down the gutters, and only as far as the type beside them goes.
        Bar(px, ColumnLeft(1) - Gutter * 0.5f, BodyTop, 0.8f, BodyBottom - BodyTop, faint);
        Bar(px, ColumnLeft(2) - Gutter * 0.5f, PhotoTop, 0.8f, BodyBottom - PhotoTop, faint);

        Age(px);
        TextureKit.Save(px, MapW, MapH, "Newsprint");
    }

    // A column of news, as the shape of it: lines of words with ragged ends, and a short one
    // wherever a paragraph stops. Nothing here is legible and nothing is meant to be — what
    // the paper says is on its WrittenPage.
    private static void Typeset(Color[] px, float x, float y, float width, int lines,
                                int linesPerParagraph, Color ink, int seed)
    {
        for (int line = 0; line < lines; line++)
        {
            float top = y + line * LineHeight;
            bool endsParagraph = linesPerParagraph > 0 && line % linesPerParagraph == linesPerParagraph - 1;
            float run = endsParagraph ? width * Mathf.Lerp(0.25f, 0.8f, Noise(line, 3, seed)) : width;
            float at = 0f;

            for (int word = 0; at < run; word++)
            {
                float length = Mathf.Lerp(5f, 21f, Noise(line, word, seed));
                if (at + length > run)
                    length = run - at;

                if (length > 1.2f)
                    Bar(px, x + at, top, length, LineWeight, ink);

                at += length + 1.2f;
            }
        }
    }

    // A press photograph of the shop, printed the way a paper of this age prints one: no
    // greys anywhere, only dots on a regular screen, close together where the picture is dark
    // and far apart where it is light.
    //
    // The screen is ordered and not random. A dot per pixel thrown at random is grain, which
    // is what a photocopier does to a photograph; a 4×4 ordered screen is what a press does,
    // and at this size its ruling is about a millimetre and a half — visible, which is the
    // whole point of printing it this way.
    private static void Halftone(Color[] px, float x, float y, float width, float height, Color ink)
    {
        int[] screen = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

        int x0 = Mathf.RoundToInt(x * Px), x1 = Mathf.RoundToInt((x + width) * Px);
        int y0 = Mathf.RoundToInt(y * Px), y1 = Mathf.RoundToInt((y + height) * Px);

        for (int j = y0; j < y1; j++)
        for (int i = x0; i < x1; i++)
        {
            float u = (i - x0) / (float)(x1 - x0);
            float v = (j - y0) / (float)(y1 - y0);
            float tone = Shopfront(u, v);

            // Darker into the corners, the way a plate of this age prints, and a little
            // unevenness in the inking so the flats do not read as a pattern swatch.
            tone += Mathf.Max(Mathf.Abs(u - 0.5f), Mathf.Abs(v - 0.5f)) * 0.10f;
            tone += (Noise(i / 3, j / 3, 91) - 0.5f) * 0.10f;

            float threshold = (screen[(j & 3) * 4 + (i & 3)] + 0.5f) / 16f;

            if (tone > threshold)
                px[j * MapW + (MapW - 1 - i)] = ink;
        }

        // Ruled off, because a picture with no rule round it reads as a hole in the page.
        Bar(px, x, y - 1.2f, width, 0.7f, ink);
        Bar(px, x, y + height + 0.5f, width, 0.7f, ink);
        Bar(px, x, y - 1.2f, 0.7f, height + 2.4f, ink);
        Bar(px, x + width - 0.7f, y - 1.2f, 0.7f, height + 2.4f, ink);
    }

    // How dark the picture wants to be at a point in it: a street, and the shop standing in
    // the middle of it with its neighbours either side.
    private static float Shopfront(float u, float v)
    {
        if (v > 0.90f) return 0.40f;                                  // the road

        if (u > 0.30f && u < 0.70f && v > 0.30f && v < 0.90f)         // the shop
        {
            // Lit from inside. Light openings on a dark front is what makes a building read
            // in a screen this coarse; dark ones on a mid-grey front read as smudges.
            if (u > 0.46f && u < 0.55f && v > 0.60f) return 0.08f;    // its door
            if (v > 0.40f && v < 0.54f && (u > 0.34f && u < 0.43f || u > 0.57f && u < 0.66f))
                return 0.06f;                                         // and its windows
            return 0.62f;
        }

        // The gable over it: a roof closing to a point above the shop front.
        if (v < 0.30f && v > 0.10f)
        {
            float half = Mathf.Lerp(0.04f, 0.22f, (v - 0.10f) / 0.20f);
            if (Mathf.Abs(u - 0.5f) < half) return 0.70f;
        }

        if (u < 0.28f && v > 0.46f && v < 0.90f) return 0.50f;        // the neighbour left
        if (u > 0.72f && v > 0.52f && v < 0.90f) return 0.54f;        // and right

        return 0.07f;                                                 // sky
    }

    // What twenty years in a shop does to a sheet: tooth, foxing, a crease where it was
    // folded once, and edges that have been handled.
    private static void Age(Color[] px)
    {
        int crease = Mathf.RoundToInt(SheetH * 0.5f * Px);

        for (int j = 0; j < MapH; j++)
        for (int i = 0; i < MapW; i++)
        {
            float wear = 0.965f + Noise(i, j, 17) * 0.035f;

            // The fold, softened either side of it so it reads as a crease and not a rule.
            float toCrease = Mathf.Abs(j - crease);
            if (toCrease < 5f)
                wear *= Mathf.Lerp(0.88f, 1f, toCrease / 5f);

            float edge = Mathf.Min(Mathf.Min(i, MapW - 1 - i), Mathf.Min(j, MapH - 1 - j)) / 26f;
            wear *= Mathf.Lerp(0.90f, 1f, Mathf.Clamp01(edge));

            px[j * MapW + i] *= wear;
        }

        // Foxing: a dozen soft brown blooms, which is the one thing that says *old* rather
        // than *cheap*.
        for (int blot = 0; blot < 14; blot++)
        {
            float cx = Noise(blot, 1, 55) * MapW;
            float cy = Noise(blot, 2, 55) * MapH;
            float r = 8f + Noise(blot, 3, 55) * 26f;

            for (int j = Mathf.RoundToInt(cy - r); j <= cy + r; j++)
            for (int i = Mathf.RoundToInt(cx - r); i <= cx + r; i++)
            {
                if (i < 0 || i >= MapW || j < 0 || j >= MapH)
                    continue;

                float fall = 1f - Vector2.Distance(new Vector2(i, j), new Vector2(cx, cy)) / r;
                if (fall > 0f)
                    px[j * MapW + i] *= 1f - fall * fall * 0.16f;
            }
        }
    }

    // ---------- pieces ----------

    // A rectangle placed in millimetres from the top left of the sheet, which is how a page
    // is laid out and is not how a Color[] is indexed.
    //
    // **The map is authored turned round, on purpose.** A cube UVs its top face with u running
    // towards -x and v towards -z — half a turn from the frame the canvas is in, and the canvas
    // is the one that cannot move, because the masthead has to be at +z for the hold to bring
    // it up the screen. So the sheet's top left corner is the map's first row, last column.
    //
    // It is a rotation and not a mirror, which is why a photograph drawn into it comes out the
    // right way round. Opened as a file this page stands on its head; on the prop it is the
    // right way up, and turning the file over to fix it turns the printing over with it.
    private static void Bar(Color[] px, float x, float y, float width, float height, Color ink)
    {
        int x0 = Mathf.RoundToInt(x * Px), x1 = Mathf.RoundToInt((x + width) * Px);
        int y0 = Mathf.RoundToInt(y * Px), y1 = Mathf.RoundToInt((y + height) * Px);

        for (int j = y0; j < y1; j++)
        for (int i = x0; i < x1; i++)
        {
            if (i >= 0 && i < MapW && j >= 0 && j < MapH)
                px[j * MapW + (MapW - 1 - i)] = ink;
        }
    }

    private static float Noise(int x, int y, int seed)
    {
        uint h = (uint)(x * 374761393 + y * 668265263 + seed * 69069);
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
    }

    private static string Read(Transform prop, string path, string fallback)
    {
        if (prop == null)
            return fallback;

        Transform found = prop.Find(path);
        return found == null ? fallback : found.GetComponent<Text>().text;
    }

    private static void Write(Transform prop, string path, string words)
    {
        prop.Find(path).GetComponent<Text>().text = words;
    }
}
