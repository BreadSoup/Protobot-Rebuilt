using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Protobot.InputEvents {
    public class RebindUI : MonoBehaviour {
        [Header("Inputs")] [SerializeField] private InputEvent inputEvent;
        public RebindAction EventRebindAction => inputEvent.rebindAction;

        [Header("UI")] [SerializeField] private Text actionNameText;
        [SerializeField] public Text rebindText;
        [SerializeField] private Button resetButton;

        [SerializeField] internal static bool rebinding;

        void Start() {
            if (EventRebindAction != null) {
                EventRebindAction.OnEndRebind += UpdateDisplayUI;
                EventRebindAction.OnCompleteRebind += () => {
                    rebinding = false;
                    UpdateDisplayUI();
                };
                EventRebindAction.OnResetRebinds += UpdateDisplayUI;
                UpdateDisplayUI();
            }
        }
        
        void UpdateDisplayUI() {
            actionNameText.text = inputEvent.name;

            if (!EventRebindAction.IsEmpty)
                rebindText.text = GetBindingDisplayString(EventRebindAction.action);
            else
                rebindText.text = GetBindingDisplayString(inputEvent.defaultAction);

            resetButton.interactable = !EventRebindAction.IsEmpty;
        }

        public string GetBindingDisplayString(InputAction action) {
            string displayString = "";
            string key = "";
            List<string> modifiers = new List<string>();
            foreach (InputBinding binding in action.bindings) {
                if ((!string.IsNullOrEmpty(binding.overridePath) || binding.overridePath == null && binding.path != "") && !binding.isComposite && !binding.isPartOfComposite) {
                    var bindingString = binding.ToDisplayString(
                        InputBinding.DisplayStringOptions.DontUseShortDisplayNames 
                        | InputBinding.DisplayStringOptions.DontIncludeInteractions);
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                    bindingString = bindingString.Replace("Left Command", "Cmd").Replace("Right Command", "Cmd").Replace("Command", "Cmd").Replace("Left System", "Cmd").Replace("Right System", "Cmd");
                    // Remove phantom Alt if not actually pressed
                    if (bindingString == "Cmd") modifiers.Add("Cmd");
                    else if (bindingString == "Alt") {
                        if (UnityEngine.InputSystem.Keyboard.current != null &&
                            (UnityEngine.InputSystem.Keyboard.current.leftAltKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightAltKey.isPressed))
                            modifiers.Add("Alt");
                    }
                    else if (bindingString == "Shift") modifiers.Add("Shift");
                    else key = bindingString;
#else
                    bindingString = bindingString.Replace("Control", "Ctrl");
                    if (bindingString == "Ctrl") modifiers.Add("Ctrl");
                    else if (bindingString == "Alt") {
                        if (UnityEngine.InputSystem.Keyboard.current != null &&
                            (UnityEngine.InputSystem.Keyboard.current.leftAltKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightAltKey.isPressed))
                            modifiers.Add("Alt");
                    }
                    else if (bindingString == "Shift") modifiers.Add("Shift");
                    else key = bindingString;
#endif
                }
            }
            if (!string.IsNullOrEmpty(key)) {
                if (modifiers.Count > 0)
                    return string.Join(" + ", modifiers) + " + " + key;
                else
                    return key;
            } else if (modifiers.Count > 0) {
                return string.Join(" + ", modifiers);
            } else {
                return "";
            }
        }

        public void StartRebind() {
            rebindText.text = "Waiting for Input...";
            EventRebindAction.AttemptRebind();
            rebinding = true;
        }

        public void ResetRebinds() {
            EventRebindAction.ResetRebinds();
        }
    }
}