using Parts_List;
using UnityEngine;
using UnityEngine.UI;

namespace Protobot.UI {
    /// <summary>
    /// The Properties Menu shows part info and editable transform fields for the
    /// currently selected part. It builds its own UI at runtime — no Inspector
    /// setup is needed at all.
    ///
    /// SETUP (the only step required):
    ///   1. In Unity, right-click in the Hierarchy → Create Empty
    ///   2. Rename it "Properties Menu Manager"
    ///   3. Click Add Component → search "PropertiesMenuUI" → add it
    ///   That's it. Press Play and the menu appears automatically.
    ///
    /// The panel appears when a part is selected and hides otherwise.
    /// Typing in Position or Rotation fields updates the part in real time.
    /// Scroll wheel on any field nudges the value by 0.1.
    /// </summary>
    public class PropertiesMenuUI : MonoBehaviour {

        // Found automatically at runtime — no Inspector wiring needed
        private ObjectLink selectedObject;

        // All UI elements are created in BuildUI()
        private GameObject panel;
        private Text partNameText;
        private Text partGroupText;
        private GameObject param1Row;
        private Text param1LabelText;
        private Text param1ValueText;
        private GameObject param2Row;
        private Text param2LabelText;
        private Text param2ValueText;
        private InputField posXInput, posYInput, posZInput;
        private InputField rotXInput, rotYInput, rotZInput;

        // Axis colors: red=X, green=Y, blue=Z
        private static readonly Color ColorX     = new Color(0.75f, 0.22f, 0.22f, 1f);
        private static readonly Color ColorY     = new Color(0.20f, 0.65f, 0.20f, 1f);
        private static readonly Color ColorZ     = new Color(0.20f, 0.40f, 0.80f, 1f);
        private static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.12f, 0.93f);
        private static readonly Color DimColor   = new Color(0.65f, 0.65f, 0.65f, 1f);

        // Cached font (grabbed from an existing Text object in the scene)
        private Font uiFont;

        // -----------------------------------------------------------------------
        // Unity Lifecycle
        // -----------------------------------------------------------------------

        private void Awake() {
            // Find the SelectionObjectLink already in the scene
            selectedObject = FindObjectOfType<SelectionObjectLink>();
            if (selectedObject == null) {
                Debug.LogError("[PropertiesMenuUI] No SelectionObjectLink found in the scene. " +
                               "The Properties Menu needs one to know what is selected.");
                enabled = false;
                return;
            }

            // Grab the font from whatever Text object is already in the scene
            var existingText = FindObjectOfType<Text>();
            uiFont = existingText != null
                ? existingText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildUI();
            SetupInputListeners();
        }

        private void Update() {
            if (panel == null) return;

            bool isActive = selectedObject.active;
            panel.SetActive(isActive);
            if (!isActive) return;

            RefreshPartInfo();
            RefreshTransformDisplays();
        }

        // -----------------------------------------------------------------------
        // UI Construction — builds the entire panel in code
        // -----------------------------------------------------------------------

        private void BuildUI() {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) {
                Debug.LogError("[PropertiesMenuUI] No Canvas found in the scene.");
                return;
            }

            // --- Root panel ---
            panel = MakeElement("Properties Menu", canvas.transform);
            var panelRT = panel.GetComponent<RectTransform>();
            // Anchor to top-left corner of the screen
            panelRT.anchorMin        = new Vector2(0f, 1f);
            panelRT.anchorMax        = new Vector2(0f, 1f);
            panelRT.pivot            = new Vector2(0f, 1f);
            panelRT.anchoredPosition = new Vector2(10f, -10f);
            panelRT.sizeDelta        = new Vector2(230f, 0f); // width fixed; height auto

            var panelImg = panel.AddComponent<Image>();
            panelImg.color = PanelColor;

            // Vertical stack layout — rows stack top to bottom automatically
            var vLayout = panel.AddComponent<VerticalLayoutGroup>();
            vLayout.childControlWidth    = true;
            vLayout.childForceExpandWidth  = true;
            vLayout.childControlHeight   = false;
            vLayout.childForceExpandHeight = false;
            vLayout.spacing              = 3f;
            vLayout.padding              = new RectOffset(8, 8, 8, 8);

            // Auto-resize the panel height to fit its contents
            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // --- Part info rows ---
            partNameText  = MakeText("Part Name",  panel.transform, "Part: ---", 13, bold: true);
            partGroupText = MakeText("Part Group", panel.transform, "",          11);
            partGroupText.color = DimColor;

            MakeDivider(panel.transform);

