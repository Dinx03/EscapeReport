using System.IO;
using UnityEditor;
using UnityEngine;

// Procedural base maps, drawn once into Assets/Textures and loaded from there afterwards.
//
// The house is built from boxes, and a box lit by one lamp reads as flat colour however
// well it is placed. A tileable base map costs nothing at runtime and is most of the
// difference between a room and a diagram. Everything here is drawn from numbers so a
// colour change is an edit rather than a trip to an image editor.
public static class TextureKit
{
    private const int Size = 512;

    // ---------- noise ----------

    // Lattice hash that wraps on `period`, so noise built on it tiles.
    private static float Hash(int x, int y, int period, int seed)
    {
        x = ((x % period) + period) % period;
        y = ((y % period) + period) % period;

        int h = x * 374761393 + y * 668265263 + seed * 1442695041;
        h = (h ^ (h >> 13)) * 1274126177;
        return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777215f;
    }

    private static float Smooth(float t) => t * t * (3f - 2f * t);

    private static float Value(float x, float y, int period, int seed)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = Smooth(x - x0), fy = Smooth(y - y0);

        float a = Mathf.Lerp(Hash(x0, y0, period, seed), Hash(x0 + 1, y0, period, seed), fx);
        float b = Mathf.Lerp(Hash(x0, y0 + 1, period, seed), Hash(x0 + 1, y0 + 1, period, seed), fx);
        return Mathf.Lerp(a, b, fy);
    }

    // Several octaves of the above. `cells` is the coarsest lattice across the whole
    // texture, and every octave doubles it, so the result still wraps.
    private static float Fbm(float u, float v, int cells, int octaves, int seed)
    {
        float sum = 0f, amp = 1f, total = 0f;

        for (int o = 0; o < octaves; o++)
        {
            int period = cells << o;
            sum += Value(u * period, v * period, period, seed + o * 101) * amp;
            total += amp;
            amp *= 0.5f;
        }

        return sum / total;
    }

    // ---------- surfaces ----------

    // Old lime plaster: broad unevenness, a fine tooth over it, and a slow vertical stain
    // so a wall is lighter at the top than at the skirting.
    public static Texture2D Plaster(string name, Color colour)
    {
        var px = new Color[Size * Size];

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float u = x / (float)Size, v = y / (float)Size;
            float broad = Fbm(u, v, 3, 4, 17) - 0.5f;
            float tooth = Fbm(u, v, 32, 2, 91) - 0.5f;

            px[y * Size + x] = colour * (1f + broad * 0.16f + tooth * 0.07f);
        }

        return Save(px, Size, Size, name);
    }

    // Striped paper with a small repeated motif, the sort of thing a Victorian parlour is
    // papered in. The stripe count has to divide the width or the seam shows.
    public static Texture2D Wallpaper(string name, Color ground, Color stripe, Color motif)
    {
        var px = new Color[Size * Size];
        const int Stripes = 8, Motifs = 4;
        int band = Size / Stripes, cell = Size / Motifs;

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float u = x / (float)Size, v = y / (float)Size;
            Color c = (x % band) < band / 2 ? ground : stripe;

            // A four-petal rosette on the middle of every cell.
            float dx = (x % cell) - cell * 0.5f, dy = (y % cell) - cell * 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float petal = cell * 0.16f * (1f + 0.55f * Mathf.Cos(4f * Mathf.Atan2(dy, dx)));
            if (r < petal)
                c = Color.Lerp(c, motif, 0.85f);

            float grain = Fbm(u, v, 24, 2, 43) - 0.5f;
            px[y * Size + x] = c * (1f + grain * 0.10f);
        }

        return Save(px, Size, Size, name);
    }

    // Planks running along U, each one its own shade, with grain that follows the board and
    // a dark line where two boards meet.
    public static Texture2D Floorboards(string name, Color pale, Color dark)
    {
        var px = new Color[Size * Size];

        // Few, wide boards with heavy joints. Six thin ones with a one-pixel line between
        // them mipped away to nothing three metres off, and a floor that ought to read as
        // timber read as large blank tiles with a seam every so often.
        const int Boards = 4, Joint = 3;
        int board = Size / Boards;

        for (int y = 0; y < Size; y++)
        {
            int index = y / board;
            float shade = Hash(index, 0, Boards, 7);
            Color bed = Color.Lerp(pale, dark, shade * 0.75f);

            for (int x = 0; x < Size; x++)
            {
                float u = x / (float)Size, v = y / (float)Size;

                // Grain is stretched hard along the board, which is what makes it read as
                // sawn timber rather than as marble.
                float grain = Fbm(u * 1.6f, v * 9f + index * 3.7f, 8, 3, 23) - 0.5f;
                Color c = bed * (1f + grain * 0.22f);

                int into = y % board;
                if (into < Joint || into >= board - Joint)
                    c *= Mathf.Lerp(0.34f, 0.85f, Mathf.Min(into, board - 1 - into) / (float)Joint);

                // Butt joints, staggered so the boards do not all end in one line. One per
                // board per tile: more than that and the joints read as a grid.
                if ((x + index * 197) % Size < Joint)
                    c *= 0.42f;

                px[y * Size + x] = c;
            }
        }

        return Save(px, Size, Size, name);
    }

    // Fine dense fibre. Read at floor level this is mostly a break-up of the specular, which
    // is exactly what stops a carpet looking like painted concrete.
    public static Texture2D Carpet(string name, Color colour)
    {
        var px = new Color[Size * Size];

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float u = x / (float)Size, v = y / (float)Size;
            float fibre = Fbm(u, v, 64, 2, 61) - 0.5f;
            float patch = Fbm(u, v, 4, 3, 13) - 0.5f;

            px[y * Size + x] = colour * (1f + fibre * 0.20f + patch * 0.10f);
        }

        return Save(px, Size, Size, name);
    }

    // A printed leaflet: the tooth of the sheet, and the border a printer rules round it.
    //
    // The border belongs to the paper, so it is drawn into the map rather than laid over the
    // prop afterwards — hanging one of these is then a material and nothing else. It is the
    // one darker value on an otherwise white map, so it tints with the sheet and a change of
    // paper colour still costs one line.
    //
    // Drawn at the sheet's own aspect, which is what makes a pixel square: the inset and the
    // rule can be taken off the width alone and still come out even on all four sides.
    public static Texture2D Paper(string name, int width, int height)
    {
        const float InsetShare = 0.043f;
        const float RuleShare = 0.004f;

        int inset = Mathf.RoundToInt(width * InsetShare);
        int rule = Mathf.Max(2, Mathf.RoundToInt(width * RuleShare));

        var px = new Color[width * height];

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float u = x / (float)width, v = y / (float)height;
            float broad = Fbm(u, v, 3, 3, 41) - 0.5f;
            float tooth = Fbm(u, v, 48, 2, 83) - 0.5f;

            // Just below one rather than around it: white clips on the way out, and a sheet
            // whose light half is flattened away is a sheet with no tooth left. Kept close
            // to one so the paper stays the colour the material asked for.
            px[y * width + x] = Color.white * (0.97f + broad * 0.045f + tooth * 0.03f);
        }

        var line = Color.white * 0.42f;

        for (int y = inset; y < height - inset; y++)
        for (int x = inset; x < width - inset; x++)
        {
            bool onRule = x < inset + rule || x >= width - inset - rule
                       || y < inset + rule || y >= height - inset - rule;

            if (onRule)
                px[y * width + x] = line;
        }

        return Save(px, width, height, name);
    }

    // Furniture timber: rings that run along U, plus a few darker streaks.
    public static Texture2D WoodGrain(string name, Color pale, Color dark)
    {
        var px = new Color[Size * Size];

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float u = x / (float)Size, v = y / (float)Size;

            // Rings close together and only slightly wavy. Wide lazy rings read as camouflage
            // on a cabinet door rather than as sawn oak.
            float wobble = Fbm(u * 1.4f, v * 3f, 3, 3, 31);
            float rings = Mathf.Abs(Mathf.Sin((v * 26f + wobble * 2.2f) * Mathf.PI));
            float streak = Fbm(u * 0.8f, v * 40f, 4, 2, 71) - 0.5f;

            px[y * Size + x] = Color.Lerp(dark, pale, Mathf.Clamp01(0.46f + rings * 0.42f + streak * 0.34f));
        }

        return Save(px, Size, Size, name);
    }

    // A woven cloth: two sets of threads crossing, plus slow shading so a curtain is not
    // one flat sheet of colour.
    public static Texture2D Fabric(string name, Color colour)
    {
        var px = new Color[Size * Size];

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float u = x / (float)Size, v = y / (float)Size;

            float warp = Mathf.Sin(x * Mathf.PI / 3f) * 0.5f + 0.5f;
            float weft = Mathf.Sin(y * Mathf.PI / 3f) * 0.5f + 0.5f;
            float weave = (warp * weft + (1f - warp) * (1f - weft)) - 0.5f;
            float slub = Fbm(u, v, 6, 3, 53) - 0.5f;

            px[y * Size + x] = colour * (1f + weave * 0.13f + slub * 0.14f);
        }

        return Save(px, Size, Size, name);
    }

    // Sooty brick for the inside of a firebox: courses offset by half a brick, mortar
    // between them, and everything blackened unevenly by however many winters.
    public static Texture2D Brick(string name, Color colour, Color mortar)
    {
        var px = new Color[Size * Size];
        const int Courses = 8;
        int course = Size / Courses, brick = Size / 4;

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float u = x / (float)Size, v = y / (float)Size;
            int row = y / course;
            int shifted = (x + (row % 2) * brick / 2) % brick;

            bool joint = (y % course) < 3 || shifted < 3;
            int id = row * 977 + (x + (row % 2) * brick / 2) / brick;

            Color c = joint ? mortar : colour * (0.82f + Hash(id, 0, 4096, 5) * 0.36f);

            // Soot, heaviest towards the top where the draught takes it.
            float soot = Mathf.Clamp01(Fbm(u, v, 4, 3, 67) * 0.9f + v * 0.5f);
            px[y * Size + x] = Color.Lerp(c, new Color(0.05f, 0.045f, 0.04f), soot * 0.75f);
        }

        return Save(px, Size, Size, name);
    }

    // A hearth slab or a flagstone: broad blotches with a fine speckle over them.
    public static Texture2D Stone(string name, Color colour)
    {
        var px = new Color[Size * Size];

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float u = x / (float)Size, v = y / (float)Size;
            float blotch = Fbm(u, v, 5, 4, 29) - 0.5f;
            float grit = Fbm(u, v, 48, 2, 83) - 0.5f;

            px[y * Size + x] = colour * (1f + blotch * 0.24f + grit * 0.12f);
        }

        return Save(px, Size, Size, name);
    }

    // A print for the frames on the wall. Sepia, because whatever hangs in this house has
    // hung a long time, and a landscape, because a landscape still reads from across a room
    // where a portrait turns into a smudge.
    //
    // `seed` picks a different set of hills and a different tree, so two frames on one wall
    // are not the same picture twice.
    public static Texture2D Mezzotint(string name, int seed)
    {
        const int W = 256, H = 256;
        var px = new Color[W * H];

        var paper = new Color(0.74f, 0.66f, 0.50f);
        var ink = new Color(0.15f, 0.11f, 0.07f);

        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float v = y / (float)H;
            px[y * W + x] = Color.Lerp(paper, ink, Mathf.Lerp(0.34f, 0.04f, Mathf.Pow(v, 1.4f)));
        }

        // Three ranges of hills, each lower and darker than the one behind it.
        for (int band = 0; band < 3; band++)
        {
            float baseline = 0.46f - band * 0.11f;
            float weight = 0.40f + band * 0.22f;

            for (int x = 0; x < W; x++)
            {
                float ridge = baseline
                    + (Fbm(x / (float)W * (1f + band), 0.3f + band, 3 + band, 3, seed + band * 37) - 0.5f) * 0.16f;

                for (int y = 0; y < Mathf.RoundToInt(ridge * H) && y < H; y++)
                    px[y * W + x] = Color.Lerp(px[y * W + x], ink, weight);
            }
        }

        // A tree to one side. Without it three grey bands are a smudge, not a view.
        int trunk = Mathf.RoundToInt((0.18f + Hash(seed, 5, 64, 9) * 0.62f) * W);
        for (int y = 0; y < Mathf.RoundToInt(H * 0.44f); y++)
        for (int d = -2; d <= 2; d++)
            Set(px, W, H, trunk + d, y, ink);

        for (int i = 0; i < 260; i++)
        {
            float a = Hash(seed * 3 + i, 6, 4096, 4) * Mathf.PI * 2f;
            float r = Hash(seed * 3 + i, 7, 4096, 4) * 26f;
            Blot(px, W, H, trunk + Mathf.Cos(a) * r, H * 0.44f + Mathf.Sin(a) * r * 0.8f, 3.4f,
                 new Color(-0.22f, -0.24f, -0.24f));
        }

        // Foxing, and a soft edge: an old print is never clean out to its border.
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            int i = y * W + x;
            float edge = Mathf.Min(Mathf.Min(x, W - 1 - x), Mathf.Min(y, H - 1 - y)) / 18f;
            px[i] = Color.Lerp(paper, px[i], Mathf.Clamp01(edge));
            px[i] *= 1f + (Fbm(x / (float)W, y / (float)H, 10, 3, seed + 5) - 0.5f) * 0.24f;
        }

        return Save(px, W, H, name);
    }

    // What is on the other side of the glass. Not tileable and not meant to be: one plane
    // hangs outside each window, and every one of them shows the same night.
    //
    // The plane is taller than the opening, so only a band across the middle of this is
    // ever seen. Roofline and moon are placed to land inside that band; drawn where they
    // would go on a real horizon they sit above and below the window and never show.
    public static Texture2D NightSky(string name)
    {
        const int W = 512, H = 512;
        var px = new Color[W * H];

        var high = new Color(0.020f, 0.028f, 0.055f);
        var low = new Color(0.075f, 0.085f, 0.125f);

        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float v = y / (float)H;

            // Sky, darkest overhead and lifting towards a town glow near the horizon.
            Color c = Color.Lerp(low, high, Mathf.Pow(v, 0.7f));

            float cloud = Fbm(x / (float)W, v, 3, 4, 5) - 0.5f;
            c *= 1f + cloud * 0.5f;

            px[y * W + x] = c;
        }

        // Stars, thinner low down where the haze would drown them.
        for (int i = 0; i < 620; i++)
        {
            float sx = Hash(i, 1, 4096, 3) * W;
            float sy = Hash(i, 2, 4096, 3) * H;
            if (sy / H < 0.44f || Hash(i, 3, 4096, 3) > Mathf.Lerp(0.15f, 0.95f, sy / H))
                continue;

            float b = 0.22f + Hash(i, 4, 4096, 3) * 0.52f;
            Blot(px, W, H, sx, sy, 0.8f, new Color(b, b, b * 0.94f));
        }

        // A moon, low and to one side so it never sits dead centre in a window.
        const float MoonX = 0.33f, MoonY = 0.61f, MoonR = 27f;
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(MoonX * W, MoonY * H));
            if (d > MoonR * 5f)
                continue;

            int i = y * W + x;

            // Halo first, then the disc over the top of it.
            px[i] += new Color(0.10f, 0.11f, 0.14f) * Mathf.Exp(-d / (MoonR * 1.6f));

            if (d < MoonR)
            {
                float lip = Mathf.SmoothStep(1f, 0f, (d - MoonR + 3f) / 3f);
                float sea = Fbm(x / 40f, y / 40f, 8, 2, 97) - 0.5f;
                px[i] = Color.Lerp(px[i], new Color(0.88f, 0.88f, 0.82f) * (1f + sea * 0.25f), lip);
            }
        }

        // Rooftops along the bottom, so the view has a distance in it.
        for (int x = 0; x < W; x++)
        {
            float ridge = 0.34f + Fbm(x / (float)W, 0.5f, 6, 2, 11) * 0.13f;
            int top = Mathf.RoundToInt(ridge * H);

            for (int y = 0; y < top; y++)
                px[y * W + x] = new Color(0.024f, 0.026f, 0.040f);

            // A lit window every so often, which is what says these are houses and not a
            // hill. They sit near the ridge, where a window is on the far side of a street.
            if (x % 31 < 4 && Hash(x / 31, 9, 64, 2) > 0.5f)
            {
                int wy = top - 14 - Mathf.RoundToInt(Hash(x / 31, 10, 64, 2) * 20f);
                for (int y = wy; y < wy + 7 && y < top; y++)
                    if (y >= 0)
                        px[y * W + x] = new Color(0.42f, 0.33f, 0.16f);
            }
        }

        return Save(px, W, H, name);
    }

    // ---------- interface ----------

    // The crosshair: a centre dot and four ticks with a gap between them, every mark carried
    // on a dark halo. A plain white dot is invisible against plaster and unreadable against
    // a lit dial, and this house is mostly one or the other.
    public static Texture2D Reticle(string name)
    {
        const int S = 64;
        var px = new Color[S * S];
        const float Mid = (S - 1) * 0.5f;

        // Halo first, marks over it, so the dark never eats into the white.
        for (int pass = 0; pass < 2; pass++)
        {
            float grow = pass == 0 ? 1.6f : 0f;
            Color colour = pass == 0 ? new Color(0f, 0f, 0f, 0.62f) : Color.white;

            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = Mathf.Abs(x - Mid), dy = Mathf.Abs(y - Mid);

                bool dot = dx <= 1.4f + grow && dy <= 1.4f + grow;
                bool tickX = dy <= 1.1f + grow && dx >= 7f - grow && dx <= 13f + grow;
                bool tickY = dx <= 1.1f + grow && dy >= 7f - grow && dy <= 13f + grow;

                if (dot || tickX || tickY)
                    px[y * S + x] = colour;
            }
        }

        return Save(px, S, S, name, true);
    }

    // ---------- writing ----------

    private static void Set(Color[] px, int w, int h, int x, int y, Color colour)
    {
        if (x >= 0 && x < w && y >= 0 && y < h)
            px[y * w + x] = colour;
    }

    private static void Blot(Color[] px, int w, int h, float cx, float cy, float radius, Color colour)
    {
        int r = Mathf.CeilToInt(radius) + 1;

        for (int y = Mathf.RoundToInt(cy) - r; y <= cy + r; y++)
        for (int x = Mathf.RoundToInt(cx) - r; x <= cx + r; x++)
        {
            if (x < 0 || x >= w || y < 0 || y >= h)
                continue;

            float fall = 1f - Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / (radius + 0.5f);
            if (fall > 0f)
                px[y * w + x] += colour * fall;
        }
    }

    public static Texture2D Save(Color[] px, int width, int height, string name) =>
        Save(px, width, height, name, false);

    public static Texture2D Save(Color[] px, int width, int height, string name, bool sprite)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.SetPixels(px);
        tex.Apply();

        string relative = BuildKit.TextureFolder + "/" + name + ".png";
        File.WriteAllBytes(Path.Combine(Application.dataPath, "..", relative), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(relative, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(relative);
        importer.wrapMode = sprite ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.anisoLevel = sprite ? 0 : 4;
        importer.mipmapEnabled = !sprite;
        importer.alphaIsTransparency = sprite;
        importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;

        // Sprite mode defaults to Multiple, and a Multiple sprite has no single sprite to
        // load: LoadAssetAtPath<Sprite> hands back null and the crosshair stays a bare box.
        if (sprite)
            importer.spriteImportMode = SpriteImportMode.Single;

        // Anything not a power of two is shrunk to the nearest one on import unless this
        // says otherwise, which silently ruins anything with lettering on it.
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(relative);
    }
}
