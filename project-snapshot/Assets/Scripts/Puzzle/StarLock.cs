using UnityEngine;

// Holds an Openable shut until the light on the wall makes the shape it is supposed to make.
//
// Sits alongside KeyLock, CodeLock, TimeLock and RunningLock and asks nothing new of
// Openable: five kinds of catch now, and the door has never been told what any of them are.
[DisallowMultipleComponent]
public class StarLock : MonoBehaviour, ILock
{
    [SerializeField] private Horologium rig;

    [SerializeField] private string lockedPrompt = "[E] ประตูไม่ขยับ — กลไกยังไม่ทำงาน";

    [SerializeField]
    private string refusedMessage = "บ้านยังไม่ปล่อย แสงบนผนังยังไม่เข้าที่";

    public bool IsLocked => !rig.Solved;

    public string LockedPrompt => lockedPrompt;

    public void RefuseOpening(IActor actor)
    {
        actor.Say(refusedMessage);
    }
}
