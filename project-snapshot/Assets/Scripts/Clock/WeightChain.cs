using UnityEngine;

// The chains under a cuckoo clock. Pulling them hauls the weights back up, which is how a
// clock like this is wound — there is no key and no crank anywhere on it.
//
// The weights then hang at whatever height the store's charge puts them, so the thing
// that powers the clock is also the gauge that shows how much power is left. A clock nobody
// has touched in years is one with its weights on the floor.
[DisallowMultipleComponent]
public class WeightChain : MonoBehaviour, IInteractable
{
    [SerializeField] private ClockMovement movement;

    [SerializeField]
    [Tooltip("The weights themselves. They ride up and down with the charge.")]
    private Transform[] weights;

    [SerializeField, Min(0.01f)]
    [Tooltip("How far a fully wound weight sits above a spent one.")]
    private float travel = 0.16f;

    [SerializeField] private string prompt = "[E] ดึงโซ่ถ่วง";
    [SerializeField] private string startedMessage = "นาฬิกาเริ่มเดินแล้ว";
    [SerializeField] private string deadMessage = "ตุ้มขึ้นแล้ว แต่เข็มไม่ขยับ";
    [SerializeField] private string toppedUpMessage = "ดึงตุ้มขึ้นอีกแล้ว";

    private IPowerStore _power;
    private Vector3[] _spent;

    public string Prompt => prompt;

    private void Awake()
    {
        // Found, not wired, the same way the crank finds it. Pulling a chain and turning a
        // handle are two ways of doing one thing, and neither knows what it is filling.
        _power = movement.GetComponentInChildren<IPowerStore>(true);

        _spent = new Vector3[weights.Length];
        for (int i = 0; i < weights.Length; i++)
            _spent[i] = weights[i].localPosition;
    }

    private void Update()
    {
        float charge = _power.Charge;

        for (int i = 0; i < weights.Length; i++)
            weights[i].localPosition = _spent[i] + Vector3.up * (travel * charge);
    }

    public void Interact(IActor actor)
    {
        bool wasStopped = !movement.IsRunning;

        _power.Fill();

        // A broken movement takes the pull and does nothing with it, and the weights go up
        // all the same. That is the point: hauling the chains never tells you whether the
        // clock is any good, only watching it does.
        actor.Say(!wasStopped ? toppedUpMessage
            : movement.IsRunning ? startedMessage
            : deadMessage);
    }
}
