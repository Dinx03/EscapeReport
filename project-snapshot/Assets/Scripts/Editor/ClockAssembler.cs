using UnityEditor;
using UnityEngine;

// Turns a `ClockRecipe` into a clock prefab.
//
// This is the piece that makes the project's own claim operable. Everything else says the
// kind of a clock is the sum of its parts; until this existed, adding a kind still meant
// writing a ninety-line method in `ClockVariants`. Now it means making an asset.
//
// It builds nothing itself. The case is a prefab, every fitting is a prefab, and the only
// component this adds is the movement — which then finds its own displays and parts by
// looking at what got bolted on. A recipe listing a kind of display nobody has written yet
// will work the day someone writes it, without this file changing.
public static class ClockAssembler
{
    private const string Folder = "Assets/Prefabs/Clocks";

    [MenuItem("Tools/Escape Room/Clocks/Build Clock From Recipe %#b")]
    public static void BuildSelected()
    {
        var recipe = Selection.activeObject as ClockRecipe;

        if (recipe == null)
        {
            Debug.LogWarning("Select a ClockRecipe asset first.");
            return;
        }

        GameObject built = Build(recipe);
        Debug.Log("Built " + built.name + " from recipe: "
                  + recipe.Fittings.Length + " fittings, "
                  + built.GetComponentsInChildren<Transform>(true).Length + " objects.");
        Selection.activeObject = built;
    }

    public static GameObject Build(ClockRecipe recipe)
    {
        GameObject clock = (GameObject)PrefabUtility.InstantiatePrefab(recipe.CaseModel);
        clock.name = recipe.name;

        // The one component the assembler adds. Everything else arrives as a fitting.
        var movement = clock.AddComponent<ClockMovement>();
        BuildKit.Wire(movement, "condition", (int)recipe.Condition);
        BuildKit.Wire(movement, "startHour", recipe.StartHour);
        BuildKit.Wire(movement, "startMinute", recipe.StartMinute);
        BuildKit.Wire(movement, "minutesPerSecond", recipe.MinutesPerSecond);

        foreach (ClockRecipe.Fitting fitting in recipe.Fittings)
        {
            var fitted = (GameObject)PrefabUtility.InstantiatePrefab(fitting.Prefab, clock.transform);
            fitted.transform.localPosition = fitting.At;
            fitted.transform.localRotation = Quaternion.Euler(fitting.Turn);
            fitted.transform.localScale = Vector3.one * Mathf.Max(fitting.Scale, 0.01f);
        }

        BuildKit.EnsureFolder(Folder);
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(clock, Folder + "/" + recipe.name + ".prefab");
        Object.DestroyImmediate(clock);
        return asset;
    }

    // The two parts that had no prefab of their own, because every clock built them inline.
    // A recipe can only list things that exist as assets, so they have to become assets —
    // which is an improvement on its own terms: one pendulum, used by every clock that has
    // one, the way `ClockDial.prefab` already works.
    [MenuItem("Tools/Escape Room/Clocks/Build Part Prefabs")]
    public static void BuildParts()
    {
        BuildKit.EnsureFolder(Folder + "/Parts");

        // A store keeps no shape: it is the wound reserve, not a thing you can point at.
        var spring = new GameObject("Mainspring");
        spring.AddComponent<Mainspring>();
        Save(spring, "Mainspring");

        // A pendulum does have a shape, and it swings about its own top, so the rod hangs
        // downwards from the origin and the prefab can be dropped straight onto a suspension
        // point without an offset.
        Material brass = BuildKit.Load("ClockBrass");
        var swing = new GameObject("Pendulum");
        BuildKit.Box("Rod", swing.transform, new Vector3(0f, -0.075f, 0f), new Vector3(0.005f, 0.150f, 0.005f), brass);
        BuildKit.Disc("Bob", swing.transform, new Vector3(0f, -0.165f, 0f), 0.055f, 0.006f, brass);
        swing.AddComponent<Pendulum>();
        Save(swing, "Pendulum");

        Debug.Log("Part prefabs rebuilt.");
    }

    private static void Save(GameObject built, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(built, Folder + "/Parts/" + name + ".prefab");
        Object.DestroyImmediate(built);
    }
}
