using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Protobot.InputEvents;

namespace Protobot {
    /// <summary>
    /// Identical in interface to <see cref="RebindUI"/>, but named separately so it
    /// is easy to find in the Inspector.  Attach this to your "Radial Menu Key" row
    /// inside Preferences → Selection and wire it up exactly the same way as every
    /// other keybind row:
    ///
    ///   1. Assign <b>Input Event</b>  → the "Radial Menu Key" InputEvent in the scene.
    ///   2. Assign <b>Action Name Text</b> → the label Text on the left.
    ///   3. Assign <b>Rebind Text</b>      → the key-name Text in the middle.
    ///   4. Assign <b>Reset Button</b>     → the Reset Rebinds Button.
    ///   5. Wire the "Key Mapper" button's onClick  → RadialMenuKeybindRow.StartRebind.
    ///   6. Wire the Reset button's onClick          → RadialMenuKeybindRow.ResetRebinds.
    /// </summary>
    public class RadialMenuKeybindRow : MonoBehaviour {

        [Header("Inputs")]
        [SerializeField] private InputEvent inputEvent;

        [Header("UI")]
        [SerializeField] private Text   actionNameText;
        [SerializeField] public  Text   rebindText;
        [SerializeField] private Button resetButton;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start() {
            if (inputEvent == null || inputEvent.rebindAction == null) return;

            // Mirror the exact event subscriptions used by RebindUI.
            inputEvent.rebindAction.OnEndRebind      += UpdateDisplayUI;
            inputEvent.rebindAction.OnCompleteRebind += () => {
                RebindUI.rebinding = false;
                UpdateDisplayUI();
            };
            inputEvent.rebindAction.OnResetRebinds += UpdateDisplayUI;

            UpdateDisplayUI();
        }

        // ── Public methods called by button onClick events ────────────────────

        /// <summary>
        /// Called by the "Key Mapper" button onClick.
        /// Identical to <see cref="RebindUI.StartRebind"/>.
        /// </summary>
        public void StartRebind() {
            if (rebindText != null) rebindText.text = "Waiting for Input...";
            inputEvent?.rebindAction?.AttemptRebind();
            RebindUI.rebinding = true;
        }

        /// <summary>
        /// Called by the "Reset Rebinds Button" onClick.
        /// Identical to <see cref="RebindUI.ResetRebinds"/>.
        /// </summary>
        public void ResetRebinds() {
            inputEvent?.rebindAction?.ResetRebinds();
        }

        // ── Display ───────────────────────────────────────────────────────────

        private void UpdateDisplayUI() {
            if (inputEvent == null) return;

            // Show the GameObject's name as the label — same as RebindUI does.
            if (actionNameText != null)
                actionNameText.text = inputEvent.name;

            // Show the active binding, falling back to the default action.
            if (rebindText != null) {
                rebindText.text = !inputEvent.rebindAction.IsEmpty
                    ? GetBindingDisplayString(inputEvent.rebindAction.action)
                    : GetBindingDisplayString(inputEvent.defaultAction);
            }

            if (resetButton != null)
                resetButton.interactable = !inputEvent.rebindAction.IsEmpty;
        }

        /// <summary>
        /// Converts an <see cref="InputAction"/>'s bindings to a human-readable string.
        /// Identical to <see cref="RebindUI.GetBindingDisplayString"/>.
        /// </summary>
        public string GetBindingDisplayString(InputAction action) {
            string display = "";
            foreach (var binding in action.bindings) {
                bool hasPath = !string.IsNullOrEmpty(binding.overridePath)
                               || (binding.overridePath == null && binding.path != "");
                if (!hasPath || binding.isComposite || binding.isPartOfComposite) continue;

                if (display != "") display += " + ";

                var s = binding.ToDisplayString(
                    InputBinding.DisplayStringOptions.DontUseShortDisplayNames |
                    InputBinding.DisplayStringOptions.DontIncludeInteractions);

                if (s == "Control") s = "Ctrl";
                display += s;
            }
            return display;
        }
    }
}
