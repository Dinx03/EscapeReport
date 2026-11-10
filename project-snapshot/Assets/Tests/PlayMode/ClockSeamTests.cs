using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// The claims the architecture makes, written as things that either pass or fail.
//
// Not one of these opens a scene, and not one of them needs a player. That is the point:
// if a clock could only be tested by walking a character up to it, the clock would not be
// a module — it would be a part of the game that happens to live in its own folder.
public class ClockSeamTests
{
    // A movement gathers its parts in Awake, so everything has to be bolted on before the
    // object wakes. Building it switched off and switching it on last is how a prefab does
    // it too, and it is the only order that works.
    private static GameObject Clock(out ClockMovement movement, params System.Type[] parts)
    {
        var go = new GameObject("clock");
        go.SetActive(false);

        movement = go.AddComponent<ClockMovement>();
        foreach (System.Type part in parts)
            go.AddComponent(part);

        return go;
    }

    private static void Set(object target, string field, object value)
    {
        target.GetType()
              .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
              .SetValue(target, value);
    }

    // ---------- IClockDisplay ----------

    // Anything that can show a time is driven by the movement, and the movement was never
    // told what this class is. Written here in the test file, it did not exist when
    // ClockMovement was compiled.
    private class CountingDisplay : MonoBehaviour, IClockDisplay
    {
        public int Shown;
        public float Last;

        public void Show(float minutes)
        {
            Shown++;
            Last = minutes;
        }
    }

    [UnityTest]
    public IEnumerator A_display_written_after_the_movement_still_gets_the_time()
    {
        GameObject go = Clock(out ClockMovement movement);
        var display = go.AddComponent<CountingDisplay>();
        Set(movement, "startHour", 4);
        Set(movement, "startMinute", 40);
        go.SetActive(true);

        yield return null;

        Assert.Greater(display.Shown, 0, "the movement never called Show");
        Assert.AreEqual(4 * 60 + 40, display.Last, 1f, "it was handed the wrong time");
        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator Two_displays_on_one_movement_agree()
    {
        GameObject go = Clock(out ClockMovement movement);
        var a = go.AddComponent<CountingDisplay>();
        var b = go.AddComponent<CountingDisplay>();
        go.SetActive(true);

        yield return null;

        Assert.AreEqual(a.Last, b.Last, "two displays on one movement disagreed");
        Object.DestroyImmediate(go);
    }

    // ---------- IClockPart ----------

    // Absence cannot veto. A movement with nothing bolted on has nobody to say no, so it
    // runs forever — which is right for a dial screwed to a wall.
    [UnityTest]
    public IEnumerator A_movement_with_no_parts_runs_forever()
    {
        GameObject go = Clock(out ClockMovement movement);
        go.SetActive(true);

        yield return null;

        Assert.IsTrue(movement.IsRunning);
        Object.DestroyImmediate(go);
    }

    // One dead part stops the clock, however healthy everything else is.
    [UnityTest]
    public IEnumerator An_empty_power_store_stops_it()
    {
        GameObject go = Clock(out ClockMovement movement, typeof(Mainspring));
        Set(movement, "condition", ClockCondition.Stopped);
        go.SetActive(true);

        yield return null;

        Assert.IsFalse(movement.IsRunning, "a clock found stopped should have nothing left in it");
        Object.DestroyImmediate(go);
    }

    // ...and filling it starts the clock, without the movement knowing what filled it.
    [UnityTest]
    public IEnumerator Filling_the_store_starts_it()
    {
        GameObject go = Clock(out ClockMovement movement, typeof(Mainspring));
        Set(movement, "condition", ClockCondition.Stopped);
        go.SetActive(true);

        yield return null;
        Assert.IsFalse(movement.IsRunning);

        go.GetComponent<IPowerStore>().Fill();

        yield return null;
        Assert.IsTrue(movement.IsRunning, "filling the store should have started it");
        Object.DestroyImmediate(go);
    }

    // A part that is switched off is one the player has not fitted yet, and it still has to
    // be in the list so it can say no. This is the whole of act four.
    [UnityTest]
    public IEnumerator An_unfitted_part_still_vetoes()
    {
        GameObject go = Clock(out ClockMovement movement);

        // No wiring: the pendulum finds the movement above it. Bolting it on is the whole
        // of fitting one, which is what lets a recipe list it as a prefab.
        var pendulum = go.AddComponent<Pendulum>();
        pendulum.enabled = false;
        go.SetActive(true);

        yield return null;
        Assert.IsFalse(movement.IsRunning, "an unfitted pendulum should stop the clock");

        pendulum.enabled = true;

        yield return null;
        Assert.IsTrue(movement.IsRunning, "fitting it should start the clock");
        Object.DestroyImmediate(go);
    }
}
