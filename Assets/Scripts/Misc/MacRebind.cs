using System.Collections;
using System.Collections.Generic;
using Protobot;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;


public class MacRebind : MonoBehaviour
{
    public Setting setting;
    public QuickPrompt quickPromptObject;
    public Protobot.InputEvents.InputEvent deleteSelection;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void RebindCheck()
    {
        print (setting.currentValue);
        if (setting.currentValue == "0")
        {
            StartCoroutine(quickPromptObject.Prompt(@"The Delete key is not supported on MacBook keyboards
Would you like to change bindings to resolve this?
This can always be changed in Edit>Preferences>Selection
", OnPromptComplete));
            
            
        }
    }
    
    void OnPromptComplete(bool accepted)
    {
        if (accepted)
        {
            
            deleteSelection.rebindAction.ManuelRebind("<Keyboard>/backspace"); // Change to desired key
            Debug.Log("Rebound deleteSelection to Backspace");
            setting.SetValue(1);
            
        }
        else
        {
            Debug.Log("User declined!");
            setting.SetValue(2);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
