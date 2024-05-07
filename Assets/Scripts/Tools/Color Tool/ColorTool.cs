using Ookii.Dialogs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ColorTool : MonoBehaviour
{
    [SerializeField] Material material;
    [SerializeField] List<Color> color;
    [SerializeField] GameObject custom;
    [SerializeField] Slider red,green,blue;
    [SerializeField] Image preview;

    private void Awake()
    {
        material.color = color[0];
    }
    public void EnableUI(GameObject UI)
    {
        UI.SetActive(!UI.active);
    }
    public void HandleInput(int index)
    {
        if (index != 13)
        {
            custom.SetActive(false);
        }
        switch (index)
        {
            case 0: material.color = color[0]; break; //white
            case 1: material.color = color[1]; break; //gray
            case 2: material.color = color[2]; break; //black
            case 3: material.color = color[3]; break; //brown
            case 4: material.color = color[4]; break; //red
            case 5: material.color = color[5]; break; //orange
            case 6: material.color = color[6]; break; //yellow
            case 7: material.color = color[7]; break; //lime
            case 8: material.color = color[8]; break; //green
            case 9: material.color = color[9]; break; //light blue
            case 10: material.color = color[10]; break; //blue
            case 11: material.color = color[11]; break; //pink
            case 12: material.color = color[12]; break; //purple
            case 13: custom.SetActive(true); break;

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
