using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;

namespace Protobot.InputEvents {
    public class InputEvent : MonoBehaviour {
        public InputAction defaultAction;
        public RebindAction rebindAction;

        public Action performed;
        public Action canceled;
        public bool IsPressed = false;

        private bool prevPressStatus = false;

        public void Awake() {
            //performed += () => Debug.Log("Performed " + name + " input event!");

            rebindAction = new RebindAction(name);

            rebindAction.OnCompleteRebind += () => defaultAction.Disable();
            rebindAction.OnSaveRebinds += () => defaultAction.Disable();

            rebindAction.OnResetRebinds += () => defaultAction.Enable();

            rebindAction.OnLoadRebinds += hasRebinds => {
                if (hasRebinds) {
                    defaultAction.Disable();
                }
            };

            if (rebindAction.IsEmpty) {
                defaultAction.Enable();
            }
        }

        public void Update() {
            if (RebindAction.Rebinding) return;
            
            IsPressed = defaultAction.AllControlsPressed() || rebindAction.action.AllControlsPressed();

            if (IsPressed != prevPressStatus) {
                if (IsPressed)
                    performed?.Invoke();
                else
                    canceled?.Invoke();
            }

            prevPressStatus = IsPressed;
        }

        public void OnDisable() {
            defaultAction.Disable();
        }
        
        public string GetCurrentKeybind()
        {
            return defaultAction.GetBindingDisplayString();
        }
        
        public bool IsKeyPressed(string keyName)
        {
            var key = Keyboard.current.FindKeyOnCurrentKeyboardLayout(keyName);
            if (key == null) print("Key not found: " + keyName);
            return key != null && key.isPressed;
        }
    }
}