using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Custom grab script that supports simultaneous two-handed manipulation.
/// Attach this to EACH controller (Left Controller and Right Controller).
///
/// Instead of snapping the object to the controller, this tracks the delta
/// position and rotation of the controller each frame and applies those deltas
/// to the grabbed object. When two controllers grab the same object, their
/// deltas are combined (positions added, quaternions multiplied).
///
/// SETUP (in Unity Editor):
/// 1. Add this script to both Left Controller and Right Controller GameObjects.
/// 2. On each controller, add a SphereCollider:
///    - Check "Is Trigger" = true
///    - Set Radius to ~0.1 (adjust for comfortable grab range)
/// 3. On each controller, add a Rigidbody:
///    - Check "Is Kinematic" = true
///    - Uncheck "Use Gravity"
/// 4. Assign the "gripAction" InputActionReference to the grip/grab button
///    (e.g., XRI LeftHand/Select or the grip action from your input actions).
/// 5. Assign the "doubleRotationToggleAction" InputActionReference to a button
///    for toggling double rotation (e.g., XRI LeftHand/PrimaryButton).
/// 6. Objects that can be grabbed must:
///    - Have a Collider with "Is Trigger" = true
///    - Be tagged "Grabbable"
///    - Have a Rigidbody (Is Kinematic = true, Use Gravity = false)
/// </summary>
public class CustomGrab : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("The grip/grab button action.")]
    public InputActionReference gripAction;

    [Tooltip("Button to toggle double rotation (Extra Credit).")]
    public InputActionReference doubleRotationToggleAction;

    [Header("Settings")]
    [Tooltip("Enable double rotation mode (doubles rotation magnitude).")]
    public bool doubleRotationEnabled = false;

    // Static tracking: which controllers are grabbing which object
    // Key = grabbed object instance ID, Value = set of CustomGrab instances grabbing it
    private static readonly Dictionary<int, HashSet<CustomGrab>> ActiveGrabs = new Dictionary<int, HashSet<CustomGrab>>();

    // Per-instance state
    private GameObject _grabbedObject;
    private bool _isGrabbing = false;
    private Vector3 _prevPosition;
    private Quaternion _prevRotation;

    // Trigger overlap tracking
    private readonly List<Collider> _overlappingGrabbables = new List<Collider>();

    void Start()
    {
        if (gripAction != null)
        {
            gripAction.action.Enable();
            gripAction.action.performed += OnGripPerformed;
            gripAction.action.canceled += OnGripReleased;
        }

        if (doubleRotationToggleAction != null)
        {
            doubleRotationToggleAction.action.Enable();
            doubleRotationToggleAction.action.performed += OnDoubleRotationToggle;
        }

        _prevPosition = transform.position;
        _prevRotation = transform.rotation;
    }

    void OnDestroy()
    {
        if (gripAction != null)
        {
            gripAction.action.performed -= OnGripPerformed;
            gripAction.action.canceled -= OnGripReleased;
        }

        if (doubleRotationToggleAction != null)
        {
            doubleRotationToggleAction.action.performed -= OnDoubleRotationToggle;
        }

        // Clean up grab tracking
        if (_isGrabbing && _grabbedObject != null)
        {
            ReleaseObject();
        }
    }

    private void OnGripPerformed(InputAction.CallbackContext ctx)
    {
        if (_isGrabbing) return;

        // Find the closest grabbable object in trigger range
        var closest = FindClosestGrabbable();
        if (closest != null)
        {
            GrabObject(closest);
        }
    }

    private void OnGripReleased(InputAction.CallbackContext ctx)
    {
        if (!_isGrabbing) return;
        ReleaseObject();
    }

    private void OnDoubleRotationToggle(InputAction.CallbackContext ctx)
    {
        doubleRotationEnabled = !doubleRotationEnabled;
        Debug.Log($"Double rotation: {(doubleRotationEnabled ? "ON" : "OFF")}");
    }

    private GameObject FindClosestGrabbable()
    {
        // Clean up null references
        _overlappingGrabbables.RemoveAll(c => c == null);

        GameObject closest = null;
        var closestDist = float.MaxValue;

        foreach (var col in _overlappingGrabbables)
        {
            if (col == null) continue;
            if (!col.CompareTag("Grabbable")) continue;

            var dist = Vector3.Distance(transform.position, col.transform.position);
            if (!(dist < closestDist)) continue;
            closestDist = dist;
            closest = col.gameObject;
        }

        return closest;
    }

    private void GrabObject(GameObject obj)
    {
        _grabbedObject = obj;
        _isGrabbing = true;

        // Reset delta tracking to current frame
        _prevPosition = transform.position;
        _prevRotation = transform.rotation;

        // Register this controller as grabbing the object
        var id = obj.GetInstanceID();
        if (!ActiveGrabs.ContainsKey(id))
        {
            ActiveGrabs[id] = new HashSet<CustomGrab>();
        }
        ActiveGrabs[id].Add(this);
    }

    private void ReleaseObject()
    {
        if (_grabbedObject != null)
        {
            var id = _grabbedObject.GetInstanceID();
            if (ActiveGrabs.ContainsKey(id))
            {
                ActiveGrabs[id].Remove(this);
                if (ActiveGrabs[id].Count == 0)
                {
                    ActiveGrabs.Remove(id);
                }
            }
        }

        _grabbedObject = null;
        _isGrabbing = false;
    }

    void Update()
    {
        if (!_isGrabbing || !_grabbedObject)
        {
            // Always track position/rotation even when not grabbing
            _prevPosition = transform.position;
            _prevRotation = transform.rotation;
            return;
        }

        // Only the first controller in the set applies the combined delta
        // to avoid double-applying. Other controllers just track their deltas.
        var id = _grabbedObject.GetInstanceID();
        if (!ActiveGrabs.TryGetValue(id, out var grabbers)) return;

        // Check if this controller is the "primary" one (first in set)
        // Only the primary applies the combined transformation
        var isPrimary = false;
        foreach (var g in grabbers)
        {
            if (g == this) { isPrimary = true; break; }
            break; // First element check
        }

        if (isPrimary)
        {
            // Collect and combine deltas from all grabbing controllers
            var combinedDeltaPos = Vector3.zero;
            var combinedDeltaRot = Quaternion.identity;

            foreach (var grabber in grabbers)
            {
                // Compute this controller's deltas
                var deltaPos = grabber.transform.position - grabber._prevPosition;
                var deltaRot = grabber.transform.rotation * Quaternion.Inverse(grabber._prevRotation);

                combinedDeltaPos += deltaPos;
                combinedDeltaRot = deltaRot * combinedDeltaRot;
            }

            // Apply double rotation if enabled (any grabber has it on)
            var anyDoubleRotation = grabbers.Any(grabber => grabber.doubleRotationEnabled);

            if (anyDoubleRotation)
            {
                combinedDeltaRot = DoubleQuaternionRotation(combinedDeltaRot);
            }

            // Apply rotation around each controller's pivot point
            // For combined grabbing, we use the average controller position as pivot
            var pivot = Vector3.zero;
            var count = 0;
            foreach (var grabber in grabbers)
            {
                pivot += grabber.transform.position;
                count++;
            }
            pivot /= count;

            // Step 1: Rotate the object around the pivot
            var objectPos = _grabbedObject.transform.position;
            var offset = objectPos - pivot;
            var rotatedOffset = combinedDeltaRot * offset;
            _grabbedObject.transform.position = pivot + rotatedOffset;

            // Step 2: Apply the translation delta
            _grabbedObject.transform.position += combinedDeltaPos;

            // Step 3: Apply the rotation to the object itself
            _grabbedObject.transform.rotation = combinedDeltaRot * _grabbedObject.transform.rotation;
        }

        // All controllers update their previous state
        _prevPosition = transform.position;
        _prevRotation = transform.rotation;
    }

    /// <summary>
    /// Doubles the rotation magnitude while keeping the rotation axis intact.
    /// Extracts axis-angle, doubles the angle, reconstructs the quaternion.
    /// </summary>
    private static Quaternion DoubleQuaternionRotation(Quaternion q)
    {
        q.ToAngleAxis(out var angle, out var axis);

        // Handle edge case where axis is zero (identity quaternion)
        if (axis == Vector3.zero || float.IsInfinity(axis.x))
            return Quaternion.identity;

        return Quaternion.AngleAxis(angle * 2f, axis);
    }

    // Trigger detection for nearby grabbable objects
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Grabbable") && !_overlappingGrabbables.Contains(other))
        {
            _overlappingGrabbables.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        _overlappingGrabbables.Remove(other);
    }
}
