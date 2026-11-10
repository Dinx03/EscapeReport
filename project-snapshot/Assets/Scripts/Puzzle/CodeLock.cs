using UnityEngine;

// Holds an Openable shut until its inputs read the right number.
//
// There is no submit button to hunt for: it checks on every change, so setting the last
// digit is the confirmation, and the catch springing open is the answer.
//
// It reads `ICodeInput`, so what the player actually turns is not its business: the wheels
// in this house step a printed strip round behind a window, and a keypad or a row of levers
// would drop in beside them without a line of this changing.
[DisallowMultipleComponent]
[RequireComponent(typeof(Openable))]
public class CodeLock : MonoBehaviour, ILock
{
    [SerializeField]
    [Tooltip("Left to right, as the player reads them. Anything implementing ICodeInput.")]
    private CodeWheel[] wheels;

    [SerializeField]
    [Tooltip("One digit per wheel, in the same order.")]
    private int[] combination = { 1, 4, 7, 9 };

    [SerializeField]
    private string lockedPrompt = "[E] ล็อกอยู่ — ตั้งตัวเลขให้ถูก";

    [SerializeField]
    private string refusedMessage = "ล็อกอยู่ ต้องตั้งตัวเลขบนหน้าปัดให้ถูกก่อน";

    private Openable _openable;
    private bool _sprung;

    public bool IsLocked => !_sprung;
    public string LockedPrompt => lockedPrompt;

    private void Awake()
    {
        _openable = GetComponent<Openable>();
    }

    private void OnEnable()
    {
        foreach (CodeWheel wheel in wheels)
            wheel.Changed += CheckWheels;
    }

    private void OnDisable()
    {
        foreach (CodeWheel wheel in wheels)
            wheel.Changed -= CheckWheels;
    }

    public void RefuseOpening(IActor actor)
    {
        actor.Say(refusedMessage);
    }

    // States what it wants, for a puzzle whose answer is not written down anywhere but read
    // off the props the player finds. Same shape as Openable.SetOpen: the field above is the
    // authored number, and this is how something else says one instead.
    public void SetCombination(int[] digits)
    {
        combination = digits;
    }

    private void CheckWheels()
    {
        for (int i = 0; i < wheels.Length; i++)
        {
            ICodeInput input = wheels[i];
            if (input.Digit != combination[i])
                return;
        }

        _sprung = true;
        _openable.SetOpen(true);
    }
}
