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

        /// <summary>
        /// When true, all InputEvents stop firing <see cref="performed"/> and
        /// <see cref="canceled"/> callbacks.  Set this to true while UI panels
        /// that should not trigger in-scene actions are open (e.g. Preferences).
        /// Use <see cref="PreferencesSuppressor"/> to toggle this automatically.
        /// </summary>
        public static bool SuppressAll { get; set; }

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
            if (SuppressAll || RebindAction.Rebinding) {
                // Keep prevPressStatus tracking the real key state so we don't
                // fire a spurious performed/canceled event the frame suppression ends.
                prevPressStatus = defaultAction.AllControlsPressed()
                                  || rebindAction.action.AllControlsPressed();
                IsPressed = false;
                return;
            }

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
        //i have no idea what im doing
        public void Rebind(string newBinding)
        {
            if (defaultAction != null)
            {
                defaultAction.ApplyBindingOverride(newBinding);
                Debug.Log($"Rebound to {newBinding}");
            }
            else
            {
                Debug.LogError("inputAction is null! Cannot rebind.");
            }
        }
    }
}