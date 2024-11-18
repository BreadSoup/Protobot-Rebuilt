using UnityEngine;
using UnityEngine.UI;

public class BackgroundColorChange : MonoBehaviour
{
    public Camera mainCamera;
    public Toggle toggleButton;

    // Public variables for background colors
    public Color onColor = Color.white;
    public Color offColor = Color.black;
    public Color gridOnColor = new(51,51,51, 51);
    public Color gridOffColor = new(204, 204, 204,204);
    public Material gridMaterial;
    public void ToggleBackground(bool isOn)
    {
        if (isOn)
        {
            mainCamera.backgroundColor = onColor;
            gridMaterial.color = gridOffColor;
        }
        else
        {
            
            mainCamera.backgroundColor = offColor;
            gridMaterial.color = gridOnColor;
        }
    }
    
}
