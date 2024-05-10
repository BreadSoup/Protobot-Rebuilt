using Ookii.Dialogs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Protobot.SelectionSystem;

public class ColorTool : MonoBehaviour
{
    [SerializeField] public static Material material, placeholder;
    [SerializeField] public static List<Material> materials = new();
    [SerializeField] Material placeHolderRef;
    [SerializeField] List<Color> color;
    [SerializeField] GameObject custom;
    [SerializeField] Slider red,green,blue;
    [SerializeField] Image preview;
    [SerializeField] public static ColorTool a;
    [SerializeField] public static int indexRef;

    private void Awake()
    {
        placeholder = placeHolderRef;
        material = placeHolderRef;

        a = gameObject.GetComponent<ColorTool>();
    }
    public void EnableUI(GameObject UI)
    {
        UI.SetActive(!UI.active);
    }
    public void HandleInput(int index)
    {
        indexRef = index;
        if(materials.Count != 0)
        {
            if (index < color.Count)
            {
                custom.SetActive(false);
                for (int i = 0; i < materials.Count; i++)
                {
                    if (materials[i].GetFloat("_Metallic") == .754f)
                    {
                        materials[i].color = color[index];
                    }
                }
            }
            else
            {
                custom.SetActive(true);
            }
            
        }
        else
        {
            if (index < color.Count)
            {
                if (material.GetFloat("_Metallic") == .754f)
                {
                    material.color = color[index];
                }
                custom.SetActive(false);
            }
            else
            {
                custom.SetActive(true);
            }
        }
        
    }

    public void UpdateCustomColor()
    {
        material.color = new Color(red.value / 255, green.value / 255, blue.value / 255, 1);
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

    public static void RunUpdateSliders()
    {
        if (indexRef == 13)
        a.UpdateColorSliders();
    }
}
