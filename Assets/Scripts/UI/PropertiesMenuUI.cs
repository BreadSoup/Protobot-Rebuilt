using Parts_List;
using UnityEngine;
using UnityEngine.UI;

namespace Protobot.UI {
    /// <summary>
    /// The Properties Menu displays information and editable transform fields for the
    /// currently selected part. It appears automatically when a part is selected and
    /// hides when nothing is selected.
    ///
    /// Shows:
    ///   - Part name (e.g. "U-Channel")
    ///   - Part group (e.g. "Structure")
    ///   - Up to two part parameters (e.g. "Profile: 1x2", "Length: 20 holes")
    ///   - Editable Position X, Y, Z (red, green, blue)
    ///   - Editable Rotation X, Y, Z (red, green, blue)
    ///
    /// Typing in the position/rotation fields updates the object in real time.
    /// Scroll wheel on a field increments/decrements its value (add ScrollableInputField
    /// to each InputField's GameObject in the Inspector).
    ///
    /// Setup in Unity Inspector:
    ///   1. Assign a SelectionObjectLink to "Selected Object"
    ///   2. Wire up all Text and InputField references
    ///   3. Assign the root panel GameObject to "Panel"
    ///   4. Add ScrollableInputField to each position/rotation InputField's GameObject
    /// </summary>
    public class PropertiesMenuUI : MonoBehaviour {

        [Header("Selection Reference")]
        [Tooltip("Drag in a SelectionObjectLink here so the panel tracks the selected part.")]
        [SerializeField] private ObjectLink selectedObject;

        [Header("Part Info Labels")]
        [SerializeField] private Text partNameText;
        [SerializeField] private Text partGroupText;

        [Header("Parameter Rows")]
        [Tooltip("The entire row GameObject for param 1 — shown/hidden automatically.")]
        [SerializeField] private GameObject param1Row;
        [SerializeField] private Text param1LabelText;
        [SerializeField] private Text param1ValueText;

        [Tooltip("The entire row GameObject for param 2 — shown/hidden automatically.")]
        [SerializeField] private GameObject param2Row;
        [SerializeField] private Text param2LabelText;
        [SerializeField] private Text param2ValueText;

        [Header("Position Inputs (set X=red, Y=green, Z=blue in Inspector)")]
        [SerializeField] private InputField posXInput;
        [SerializeField] private InputField posYInput;
        [SerializeField] private InputField posZInput;

        [Header("Rotation Inputs (set X=red, Y=green, Z=blue in Inspector)")]
        [SerializeField] private InputField rotXInput;
        [SerializeField] private InputField rotYInput;
        [SerializeField] private InputField rotZInput;

        [Header("Panel Root")]
        [Tooltip("The root panel GameObject. Gets shown when an object is selected.")]
        [SerializeField] private GameObject panel;

        // -----------------------------------------------------------------------
        // Unity Lifecycle
        // -----------------------------------------------------------------------

        private void Start() {
            SetupInputListeners();
        }

        private void Update() {
            bool isActive = selectedObject.active;
            panel.SetActive(isActive);

            if (!isActive) return;

            RefreshPartInfo();
            RefreshTransformDisplays();
        }

        // -----------------------------------------------------------------------
        // Input Listener Setup
        // -----------------------------------------------------------------------

        /// <summary>
        /// Wires up all six position/rotation InputFields so that typing immediately
        /// updates the selected object's transform.
        /// </summary>
        private void SetupInputListeners() {
            // --- Position ---
            posXInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var pos = RootTransform.position;
                pos.x = f;
                RootTransform.position = pos;
            });

            posYInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var pos = RootTransform.position;
                pos.y = f;
                RootTransform.position = pos;
            });

            posZInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var pos = RootTransform.position;
                pos.z = f;
                RootTransform.position = pos;
            });

            // --- Rotation ---
            rotXInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var rot = RootTransform.eulerAngles;
                rot.x = f;
                RootTransform.eulerAngles = rot;
            });

            rotYInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var rot = RootTransform.eulerAngles;
                rot.y = f;
                RootTransform.eulerAngles = rot;
            });

            rotZInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var rot = RootTransform.eulerAngles;
                rot.z = f;
                RootTransform.eulerAngles = rot;
            });
        }

        // -----------------------------------------------------------------------
        // Part Info Display
        // -----------------------------------------------------------------------

        /// <summary>
        /// Reads the selected part's PartName and PartType components and updates
        /// the info labels. Param rows are shown/hidden based on what the part has.
        /// </summary>
        private void RefreshPartInfo() {
            var root = RootObject;
            if (root == null) return;

            // Part name — uses PartName.name if available, else falls back to GameObject name
            if (root.TryGetComponent(out PartName partName)) {
                partNameText.text = "Part: " + partName.name;

                // Show param rows if this part has labelled parameters stored on PartName
                if (!string.IsNullOrEmpty(partName.param1Label)) {
                    param1Row.SetActive(true);
                    param1LabelText.text = partName.param1Label + ":";
                    param1ValueText.text = partName.param1Display;
                } else {
                    param1Row.SetActive(false);
                }

                if (!string.IsNullOrEmpty(partName.param2Label)) {
                    param2Row.SetActive(true);
                    param2LabelText.text = partName.param2Label + ":";
                    param2ValueText.text = partName.param2Display;
                } else {
                    param2Row.SetActive(false);
                }
            } else {
                partNameText.text = "Part: " + root.name;
                param1Row.SetActive(false);
                param2Row.SetActive(false);
            }

            // Part group — Structure, Motion, Electronics, etc.
            if (root.TryGetComponent(out PartType partType)) {
                partGroupText.text = "Type: " + partType.group.ToString();
            } else {
                partGroupText.text = string.Empty;
            }
        }

        // -----------------------------------------------------------------------
        // Transform Display
        // -----------------------------------------------------------------------

        /// <summary>
        /// Refreshes position and rotation input fields from the object's current transform.
        /// Skips fields that are actively focused so the user's typing isn't interrupted.
        /// </summary>
        private void RefreshTransformDisplays() {
            Vector3 pos = RootTransform.position;
            Vector3 rot = RootTransform.eulerAngles;

            if (!posXInput.isFocused) posXInput.SetTextWithoutNotify(pos.x.ToString("F3"));
            if (!posYInput.isFocused) posYInput.SetTextWithoutNotify(pos.y.ToString("F3"));
            if (!posZInput.isFocused) posZInput.SetTextWithoutNotify(pos.z.ToString("F3"));

            if (!rotXInput.isFocused) rotXInput.SetTextWithoutNotify(rot.x.ToString("F1"));
            if (!rotYInput.isFocused) rotYInput.SetTextWithoutNotify(rot.y.ToString("F1"));
            if (!rotZInput.isFocused) rotZInput.SetTextWithoutNotify(rot.z.ToString("F1"));
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns the root GameObject of the selected part — the one that has PartName
        /// on it (not a child mesh). Falls back to the directly selected object if no
        /// PartName is found in the hierarchy.
        /// </summary>
        private GameObject RootObject {
            get {
                if (!selectedObject.active) return null;
                // Walk up the hierarchy to find the object that has PartName
                var partName = selectedObject.obj.GetComponentInParent<PartName>();
                return partName != null ? partName.gameObject : selectedObject.obj;
            }
        }

        /// <summary>
        /// Shortcut to the root object's Transform.
        /// Falls back to the selected object's transform if no root is found.
        /// </summary>
        private Transform RootTransform {
            get {
                var root = RootObject;
                return root != null ? root.transform : selectedObject.tform;
            }
        }
    }
}
