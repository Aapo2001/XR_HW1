using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwitchLight : MonoBehaviour
{
    // Start is called before the first frame update
    public Light light;
    public InputActionReference action;
    public Color baseLightColor = Color.white;
    public Color newLightColor = Color.red;
    void Start()
    {
        action.action.Enable();
        light = GetComponent<Light>();
        action.action.performed += (ctx) =>
        {
            if (light.color == baseLightColor)
            {
                light.color = newLightColor;
            }
            else
            {
                light.color = baseLightColor;
            }
        };
    }



}
