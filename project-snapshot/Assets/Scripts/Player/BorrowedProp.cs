using System.Collections.Generic;
using UnityEngine;

// A prop taken out of the world for a moment, and the note of how to put it back.
//
// Lifting something to the eyes means three separate loans: its colliders go quiet, its
// layer changes to the one only the overlay camera draws, and its renderers leave the
// room's lighting for the inspect lights. All three have to be handed back exactly,
// including on the parts that were already switched off before we touched them — which
// is why this records what it found rather than assuming.
public class BorrowedProp
{
    private readonly List<Collider> _silenced = new List<Collider>();
    private readonly Dictionary<Transform, int> _layers = new Dictionary<Transform, int>();
    private readonly Dictionary<Renderer, uint> _renderLayers = new Dictionary<Renderer, uint>();

    private readonly Transform _prop;
    private readonly Transform _homeParent;
    private readonly Vector3 _homeLocalPosition;
    private readonly Quaternion _homeLocalRotation;

    public Transform Prop => _prop;

    // Home follows its parent rather than sitting at a world position frozen on pickup,
    // so a key lifted out of a drawer lands back in the drawer even if the drawer moved
    // while the key was in the air.
    public Vector3 HomePosition =>
        _homeParent != null ? _homeParent.TransformPoint(_homeLocalPosition) : _homeLocalPosition;

    public Quaternion HomeRotation =>
        _homeParent != null ? _homeParent.rotation * _homeLocalRotation : _homeLocalRotation;

    public BorrowedProp(Transform prop, int overlayLayer, uint overlayRenderingMask)
    {
        _prop = prop;
        _homeParent = prop.parent;
        _homeLocalPosition = prop.localPosition;
        _homeLocalRotation = prop.localRotation;

        // Out of reach while it is in the air: nothing to pick up mid-flight, and no
        // second F landing on the very thing being put away.
        foreach (Collider c in prop.GetComponentsInChildren<Collider>())
        {
            if (!c.enabled)
                continue;

            c.enabled = false;
            _silenced.Add(c);
        }

        foreach (Transform t in prop.GetComponentsInChildren<Transform>(true))
        {
            _layers[t] = t.gameObject.layer;
            t.gameObject.layer = overlayLayer;
        }

        foreach (Renderer r in prop.GetComponentsInChildren<Renderer>(true))
        {
            _renderLayers[r] = r.renderingLayerMask;
            r.renderingLayerMask = overlayRenderingMask;
        }
    }

    public void Return()
    {
        _prop.SetLocalPositionAndRotation(_homeLocalPosition, _homeLocalRotation);

        foreach (Collider c in _silenced)
        {
            if (c != null)
                c.enabled = true;
        }

        foreach (KeyValuePair<Transform, int> entry in _layers)
        {
            if (entry.Key != null)
                entry.Key.gameObject.layer = entry.Value;
        }

        foreach (KeyValuePair<Renderer, uint> entry in _renderLayers)
        {
            if (entry.Key != null)
                entry.Key.renderingLayerMask = entry.Value;
        }
    }
}
