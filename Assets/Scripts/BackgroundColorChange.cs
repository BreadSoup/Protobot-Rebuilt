using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundColorChange : MonoBehaviour
{
    public Camera mainCamera;
    public Toggle toggleButton;

    // Public variables for background colors
    public Color onColor = Color.white;
    public Color offColor = Color.black;

    private void Start()
    {
        // Assign the function ToggleBackground to be called whenever the value of the Toggle changes
        toggleButton.onValueChanged.AddListener(ToggleBackground);
    }

    private void ToggleBackground(bool isOn)
    {
        if (isOn)
        {
            mainCamera.backgroundColor = onColor;
        }
        else
        {
            mainCamera.backgroundColor = offColor;
        }
    }
}
