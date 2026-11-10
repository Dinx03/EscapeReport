using UnityEngine;

// The combination, read off the props rather than typed into the lock.
//
// Four ordinary things stood in a row on the chest in an old photograph, and each has a
// number on the back of it. What opens the wardrobe is those numbers in the order the
// photograph puts them — never the order they happen to be lying about the room in, which
// is the whole of the puzzle.
//
// The answer is therefore written down nowhere: change a digit on a prop, or the order of
// this list, and the lock follows. A combination typed in beside it would be a second place
// for it to be wrong, and the player would be the one to find out.
[DisallowMultipleComponent]
[RequireComponent(typeof(CodeLock))]
public class StampedCombination : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The numbered things, left to right as the photograph stands them on the table.")]
    private StampedNumber[] asPictured;

    private void Awake()
    {
        var digits = new int[asPictured.Length];
        for (int i = 0; i < digits.Length; i++)
            digits[i] = asPictured[i].Digit;

        GetComponent<CodeLock>().SetCombination(digits);
    }
}
