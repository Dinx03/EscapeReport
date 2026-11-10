using UnityEditor;
using UnityEngine;

// The blocks every prop builder is made of.
//
// There are three builders now and they were all growing their own copy of "put a box
// here". This is that copy, once. Nothing in here knows anything about clocks.
public static class BuildKit
{
    public const string TextureFolder = "Assets/Textures";
    public const string MaterialFolder = "Assets/Materials";

    // ---------- primitives ----------

    public static Transform Group(string name, Transform parent, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    // A hand that turns about its own end rather than its middle, so the blade sweeps out
    // from the centre of a dial the way a hand does.
    public static Transform HandPivot(string name, Transform parent, float z, Vector3 size, Material material)
    {
        var pivot = new GameObject(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = new Vector3(0f, 0f, z);
        Box("Blade", pivot.transform, new Vector3(0f, size.y * 0.5f, 0f), size, material);
        return pivot.transform;
    }

    public static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Material material)
    {
        GameObject go = Primitive(PrimitiveType.Cube, name, parent, localPos, material);
        go.transform.localScale = size;
        return go;
    }

    public static GameObject Disc(string name, Transform parent, Vector3 localPos, float diameter, float depth, Material material)
    {
        GameObject go = Primitive(PrimitiveType.Cylinder, name, parent, localPos, material);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // stand the cylinder on its Z axis
        go.transform.localScale = new Vector3(diameter, depth * 0.5f, diameter);
        return go;
    }

    public static GameObject Ellipse(string name, Transform parent, Vector3 localPos, float width, float height, float depth, Material material)
    {
        GameObject go = Primitive(PrimitiveType.Cylinder, name, parent, localPos, material);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = new Vector3(width, depth * 0.5f, height);
        return go;
    }

    public static GameObject Cylinder(string name, Transform parent, Vector3 localPos, float diameter, float height, Material material)
    {
        GameObject go = Primitive(PrimitiveType.Cylinder, name, parent, localPos, material);
        go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
        return go;
    }

    public static GameObject Sphere(string name, Transform parent, Vector3 localPos, float diameter, Material material)
    {
        GameObject go = Primitive(PrimitiveType.Sphere, name, parent, localPos, material);
        go.transform.localScale = Vector3.one * diameter;
        return go;
    }

    public static GameObject Quad(string name, Transform parent, Vector3 localPos, float size, Material material)
    {
        GameObject go = Primitive(PrimitiveType.Quad, name, parent, localPos, material);
        // The built-in quad faces -Z, which leaves it invisible sunk in a recess. Half a
        // turn is a rotation, not a mirror, so anything printed on it still reads.
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        go.transform.localScale = Vector3.one * size;
        return go;
    }

    // Ten short bars round a circle. Cheaper than a real torus and reads the same at the
    // size a key or a watch bow is actually seen.
    public static Transform Ring(string name, Transform parent, Vector3 localPos, float diameter, float thickness, Material material)
    {
        Transform hub = Group(name, parent, localPos);

        const int Segments = 10;
        float radius = diameter * 0.5f;
        float side = Mathf.Tan(Mathf.PI / Segments) * radius * 2f + thickness * 0.5f;

        for (int i = 0; i < Segments; i++)
        {
            float a = i / (float)Segments * Mathf.PI * 2f;
            GameObject bar = Box("Seg" + i, hub,
                new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f),
                new Vector3(thickness, side, thickness), material);
            bar.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
        }

        return hub;
    }

    // Colliders come off every primitive. Anything that should stop the player or catch the
    // crosshair says so out loud, with a collider sized for the job rather than for the art.
    public static BoxCollider Hull(GameObject target, Vector3 center, Vector3 size)
    {
        var box = target.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
        return box;
    }

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    // A base map scaled to the world rather than to the box. Every wall and floor in the
    // house is a stretched cube, and a cube UVs each face 0..1, so one tile would smear
    // across a six-metre run. The scale rides on the renderer, not the material, because
    // the same plaster has to serve a doorpost and a whole wall.
    public static void Tile(GameObject target, float metresPerTile)
    {
        Vector3 s = target.transform.lossyScale;

        // Which pair of axes the big face is UV-ed by follows which way the box is thin.
        Vector2 face =
            s.y <= s.x && s.y <= s.z ? new Vector2(s.x, s.z) :
            s.x <= s.z ? new Vector2(s.z, s.y) : new Vector2(s.x, s.y);

        // The offset comes from where the box is in the world, not from its own corner, so
        // two slabs meeting in a wall carry on the same run of plaster instead of each
        // starting the texture again and drawing a seam down the join.
        Vector3 p = target.transform.position;
        Vector2 corner =
            s.y <= s.x && s.y <= s.z ? new Vector2(p.x - s.x * 0.5f, p.z - s.z * 0.5f) :
            s.x <= s.z ? new Vector2(p.z - s.z * 0.5f, p.y - s.y * 0.5f)
                       : new Vector2(p.x - s.x * 0.5f, p.y - s.y * 0.5f);

        var block = new MaterialPropertyBlock();
        var renderer = target.GetComponent<Renderer>();
        renderer.GetPropertyBlock(block);
        block.SetVector("_BaseMap_ST", new Vector4(
            face.x / metresPerTile, face.y / metresPerTile,
            corner.x / metresPerTile, corner.y / metresPerTile));
        renderer.SetPropertyBlock(block);
    }

    // ---------- materials ----------

    public static Material Load(string name) =>
        AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + name + ".mat");

    public static Material Opaque(string name, Color colour, float smoothness, float metallic, Texture baseMap)
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

    public static Material Glass(string name, Color colour)
    {
        Material mat = Opaque(name, colour, 0.95f, 0f, null);

        // URP wants the surface mode, the blend factors and the keyword set together. Any
        // one of them on its own leaves the glass stubbornly opaque.
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------- assets ----------

    // Sets a private [SerializeField] the way the Inspector would, since a builder has no
    // other way in.
    public static void Wire(Object target, string property, object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(property);

        if (value is float f) p.floatValue = f;
        else if (value is int i) p.intValue = i;
        else if (value is bool b) p.boolValue = b;
        else if (value is string s) p.stringValue = s;
        else if (value is Vector3 v) p.vector3Value = v;
        else if (value is System.Array a) Fill(p, a);
        else p.objectReferenceValue = (Object)value;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Serialized arrays, so a builder can hand over "these two weights" in one line.
    private static void Fill(SerializedProperty list, System.Array values)
    {
        list.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = (Object)values.GetValue(i);
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        int slash = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
