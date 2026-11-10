using UnityEngine;

// A needle that keeps pointing the same way whatever is carrying it.
//
// It is given a bearing in the *world*, not a direction on the prop, and every frame it turns
// about the prop's own face to lie as near that bearing as the face allows. Pick the prop up,
// carry it to the far end of the house, turn round and look again: the needle is still
// pointing where it was pointing. That is the whole of what it says — *this* way — and it is
// why it cannot simply be drawn on.
//
// It turns in the face rather than in free space because a flat needle cannot point out of
// its own plane and look like anything but a dot. Whatever it is fixed to therefore has to be
// held at a slant rather than face on, or there is a bearing it cannot show.
[DisallowMultipleComponent]
public class CompassNeedle : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Which way in the world it points. Only the bearing matters — it is flattened " +
             "into the face it can turn in and normalised.")]
    private Vector3 pointsTowards = Vector3.right;

    // After whatever is carrying it has been moved. InspectView flies a held prop to the eyes
    // in LateUpdate, and a needle read before that is a needle a frame behind the coin.
    private void LateUpdate()
    {
        Vector3 face = transform.parent.up;
        Vector3 bearing = Vector3.ProjectOnPlane(pointsTowards, face);

        // Edge on to the bearing there is nothing left to point at, and turning to a vector
        // this short is turning to rounding error. Leave it where it stood; a hand holding
        // something is never still for more than a frame.
        if (bearing.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(bearing, face);
    }
}
