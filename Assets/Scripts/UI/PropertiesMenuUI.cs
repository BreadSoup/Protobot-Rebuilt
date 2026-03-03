using Parts_List;
using System.Collections.Generic;
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
    /// Moving a part via the position fields moves its whole connected group
    /// (so screws stay attached to their parts).
    /// Scroll wheel on position fields nudges by 0.1; on rotation fields by 5°.
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

        // ---- Colors ----------------------------------------------------------------
        // Axis colors matching the rest of the app (red=X, green=Y, blue=Z)
        private static readonly Color ColorX = new Color(0.75f, 0.22f, 0.22f, 1f);
        private static readonly Color ColorY = new Color(0.20f, 0.65f, 0.20f, 1f);
        private static readonly Color ColorZ = new Color(0.20f, 0.40f, 0.80f, 1f);
        // Panel background — matches the transform buttons / sidebar panels (0.26 gray)
        private static readonly Color PanelBg     = new Color(0.26f, 0.26f, 0.26f, 1f);
        // Header bar — blue accent matching the Preferences panel and toolbar
        private static readonly Color HeaderColor = new Color(0.22f, 0.45f, 0.78f, 1f);
        // Dim / secondary text
        private static readonly Color DimColor   = new Color(0.60f, 0.60f, 0.65f, 1f);
        // Divider lines
        private static readonly Color DividerColor = new Color(1f, 1f, 1f, 0.12f);
        // Dark box inside each input field
        private static readonly Color InputBg     = new Color(0.15f, 0.15f, 0.17f, 1f);
        // Placeholder text
        private static readonly Color PlaceholderColor = new Color(0.45f, 0.45f, 0.50f, 1f);

        // Cached at startup
        private Font   uiFont;
        private Sprite roundedSprite; // the "Rounded Square" 9-slice sprite used project-wide

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

            // Grab the font from whatever Text already exists in the scene
            var existingText = FindObjectOfType<Text>();
            uiFont = existingText != null
                ? existingText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Find the shared "Rounded Square" 9-slice sprite used across all panels.
            // We scan existing Image components rather than hard-coding an asset path.
            foreach (var img in FindObjectsOfType<Image>()) {
                if (img.sprite != null && img.sprite.name.Contains("Rounded")) {
                    roundedSprite = img.sprite;
                    break;
                }
            }

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
            // Find the Screen Space Overlay canvas specifically.
            // The scene has multiple canvases (World Space tool canvases, etc.),
            // so FindObjectOfType<Canvas>() would often return the wrong one and
            // place the panel inside the 3D scene instead of on screen.
            Canvas canvas = null;
            foreach (var c in FindObjectsOfType<Canvas>()) {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay) {
                    canvas = c;
                    break;
                }
            }
            if (canvas == null) {
                Debug.LogError("[PropertiesMenuUI] No Screen Space Overlay Canvas found in the scene. " +
                               "The Properties Menu needs one to display correctly.");
                return;
            }

            // --- Root panel ---
            panel = MakeElement("Properties Menu", canvas.transform);
            var panelRT = panel.GetComponent<RectTransform>();
            // Offset far enough from the top-left to clear the tool icon strip.
            // Anchor = top-left corner of canvas.
            panelRT.anchorMin        = new Vector2(0f, 1f);
            panelRT.anchorMax        = new Vector2(0f, 1f);
            panelRT.pivot            = new Vector2(0f, 1f);
            panelRT.anchoredPosition = new Vector2(115f, -65f);
            panelRT.sizeDelta        = new Vector2(230f, 0f); // width fixed, height auto

            // Background image — rounded 9-slice sprite matching every other panel
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = PanelBg;
            if (roundedSprite != null) {
                panelImg.sprite                  = roundedSprite;
                panelImg.type                    = Image.Type.Sliced;
                panelImg.pixelsPerUnitMultiplier = 10f;
            }

            // Vertical stack — header + content rows stack top-to-bottom
            var vLayout = panel.AddComponent<VerticalLayoutGroup>();
            vLayout.childControlWidth     = true;
            vLayout.childForceExpandWidth  = true;
            vLayout.childControlHeight    = false;
            vLayout.childForceExpandHeight = false;
            vLayout.spacing               = 0f;
            vLayout.padding               = new RectOffset(0, 0, 0, 8);

            // Auto-resize panel height to fit contents
            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ── Blue header bar ──────────────────────────────────────────────
            var header = MakeElement("Header", panel.transform);
            header.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 26f);
            var headerImg = header.AddComponent<Image>();
            headerImg.color = HeaderColor;
            if (roundedSprite != null) {
                headerImg.sprite                  = roundedSprite;
                headerImg.type                    = Image.Type.Sliced;
                headerImg.pixelsPerUnitMultiplier = 10f;
            }
            var headerTitle = MakeElement("Title", header.transform);
            var headerTitleRT = headerTitle.GetComponent<RectTransform>();
            headerTitleRT.anchorMin        = Vector2.zero;
            headerTitleRT.anchorMax        = Vector2.one;
            headerTitleRT.offsetMin        = new Vector2(10f, 0f);
            headerTitleRT.offsetMax        = Vector2.zero;
            var headerText = headerTitle.AddComponent<Text>();
            headerText.text      = "PROPERTIES";
            headerText.font      = uiFont;
            headerText.fontSize  = 11;
            headerText.fontStyle = FontStyle.Bold;
            headerText.color     = Color.white;
            headerText.alignment = TextAnchor.MiddleLeft;

            // ── Content area (padded inner section) ─────────────────────────
            var content = MakeElement("Content", panel.transform);
            content.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 0f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth     = true;
            contentLayout.childForceExpandWidth  = true;
            contentLayout.childControlHeight    = false;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing               = 4f;
            contentLayout.padding               = new RectOffset(8, 8, 6, 0);
            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Part name and group
            partNameText  = MakeText("Part Name",  content.transform, "Part: ---", 13, bold: true);
            partGroupText = MakeText("Part Group", content.transform, "",           11);
            partGroupText.color = DimColor;

            MakeDivider(content.transform);

            // Parameter rows (shown/hidden per part type)
            param1Row = MakeParamRow(content.transform, out param1LabelText, out param1ValueText);
            param2Row = MakeParamRow(content.transform, out param2LabelText, out param2ValueText);

            MakeDivider(content.transform);

            // Position fields
            MakeText("PosHeader", content.transform, "POSITION", 10).color = DimColor;
            var posRow = MakeElement("Pos Row", content.transform);
            SetRowLayout(posRow, 24f);
            posXInput = MakeAxisField("Pos X", posRow.transform, "X", ColorX, 0.1f, "F3");
            posYInput = MakeAxisField("Pos Y", posRow.transform, "Y", ColorY, 0.1f, "F3");
            posZInput = MakeAxisField("Pos Z", posRow.transform, "Z", ColorZ, 0.1f, "F3");

            // Rotation fields — scroll nudges 5° to match ring snap increment
            MakeText("RotHeader", content.transform, "ROTATION", 10).color = DimColor;
            var rotRow = MakeElement("Rot Row", content.transform);
            SetRowLayout(rotRow, 24f);
            rotXInput = MakeAxisField("Rot X", rotRow.transform, "X", ColorX, 5f, "F1");
            rotYInput = MakeAxisField("Rot Y", rotRow.transform, "Y", ColorY, 5f, "F1");
            rotZInput = MakeAxisField("Rot Z", rotRow.transform, "Z", ColorZ, 5f, "F1");
        }

        // -----------------------------------------------------------------------
        // Input Listeners — update the part transform as the user types
        // -----------------------------------------------------------------------

        private void SetupInputListeners() {
            // Position: move the whole connected group so screws stay in their holes
            posXInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var root = RootObject; if (root == null) return;
                MoveGroupBy(new Vector3(f - root.transform.position.x, 0f, 0f), root);
            });
            posYInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var root = RootObject; if (root == null) return;
                MoveGroupBy(new Vector3(0f, f - root.transform.position.y, 0f), root);
            });
            posZInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var root = RootObject; if (root == null) return;
                MoveGroupBy(new Vector3(0f, 0f, f - root.transform.position.z), root);
            });

            // Rotation: sets the selected part's euler angles directly
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

        /// <summary>
        /// Moves all objects in the connected group by delta in world space,
        /// so screws and the parts they're attached to move together.
        /// </summary>
        private void MoveGroupBy(Vector3 delta, GameObject root) {
            if (delta == Vector3.zero) return;
            if (root.TryGetGroup(out List<GameObject> group)) {
                foreach (var obj in group)
                    obj.transform.position += delta;
            } else {
                root.transform.position += delta;
            }
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

        /// <summary>Root part object (has PartName). Falls back to the selected object.</summary>
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
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, fontSize + 8f);
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
            var go = MakeElement("Divider", parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 1f);
            go.AddComponent<Image>().color = DividerColor;
        }

        /// <summary>Creates a two-column row: dim label on the left, white value on the right.</summary>
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
            h.childControlWidth     = false;
            h.childForceExpandWidth  = false;
            h.childControlHeight    = true;
            h.childForceExpandHeight = true;
            h.spacing               = 4f;
        }

        /// <summary>
        /// Creates a colored axis InputField (red X, green Y, or blue Z).
        /// scrollIncrement is how much one scroll tick changes the value
        /// (0.1 for position, 5 for rotation).
        /// </summary>
        private InputField MakeAxisField(string name, Transform parent,
                                         string axisLabel, Color axisColor,
                                         float scrollIncrement, string scrollFormat) {
            // Outer box — colored axis background
            var container = MakeElement(name, parent);
            container.GetComponent<RectTransform>().sizeDelta = new Vector2(66f, 24f);
            container.AddComponent<Image>().color = axisColor;

            // Axis letter (X / Y / Z) pinned to the left
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

            // Dark input box — fills the right portion of the container
            var inputGO = MakeElement("Input", container.transform);
            var inputRT = inputGO.GetComponent<RectTransform>();
            inputRT.anchorMin = Vector2.zero;
            inputRT.anchorMax = Vector2.one;
            inputRT.offsetMin = new Vector2(17f, 2f);
            inputRT.offsetMax = new Vector2(-2f, -2f);
            inputGO.AddComponent<Image>().color = InputBg;

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

            // Placeholder
            var phGO = MakeElement("Placeholder", inputGO.transform);
            var phRT = phGO.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(3f, 1f);
            phRT.offsetMax = new Vector2(-2f, -1f);
            var phText = phGO.AddComponent<Text>();
            phText.font      = uiFont;
            phText.fontSize  = 11;
            phText.color     = PlaceholderColor;
            phText.fontStyle = FontStyle.Italic;
            phText.text      = "0.000";

            // InputField — decimal numbers only
            var inputField = inputGO.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder   = phText;
            inputField.contentType   = InputField.ContentType.DecimalNumber;

            // Scroll-wheel support — different increment for pos vs rot fields
            inputGO.AddComponent<ScrollableInputField>().Init(scrollIncrement, scrollFormat);

            return inputField;
        }
    }
}
