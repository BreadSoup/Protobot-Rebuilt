using System.Collections;
using System.Collections.Generic;
using Protobot.InputEvents;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using InputEvent = Protobot.InputEvents.InputEvent;

public class ColorPickerUpdater : MonoBehaviour
{
    [SerializeField]  TextMeshProUGUI keybindBox;
    [SerializeField] GameObject rebindUI;
    [SerializeField] GameObject selectionManager;
    private ColorPicker colorPicker;
    void Start()
    {
        colorPicker = selectionManager.GetComponent<ColorPicker>();
        UpdateString();
    }
    
    public void Rebind()
    {
        if (keybindBox.IsActive())
        {
            StartCoroutine(AwaitRebinding());
        }
    }
    IEnumerator AwaitRebinding()
    {
        while (RebindUI.rebinding)
        {
            yield return new WaitForSeconds(0.01f);
        }
        UpdateString();
    }
    public void UpdateString()
    {
        colorPicker.colorPickerKeybind = rebindUI.GetComponent<RebindUI>()
            .GetBindingDisplayString(rebindUI.GetComponent<RebindUI>().EventRebindAction.action);
        keybindBox.text = colorPicker.colorPickerKeybind;
        if (keybindBox.text == "")
        {
            colorPicker.colorPickerKeybind = "C";
            keybindBox.text  = "C";
        }
    }
}