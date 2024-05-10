using Ookii.Dialogs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Protobot.SelectionSystem;

public class ColorTool : MonoBehaviour
{
    [SerializeField] public static Material material;
    [SerializeField] public static List<Material> materials = new();
    [SerializeField] List<Color> color;
    [SerializeField] GameObject custom;
    [SerializeField] Slider red,green,blue;
    [SerializeField] Image preview;
    [SerializeField] public static Color colorToSet;
    public void EnableUI(GameObject UI)
    {
        UI.SetActive(!UI.active);
    }
    public void HandleInput(int index)
    {
        /*if(materials.Count != 0)
        {
            if (index < color.Count)
            {
                custom.SetActive(false);
                for (int i = 0; i < materials.Count; i++)
                {
                    if (materials[i].GetFloat("_Metallic") == .754f)
                    {
                        colorToSet = color[index];
                    }
                }
            }
            else
            {
                custom.SetActive(true);
            }
            
        }
        else
        {}*/ //commented because it is a prototype theory for coloring multiple metal at once
        if (index < color.Count)
        {
            colorToSet = color[index];
            custom.SetActive(false);
        }
        else
        {
            custom.SetActive(true);
        }
    }

    public void UpdateCustomColor()
    {
        colorToSet = new Color(red.value / 255, green.value / 255, blue.value / 255, 1);
    }

    public void UpdatePreview()
    {
        preview.color = new Color(red.value/255,green.value/255,blue.value/255,1);
    }

    public void UpdateColorSliders()
    {
        red.value = material.color.r * 255;
        green.value = material.color.g * 255;
        blue.value = material.color.b * 255;
    }
}
