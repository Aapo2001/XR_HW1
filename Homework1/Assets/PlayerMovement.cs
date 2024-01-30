using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // Start is called before the first frame update
    public InputActionReference action;

    public Vector3 basePosition = new(0, 0, 0);
    public Vector3 newPosition = new(0, 0, -50);

    void Start()
    {
        action.action.Enable();
        action.action.performed += (ctx) =>
        {
            if (transform.position == basePosition)
            {
                transform.position = newPosition;
            }
            else
            {
                transform.position = basePosition;
            }
        };

    }

    // Update is called once per frame
    void Update()
    {

    }
}