            // Parameter rows (shown/hidden per part type)
            param1Row = MakeParamRow(panel.transform, out param1LabelText, out param1ValueText);
            param2Row = MakeParamRow(panel.transform, out param2LabelText, out param2ValueText);

            MakeDivider(panel.transform);

            // --- Position ---
            MakeText("PosHeader", panel.transform, "POSITION", 10).color = DimColor;

            var posRow = MakeElement("Pos Row", panel.transform);
            SetRowLayout(posRow, 24f);
            posXInput = MakeAxisField("Pos X", posRow.transform, "X", ColorX);
            posYInput = MakeAxisField("Pos Y", posRow.transform, "Y", ColorY);
            posZInput = MakeAxisField("Pos Z", posRow.transform, "Z", ColorZ);

            // --- Rotation ---
            MakeText("RotHeader", panel.transform, "ROTATION", 10).color = DimColor;

            var rotRow = MakeElement("Rot Row", panel.transform);
            SetRowLayout(rotRow, 24f);
            rotXInput = MakeAxisField("Rot X", rotRow.transform, "X", ColorX);
            rotYInput = MakeAxisField("Rot Y", rotRow.transform, "Y", ColorY);
            rotZInput = MakeAxisField("Rot Z", rotRow.transform, "Z", ColorZ);
        }

        // -----------------------------------------------------------------------
        // Input Listeners — update the part transform as the user types
        // -----------------------------------------------------------------------

        private void SetupInputListeners() {
            posXInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var p = RootTransform.position; p.x = f; RootTransform.position = p;
            });
            posYInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var p = RootTransform.position; p.y = f; RootTransform.position = p;
            });
            posZInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var p = RootTransform.position; p.z = f; RootTransform.position = p;
            });
            rotXInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var r = RootTransform.eulerAngles; r.x = f; RootTransform.eulerAngles = r;
            });
            rotYInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var r = RootTransform.eulerAngles; r.y = f; RootTransform.eulerAngles = r;
            });
            rotZInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var r = RootTransform.eulerAngles; r.z = f; RootTransform.eulerAngles = r;
            });
        }

        // -----------------------------------------------------------------------
        // Refresh — called every frame while something is selected
        // -----------------------------------------------------------------------

        private void RefreshPartInfo() {
            var root = RootObject;
            if (root == null) return;

            if (root.TryGetComponent(out PartName partName)) {
                partNameText.text = "Part: " + partName.name;

                bool hasP1 = !string.IsNullOrEmpty(partName.param1Label);
                param1Row.SetActive(hasP1);
                if (hasP1) {
                    param1LabelText.text = partName.param1Label + ":";
                    param1ValueText.text = partName.param1Display;
                }

                bool hasP2 = !string.IsNullOrEmpty(partName.param2Label);
                param2Row.SetActive(hasP2);
                if (hasP2) {
                    param2LabelText.text = partName.param2Label + ":";
                    param2ValueText.text = partName.param2Display;
                }
            } else {
                partNameText.text = "Part: " + root.name;
                param1Row.SetActive(false);
                param2Row.SetActive(false);
            }

            partGroupText.text = root.TryGetComponent(out PartType pt)
                ? pt.group.ToString()
                : string.Empty;
        }

        private void RefreshTransformDisplays() {
            var pos = RootTransform.position;
            var rot = RootTransform.eulerAngles;

            // Only update a field if the user isn't currently typing in it
            if (!posXInput.isFocused) posXInput.SetTextWithoutNotify(pos.x.ToString("F3"));
            if (!posYInput.isFocused) posYInput.SetTextWithoutNotify(pos.y.ToString("F3"));
            if (!posZInput.isFocused) posZInput.SetTextWithoutNotify(pos.z.ToString("F3"));
            if (!rotXInput.isFocused) rotXInput.SetTextWithoutNotify(rot.x.ToString("F1"));
            if (!rotYInput.isFocused) rotYInput.SetTextWithoutNotify(rot.y.ToString("F1"));
            if (!rotZInput.isFocused) rotZInput.SetTextWithoutNotify(rot.z.ToString("F1"));
        }

        // -----------------------------------------------------------------------
        // Helpers — selection
        // -----------------------------------------------------------------------

        /// <summary>Root part object (has PartName). Falls back to selected object.</summary>
        private GameObject RootObject {
            get {
                if (!selectedObject.active) return null;
                var pn = selectedObject.obj.GetComponentInParent<PartName>();
                return pn != null ? pn.gameObject : selectedObject.obj;
            }
        }

        private Transform RootTransform {
            get { var r = RootObject; return r != null ? r.transform : selectedObject.tform; }
        }

        // -----------------------------------------------------------------------
        // Helpers — UI creation
        // -----------------------------------------------------------------------

        /// <summary>Creates a named empty GameObject with a RectTransform, parented to parent.</summary>
        private GameObject MakeElement(string name, Transform parent) {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Creates a Text label with a fixed row height.</summary>
        private Text MakeText(string name, Transform parent, string content,
                              int fontSize, bool bold = false) {
            var go = MakeElement(name, parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, fontSize + 7f);
            var t = go.AddComponent<Text>();
            t.text      = content;
            t.font      = uiFont;
            t.fontSize  = fontSize;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.color     = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            return t;
        }

        /// <summary>Creates a thin horizontal divider line.</summary>
        private void MakeDivider(Transform parent) {
            var go  = MakeElement("Divider", parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 1f);
            go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        }

        /// <summary>Creates a two-column row with a dim label and a white value text.</summary>
        private GameObject MakeParamRow(Transform parent, out Text label, out Text value) {
            var row = MakeElement("Param Row", parent);
            SetRowLayout(row, 20f);

            label = MakeText("Label", row.transform, "", 11);
            label.color = DimColor;
            label.GetComponent<RectTransform>().sizeDelta = new Vector2(72f, 20f);

            value = MakeText("Value", row.transform, "", 11);
            value.GetComponent<RectTransform>().sizeDelta = new Vector2(130f, 20f);

            return row;
        }

        /// <summary>Adds a HorizontalLayoutGroup to a row at a fixed height.</summary>
        private void SetRowLayout(GameObject row, float height) {
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, height);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth    = false;
            h.childForceExpandWidth  = false;
            h.childControlHeight   = true;
            h.childForceExpandHeight = true;
            h.spacing              = 4f;
        }

        /// <summary>
        /// Creates a colored axis InputField (e.g. red X, green Y, blue Z).
        /// The InputField has a dark inner box, the axis letter on the left,
        /// and a ScrollableInputField for scroll-wheel support.
        /// </summary>
        private InputField MakeAxisField(string name, Transform parent,
                                         string axisLabel, Color axisColor) {
            // Outer container — colored background
            var container = MakeElement(name, parent);
            container.GetComponent<RectTransform>().sizeDelta = new Vector2(66f, 24f);
            container.AddComponent<Image>().color = axisColor;

            // Axis letter (X / Y / Z) on the left
            var letterGO = MakeElement("Letter", container.transform);
            var letterRT = letterGO.GetComponent<RectTransform>();
            letterRT.anchorMin        = new Vector2(0f, 0f);
            letterRT.anchorMax        = new Vector2(0f, 1f);
            letterRT.pivot            = new Vector2(0f, 0.5f);
            letterRT.anchoredPosition = new Vector2(2f, 0f);
            letterRT.sizeDelta        = new Vector2(14f, 0f);
            var letterText = letterGO.AddComponent<Text>();
            letterText.text      = axisLabel;
            letterText.font      = uiFont;
            letterText.fontSize  = 11;
            letterText.fontStyle = FontStyle.Bold;
            letterText.color     = Color.white;
            letterText.alignment = TextAnchor.MiddleCenter;

            // Dark input box
            var inputGO = MakeElement("Input", container.transform);
            var inputRT = inputGO.GetComponent<RectTransform>();
            inputRT.anchorMin  = Vector2.zero;
            inputRT.anchorMax  = Vector2.one;
            inputRT.offsetMin  = new Vector2(17f, 2f);
            inputRT.offsetMax  = new Vector2(-2f, -2f);
            inputGO.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

            // Text inside the input
            var textGO = MakeElement("Text", inputGO.transform);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(3f, 1f);
            textRT.offsetMax = new Vector2(-2f, -1f);
            var inputText = textGO.AddComponent<Text>();
            inputText.font      = uiFont;
            inputText.fontSize  = 11;
            inputText.color     = Color.white;
            inputText.alignment = TextAnchor.MiddleLeft;

            // Placeholder text
            var phGO = MakeElement("Placeholder", inputGO.transform);
            var phRT = phGO.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(3f, 1f);
            phRT.offsetMax = new Vector2(-2f, -1f);
            var phText = phGO.AddComponent<Text>();
            phText.font      = uiFont;
            phText.fontSize  = 11;
            phText.color     = new Color(0.45f, 0.45f, 0.45f);
            phText.fontStyle = FontStyle.Italic;
            phText.text      = "0.000";

            // InputField component — decimal numbers only
            var inputField = inputGO.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder   = phText;
            inputField.contentType   = InputField.ContentType.DecimalNumber;

            // Scroll-wheel support — nudges value by 0.1 per tick
            inputGO.AddComponent<ScrollableInputField>();

            return inputField;
        }
    }
}
