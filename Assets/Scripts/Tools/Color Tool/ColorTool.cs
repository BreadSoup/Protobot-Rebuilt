//using Ookii.Dialogs; honest to god i have no idea what this if for but it causes build errors on linux
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Protobot.SelectionSystem;

public class ColorTool : MonoBehaviour
{
    [SerializeField] public static Material Material;
    [SerializeField] public static List<Material> Materials = new();
    [SerializeField] List<Color> color;
    [SerializeField] GameObject custom;
    [SerializeField] Slider red,green,blue;
    [SerializeField] Image preview;
    [SerializeField] public static Color ColorToSet;
    public static bool CustomColor;
    public void EnableUI(GameObject ui)
    {
        ui.SetActive(!ui.gameObject.activeSelf);
        ColorToSet = color[0]; //If this is not set when started it will be 0,0,0 which we dont want unless custom is selected
        UpdatePreview();
        red.value = 242;
        green.value = 242;
        blue.value = 242;
    }
    public void HandleInput(int index)
    {
        if (index < color.Count)
        {
            ColorToSet = color[index];
            custom.SetActive(false);
            CustomColor = false;
        }
        else
        {
            UpdateCustomColor();
            custom.SetActive(true);
            CustomColor = true;
        }
    }

    public void UpdateCustomColor()
    {
        ColorToSet = new Color(red.value / 255, green.value / 255, blue.value / 255, 1);
    }

    public void UpdatePreview()
    {
        preview.color = new Color(red.value/255,green.value/255,blue.value/255,1);
    }

    public void UpdateColorSliders()
    {
        red.value = Material.color.r * 255;
        green.value = Material.color.g * 255;
        blue.value = Material.color.b * 255;
    }
}
