using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// The proof that `IActor` did what it was for.
//
// Every prop in the house used to take a `PlayerInteractor`. These tests work the same
// props with a plain C# object that is not a MonoBehaviour, is not in the scene, and has
// never heard of a player — which was impossible before, and is the whole reason the
// clock and puzzle modules can now compile without one.
public class ActorSeamTests
{
    // Twelve lines, and it can operate anything in the game. That is the size of the
    // contract a prop actually needs.
    private class RecordingActor : IActor
    {
        public readonly List<string> Said = new List<string>();
        public readonly List<IItemReceiver> BagOfferedTo = new List<IItemReceiver>();
        public readonly List<ItemData> Taken = new List<ItemData>();

        public void Say(string message) => Said.Add(message);
        public void OfferBag(IItemReceiver to) => BagOfferedTo.Add(to);
        public void Take(ItemData item) => Taken.Add(item);
    }

    private static void Set(object target, string field, object value)
    {
        target.GetType()
              .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
              .SetValue(target, value);
    }

    [UnityTest]
    public IEnumerator A_locked_cabinet_offers_the_bag_to_whoever_works_it()
    {
        var go = new GameObject("cabinet");
        go.SetActive(false);
        var openable = go.AddComponent<Openable>();
        var keyLock = go.AddComponent<KeyLock>();
        Set(keyLock, "requiredKey", ScriptableObject.CreateInstance<ItemData>());
        go.SetActive(true);

        yield return null;

        var actor = new RecordingActor();
        openable.Interact(actor);

        Assert.AreEqual(1, actor.BagOfferedTo.Count, "a locked thing should offer the bag");
        Assert.AreSame(keyLock, actor.BagOfferedTo[0]);
        Assert.IsFalse(openable.IsOpen, "it should not have opened for the wrong actor");
        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator The_right_key_opens_it_and_a_wrong_one_does_not()
    {
        var wanted = ScriptableObject.CreateInstance<ItemData>();
        var wrong = ScriptableObject.CreateInstance<ItemData>();

        var go = new GameObject("cabinet");
        go.SetActive(false);
        var openable = go.AddComponent<Openable>();
        var keyLock = go.AddComponent<KeyLock>();
        Set(keyLock, "requiredKey", wanted);
        go.SetActive(true);

        yield return null;

        Assert.IsFalse(keyLock.TryUseItem(wrong), "the wrong key should be refused");
        Assert.IsTrue(((ILock)keyLock).IsLocked);

        Assert.IsTrue(keyLock.TryUseItem(wanted), "the right key should turn it");
        Assert.IsFalse(((ILock)keyLock).IsLocked);

        openable.Interact(new RecordingActor());
        Assert.IsTrue(openable.IsOpen, "it should open once the lock is satisfied");
        Object.DestroyImmediate(go);
    }

    // Two locks of different kinds on one thing, and it stays shut until both are satisfied.
    // No class was ever written for "needs a key and a running clock" — the panel simply has
    // both components, and `Openable` asks each of them in turn without knowing what either
    // one is. This is act four in miniature, and it crosses three modules.
    //
    // (Two locks of the *same* kind is not a case the game has: `KeyLock` is
    // `[DisallowMultipleComponent]`, so Unity refuses the second one.)
    [UnityTest]
    public IEnumerator A_key_and_a_running_clock_both_have_to_agree()
    {
        var key = ScriptableObject.CreateInstance<ItemData>();

        // A clock found stopped: its store is primed empty, so it is not running.
        var clockGo = new GameObject("clock");
        clockGo.SetActive(false);
        var movement = clockGo.AddComponent<ClockMovement>();
        clockGo.AddComponent<Mainspring>();
        Set(movement, "condition", ClockCondition.Stopped);
        clockGo.SetActive(true);

        var panel = new GameObject("panel");
        panel.SetActive(false);
        var openable = panel.AddComponent<Openable>();
        var keyLock = panel.AddComponent<KeyLock>();
        var runningLock = panel.AddComponent<RunningLock>();
        Set(keyLock, "requiredKey", key);
        Set(runningLock, "movement", movement);
        panel.SetActive(true);

        yield return null;

        var actor = new RecordingActor();
        openable.Interact(actor);
        Assert.IsFalse(openable.IsOpen, "neither lock is satisfied yet");

        keyLock.TryUseItem(key);
        openable.Interact(actor);
        Assert.IsFalse(openable.IsOpen, "the key alone is not enough while the clock is dead");

        clockGo.GetComponent<IPowerStore>().Fill();
        yield return null;

        openable.Interact(actor);
        Assert.IsTrue(openable.IsOpen, "a turned key and a running clock should open it");

        Object.DestroyImmediate(panel);
        Object.DestroyImmediate(clockGo);
    }

    // The sixth kind of catch, and the only one that is never put to the player at all.
    //
    // Every other lock is a question — a key, a code, a time, a pattern of stars — and the
    // actor answers it. No actor can answer this one. What opens it is `SetOpen` from the
    // machinery beside it, which is the same call every lock makes when its own catch
    // springs; nothing new was asked of `Openable` to get a thing that opens itself.
    [UnityTest]
    public IEnumerator A_sealed_catch_opens_for_no_one_and_stands_down_once_it_has()
    {
        var panel = new GameObject("panel");
        panel.SetActive(false);
        var openable = panel.AddComponent<Openable>();
        panel.AddComponent<SealedLock>();
        panel.SetActive(true);

        yield return null;

        var actor = new RecordingActor();
        openable.Interact(actor);
        Assert.IsFalse(openable.IsOpen, "nothing an actor presses may open a sealed thing");
        Assert.AreEqual(1, actor.Said.Count, "and it should say so rather than sit there mute");

        // The machinery, from the far side.
        openable.SetOpen(true);
        Assert.IsTrue(openable.IsOpen);

        openable.Interact(actor);
        Assert.IsFalse(openable.IsOpen, "once open it is a door like any other, and shuts");

        openable.Interact(actor);
        Assert.IsTrue(openable.IsOpen, "and opens again — nobody is sealed in behind it");

        Object.DestroyImmediate(panel);
    }

    // Act four's ending, with no scene and no player: fit the last part, the clock starts,
    // it rings, and the wall lets go when the ringing is done — not before, and not because
    // anyone pressed anything.
    [UnityTest]
    public IEnumerator The_wall_lets_go_when_the_ringing_is_done_and_not_before()
    {
        var clockGo = new GameObject("clock");
        clockGo.SetActive(false);
        var movement = clockGo.AddComponent<ClockMovement>();
        clockGo.AddComponent<Mainspring>();
        Set(movement, "condition", ClockCondition.Stopped);
        clockGo.SetActive(true);

        var panel = new GameObject("panel");
        panel.SetActive(false);
        var openable = panel.AddComponent<Openable>();
        panel.AddComponent<SealedLock>();

        // A test scene has no camera in it and so no ear either, and Unity says so once per
        // frame that something is playing. The sound is not what is under test; the silence
        // in the console is worth the two lines.
        var ear = new GameObject("ear", typeof(AudioListener));

        var bell = panel.AddComponent<AudioSource>();
        bell.clip = AudioClip.Create("bell", 4410, 1, 44100, false);
        bell.playOnAwake = false;

        var release = panel.AddComponent<ChimeRelease>();
        Set(release, "movement", movement);
        Set(release, "bell", bell);
        Set(release, "ringSeconds", 0.25f);
        panel.SetActive(true);

        yield return null;

        var actor = new RecordingActor();
        openable.Interact(actor);
        Assert.IsFalse(openable.IsOpen, "a dead clock opens nothing, and no press does either");

        clockGo.GetComponent<IPowerStore>().Fill();
        yield return null;
        Assert.IsFalse(openable.IsOpen, "and it does not go the instant the clock comes alive");

        yield return new WaitForSeconds(0.4f);
        Assert.IsTrue(openable.IsOpen, "it goes once the ringing has run its length");

        Object.DestroyImmediate(panel);
        Object.DestroyImmediate(clockGo);
        Object.DestroyImmediate(ear);
    }

    // A clock part socket is worked the same way, from the same interface — and this one
    // lives in the Clock module, which no longer knows a player exists.
    [UnityTest]
    public IEnumerator A_clock_part_socket_is_worked_by_the_same_contract()
    {
        var part = ScriptableObject.CreateInstance<ItemData>();

        var go = new GameObject("socket");
        go.SetActive(false);
        var socket = go.AddComponent<ClockPartSocket>();
        Set(socket, "part", part);
        Set(socket, "fitted", new GameObject[0]);
        go.SetActive(true);

        yield return null;

        var actor = new RecordingActor();
        socket.Interact(actor);
        Assert.AreEqual(1, actor.BagOfferedTo.Count, "an empty socket should offer the bag");

        Assert.IsTrue(socket.TryUseItem(part));
        Assert.IsTrue(socket.IsFitted);
        Object.DestroyImmediate(go);
    }
}
