using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CustomGrab : MonoBehaviour
{
    private CustomGrab _otherHand;
    public List<Transform> nearObjects = new();
    public Transform grabbedObject;
    public InputActionReference action;
    private bool _grabbing;
    
    public InputActionReference doubleRotationToggleAction;
    public bool doubleRotation;
    
    private Vector3 _previousPosition;
    private Quaternion _previousRotation;

    private void Start()
    {
        action.action.Enable();
        
        foreach (var c in transform.parent.GetComponentsInChildren<CustomGrab>())
        {
            if (c != this)
                _otherHand = c;
        }
        
        if (doubleRotationToggleAction)
        {
            doubleRotationToggleAction.action.Enable();
            doubleRotationToggleAction.action.performed += _ =>
            {
                doubleRotation = !doubleRotation;
                
                if (_otherHand)
                    _otherHand.doubleRotation = doubleRotation;
            };
        }
        
        _previousPosition = transform.position;
        _previousRotation = transform.rotation;
    }

    private void Update()
    {
        var deltaPosition = transform.position - _previousPosition;
        var deltaRotation = transform.rotation * Quaternion.Inverse(_previousRotation);

        _grabbing = action.action.IsPressed();
        var rotationToApply = deltaRotation;
        if (_grabbing)
        {
            if (!grabbedObject)
                grabbedObject = nearObjects.Count > 0 ? nearObjects[0] : _otherHand.grabbedObject;

            if (grabbedObject)
            {
                if (doubleRotation)
                {
                    rotationToApply = DoubleQuaternionAngle(deltaRotation);
                }
                
                grabbedObject.rotation = rotationToApply * grabbedObject.rotation;
                var offset = grabbedObject.position - transform.position;
                var rotatedOffset = rotationToApply * offset;
                grabbedObject.position = transform.position + rotatedOffset;
                
                grabbedObject.position += deltaPosition;
            }
        }
        else if (grabbedObject)
            grabbedObject = null;
        
        _previousPosition = transform.position;
        _previousRotation = transform.rotation;
    }
    
    private static Quaternion DoubleQuaternionAngle(Quaternion q)
    {
        q.ToAngleAxis(out var angle, out var axis);
        if (angle > 180f) angle -= 360f;
        return Quaternion.AngleAxis(angle * 2f, axis);
    }

    private void OnTriggerEnter(Collider other)
    {
        var t = other.transform;
        if (t && t.tag.ToLower() == "grabbable")
            nearObjects.Add(t);
    }

    private void OnTriggerExit(Collider other)
    {
        var t = other.transform;
        if (t && t.tag.ToLower() == "grabbable")
            nearObjects.Remove(t);
    }
}
