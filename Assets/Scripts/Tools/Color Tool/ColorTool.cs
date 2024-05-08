using Ookii.Dialogs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Protobot.SelectionSystem;

public class ColorTool : MonoBehaviour
{
    [SerializeField] Material material;
    [SerializeField] List<Color> color;
    [SerializeField] GameObject custom;
    [SerializeField] Slider red,green,blue;
    [SerializeField] Image preview;

    public void EnableUI(GameObject UI)
    {
        UI.SetActive(!UI.active);
    }
    public void HandleInput(int index)
    {
        if (index < color.Count)
        {
            material.color = color[index];
            custom.SetActive(false);
        }
        else
        {
            custom.SetActive(true);
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
}
