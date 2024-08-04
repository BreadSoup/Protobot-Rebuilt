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
        private RebindAction EventRebindAction => inputEvent.rebindAction;

        [Header("UI")] [SerializeField] private Text actionNameText;
        [SerializeField] private Text rebindText;
        [SerializeField] private Button resetButton;

        [SerializeField] private bool rebinding;

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

            foreach (InputBinding binding in action.bindings) {
                if ((!string.IsNullOrEmpty(binding.overridePath) || binding.overridePath == null && binding.path != "") && !binding.isComposite && !binding.isPartOfComposite) {
                    if (displayString != "")
                        displayString += " + ";
                    
                    var bindingString = binding.ToDisplayString(
                        InputBinding.DisplayStringOptions.DontUseShortDisplayNames 
                        | InputBinding.DisplayStringOptions.DontIncludeInteractions);

                    if (bindingString == "Control")
                        bindingString = "Ctrl";

                    displayString += bindingString;
                }
            }
            return displayString;
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