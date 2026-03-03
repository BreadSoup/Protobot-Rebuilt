using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Protobot.UI {
    /// <summary>
    /// Attach this alongside an InputField to add scroll-wheel support.
    /// Scrolling up increments the numeric value, scrolling down decrements it.
    /// Used on position and rotation fields in the Properties Menu.
    ///
    /// Usage: Add this component to the same GameObject as an InputField.
    ///        Set the increment and format in the Inspector.
    /// </summary>
    [RequireComponent(typeof(InputField))]
    public class ScrollableInputField : MonoBehaviour, IScrollHandler {
        [Tooltip("How much the value changes per scroll tick.")]
        [SerializeField] private float increment = 0.1f;

        [Tooltip("Number format for displaying the value after scrolling (e.g. 'F3' = 3 decimal places).")]
        [SerializeField] private string format = "F3";

        private InputField inputField;

        private void Awake() {
            inputField = GetComponent<InputField>();
        }

        /// <summary>
        /// Called by Unity's EventSystem when the scroll wheel moves over this element.
        /// Increments or decrements the field's numeric value by the configured amount.
        /// </summary>
        public void OnScroll(PointerEventData eventData) {
            int dir = eventData.scrollDelta.y > 0 ? 1 : -1;

            if (!float.TryParse(inputField.text, out float val)) return;

            val += dir * increment;
            inputField.text = val.ToString(format);
        }
    }
}
