using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Protobot {
    /// <summary>
    /// UI component that displays and manages a single part parameter (e.g. shaft length, type).
    /// Supports two display modes:
    ///   - Dropdown: for parameters with a fixed list of options (e.g. "Normal" / "High Strength")
    ///   - Custom input field: for parameters the user types in (e.g. length in inches or holes)
    ///
    /// Clamping to min/max limits is only applied when the user finishes typing (on end edit),
    /// NOT on every keystroke — this prevents the bug where typing "12.2" would be immediately
    /// cut to "12" because the mid-type value temporarily exceeded the integer limit.
    /// </summary>
    public class ParamDisplay : MonoBehaviour, IScrollHandler {
        [SerializeField] private Text title;
        [SerializeField] private Dropdown dropdown;
        [SerializeField] private InputField customInput;
        [SerializeField] private Text customUnit;

        [SerializeField] private UnityEvent OnSetDisplay;
        [SerializeField] private StringUnityEvent OnUpdateValue;

        private List<string> dropdownOptions;
        private Parameter parameter;
        
        private int ParamIndex => dropdownOptions.IndexOf(parameter.value);

        private void Start() {
            dropdown.onValueChanged.AddListener(index => {
                OnUpdateValue?.Invoke(dropdown.options[index].text);
            });

            // While the user is typing, only update the parameter value if the input
            // is a valid number. We do NOT clamp here — clamping on every keystroke
            // causes a bug where values like "12.2" are immediately cut to "12" because
            // the mid-type value exceeds the integer limit before the decimal is finished.
            customInput.onValueChanged.AddListener(inputText => {
                if (inputText.Length > 0 && float.TryParse(inputText, out _))
                    OnUpdateValue?.Invoke(inputText);
            });

            // Once the user finishes editing (presses Enter or clicks away),
            // clamp the value to the allowed min/max range and refresh the display.
            customInput.onEndEdit.AddListener(inputText => {
                if (inputText.Length > 0 && float.TryParse(inputText, out _))
                    OnUpdateValue?.Invoke(ClampCustomInput(parameter, inputText));
                customInput.SetTextWithoutNotify(parameter.value);
            });
        }

        public void SetDisplay(Parameter parameter, List<string> options) {
            bool sameParam = this.parameter == parameter;
            
            this.parameter = parameter;

            title.text = parameter.name + ":";
            if (!sameParam) SetCustomDisplay(parameter);

            if (!parameter.custom) {
                SetDropdownOptions(options);
                dropdown.value = ParamIndex;
            }
            

            OnSetDisplay?.Invoke();
        }
        
        public void SetDropdownOptions(List<string> options) {
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdownOptions = options;

            dropdown.interactable = options.Count > 1;

        }

        public void SetCustomDisplay(Parameter p) {
            bool enable = p.custom;
            
            customInput.gameObject.SetActive(enable);
            dropdown.gameObject.SetActive(!enable);

            if (enable) {
                customUnit.text = parameter.customUnit;
                customInput.text = parameter.customDefault;

                if (p.customUnit == "Holes") {
                    customInput.contentType = InputField.ContentType.IntegerNumber;
                }
                else {
                    customInput.contentType = InputField.ContentType.DecimalNumber;
                }
            }
        }

        /// <summary>
        /// Clamps a user-typed string value to the parameter's allowed min/max range.
        /// Returns the clamped value as a string, or the parameter's default if unparseable.
        /// Uses TryParse to safely handle any malformed input without throwing exceptions.
        /// </summary>
        private string ClampCustomInput(Parameter p, string value) {
            if (!float.TryParse(value, out float valueFloat))
                return p.customDefault;

            if (valueFloat > p.customLimits.y) return p.customLimits.y.ToString();
            if (valueFloat < p.customLimits.x) return p.customLimits.x.ToString();

            return value;
        }

        /// <summary>
        /// Handles mouse scroll wheel input on the parameter display.
        /// Scrolling up/down increments or decrements the value by 1 (for custom inputs)
        /// or moves to the next/previous dropdown option.
        /// </summary>
        public void OnScroll(PointerEventData eventData) {
            int dir = (eventData.scrollDelta.y < 1) ? -1 : 1;

            if (parameter.custom) {
                // Use TryParse to safely read the current value before adding scroll delta
                if (!float.TryParse(parameter.value, out float currentVal)) return;

                float newVal = currentVal + dir;

                if (newVal >= parameter.customLimits.x && newVal <= parameter.customLimits.y)
                    customInput.text = newVal.ToString();
            }
            else {
                int newIndex = dropdown.value + dir;

                if (newIndex < dropdownOptions.Count && newIndex >= 0)
                    dropdown.value = newIndex;
            }
        }
    }
}