using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// A clock assembled from data rather than from code, and then worked like any other.
//
// `ClockAssembler` lives in the editor assembly, so these tests do not build the prefab —
// they load the one it built and check that the thing which came out of a data file behaves
// exactly like the eight kinds that came out of C# methods. If it does, then "a new kind of
// clock is a new asset" is true rather than merely claimed.
public class RecipeSeamTests
{
    private const string Assembled = "Assets/Prefabs/Clocks/PlankClock.prefab";

    private static GameObject Load()
    {
        var prefab = Resources.Load<GameObject>("PlankClock");

#if UNITY_EDITOR
        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(Assembled);
#endif
        return prefab;
    }

    [UnityTest]
    public IEnumerator A_clock_built_from_a_recipe_keeps_time_like_any_other()
    {
        GameObject prefab = Load();
        Assert.IsNotNull(prefab, "build it with Tools > Escape Room > Clocks > Build Clock From Recipe");

        GameObject clock = Object.Instantiate(prefab);
        yield return null;

        var movement = clock.GetComponent<ClockMovement>();
        Assert.IsNotNull(movement, "the assembler adds the movement; nothing else does");

        // The recipe said 3:25, and said the clock was found stopped.
        Assert.AreEqual(3, movement.Hour);
        Assert.AreEqual(25, movement.Minute);
        Assert.IsFalse(movement.IsRunning, "the recipe said Stopped");

        Object.DestroyImmediate(clock);
    }

    // The parts the recipe listed are real parts, with real vetoes — the movement found
    // them by interface, having been handed a list of prefabs and nothing else.
    [UnityTest]
    public IEnumerator The_parts_a_recipe_lists_behave_like_parts()
    {
        GameObject clock = Object.Instantiate(Load());
        yield return null;

        var movement = clock.GetComponent<ClockMovement>();
        clock.GetComponentInChildren<IPowerStore>(true).Fill();

        yield return null;
        Assert.IsTrue(movement.IsRunning, "filling the store the recipe listed should start it");

        clock.GetComponentInChildren<Pendulum>(true).enabled = false;
        Assert.IsFalse(movement.IsRunning, "the pendulum the recipe listed should be able to veto");

        Object.DestroyImmediate(clock);
    }

    // And the display it listed is being driven, without the movement or the dial having
    // been told the other exists.
    [UnityTest]
    public IEnumerator The_display_a_recipe_lists_is_driven()
    {
        GameObject clock = Object.Instantiate(Load());
        yield return null;

        var dial = clock.GetComponentInChildren<ClockDial>(true);
        Assert.IsNotNull(dial, "the recipe listed a dial");

        Transform hand = (Transform)typeof(ClockDial)
            .GetField("hourHand", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(dial);

        // 3:25 is a quarter past three on the dial, so the hour hand is round past the 3.
        float turned = hand.localEulerAngles.z;
        Assert.Greater(turned, 80f, "the hour hand was never moved to the recipe's time");
        Assert.Less(turned, 130f);

        Object.DestroyImmediate(clock);
    }
}
