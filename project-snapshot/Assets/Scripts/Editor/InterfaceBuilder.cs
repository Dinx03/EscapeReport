using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// The title card, built from data the way the rooms are.
//
// The rest of the HUD was placed by hand and is left alone; this only ever replaces
// `HUD/TitlePanel`. Fonts, colours and sizes are read off the panel that already exists, so
// a new screen matches the old one without a palette being written down twice.
public static class InterfaceBuilder
{
    private const string HudRoot = "HUD";

    private static readonly Color Ground = new Color32(5, 8, 10, 240);
    private static readonly Color Ink = new Color32(255, 255, 255, 255);
    private static readonly Color Quiet = new Color32(178, 178, 191, 255);
    private static readonly Color Faint = new Color32(150, 148, 140, 255);
    private static readonly Color Button = new Color32(235, 219, 178, 235);
    private static readonly Color OnButton = new Color32(36, 31, 26, 255);

    // Two things the HUD was missing rather than getting wrong.
    //
    // The crosshair was a seven-pixel white square with no sprite on it, which on a 1080p
    // screen is a dead pixel; and every line of text was flat white with nothing behind it,
    // so a prompt over pale plaster — most of this house — read as nothing at all.
    [MenuItem("Tools/Escape Room/House/Restyle HUD")]
    public static void RestyleHud()
    {
        Transform hud = GameObject.Find(HudRoot).transform;

        Texture2D drawn = TextureKit.Reticle("Reticle");
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(drawn));

        var crosshair = hud.Find("Crosshair").GetComponent<Image>();
        Undo.RecordObject(crosshair, "restyle hud");
        crosshair.sprite = sprite;
        ((RectTransform)crosshair.transform).sizeDelta = new Vector2(30f, 30f);

        foreach (string line in new[] { "Prompt", "Message" })
        {
            GameObject label = hud.Find(line).gameObject;
            var shadow = label.GetComponent<Outline>() ?? Undo.AddComponent<Outline>(label);
            Undo.RecordObject(shadow, "restyle hud");
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.6f, -1.6f);
        }

        // The end screen still called itself a demo, and its subtitle sat close enough to
        // the buttons to read as a label on them.
        Transform win = hud.Find("WinPanel");
        Retitle(win.Find("WinText"), "หนีออกมาได้แล้ว", new Vector2(0f, 60f));
        Retitle(win.Find("WinSub"), "เข็มทุกเรือนในบ้านหลังนี้ชี้ 11:57 — เวลาที่เขาหยุดมันเอาไว้", new Vector2(0f, -20f));
        Reposition(win.Find("RestartButton"), new Vector2(-150f, -140f));
        Reposition(win.Find("QuitButton"), new Vector2(150f, -140f));

        Debug.Log("HUD restyled.");
    }

    private static void Retitle(Transform label, string text, Vector2 at)
    {
        var caption = label.GetComponent<Text>();
        Undo.RecordObject(caption, "restyle hud");
        caption.text = text;
        Reposition(label, at);
    }

    private static void Reposition(Transform element, Vector2 at)
    {
        var rect = (RectTransform)element;
        Undo.RecordObject(rect, "restyle hud");
        rect.anchoredPosition = at;
    }

    [MenuItem("Tools/Escape Room/House/Build Title Card")]
    public static void BuildTitleCard()
    {
        Transform hud = GameObject.Find(HudRoot).transform;
        Font face = hud.Find("Prompt").GetComponent<Text>().font;

        Transform old = hud.Find("TitlePanel");
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);

        GameObject panel = Panel("TitlePanel", hud);

        Label(panel.transform, "Title", "เรือนนาฬิกา", face, 76, Ink, new Vector2(0f, 210f), new Vector2(1400f, 110f));
        Label(panel.transform, "Rule", "— — —", face, 28, Faint, new Vector2(0f, 148f), new Vector2(400f, 40f));

        Label(panel.transform, "Premise",
              "บ้านของช่างนาฬิกาที่หมกมุ่นกับเวลา\nประตูทุกบานล็อกไว้ และคำตอบซ่อนอยู่บนหน้าปัด",
              face, 28, Quiet, new Vector2(0f, 70f), new Vector2(1200f, 96f));

        Label(panel.transform, "Controls",
              "W A S D  เดิน            F  ยกขึ้นดู / เพ่งดู            E  ใช้ · หยิบ · ไขลาน            Q  ถอยออก",
              face, 24, Faint, new Vector2(0f, -40f), new Vector2(1500f, 40f));

        Label(panel.transform, "Aside",
              "นาฬิกาที่ยังเดินอยู่เท่านั้นที่บอกความจริง",
              face, 22, new Color32(120, 118, 112, 255), new Vector2(0f, -84f), new Vector2(1200f, 36f));

        GameObject start = Push(panel.transform, "StartButton", "เริ่มเล่น", face, new Vector2(0f, -180f));

        var screen = hud.GetComponent<TitleScreen>() ?? Undo.AddComponent<TitleScreen>(hud.gameObject);
        var so = new SerializedObject(screen);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.FindProperty("modes").objectReferenceValue =
            GameObject.Find("Player").GetComponent<PlayerModeController>();
        so.ApplyModifiedPropertiesWithoutUndo();

        // The button calls straight into the screen, the way the win panel's two do.
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
            start.GetComponent<Button>().onClick, screen.Begin);

        // Frozen until Begin says otherwise. Without this the player can walk off while the
        // card is still up, and the first thing the game does is contradict itself.
        var modes = GameObject.Find("Player").GetComponent<PlayerModeController>();
        var mso = new SerializedObject(modes);
        mso.FindProperty("startingMode").enumValueIndex = (int)PlayerMode.Frozen;
        mso.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeObject = panel;
        Debug.Log("Title card rebuilt.");
    }

    private static GameObject Panel(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "title card");
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        go.GetComponent<Image>().color = Ground;
        return go;
    }

    private static void Label(Transform parent, string name, string text, Font face, int size,
                              Color colour, Vector2 at, Vector2 box)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = at;
        rect.sizeDelta = box;

        var label = go.GetComponent<Text>();
        label.text = text;
        label.font = face;
        label.fontSize = size;
        label.color = colour;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.lineSpacing = 1.25f;
    }

    private static GameObject Push(Transform parent, string name, string text, Font face, Vector2 at)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = at;
        rect.sizeDelta = new Vector2(280f, 68f);

        go.GetComponent<Image>().color = Button;

        var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        label.transform.SetParent(go.transform, false);
        var lrect = (RectTransform)label.transform;
        lrect.anchorMin = Vector2.zero;
        lrect.anchorMax = Vector2.one;
        lrect.offsetMin = Vector2.zero;
        lrect.offsetMax = Vector2.zero;

        var caption = label.GetComponent<Text>();
        caption.text = text;
        caption.font = face;
        caption.fontSize = 30;
        caption.color = OnButton;
        caption.alignment = TextAnchor.MiddleCenter;

        return go;
    }
}
