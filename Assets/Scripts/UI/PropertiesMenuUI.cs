using Parts_List;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Protobot;
using Protobot.SelectionSystem;
using Protobot.Tools;

namespace Protobot.UI {
    /// <summary>
    /// The Properties Menu shows part info and editable transform fields for the
    /// currently selected part. Builds its entire UI at runtime — no Inspector
    /// setup needed beyond adding the component to an empty GameObject.
    ///
    /// Features:
    ///  • Shows part name and parameters for the selected part.
    ///  • When a hole is selected it shows data for the part the hole belongs to.
    ///  • Position inputs move the whole connected group (screws stay attached).
    ///  • Rotation inputs: each axis is independent — editing X never changes Y or Z.
    ///  • Scroll wheel: position ±0.1, rotation ±5°.
    ///  • Local Space toggle: switches RotateRing axis math AND sets matchRotation
    ///    on each ring container so the gizmo visually reorients with the object.
    /// </summary>
    public class PropertiesMenuUI : MonoBehaviour {

        // ── Runtime references (all found in Awake, no Inspector wiring) ────────
        private SelectionObjectLink selectedObject;
        private SelectionManager    _selectionManager; // the SAME manager selectedObject reads from
        private Font             uiFont;
        private Sprite           roundedSprite;

        // ── UI element references ────────────────────────────────────────────────
        private GameObject panel;
        private Text       partNameText;
        private GameObject param1Row;
        private Text       param1LabelText, param1ValueText;
        private GameObject param2Row;
        private Text       param2LabelText, param2ValueText;
        private InputField posXInput, posYInput, posZInput;
        private InputField rotXInput, rotYInput, rotZInput;
        private Text       posHeaderText; // updated to "LOCAL / GLOBAL POSITION" on toggle
        private Text       rotHeaderText; // updated to "LOCAL / GLOBAL ROTATION" on toggle
        private Toggle     localToggle;

        // The euler angles WE are managing for the rotation display.
        // We only re-sync from transform.eulerAngles when an EXTERNAL source (ring
        // gizmo, undo, new selection) changes the rotation.  If we set the rotation
        // ourselves via an input field, the actual rotation matches lastKnownRotation
        // and we leave manualEuler alone — so Y/Z never jump due to Unity's euler
        // normalisation (e.g. X=95° being silently converted to (85, 180, 180)).
        private Vector3    manualEuler;
        private Quaternion lastKnownRotation  = Quaternion.identity;
        // In local-space mode we compare localRotation (not world rotation) so
        // that changes to the object's world position (which alter worldRotation
        // only through the parent chain) are not mistaken for external rotations.
        private Quaternion lastKnownLocalRot  = Quaternion.identity;
        // Detects when the selected object changes so we can force a full re-sync
        // of manualEuler instead of trying to find the "closest" representation.
        private GameObject prevSelectedObj    = null;

        // ── Hole-count resizing ──────────────────────────────────────────────────
        // Shown only for aluminum parts that have an AluminumResizeData component.
        private GameObject holeSection;      // parent — hidden when part isn't resizable
        private InputField  holeCountInput;  // shows current / target hole count
        private Button      saveHoleBtn;     // commits the change (✓)
        private Button      flipBtn;         // flips which end is modified
        private Text        flipBtnText;     // label on the flip button

        private bool        resizeFromLeft = true; // which end the preview targets

        // Prevents RefreshHoleSection from resetting the input field while a preview
        // is active.  Set true when a preview starts; false when it is cleared.
        private bool         _holePendingChange   = false;

        // For REMOVAL previews the actual part's MeshFilter is temporarily given a
        // two-submesh mesh (submesh 0 = kept sections, submesh 1 = removed sections)
        // so interior faces (e.g. inside a C-channel) also turn red.  These fields
        // store the originals so ClearHolePreview can restore the part exactly.
        private MeshFilter   _previewMeshFilter   = null;
        private MeshRenderer _previewMeshRenderer = null;
        private Mesh         _previewOriginalMesh = null;
        private Material[]   _previewOriginalMats = null;
        private Mesh         _previewDisplayMesh  = null; // the two-submesh mesh we assigned

        // ── Preview objects (3-D overlays in world space) ────────────────────────
        // Green (addition) ghost mesh + blue edge indicator.
        private readonly System.Collections.Generic.List<GameObject> _previewObjs
            = new System.Collections.Generic.List<GameObject>();
        private GameObject _previewArrow;   // blue + symbol at the modified edge
        private Material   _previewRemoveMat, _previewAddMat, _previewArrowMat;

        // Shaft and plate previews use SEPARATE lists so RefreshHoleSection's
        // ClearHolePreview() cannot accidentally destroy them on every frame.
        private readonly System.Collections.Generic.List<GameObject> _shaftPreviewObjs
            = new System.Collections.Generic.List<GameObject>();
        private readonly System.Collections.Generic.List<GameObject> _platePreviewObjs
            = new System.Collections.Generic.List<GameObject>();

        // ── Child-part variant selector (screw / spacer size picker) ────────────
        // Shown only for parts generated by ChildPartGenerator (has ChildPartResizeData).
        // Uses ◄ / ► buttons to cycle through the generator's param1 options.
        private GameObject childPartSection;     // container row
        private Text        childPartLabelText;  // shows generator's param1.name (e.g. "Size:")
        private Text        childPartValueText;  // displays the current variant value
        private Button      childPartPrevBtn;    // cycles to the previous option
        private Button      childPartNextBtn;    // cycles to the next option

        // ── Plate resizing (length × width) ─────────────────────────────────────
        // Shown only for plate parts that have a PlateResizeData component.
        private GameObject plateSection;          // parent container (VLayout, 3 rows)
        private InputField  plateLengthInput;     // hole count for X axis
        private InputField  plateWidthInput;      // hole count for Y axis
        private Button      savePlateBtn;         // commits both dimensions (✓ Apply)
        private Button      plateLengthFlipBtn;   // flips which X end is fixed
        private Button      plateWidthFlipBtn;    // flips which Y end is fixed
        private Text        plateLengthFlipText;  // label on the length flip button
        private Text        plateWidthFlipText;   // label on the width flip button

        // true = modify from left  (keep right/+X edge fixed)
        // false = modify from right (keep left/−X edge fixed)
        private bool        plateResizeLengthFromLeft  = true;
        // true = modify from bottom (keep top/+Y edge fixed)
        // false = modify from top   (keep bottom/−Y edge fixed)
        private bool        plateResizeWidthFromBottom = true;
        // Prevents RefreshPlateSection from resetting inputs while the user is typing.
        private bool        _platePendingChange = false;

        // ── Shaft length resizing ────────────────────────────────────────────────
        // Shown only for shaft parts that have a ShaftResizeData component.
        private GameObject shaftSection;       // parent — hidden when not a shaft
        private InputField  shaftLengthInput;  // shows current / target length (inches)
        private Button      saveShaftBtn;      // commits the change (✓)
        private Button      shaftFlipBtn;      // flips which end is kept fixed
        private Text        shaftFlipBtnText;  // label on the flip button

        private bool        shaftResizeFromFar = false; // true = keep far (+Z) end fixed
        // Prevents RefreshShaftSection from resetting the input field while the user
        // has typed a new length but not yet confirmed it with ✓.
        private bool        _shaftPendingChange = false;

        // ── Colors (matched to the transform-tool panel family) ──────────────────
        private static readonly Color PanelBg      = new Color(0.14f, 0.14f, 0.14f, 0.749f);
        private static readonly Color HeaderColor   = new Color(0.22f, 0.45f, 0.78f, 1f);
        private static readonly Color DimColor      = new Color(0.58f, 0.58f, 0.62f, 1f);
        private static readonly Color DividerColor  = new Color(1f,    1f,    1f,    0.10f);
        private static readonly Color InputBg       = new Color(0.15f, 0.15f, 0.17f, 1f);
        private static readonly Color PlaceholderCl = new Color(0.42f, 0.42f, 0.46f, 1f);
        private static readonly Color ColorX        = new Color(0.75f, 0.22f, 0.22f, 1f);
        private static readonly Color ColorY        = new Color(0.20f, 0.65f, 0.20f, 1f);
        private static readonly Color ColorZ        = new Color(0.20f, 0.40f, 0.80f, 1f);

        // ── Unity Lifecycle ──────────────────────────────────────────────────────

        private void Awake() {
            selectedObject = FindObjectOfType<SelectionObjectLink>();
            if (selectedObject == null) {
                Debug.LogError("[PropertiesMenuUI] No SelectionObjectLink found.");
                enabled = false;
                return;
            }
            // Get the SelectionManager from the same link that selectedObject.active reads
            // from — NOT from FindObjectOfType<SelectionManager>() which could return any of
            // the multiple managers in the scene (Hover Tags, Hover Hole, Primary) and would
            // set the wrong one, leaving selectedObject.active = false after a child-part swap.
            _selectionManager = selectedObject.Manager;

            // Load Roboto-Regular for the menu text.  Pass true to FindObjectsOfType so
            // inactive GameObjects (e.g. hidden UI panels) are included — otherwise the
            // search can miss the correct font and accidentally pick up a Bold variant.
            uiFont = null;
            foreach (var t in FindObjectsOfType<Text>(true))
                if (t.font != null && t.font.name == "Roboto-Regular") { uiFont = t.font; break; }
            // Fall back to any non-bold Roboto variant, then any Roboto, then any font.
            if (uiFont == null)
                foreach (var t in FindObjectsOfType<Text>(true))
                    if (t.font != null && t.font.name.Contains("Roboto") && !t.font.name.Contains("Bold")) { uiFont = t.font; break; }
            if (uiFont == null)
                foreach (var t in FindObjectsOfType<Text>(true))
                    if (t.font != null && t.font.name.Contains("Roboto")) { uiFont = t.font; break; }
            if (uiFont == null) {
                var first = FindObjectOfType<Text>(true);
                uiFont = first != null ? first.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            // Grab the project-wide rounded 9-slice sprite from any existing Image
            foreach (var img in FindObjectsOfType<Image>()) {
                if (img.sprite != null && img.sprite.name.Contains("Rounded")) {
                    roundedSprite = img.sprite;
                    break;
                }
            }

            BuildUI();
            SetupListeners();

            // On startup, sync all ring containers to the current IsLocalSpace value
            // (the prefab ships with matchRotation=1, so we reset to match the default false).
            SyncRingLocalSpace(RotateRing.IsLocalSpace);

            // Create transparent materials used by the 3-D resize preview.
            _previewRemoveMat = MakeTransparentMat(new Color(0.90f, 0.15f, 0.15f, 0.40f));
            _previewAddMat    = MakeTransparentMat(new Color(0.15f, 0.85f, 0.15f, 0.40f));
            _previewArrowMat  = MakeTransparentMat(new Color(0.20f, 0.50f, 1.00f, 0.85f));
        }

        private void Update() {
            if (panel == null) return;

            bool show = selectedObject.active;
            panel.SetActive(show);
            if (!show) {
                // Remove all 3-D resize overlays and reset any pending-change flags so
                // the next selection always shows fresh, correctly-sized input values.
                ClearHolePreview();
                ClearShaftPreview();
                ClearPlatePreview();
                _shaftPendingChange = false;
                _platePendingChange = false;
                return;
            }

            // Reset pending-change flags when the selected object changes so the
            // newly selected part always shows its own actual dimensions instead
            // of carrying over unsaved edits from the previously selected part.
            // (prevSelectedObj is updated inside RefreshTransform, so it still
            // holds the previous frame's object here — perfect for comparison.)
            if (RootObject != prevSelectedObj) {
                _platePendingChange = false;
                _shaftPendingChange = false;
                ClearPlatePreview();
                ClearShaftPreview();
            }

            RefreshInfo();
            RefreshTransform();
            RefreshHoleSection();
            RefreshShaftSection();
            RefreshPlateSection();
            RefreshChildPartSection();
        }

        // ── RootObject resolution ────────────────────────────────────────────────

        /// <summary>
        /// Returns the root part GameObject to display.
        /// • If a HoleFace (world-space overlay) is selected, uses its stored
        ///   HoleData.part reference so the part's data is shown, not the hole's.
        /// • For everything else, walks up the hierarchy to find PartName.
        /// </summary>
        private GameObject RootObject {
            get {
                if (!selectedObject.active) return null;
                var obj = selectedObject.obj;

                // HoleFace is a world-space overlay not parented to the part,
                // so we must use its stored part reference directly.
                if (obj.TryGetComponent(out HoleFace holeFace) && holeFace.hole?.part != null)
                    return holeFace.hole.part;

                // HoleCollider IS a child of the part, but walk up anyway for safety.
                var pn = obj.GetComponentInParent<PartName>();
                return pn != null ? pn.gameObject : obj;
            }
        }

        private Transform RootTransform {
            get { var r = RootObject; return r != null ? r.transform : selectedObject.tform; }
        }

        // ── UI Construction ──────────────────────────────────────────────────────

        private void BuildUI() {
            // ── Find the Screen Space Overlay canvas ─────────────────────────
            Canvas canvas = null;
            foreach (var c in FindObjectsOfType<Canvas>())
                if (c.renderMode == RenderMode.ScreenSpaceOverlay) { canvas = c; break; }
            if (canvas == null) {
                Debug.LogError("[PropertiesMenuUI] No Screen Space Overlay Canvas found.");
                return;
            }

            // ── Root panel ───────────────────────────────────────────────────
            panel = MakeEl("Properties Menu", canvas.transform);
            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin        = new Vector2(0f, 0f); //changes where UI is anchored
            panelRT.anchorMax        = new Vector2(0f, 0f);
            panelRT.pivot            = new Vector2(0f, 0f);
            panelRT.anchoredPosition = new Vector2(20f, 20f); // position set by user
            panelRT.sizeDelta        = new Vector2(210f, 0f);

            ApplyPanelImage(panel, PanelBg);
            AddVLayout(panel, new RectOffset(0, 0, 0, 6), 0f, autoHeight: true);

            // ── Header bar ───────────────────────────────────────────────────
            var header = MakeEl("Header", panel.transform);
            header.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 22f);
            ApplyPanelImage(header, HeaderColor);

            var headerLabel = MakeEl("Label", header.transform);
            var hlRT = headerLabel.GetComponent<RectTransform>();
            hlRT.anchorMin = Vector2.zero; hlRT.anchorMax = Vector2.one;
            hlRT.offsetMin = new Vector2(8f, 0f); hlRT.offsetMax = Vector2.zero;
            MakeTextOn(headerLabel, "PROPERTIES", 10, bold: true).alignment = TextAnchor.MiddleLeft;

            // ── Content section ──────────────────────────────────────────────
            var content = MakeEl("Content", panel.transform);
            AddVLayout(content, new RectOffset(7, 7, 4, 0), 3f, autoHeight: true);

            // Part name (size 12 is already visually distinct — no bold needed)
            partNameText = MakeText("Name", content.transform, "---", 12);

            // Param rows (hidden when empty)
            param1Row = MakeParamRow(content.transform, out param1LabelText, out param1ValueText);
            param2Row = MakeParamRow(content.transform, out param2LabelText, out param2ValueText);

            // Hole-count editor sits right where the "Length: N holes" param row is.
            // When a resizable part is selected, param2Row is hidden and holeSection shows.
            BuildHoleSection(content.transform);

            // Shaft length editor — mirrors the hole section but for shaft parts.
            // Shown only when a ShaftResizeData component is present.
            BuildShaftSection(content.transform);

            // Plate length × width editor — shown only when a PlateResizeData component is present.
            BuildPlateSection(content.transform);

            // Child-part variant selector — shown for screws, spacers, etc. (ChildPartResizeData).
            BuildChildPartSection(content.transform);

            MakeDivider(content.transform);

            // ── Position ─────────────────────────────────────────────────────
            posHeaderText = MakeText("PH", content.transform, "GLOBAL POSITION", 9);
            posHeaderText.color = DimColor;
            var posRow = MakeEl("PosRow", content.transform);
            SetHRow(posRow, 20f);
            posXInput = MakeAxisField("PX", posRow.transform, "X", ColorX, 0.1f, "F3");
            posYInput = MakeAxisField("PY", posRow.transform, "Y", ColorY, 0.1f, "F3");
            posZInput = MakeAxisField("PZ", posRow.transform, "Z", ColorZ, 0.1f, "F3");

            // ── Rotation ─────────────────────────────────────────────────────
            rotHeaderText = MakeText("RH", content.transform, "GLOBAL ROTATION", 9);
            rotHeaderText.color = DimColor;
            var rotRow = MakeEl("RotRow", content.transform);
            SetHRow(rotRow, 20f);
            rotXInput = MakeAxisField("RX", rotRow.transform, "X", ColorX, 5f, "F1");
            rotYInput = MakeAxisField("RY", rotRow.transform, "Y", ColorY, 5f, "F1");
            rotZInput = MakeAxisField("RZ", rotRow.transform, "Z", ColorZ, 5f, "F1");

            MakeDivider(content.transform);

            // ── Local / Global toggle ─────────────────────────────────────────
            var toggleRow = MakeEl("ToggleRow", content.transform);
            SetHRow(toggleRow, 18f);

            var toggleLabel = MakeEl("TL", toggleRow.transform);
            toggleLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 18f);
            var tlt = toggleLabel.AddComponent<Text>();
            tlt.text = "Local Space"; tlt.font = uiFont; tlt.fontSize = 10;
            tlt.color = Color.white; tlt.alignment = TextAnchor.MiddleLeft;

            localToggle = MakeToggle("Toggle", toggleRow.transform);
            localToggle.isOn = RotateRing.IsLocalSpace;
            localToggle.onValueChanged.AddListener(val => {
                RotateRing.IsLocalSpace = val;
                // Reorient the ring gizmo containers immediately.
                SyncRingLocalSpace(val);
                // Switch the rotation display between local and global euler angles
                // right away so the fields reflect the new interpretation instantly.
                if (selectedObject.active) {
                    manualEuler = val
                        ? RootTransform.localEulerAngles
                        : RootTransform.eulerAngles;
                    lastKnownRotation = RootTransform.rotation;
                    lastKnownLocalRot = RootTransform.localRotation;
                }
                posHeaderText.text = val ? "LOCAL POSITION" : "GLOBAL POSITION";
                rotHeaderText.text = val ? "LOCAL ROTATION"  : "GLOBAL ROTATION";
            });

        }

        // ── Listeners ────────────────────────────────────────────────────────────

        private void SetupListeners() {
            // Position: in GLOBAL mode move along world axes (absolute world coords).
            //           in LOCAL  mode move along the object's own local axes.
            //           The displayed value in local mode is the projection of world
            //           position onto that local axis, so the delta is computed the
            //           same way in both modes — field value minus current displayed value.
            posXInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var root = RootObject; if (root == null) return;
                if (RotateRing.IsLocalSpace) {
                    float cur = Vector3.Dot(root.transform.position, root.transform.right);
                    MoveGroupBy((f - cur) * root.transform.right, root);
                } else {
                    MoveGroupBy(new Vector3(f - root.transform.position.x, 0f, 0f), root);
                }
            });
            posYInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var root = RootObject; if (root == null) return;
                if (RotateRing.IsLocalSpace) {
                    float cur = Vector3.Dot(root.transform.position, root.transform.up);
                    MoveGroupBy((f - cur) * root.transform.up, root);
                } else {
                    MoveGroupBy(new Vector3(0f, f - root.transform.position.y, 0f), root);
                }
            });
            posZInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                var root = RootObject; if (root == null) return;
                if (RotateRing.IsLocalSpace) {
                    float cur = Vector3.Dot(root.transform.position, root.transform.forward);
                    MoveGroupBy((f - cur) * root.transform.forward, root);
                } else {
                    MoveGroupBy(new Vector3(0f, 0f, f - root.transform.position.z), root);
                }
            });

            // Rotation: both modes use an incremental DELTA applied via Transform.Rotate
            // so each field genuinely rotates around a single axis.
            //   Local  (Space.Self)  — rotates around the object's own current axis.
            //   Global (Space.World) — rotates around the world axis.
            // Using Quaternion.Euler(manualEuler) would cause ZXY-order artifacts:
            // Z is always innermost so it always looks local, Y is outermost and always
            // looks global. The delta approach avoids this entirely.
            rotXInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                float d = f - manualEuler.x; manualEuler.x = f; ApplyDelta(d, 0f, 0f);
            });
            rotYInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                float d = f - manualEuler.y; manualEuler.y = f; ApplyDelta(0f, d, 0f);
            });
            rotZInput.onValueChanged.AddListener(val => {
                if (!selectedObject.active || !float.TryParse(val, out float f)) return;
                float d = f - manualEuler.z; manualEuler.z = f; ApplyDelta(0f, 0f, d);
            });

            // Hole count — preview updates on every keystroke; changes only apply
            // when the user clicks the green ✓ save button.
            holeCountInput.onValueChanged.AddListener(OnHoleCountChanged);

            saveHoleBtn.onClick.AddListener(OnHoleSave);
            flipBtn.onClick.AddListener(OnFlipSide);

            // Shaft length — changes only apply when the user clicks ✓.
            shaftLengthInput.onValueChanged.AddListener(OnShaftLengthChanged);
            saveShaftBtn.onClick.AddListener(OnShaftSave);
            shaftFlipBtn.onClick.AddListener(OnShaftFlipSide);

            // Plate dimensions — changes only apply when the user clicks ✓ Apply.
            plateLengthInput.onValueChanged.AddListener(OnPlateLengthChanged);
            plateWidthInput.onValueChanged.AddListener(OnPlateWidthChanged);
            savePlateBtn.onClick.AddListener(OnPlateSave);
            plateLengthFlipBtn.onClick.AddListener(OnPlateLengthFlip);
            plateWidthFlipBtn.onClick.AddListener(OnPlateWidthFlip);

            // Child-part size selector — ◄ cycles back, ► cycles forward through options.
            childPartPrevBtn.onClick.AddListener(() => OnChildPartCycle(-1));
            childPartNextBtn.onClick.AddListener(() => OnChildPartCycle(+1));
        }

        // ── Refresh ──────────────────────────────────────────────────────────────

        private void RefreshInfo() {
            var root = RootObject;
            if (root == null) return;

            if (root.TryGetComponent(out PartName pn)) {
                partNameText.text = pn.name;

                bool h1 = !string.IsNullOrEmpty(pn.param1Label);
                param1Row.SetActive(h1);
                if (h1) { param1LabelText.text = pn.param1Label + ":"; param1ValueText.text = pn.param1Display; }

                bool h2 = !string.IsNullOrEmpty(pn.param2Label);
                param2Row.SetActive(h2);
                if (h2) { param2LabelText.text = pn.param2Label + ":"; param2ValueText.text = pn.param2Display; }
            } else {
                partNameText.text = root.name;
                param1Row.SetActive(false);
                param2Row.SetActive(false);
            }
        }

        private void RefreshTransform() {
            var pos = RootTransform.position;

            // In local mode the position fields show the projection of world position
            // onto the object's own local axes (right, up, forward).  This matches
            // the movement direction used by the listeners, so scroll increments and
            // absolute typed values are consistent with what's displayed.
            // In global mode the fields show plain world X/Y/Z as before.
            if (RotateRing.IsLocalSpace) {
                var t = RootTransform;
                if (!posXInput.isFocused) posXInput.SetTextWithoutNotify(Vector3.Dot(pos, t.right  ).ToString("F3"));
                if (!posYInput.isFocused) posYInput.SetTextWithoutNotify(Vector3.Dot(pos, t.up     ).ToString("F3"));
                if (!posZInput.isFocused) posZInput.SetTextWithoutNotify(Vector3.Dot(pos, t.forward).ToString("F3"));
            } else {
                if (!posXInput.isFocused) posXInput.SetTextWithoutNotify(pos.x.ToString("F3"));
                if (!posYInput.isFocused) posYInput.SetTextWithoutNotify(pos.y.ToString("F3"));
                if (!posZInput.isFocused) posZInput.SetTextWithoutNotify(pos.z.ToString("F3"));
            }

            // ── Detect new object selection ──────────────────────────────────
            // When the selected object changes we must force a full re-sync of
            // manualEuler from the transform (no "closest" attempt needed because
            // we have no prior manualEuler to stay near).
            var  currentObj  = RootObject;
            bool newSel      = currentObj != prevSelectedObj;
            prevSelectedObj  = currentObj;

            // ── Detect external rotation change ──────────────────────────────
            // In LOCAL mode compare localRotation so a parent position change
            // (which shifts world rotation through the hierarchy) is NOT falsely
            // treated as an external rotation. Without this, every time the ring
            // gizmo's DOTween tween ticks the world quaternion changes slightly,
            // triggering a re-sync and corrupting manualEuler via localEulerAngles
            // normalisation (e.g. X=95° → (85, 180, 180)).
            bool externalChange;
            if (RotateRing.IsLocalSpace) {
                externalChange = newSel ||
                                 Quaternion.Angle(RootTransform.localRotation, lastKnownLocalRot) > 0.01f;
                lastKnownLocalRot = RootTransform.localRotation;
            } else {
                externalChange = newSel ||
                                 Quaternion.Angle(RootTransform.rotation, lastKnownRotation) > 0.01f;
            }
            lastKnownRotation = RootTransform.rotation;

            if (externalChange) {
                Vector3 raw = RotateRing.IsLocalSpace
                    ? RootTransform.localEulerAngles
                    : RootTransform.eulerAngles;
                // On new selection take the raw euler as-is (no previous manualEuler
                // to compare against). On external-change mid-edit, pick whichever of
                // Unity's two valid representations is closest to what we were already
                // showing — this avoids the 90°/270° normalisation flip.
                manualEuler = newSel ? raw : ClosestEuler(raw);
            }

            if (!rotXInput.isFocused) rotXInput.SetTextWithoutNotify(manualEuler.x.ToString("F1"));
            if (!rotYInput.isFocused) rotYInput.SetTextWithoutNotify(manualEuler.y.ToString("F1"));
            if (!rotZInput.isFocused) rotZInput.SetTextWithoutNotify(manualEuler.z.ToString("F1"));
        }

        // ── Rotation application ─────────────────────────────────────────────────

        /// <summary>
        /// Applies manualEuler to the transform in whichever space matches the
        /// current toggle state, then snapshots the resulting quaternion(s) so
        /// RefreshTransform knows the change came from us (not an external source).
        ///
        /// We snapshot BOTH lastKnownLocalRot AND lastKnownRotation every time so
        /// that switching modes mid-edit never leaves a stale snapshot.
        /// </summary>
        private void ApplyManualEuler() {
            if (RotateRing.IsLocalSpace)
                RootTransform.localRotation = Quaternion.Euler(manualEuler);
            else
                RootTransform.rotation = Quaternion.Euler(manualEuler);
            // Always update both snapshots — keeps them in sync regardless of mode.
            lastKnownRotation = RootTransform.rotation;
            lastKnownLocalRot = RootTransform.localRotation;
        }

        /// <summary>
        /// Applies an incremental rotation around a single axis, in the space
        /// that matches the current toggle state:
        ///   Local  (Space.Self)  — rotates around the object's own local axis.
        ///   Global (Space.World) — rotates around the world axis.
        ///
        /// Passing only one non-zero component gives a pure single-axis rotation
        /// with no ZXY-order cross-contamination (which Quaternion.Euler would cause).
        ///
        /// After rotating, both quaternion snapshots are updated so RefreshTransform
        /// won't mistake this self-applied change for an external rotation.
        /// </summary>
        private void ApplyDelta(float dx, float dy, float dz) {
            Space space = RotateRing.IsLocalSpace ? Space.Self : Space.World;
            RootTransform.Rotate(dx, dy, dz, space);
            lastKnownRotation = RootTransform.rotation;
            lastKnownLocalRot = RootTransform.localRotation;
        }

        // ── Euler helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Unity's eulerAngles / localEulerAngles can represent the same rotation
        /// in two different ways (e.g. (95, 0, 0) ↔ (85, 180, 180)).  When we
        /// detect an external rotation change and re-read the transform we pick
        /// whichever representation is numerically closest to our current manualEuler.
        /// This prevents the display from jumping when the user scrolls an axis
        /// near 90° or 270°.
        /// </summary>
        private Vector3 ClosestEuler(Vector3 raw) {
            // Unity's "supplement" form of the same quaternion.
            Vector3 alt = new Vector3(
                Mathf.Repeat(180f - raw.x, 360f),
                Mathf.Repeat(raw.y + 180f, 360f),
                Mathf.Repeat(raw.z + 180f, 360f));
            return EulerDist(raw, manualEuler) <= EulerDist(alt, manualEuler) ? raw : alt;
        }

        private static float EulerDist(Vector3 a, Vector3 b) {
            return Mathf.Abs(Mathf.DeltaAngle(a.x, b.x))
                 + Mathf.Abs(Mathf.DeltaAngle(a.y, b.y))
                 + Mathf.Abs(Mathf.DeltaAngle(a.z, b.z));
        }

        // ── Group movement ───────────────────────────────────────────────────────

        private void MoveGroupBy(Vector3 delta, GameObject root) {
            if (delta == Vector3.zero) return;
            if (root.TryGetGroup(out List<GameObject> group))
                foreach (var obj in group) obj.transform.position += delta;
            else
                root.transform.position += delta;
        }

        // ── Local / Global sync ──────────────────────────────────────────────────

        /// <summary>
        /// Finds every RotateRing in the scene and sets matchRotation on its parent
        /// MatchObjectLinkTransform component.
        ///
        /// isLocal = true  → ring container follows the object's rotation (local gizmo).
        /// isLocal = false → ring container stays world-aligned (global gizmo).
        ///
        /// This is the component whose matchRotation=1 flag in the prefab causes
        /// the rings to visually track the object's orientation.
        /// </summary>
        private void SyncRingLocalSpace(bool isLocal) {
            foreach (var ring in FindObjectsOfType<RotateRing>()) {
                var matcher = ring.GetComponentInParent<MatchObjectLinkTransform>();
                if (matcher == null) continue;

                matcher.matchRotation = isLocal;

                // When switching back to global: the ring container still has the
                // object's local rotation baked in from the last local-mode update.
                // Reset it to identity so the rings are world-aligned again.
                if (!isLocal)
                    matcher.transform.rotation = Quaternion.identity;
            }
        }

        // ── UI helpers ───────────────────────────────────────────────────────────

        private GameObject MakeEl(string name, Transform parent) {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Applies the rounded panel sprite + color to an Image on go.</summary>
        private void ApplyPanelImage(GameObject go, Color color) {
            var img = go.AddComponent<Image>();
            img.color = color;
            if (roundedSprite != null) {
                img.sprite                  = roundedSprite;
                img.type                    = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 10f;
            }
        }

        private void AddVLayout(GameObject go, RectOffset padding, float spacing, bool autoHeight) {
            var vl = go.AddComponent<VerticalLayoutGroup>();
            vl.childControlWidth      = true;
            vl.childForceExpandWidth   = true;
            vl.childControlHeight     = false;
            vl.childForceExpandHeight  = false;
            vl.padding                = padding;
            vl.spacing                = spacing;
            if (autoHeight) {
                var csf = go.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private Text MakeText(string name, Transform parent, string content,
                              int size, bool bold = false) {
            var go = MakeEl(name, parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, size + 7f);
            return MakeTextOn(go, content, size, bold);
        }

        private Text MakeTextOn(GameObject go, string content, int size, bool bold = false) {
            var t = go.AddComponent<Text>();
            t.text      = content;
            t.font      = uiFont;
            t.fontSize  = size;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.color     = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            return t;
        }

        private void MakeDivider(Transform parent) {
            var go = MakeEl("Div", parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 1f);
            go.AddComponent<Image>().color = DividerColor;
        }

        private GameObject MakeParamRow(Transform parent, out Text label, out Text value) {
            var row = MakeEl("PR", parent);
            SetHRow(row, 18f);

            label = MakeText("L", row.transform, "", 10);
            label.color = DimColor;
            label.GetComponent<RectTransform>().sizeDelta = new Vector2(68f, 18f);

            value = MakeText("V", row.transform, "", 10);
            value.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 18f);

            return row;
        }

        private void SetHRow(GameObject go, float height) {
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, height);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth      = false;
            h.childForceExpandWidth   = false;
            h.childControlHeight     = true;
            h.childForceExpandHeight  = true;
            h.spacing                = 3f;
        }

        private InputField MakeAxisField(string name, Transform parent,
                                          string axis, Color axisColor,
                                          float scrollInc, string scrollFmt) {
            var container = MakeEl(name, parent);
            container.GetComponent<RectTransform>().sizeDelta = new Vector2(60f, 20f);
            container.AddComponent<Image>().color = axisColor;

            // Axis letter
            var letter = MakeEl("L", container.transform);
            var lRT = letter.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0f, 0f); lRT.anchorMax = new Vector2(0f, 1f);
            lRT.pivot = new Vector2(0f, 0.5f);
            lRT.anchoredPosition = new Vector2(2f, 0f);
            lRT.sizeDelta = new Vector2(12f, 0f);
            var lt = letter.AddComponent<Text>();
            lt.text = axis; lt.font = uiFont; lt.fontSize = 10;
            lt.fontStyle = FontStyle.Normal; lt.color = Color.white;
            lt.alignment = TextAnchor.MiddleCenter;

            // Input box
            var inputGO = MakeEl("I", container.transform);
            var iRT = inputGO.GetComponent<RectTransform>();
            iRT.anchorMin = Vector2.zero; iRT.anchorMax = Vector2.one;
            iRT.offsetMin = new Vector2(15f, 2f); iRT.offsetMax = new Vector2(-2f, -2f);
            inputGO.AddComponent<Image>().color = InputBg;

            // Text
            var textGO = MakeEl("T", inputGO.transform);
            var tRT = textGO.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = new Vector2(3f, 1f); tRT.offsetMax = new Vector2(-2f, -1f);
            var inputText = textGO.AddComponent<Text>();
            inputText.font = uiFont; inputText.fontSize = 10;
            inputText.color = Color.white; inputText.alignment = TextAnchor.MiddleLeft;

            // Placeholder
            var phGO = MakeEl("P", inputGO.transform);
            var phRT = phGO.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(3f, 1f); phRT.offsetMax = new Vector2(-2f, -1f);
            var phText = phGO.AddComponent<Text>();
            phText.font = uiFont; phText.fontSize = 10;
            phText.color = PlaceholderCl; phText.fontStyle = FontStyle.Italic;
            phText.text = "0";

            var inputField = inputGO.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder   = phText;
            inputField.contentType   = InputField.ContentType.DecimalNumber;

            inputGO.AddComponent<ScrollableInputField>().Init(scrollInc, scrollFmt);

            return inputField;
        }

        // ── Hole-count section ────────────────────────────────────────────────────

        /// <summary>
        /// Builds the HOLES UI block: a header, an integer input field, a flip
        /// button, and a small hint label showing which end will be modified.
        /// The whole block starts hidden and is shown only for resizable parts.
        /// </summary>
        private void BuildHoleSection(Transform parent) {
            // The hole section replaces param2Row in the UI hierarchy.
            // It is a single row: [Length:] [count input] [✓] [← left / right →]
            // widths: label 68 + input 46 + spacing 3 + save 22 + spacing 3 + flip 48 = 190
            holeSection = MakeEl("HoleSection", parent);
            SetHRow(holeSection, 18f);

            // "Length:" label (matches style of other param labels)
            var lbl = MakeText("HL", holeSection.transform, "Length:", 10);
            lbl.color = DimColor;
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(52f, 18f);

            // Integer input field
            var fieldContainer = MakeEl("HFC", holeSection.transform);
            fieldContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(46f, 18f);
            ApplyPanelImage(fieldContainer, InputBg);

            var ftGO = MakeEl("FT", fieldContainer.transform);
            var ftRT = ftGO.GetComponent<RectTransform>();
            ftRT.anchorMin = Vector2.zero; ftRT.anchorMax = Vector2.one;
            ftRT.offsetMin = new Vector2(3f, 1f); ftRT.offsetMax = new Vector2(-2f, -1f);
            var ftText = ftGO.AddComponent<Text>();
            ftText.font = uiFont; ftText.fontSize = 10;
            ftText.color = Color.white; ftText.alignment = TextAnchor.MiddleLeft;

            var fphGO = MakeEl("FP", fieldContainer.transform);
            var fphRT = fphGO.GetComponent<RectTransform>();
            fphRT.anchorMin = Vector2.zero; fphRT.anchorMax = Vector2.one;
            fphRT.offsetMin = new Vector2(3f, 1f); fphRT.offsetMax = new Vector2(-2f, -1f);
            var fphText = fphGO.AddComponent<Text>();
            fphText.font = uiFont; fphText.fontSize = 10;
            fphText.color = PlaceholderCl; fphText.fontStyle = FontStyle.Italic;
            fphText.text = "0";

            holeCountInput = fieldContainer.AddComponent<InputField>();
            holeCountInput.textComponent = ftText;
            holeCountInput.placeholder   = fphText;
            holeCountInput.contentType   = InputField.ContentType.IntegerNumber;
            fieldContainer.AddComponent<ScrollableInputField>().Init(1f, "F0");

            // ✓ Save button — commits the typed count to the real part
            var saveGO = MakeEl("SaveBtn", holeSection.transform);
            saveGO.GetComponent<RectTransform>().sizeDelta = new Vector2(22f, 18f);
            ApplyPanelImage(saveGO, new Color(0.18f, 0.52f, 0.18f, 1f));
            var saveTxt = MakeEl("ST", saveGO.transform).AddComponent<Text>();
            saveTxt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            saveTxt.GetComponent<RectTransform>().anchorMax = Vector2.one;
            saveTxt.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            saveTxt.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            saveTxt.font = uiFont; saveTxt.fontSize = 12;
            saveTxt.color = Color.white; saveTxt.alignment = TextAnchor.MiddleCenter;
            saveTxt.text = "✓";
            saveHoleBtn = saveGO.AddComponent<Button>();
            saveHoleBtn.targetGraphic = saveGO.GetComponent<Image>();
            var scb = ColorBlock.defaultColorBlock;
            scb.normalColor      = new Color(0.18f, 0.52f, 0.18f, 1f);
            scb.highlightedColor = new Color(0.24f, 0.66f, 0.24f, 1f);
            scb.pressedColor     = new Color(0.13f, 0.38f, 0.13f, 1f);
            scb.selectedColor    = scb.normalColor;
            saveHoleBtn.colors   = scb;

            // ← left / right → flip button
            var flipGO = MakeEl("FlipBtn", holeSection.transform);
            flipGO.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 18f);
            ApplyPanelImage(flipGO, new Color(0.25f, 0.25f, 0.30f, 1f));
            flipBtnText = MakeEl("FBT", flipGO.transform).AddComponent<Text>();
            flipBtnText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            flipBtnText.GetComponent<RectTransform>().anchorMax = Vector2.one;
            flipBtnText.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            flipBtnText.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            flipBtnText.font = uiFont; flipBtnText.fontSize = 8;
            flipBtnText.color = Color.white; flipBtnText.alignment = TextAnchor.MiddleCenter;
            flipBtnText.text = "← left";
            flipBtn = flipGO.AddComponent<Button>();
            flipBtn.targetGraphic = flipGO.GetComponent<Image>();
            var fcb = ColorBlock.defaultColorBlock;
            fcb.normalColor      = new Color(0.25f, 0.25f, 0.30f, 1f);
            fcb.highlightedColor = new Color(0.35f, 0.35f, 0.42f, 1f);
            fcb.pressedColor     = new Color(0.20f, 0.20f, 0.25f, 1f);
            fcb.selectedColor    = fcb.normalColor;
            flipBtn.colors       = fcb;

            holeSection.SetActive(false);
        }

        /// <summary>
        /// Builds the SHAFT LENGTH UI block: a decimal input field, a ✓ save button,
        /// and a flip button that picks which end of the shaft stays fixed.
        /// Starts hidden — shown only when a ShaftResizeData part is selected.
        /// </summary>
        private void BuildShaftSection(Transform parent) {
            shaftSection = MakeEl("ShaftSection", parent);
            SetHRow(shaftSection, 18f);

            // "Length:" label
            var lbl = MakeText("ShL", shaftSection.transform, "Length:", 10);
            lbl.color = DimColor;
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(52f, 18f);

            // Decimal input field (length in inches)
            var fc = MakeEl("ShFC", shaftSection.transform);
            fc.GetComponent<RectTransform>().sizeDelta = new Vector2(46f, 18f);
            ApplyPanelImage(fc, InputBg);

            var ftGO = MakeEl("FT", fc.transform);
            var ftRT = ftGO.GetComponent<RectTransform>();
            ftRT.anchorMin = Vector2.zero; ftRT.anchorMax = Vector2.one;
            ftRT.offsetMin = new Vector2(3f, 1f); ftRT.offsetMax = new Vector2(-2f, -1f);
            var ftText = ftGO.AddComponent<Text>();
            ftText.font = uiFont; ftText.fontSize = 10;
            ftText.color = Color.white; ftText.alignment = TextAnchor.MiddleLeft;

            var fphGO = MakeEl("FP", fc.transform);
            var fphRT = fphGO.GetComponent<RectTransform>();
            fphRT.anchorMin = Vector2.zero; fphRT.anchorMax = Vector2.one;
            fphRT.offsetMin = new Vector2(3f, 1f); fphRT.offsetMax = new Vector2(-2f, -1f);
            var fphText = fphGO.AddComponent<Text>();
            fphText.font = uiFont; fphText.fontSize = 10;
            fphText.color = PlaceholderCl; fphText.fontStyle = FontStyle.Italic;
            fphText.text = "0";

            shaftLengthInput = fc.AddComponent<InputField>();
            shaftLengthInput.textComponent = ftText;
            shaftLengthInput.placeholder   = fphText;
            shaftLengthInput.contentType   = InputField.ContentType.DecimalNumber;
            fc.AddComponent<ScrollableInputField>().Init(0.5f, "F1");

            // ✓ Save button
            var saveGO = MakeEl("ShSaveBtn", shaftSection.transform);
            saveGO.GetComponent<RectTransform>().sizeDelta = new Vector2(22f, 18f);
            ApplyPanelImage(saveGO, new Color(0.18f, 0.52f, 0.18f, 1f));
            var saveTxt = MakeEl("ST", saveGO.transform).AddComponent<Text>();
            saveTxt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            saveTxt.GetComponent<RectTransform>().anchorMax = Vector2.one;
            saveTxt.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            saveTxt.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            saveTxt.font = uiFont; saveTxt.fontSize = 12;
            saveTxt.color = Color.white; saveTxt.alignment = TextAnchor.MiddleCenter;
            saveTxt.text = "✓";
            saveShaftBtn = saveGO.AddComponent<Button>();
            saveShaftBtn.targetGraphic = saveGO.GetComponent<Image>();
            var sscb = ColorBlock.defaultColorBlock;
            sscb.normalColor      = new Color(0.18f, 0.52f, 0.18f, 1f);
            sscb.highlightedColor = new Color(0.24f, 0.66f, 0.24f, 1f);
            sscb.pressedColor     = new Color(0.13f, 0.38f, 0.13f, 1f);
            sscb.selectedColor    = sscb.normalColor;
            saveShaftBtn.colors   = sscb;

            // "far →"  = near (−Z) end stays fixed, shaft grows/shrinks at far end.
            // "← near" = far  (+Z) end stays fixed, shaft grows/shrinks at near end.
            var sfGO = MakeEl("ShFlipBtn", shaftSection.transform);
            sfGO.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 18f);
            ApplyPanelImage(sfGO, new Color(0.25f, 0.25f, 0.30f, 1f));
            shaftFlipBtnText = MakeEl("FBT", sfGO.transform).AddComponent<Text>();
            shaftFlipBtnText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            shaftFlipBtnText.GetComponent<RectTransform>().anchorMax = Vector2.one;
            shaftFlipBtnText.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            shaftFlipBtnText.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            shaftFlipBtnText.font = uiFont; shaftFlipBtnText.fontSize = 8;
            shaftFlipBtnText.color = Color.white;
            shaftFlipBtnText.alignment = TextAnchor.MiddleCenter;
            shaftFlipBtnText.text = "far →";
            shaftFlipBtn = sfGO.AddComponent<Button>();
            shaftFlipBtn.targetGraphic = sfGO.GetComponent<Image>();
            var sfcb = ColorBlock.defaultColorBlock;
            sfcb.normalColor      = new Color(0.25f, 0.25f, 0.30f, 1f);
            sfcb.highlightedColor = new Color(0.35f, 0.35f, 0.42f, 1f);
            sfcb.pressedColor     = new Color(0.20f, 0.20f, 0.25f, 1f);
            sfcb.selectedColor    = sfcb.normalColor;
            shaftFlipBtn.colors   = sfcb;

            shaftSection.SetActive(false);
        }

        /// <summary>
        /// Builds the PLATE DIMENSIONS UI block: a Length row (input + flip),
        /// a Width row (input + flip), and a single ✓ Apply button that commits
        /// both axes at once. Starts hidden — shown only when a PlateResizeData
        /// component is present on the selected part.
        /// </summary>
        private void BuildPlateSection(Transform parent) {
            // VLayout container — three rows: Length, Width, Save.
            plateSection = MakeEl("PlateSection", parent);
            var vl = plateSection.AddComponent<VerticalLayoutGroup>();
            vl.childControlWidth     = true;
            vl.childForceExpandWidth  = true;
            vl.childControlHeight    = false;
            vl.childForceExpandHeight = false;
            vl.padding  = new RectOffset(0, 0, 0, 0);
            vl.spacing  = 2f;
            var csf = plateSection.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ── Length row ───────────────────────────────────────────────────────
            var lenRow = MakeEl("PlateLenRow", plateSection.transform);
            SetHRow(lenRow, 18f);

            var lenLbl = MakeText("PLL", lenRow.transform, "L:", 10);
            lenLbl.color = DimColor;
            lenLbl.GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 18f);

            // Length input field
            var lenFC = MakeEl("PlLFC", lenRow.transform);
            lenFC.GetComponent<RectTransform>().sizeDelta = new Vector2(46f, 18f);
            ApplyPanelImage(lenFC, InputBg);

            var lftGO = MakeEl("FT", lenFC.transform);
            var lftRT = lftGO.GetComponent<RectTransform>();
            lftRT.anchorMin = Vector2.zero; lftRT.anchorMax = Vector2.one;
            lftRT.offsetMin = new Vector2(3f, 1f); lftRT.offsetMax = new Vector2(-2f, -1f);
            var lftText = lftGO.AddComponent<Text>();
            lftText.font = uiFont; lftText.fontSize = 10;
            lftText.color = Color.white; lftText.alignment = TextAnchor.MiddleLeft;

            var lfphGO = MakeEl("FP", lenFC.transform);
            var lfphRT = lfphGO.GetComponent<RectTransform>();
            lfphRT.anchorMin = Vector2.zero; lfphRT.anchorMax = Vector2.one;
            lfphRT.offsetMin = new Vector2(3f, 1f); lfphRT.offsetMax = new Vector2(-2f, -1f);
            var lfphText = lfphGO.AddComponent<Text>();
            lfphText.font = uiFont; lfphText.fontSize = 10;
            lfphText.color = PlaceholderCl; lfphText.fontStyle = FontStyle.Italic;
            lfphText.text = "0";

            plateLengthInput = lenFC.AddComponent<InputField>();
            plateLengthInput.textComponent = lftText;
            plateLengthInput.placeholder   = lfphText;
            plateLengthInput.contentType   = InputField.ContentType.IntegerNumber;
            lenFC.AddComponent<ScrollableInputField>().Init(1f, "F0");

            // Length flip button: "← left" keeps right edge fixed, "right →" keeps left edge fixed.
            var lfGO = MakeEl("PlLFlipBtn", lenRow.transform);
            lfGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 18f);
            ApplyPanelImage(lfGO, new Color(0.25f, 0.25f, 0.30f, 1f));
            plateLengthFlipText = MakeEl("FBT", lfGO.transform).AddComponent<Text>();
            plateLengthFlipText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            plateLengthFlipText.GetComponent<RectTransform>().anchorMax = Vector2.one;
            plateLengthFlipText.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            plateLengthFlipText.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            plateLengthFlipText.font = uiFont; plateLengthFlipText.fontSize = 8;
            plateLengthFlipText.color = Color.white;
            plateLengthFlipText.alignment = TextAnchor.MiddleCenter;
            plateLengthFlipText.text = "← left";
            plateLengthFlipBtn = lfGO.AddComponent<Button>();
            plateLengthFlipBtn.targetGraphic = lfGO.GetComponent<Image>();
            var lfcb = ColorBlock.defaultColorBlock;
            lfcb.normalColor      = new Color(0.25f, 0.25f, 0.30f, 1f);
            lfcb.highlightedColor = new Color(0.35f, 0.35f, 0.42f, 1f);
            lfcb.pressedColor     = new Color(0.20f, 0.20f, 0.25f, 1f);
            lfcb.selectedColor    = lfcb.normalColor;
            plateLengthFlipBtn.colors = lfcb;

            // ── Width row ────────────────────────────────────────────────────────
            var widRow = MakeEl("PlateWidRow", plateSection.transform);
            SetHRow(widRow, 18f);

            var widLbl = MakeText("PWL", widRow.transform, "W:", 10);
            widLbl.color = DimColor;
            widLbl.GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 18f);

            // Width input field
            var widFC = MakeEl("PlWFC", widRow.transform);
            widFC.GetComponent<RectTransform>().sizeDelta = new Vector2(46f, 18f);
            ApplyPanelImage(widFC, InputBg);

            var wftGO = MakeEl("FT", widFC.transform);
            var wftRT = wftGO.GetComponent<RectTransform>();
            wftRT.anchorMin = Vector2.zero; wftRT.anchorMax = Vector2.one;
            wftRT.offsetMin = new Vector2(3f, 1f); wftRT.offsetMax = new Vector2(-2f, -1f);
            var wftText = wftGO.AddComponent<Text>();
            wftText.font = uiFont; wftText.fontSize = 10;
            wftText.color = Color.white; wftText.alignment = TextAnchor.MiddleLeft;

            var wfphGO = MakeEl("FP", widFC.transform);
            var wfphRT = wfphGO.GetComponent<RectTransform>();
            wfphRT.anchorMin = Vector2.zero; wfphRT.anchorMax = Vector2.one;
            wfphRT.offsetMin = new Vector2(3f, 1f); wfphRT.offsetMax = new Vector2(-2f, -1f);
            var wfphText = wfphGO.AddComponent<Text>();
            wfphText.font = uiFont; wfphText.fontSize = 10;
            wfphText.color = PlaceholderCl; wfphText.fontStyle = FontStyle.Italic;
            wfphText.text = "0";

            plateWidthInput = widFC.AddComponent<InputField>();
            plateWidthInput.textComponent = wftText;
            plateWidthInput.placeholder   = wfphText;
            plateWidthInput.contentType   = InputField.ContentType.IntegerNumber;
            widFC.AddComponent<ScrollableInputField>().Init(1f, "F0");

            // Width flip button: "↓ bot" keeps top edge fixed, "top ↑" keeps bottom edge fixed.
            var wfGO = MakeEl("PlWFlipBtn", widRow.transform);
            wfGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 18f);
            ApplyPanelImage(wfGO, new Color(0.25f, 0.25f, 0.30f, 1f));
            plateWidthFlipText = MakeEl("FBT", wfGO.transform).AddComponent<Text>();
            plateWidthFlipText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            plateWidthFlipText.GetComponent<RectTransform>().anchorMax = Vector2.one;
            plateWidthFlipText.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            plateWidthFlipText.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            plateWidthFlipText.font = uiFont; plateWidthFlipText.fontSize = 8;
            plateWidthFlipText.color = Color.white;
            plateWidthFlipText.alignment = TextAnchor.MiddleCenter;
            plateWidthFlipText.text = "↓ bot";
            plateWidthFlipBtn = wfGO.AddComponent<Button>();
            plateWidthFlipBtn.targetGraphic = wfGO.GetComponent<Image>();
            var wfcb = ColorBlock.defaultColorBlock;
            wfcb.normalColor      = new Color(0.25f, 0.25f, 0.30f, 1f);
            wfcb.highlightedColor = new Color(0.35f, 0.35f, 0.42f, 1f);
            wfcb.pressedColor     = new Color(0.20f, 0.20f, 0.25f, 1f);
            wfcb.selectedColor    = wfcb.normalColor;
            plateWidthFlipBtn.colors = wfcb;

            // ── Save row (spans full width) ──────────────────────────────────────
            var saveRow = MakeEl("PlateSaveRow", plateSection.transform);
            SetHRow(saveRow, 18f);

            var spGO = MakeEl("PlSaveBtn", saveRow.transform);
            spGO.GetComponent<RectTransform>().sizeDelta = new Vector2(186f, 18f);
            ApplyPanelImage(spGO, new Color(0.18f, 0.52f, 0.18f, 1f));
            var spTxt = MakeEl("ST", spGO.transform).AddComponent<Text>();
            spTxt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            spTxt.GetComponent<RectTransform>().anchorMax = Vector2.one;
            spTxt.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            spTxt.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            spTxt.font = uiFont; spTxt.fontSize = 12;
            spTxt.color = Color.white; spTxt.alignment = TextAnchor.MiddleCenter;
            spTxt.text = "✓ Apply";
            savePlateBtn = spGO.AddComponent<Button>();
            savePlateBtn.targetGraphic = spGO.GetComponent<Image>();
            var spcb = ColorBlock.defaultColorBlock;
            spcb.normalColor      = new Color(0.18f, 0.52f, 0.18f, 1f);
            spcb.highlightedColor = new Color(0.24f, 0.66f, 0.24f, 1f);
            spcb.pressedColor     = new Color(0.13f, 0.38f, 0.13f, 1f);
            spcb.selectedColor    = spcb.normalColor;
            savePlateBtn.colors   = spcb;

            plateSection.SetActive(false);
        }

        /// <summary>
        /// Called every frame when the panel is visible. Shows the hole section
        /// only when the selected part has an AluminumResizeData component, and
        /// keeps the field in sync (unless the user is currently editing it).
        /// </summary>
        private void RefreshHoleSection() {
            var root = RootObject;
            AluminumResizeData rd = root != null ? root.GetComponent<AluminumResizeData>() : null;

            bool show = rd != null;
            holeSection.SetActive(show);
            // When the hole section is visible it replaces the static param2 row.
            param2Row.SetActive(!show);

            if (!show) {
                ClearHolePreview();
                return;
            }

            // Keep the displayed number in sync unless the user is typing or a
            // preview is currently active.  Without the _holePendingChange guard,
            // clicking the ✓ button causes the input to lose focus first, which
            // would reset the text to the old value before onClick fires.
            if (!holeCountInput.isFocused && !_holePendingChange)
                holeCountInput.SetTextWithoutNotify(rd.holeCount.ToString());
        }

        /// <summary>
        /// Called every frame when the panel is visible. Shows the shaft section
        /// only when the selected part has a ShaftResizeData component, and keeps
        /// the field in sync (unless the user is currently editing it).
        /// </summary>
        private void RefreshShaftSection() {
            var root = RootObject;
            ShaftResizeData srd = root != null ? root.GetComponent<ShaftResizeData>() : null;

            bool show = srd != null;
            shaftSection.SetActive(show);
            // When the shaft section is visible it replaces the static param2 row.
            // (RefreshHoleSection already sets param2Row visible when hole section is
            //  hidden; overriding it here is safe since shaft and aluminum are mutually
            //  exclusive.)
            if (show) param2Row.SetActive(false);
            if (!show) { ClearShaftPreview(); return; }

            // Sync the displayed length unless the user is typing a new value.
            // Without the _shaftPendingChange guard, clicking ✓ causes the input to
            // lose focus first, which would reset the text before onClick fires.
            if (!shaftLengthInput.isFocused && !_shaftPendingChange)
                shaftLengthInput.SetTextWithoutNotify(srd.length.ToString());
        }

        /// <summary>
        /// Called every frame when the panel is visible. Shows the plate section
        /// only when the selected part has a PlateResizeData component, and keeps
        /// both input fields in sync (unless the user is currently editing them).
        /// Hides both param rows while the plate section is visible, since the
        /// section itself shows the same information in editable form.
        /// </summary>
        private void RefreshPlateSection() {
            var root = RootObject;
            PlateResizeData prd = root != null ? root.GetComponent<PlateResizeData>() : null;

            bool show = prd != null;
            plateSection.SetActive(show);
            // When the plate section is visible it replaces both static param rows.
            if (show) {
                param1Row.SetActive(false);
                param2Row.SetActive(false);
            }
            if (!show) { ClearPlatePreview(); return; }

            // Sync inputs unless the user is typing or has uncommitted changes.
            // Without the _platePendingChange guard, clicking ✓ Apply causes inputs
            // to lose focus first, which would reset the values before onClick fires.
            if (!plateLengthInput.isFocused && !_platePendingChange)
                plateLengthInput.SetTextWithoutNotify(prd.length.ToString());
            if (!plateWidthInput.isFocused && !_platePendingChange)
                plateWidthInput.SetTextWithoutNotify(prd.width.ToString());
        }

        /// <summary>
        /// Builds the CHILD-PART SIZE SELECTOR UI block:
        ///   [label]  [◄]  [current value]  [►]
        /// The label shows the generator's param1.name (e.g. "Size:").
        /// ◄ / ► cycle backwards / forwards through param1 options.
        /// Starts hidden — shown only when a ChildPartResizeData component is present
        /// and the generator has more than one param1 option to choose from.
        /// </summary>
        private void BuildChildPartSection(Transform parent) {
            childPartSection = MakeEl("ChildPartSection", parent);
            SetHRow(childPartSection, 18f);

            // Label — updated at runtime by RefreshChildPartSection
            childPartLabelText = MakeText("CPL", childPartSection.transform, "Size:", 10);
            childPartLabelText.color = DimColor;
            childPartLabelText.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 18f);

            // ◄ Previous button
            var prevGO = MakeEl("CPPrev", childPartSection.transform);
            prevGO.GetComponent<RectTransform>().sizeDelta = new Vector2(22f, 18f);
            ApplyPanelImage(prevGO, new Color(0.25f, 0.25f, 0.30f, 1f));
            var prevTxt = MakeEl("T", prevGO.transform).AddComponent<Text>();
            prevTxt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            prevTxt.GetComponent<RectTransform>().anchorMax = Vector2.one;
            prevTxt.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            prevTxt.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            prevTxt.font = uiFont; prevTxt.fontSize = 12;
            prevTxt.color = Color.white; prevTxt.alignment = TextAnchor.MiddleCenter;
            prevTxt.text = "◄";
            childPartPrevBtn = prevGO.AddComponent<Button>();
            childPartPrevBtn.targetGraphic = prevGO.GetComponent<Image>();
            var pvcb = ColorBlock.defaultColorBlock;
            pvcb.normalColor      = new Color(0.25f, 0.25f, 0.30f, 1f);
            pvcb.highlightedColor = new Color(0.35f, 0.35f, 0.42f, 1f);
            pvcb.pressedColor     = new Color(0.20f, 0.20f, 0.25f, 1f);
            pvcb.selectedColor    = pvcb.normalColor;
            childPartPrevBtn.colors = pvcb;

            // Current value display (read-only, dark background)
            var valGO = MakeEl("CPVal", childPartSection.transform);
            valGO.GetComponent<RectTransform>().sizeDelta = new Vector2(82f, 18f);
            ApplyPanelImage(valGO, InputBg);
            childPartValueText = MakeEl("T", valGO.transform).AddComponent<Text>();
            childPartValueText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            childPartValueText.GetComponent<RectTransform>().anchorMax = Vector2.one;
            childPartValueText.GetComponent<RectTransform>().offsetMin = new Vector2(3f, 0f);
            childPartValueText.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            childPartValueText.font = uiFont; childPartValueText.fontSize = 10;
            childPartValueText.color = Color.white;
            childPartValueText.alignment = TextAnchor.MiddleLeft;

            // Scroll-wheel support: hovering over the value display and scrolling
            // cycles through part sizes just like clicking ◄/►.
            // valGO already has an Image (from ApplyPanelImage) so the EventSystem
            // can raycast it and deliver IScrollHandler events.
            var cycler = valGO.AddComponent<ScrollableCycler>();
            cycler.onScrollUp   = () => OnChildPartCycle(+1);
            cycler.onScrollDown = () => OnChildPartCycle(-1);

            // ► Next button
            var nextGO = MakeEl("CPNext", childPartSection.transform);
            nextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(22f, 18f);
            ApplyPanelImage(nextGO, new Color(0.25f, 0.25f, 0.30f, 1f));
            var nextTxt = MakeEl("T", nextGO.transform).AddComponent<Text>();
            nextTxt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            nextTxt.GetComponent<RectTransform>().anchorMax = Vector2.one;
            nextTxt.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            nextTxt.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            nextTxt.font = uiFont; nextTxt.fontSize = 12;
            nextTxt.color = Color.white; nextTxt.alignment = TextAnchor.MiddleCenter;
            nextTxt.text = "►";
            childPartNextBtn = nextGO.AddComponent<Button>();
            childPartNextBtn.targetGraphic = nextGO.GetComponent<Image>();
            var nvcb = ColorBlock.defaultColorBlock;
            nvcb.normalColor      = new Color(0.25f, 0.25f, 0.30f, 1f);
            nvcb.highlightedColor = new Color(0.35f, 0.35f, 0.42f, 1f);
            nvcb.pressedColor     = new Color(0.20f, 0.20f, 0.25f, 1f);
            nvcb.selectedColor    = nvcb.normalColor;
            childPartNextBtn.colors = nvcb;

            childPartSection.SetActive(false);
        }

        /// <summary>
        /// Called every frame when the panel is visible. Shows the child-part selector
        /// only when the selected part has a ChildPartResizeData component AND its
        /// generator offers more than one param1 option (no point showing a selector
        /// with only one choice). Hides param1Row while the section is visible since
        /// the selector already displays that information interactively.
        /// </summary>
        private void RefreshChildPartSection() {
            var root = RootObject;
            ChildPartResizeData cpd = root != null ? root.GetComponent<ChildPartResizeData>() : null;

            // For two-param parts (e.g. spacers with param1=Type, param2=Size) we cycle
            // param2.  GetParam2Options() filters by param1.value, so restore BOTH params
            // first.  Leaving param2 in a stale state causes StandoffPlaceDisplacement
            // (and any other system that calls TryGetPartData) to throw IndexOutOfRangeException
            // because no child PartData matches the partial param state.
            // For single-param parts (e.g. screws with param1=Size) we cycle param1.
            bool show;
            if (cpd != null && cpd.generator != null && cpd.generator.UsesTwoParams) {
                cpd.generator.param1.value = cpd.currentParam1Value;
                if (!string.IsNullOrEmpty(cpd.currentParam2Value))
                    cpd.generator.param2.value = cpd.currentParam2Value;
                show = cpd.generator.GetParam2Options().Count > 1;
            } else {
                show = cpd != null
                       && cpd.generator != null
                       && cpd.generator.GetParam1Options().Count > 1;
            }

            childPartSection.SetActive(show);
            // When the child-part section is visible it replaces both static param rows.
            // param2Row is also hidden to prevent an empty gap caused by RefreshHoleSection
            // (which forces param2Row visible for any non-aluminum part).
            if (show) {
                param1Row.SetActive(false);
                param2Row.SetActive(false);
            }
            if (!show) return;

            // Update the label and current-value text to reflect the parameter being cycled.
            if (cpd.generator.UsesTwoParams) {
                // Spacers: cycle param2 (size) — show its name and current value.
                string lbl = !string.IsNullOrEmpty(cpd.generator.param2.name)
                    ? cpd.generator.param2.name : "Size";
                childPartLabelText.text = lbl + ":";
                childPartValueText.text = cpd.currentParam2Value;
            } else {
                // Screws etc: cycle param1 — show its name and current value.
                string lbl = !string.IsNullOrEmpty(cpd.generator.param1.name)
                    ? cpd.generator.param1.name : "Size";
                childPartLabelText.text = lbl + ":";
                childPartValueText.text = cpd.currentParam1Value;
            }
        }

        // ── Hole-count event handlers ─────────────────────────────────────────────

        private void OnHoleCountChanged(string val) {
            var root = RootObject;
            if (root == null) return;
            var rd = root.GetComponent<AluminumResizeData>();
            if (rd == null) return;

            if (!int.TryParse(val, out int target)) { ClearHolePreview(); return; }
            target = Mathf.Clamp(target, rd.minHoleCount, rd.maxHoleCount);
            if (target == rd.holeCount) { ClearHolePreview(); return; }

            ShowHolePreview(rd, target);
        }

        /// <summary>Called when the user clicks the green ✓ button to commit the typed count.</summary>
        private void OnHoleSave() {
            var root = RootObject;
            if (root == null) return;
            var rd = root.GetComponent<AluminumResizeData>();
            if (rd == null) return;

            if (!int.TryParse(holeCountInput.text, out int target)) {
                holeCountInput.SetTextWithoutNotify(rd.holeCount.ToString());
                ClearHolePreview();
                return;
            }
            target = Mathf.Clamp(target, rd.minHoleCount, rd.maxHoleCount);
            if (target == rd.holeCount) { ClearHolePreview(); return; }

            ApplyHoleResize(rd, target);
        }

        private void OnFlipSide() {
            resizeFromLeft = !resizeFromLeft;
            flipBtnText.text = resizeFromLeft ? "← left" : "right →";

            // Refresh the preview in the new direction if we're mid-edit.
            var root = RootObject;
            if (root == null) return;
            var rd = root.GetComponent<AluminumResizeData>();
            if (rd == null) return;
            if (int.TryParse(holeCountInput.text, out int t) && t != rd.holeCount)
                ShowHolePreview(rd, Mathf.Clamp(t, rd.minHoleCount, rd.maxHoleCount));
        }

        // ── Resize apply (with undo) ──────────────────────────────────────────────

        private void ApplyHoleResize(AluminumResizeData rd, int newCount) {
            // 1. Capture PRE-resize state for undo.
            Protobot.StateSystems.StateSystem.AddElement(
                new Protobot.StateSystems.ResizeElement(rd));

            // 2. Clear the preview BEFORE applying the resize.
            //    For removal previews the part's MeshFilter currently holds our
            //    temporary two-submesh mesh; clearing it first restores the original
            //    single-mesh so AluminumResizer.ApplyResize can replace it cleanly.
            ClearHolePreview();

            // 3. Move the part so the chosen edge stays world-fixed.
            rd.transform.position = AluminumResizer.CalcNewPosition(
                rd.transform, rd.holeCount, newCount, keepRightEdge: resizeFromLeft);

            // 4. Rebuild mesh + holes + metadata.
            AluminumResizer.ApplyResize(rd, newCount);

            // 5. Re-select the root part so gizmos reposition to the new centre.
            //    If the previously selected object was a child HoleCollider that was
            //    destroyed by ApplyResize, the gizmo's ObjectLink loses its target and
            //    freezes at the old position.  Re-selecting restores gizmo tracking.
            if (_selectionManager != null)
                _selectionManager.SetCurrent(
                    new ObjectSelection { gameObject = rd.gameObject, selector = null });

            // 6. Push POST-resize snapshot so Ctrl+Y can redo.
            Protobot.StateSystems.StateSystem.AddState(
                new Protobot.StateSystems.ResizeElement(rd));

            holeCountInput.SetTextWithoutNotify(newCount.ToString());
        }

        // ── Shaft-length event handlers ───────────────────────────────────────────

        private void OnShaftLengthChanged(string val) {
            var root = RootObject;
            var srd  = root != null ? root.GetComponent<ShaftResizeData>() : null;
            if (srd == null) { _shaftPendingChange = false; ClearShaftPreview(); return; }

            if (!float.TryParse(val, out float f)) { _shaftPendingChange = false; ClearShaftPreview(); return; }
            float clamped = Mathf.Clamp(f, srd.minLength, srd.maxLength);
            if (Mathf.Approximately(clamped, srd.length)) { _shaftPendingChange = false; ClearShaftPreview(); return; }

            _shaftPendingChange = true;
            ShowShaftPreview(srd, clamped);
        }

        /// <summary>Called when the user clicks ✓ to commit the typed shaft length.</summary>
        private void OnShaftSave() {
            _shaftPendingChange = false;
            var root = RootObject;
            if (root == null) return;
            var srd = root.GetComponent<ShaftResizeData>();
            if (srd == null) return;

            if (!float.TryParse(shaftLengthInput.text, out float target)) {
                shaftLengthInput.SetTextWithoutNotify(srd.length.ToString());
                return;
            }
            target = Mathf.Clamp(target, srd.minLength, srd.maxLength);
            if (Mathf.Approximately(target, srd.length)) return;

            ApplyShaftResize(srd, target);
        }

        private void OnShaftFlipSide() {
            shaftResizeFromFar = !shaftResizeFromFar;
            // "far →"  = near end fixed, shaft changes at far (+Z) end.
            // "← near" = far  end fixed, shaft changes at near (−Z) end.
            shaftFlipBtnText.text = shaftResizeFromFar ? "← near" : "far →";

            // Refresh the preview in the new direction if we're mid-edit.
            var root = RootObject;
            var srd  = root != null ? root.GetComponent<ShaftResizeData>() : null;
            if (srd == null) return;
            if (float.TryParse(shaftLengthInput.text, out float t)) {
                float clamped = Mathf.Clamp(t, srd.minLength, srd.maxLength);
                if (!Mathf.Approximately(clamped, srd.length))
                    ShowShaftPreview(srd, clamped);
            }
        }

        /// <summary>
        /// Commits a shaft length change, wrapping it in an undo/redo state pair.
        /// </summary>
        private void ApplyShaftResize(ShaftResizeData srd, float newLength) {
            _shaftPendingChange = false;
            ClearShaftPreview(); // remove ghost before modifying the actual shaft

            // 1. Capture PRE-resize state for undo.
            Protobot.StateSystems.StateSystem.AddElement(
                new Protobot.StateSystems.ShaftResizeElement(srd));

            // 2. Move shaft so the chosen end stays world-fixed.
            srd.transform.position = ShaftResizer.CalcNewPosition(
                srd.transform, srd.length, newLength, keepFarEnd: shaftResizeFromFar);

            // 3. Apply the length change (updates localScale.z, PartName, SavedObject.id).
            ShaftResizer.ApplyResize(srd, newLength);

            // 4. Re-select root to refresh gizmo position.
            if (_selectionManager != null)
                _selectionManager.SetCurrent(
                    new ObjectSelection { gameObject = srd.gameObject, selector = null });

            // 5. Push POST-resize snapshot so Ctrl+Y can redo.
            Protobot.StateSystems.StateSystem.AddState(
                new Protobot.StateSystems.ShaftResizeElement(srd));

            shaftLengthInput.SetTextWithoutNotify(newLength.ToString());
        }

        // ── Plate-resize event handlers ───────────────────────────────────────────

        private void OnPlateLengthChanged(string val) {
            var root = RootObject;
            var prd  = root != null ? root.GetComponent<PlateResizeData>() : null;
            if (prd == null) { _platePendingChange = false; ClearPlatePreview(); return; }

            if (!int.TryParse(val, out int l)) { _platePendingChange = false; ClearPlatePreview(); return; }
            int newLen = Mathf.Clamp(l, prd.minLength, prd.maxLength);
            int.TryParse(plateWidthInput.text, out int w);
            int newWid = Mathf.Clamp(w == 0 ? prd.width : w, prd.minWidth, prd.maxWidth);
            _platePendingChange = newLen != prd.length || newWid != prd.width;
            if (_platePendingChange) ShowPlatePreview(prd, newLen, newWid);
            else ClearPlatePreview();
        }

        private void OnPlateWidthChanged(string val) {
            var root = RootObject;
            var prd  = root != null ? root.GetComponent<PlateResizeData>() : null;
            if (prd == null) { _platePendingChange = false; ClearPlatePreview(); return; }

            if (!int.TryParse(val, out int w)) { _platePendingChange = false; ClearPlatePreview(); return; }
            int newWid = Mathf.Clamp(w, prd.minWidth, prd.maxWidth);
            int.TryParse(plateLengthInput.text, out int l);
            int newLen = Mathf.Clamp(l == 0 ? prd.length : l, prd.minLength, prd.maxLength);
            _platePendingChange = newLen != prd.length || newWid != prd.width;
            if (_platePendingChange) ShowPlatePreview(prd, newLen, newWid);
            else ClearPlatePreview();
        }

        /// <summary>Called when the user clicks ✓ Apply to commit the typed dimensions.</summary>
        private void OnPlateSave() {
            _platePendingChange = false;
            var root = RootObject;
            if (root == null) return;
            var prd = root.GetComponent<PlateResizeData>();
            if (prd == null) return;

            int.TryParse(plateLengthInput.text, out int newLen);
            int.TryParse(plateWidthInput.text,  out int newWid);
            // Treat 0 (parse failure) as "keep current".
            if (newLen == 0) newLen = prd.length;
            if (newWid == 0) newWid = prd.width;
            newLen = Mathf.Clamp(newLen, prd.minLength, prd.maxLength);
            newWid = Mathf.Clamp(newWid, prd.minWidth,  prd.maxWidth);
            if (newLen == prd.length && newWid == prd.width) return;

            ApplyPlateResize(prd, newLen, newWid);
        }

        private void OnPlateLengthFlip() {
            plateResizeLengthFromLeft = !plateResizeLengthFromLeft;
            // "← left"  = modify from left  (keep right/+X edge fixed)
            // "right →"  = modify from right (keep left/−X edge fixed)
            plateLengthFlipText.text = plateResizeLengthFromLeft ? "← left" : "right →";
            RefreshPlatePreviewIfActive();
        }

        private void OnPlateWidthFlip() {
            plateResizeWidthFromBottom = !plateResizeWidthFromBottom;
            // "↓ bot" = modify from bottom (keep top/+Y edge fixed)
            // "top ↑" = modify from top   (keep bottom/−Y edge fixed)
            plateWidthFlipText.text = plateResizeWidthFromBottom ? "↓ bot" : "top ↑";
            RefreshPlatePreviewIfActive();
        }

        /// <summary>Refreshes the plate preview ghost after a flip direction change.</summary>
        private void RefreshPlatePreviewIfActive() {
            if (!_platePendingChange) return;
            var root = RootObject;
            var prd  = root != null ? root.GetComponent<PlateResizeData>() : null;
            if (prd == null) return;
            int.TryParse(plateLengthInput.text, out int l);
            int.TryParse(plateWidthInput.text,  out int w);
            int newLen = Mathf.Clamp(l == 0 ? prd.length : l, prd.minLength, prd.maxLength);
            int newWid = Mathf.Clamp(w == 0 ? prd.width  : w, prd.minWidth,  prd.maxWidth);
            ShowPlatePreview(prd, newLen, newWid);
        }

        /// <summary>
        /// Commits a plate dimension change, wrapping it in an undo/redo state pair.
        /// </summary>
        private void ApplyPlateResize(PlateResizeData prd, int newLength, int newWidth) {
            _platePendingChange = false;
            ClearPlatePreview(); // remove ghost before rebuilding the actual plate

            // 1. Capture PRE-resize state for undo.
            Protobot.StateSystems.StateSystem.AddElement(
                new Protobot.StateSystems.PlateResizeElement(prd));

            // 2. Move the plate so the chosen edges stay world-fixed.
            //    plateResizeLengthFromLeft=true  → keep right/+X edge fixed (keepRightEdge=true)
            //    plateResizeWidthFromBottom=true → keep top/+Y  edge fixed (keepTopEdge=true)
            prd.transform.position = PlateResizer.CalcNewPosition(
                prd.transform,
                prd.length, newLength, keepRightEdge: plateResizeLengthFromLeft,
                prd.width,  newWidth,  keepTopEdge:   plateResizeWidthFromBottom);

            // 3. Rebuild mesh + holes + metadata.
            PlateResizer.ApplyResize(prd, newLength, newWidth);

            // 4. Re-select root to refresh gizmo position.
            if (_selectionManager != null)
                _selectionManager.SetCurrent(
                    new ObjectSelection { gameObject = prd.gameObject, selector = null });

            // 5. Push POST-resize snapshot so Ctrl+Y can redo.
            Protobot.StateSystems.StateSystem.AddState(
                new Protobot.StateSystems.PlateResizeElement(prd));

            plateLengthInput.SetTextWithoutNotify(newLength.ToString());
            plateWidthInput.SetTextWithoutNotify(newWidth.ToString());
        }

        // ── Child-part variant event handlers ─────────────────────────────────────

        /// <summary>
        /// Cycles through the generator's param options by <paramref name="direction"/>
        /// steps (+1 = next, −1 = previous). Stops at the first/last option rather
        /// than wrapping around, so ◄/► and scroll wheel have a clear end-point.
        /// </summary>
        private void OnChildPartCycle(int direction) {
            var root = RootObject;
            if (root == null) return;
            var cpd = root.GetComponent<ChildPartResizeData>();
            if (cpd == null || cpd.generator == null) return;

            if (cpd.generator.UsesTwoParams) {
                // Two-param parts (e.g. spacers): cycle param2 (size), keep param1 (type) fixed.
                // GetParam2Options() filters by param1.value, so restore both params first.
                cpd.generator.param1.value = cpd.currentParam1Value;
                if (!string.IsNullOrEmpty(cpd.currentParam2Value))
                    cpd.generator.param2.value = cpd.currentParam2Value;
                var options = cpd.generator.GetParam2Options();
                if (options.Count <= 1) return;

                int idx = options.IndexOf(cpd.currentParam2Value);
                if (idx < 0) idx = 0;
                // Clamp: stop at first / last instead of wrapping.
                int newIdx = Mathf.Clamp(idx + direction, 0, options.Count - 1);
                string newParam2 = options[newIdx];
                if (newParam2 == cpd.currentParam2Value) return; // already at boundary

                ApplyChildPartVariantChange(cpd, cpd.currentParam1Value, newParam2);
            } else {
                // Single-param parts (e.g. screws, standoffs): cycle param1.
                var options = cpd.generator.GetParam1Options();
                if (options.Count <= 1) return;

                int idx = options.IndexOf(cpd.currentParam1Value);
                if (idx < 0) idx = 0;
                // Clamp: stop at first / last instead of wrapping.
                int newIdx = Mathf.Clamp(idx + direction, 0, options.Count - 1);
                string newParam1 = options[newIdx];
                if (newParam1 == cpd.currentParam1Value) return; // already at boundary

                ApplyChildPartVariantChange(cpd, newParam1, null);
            }
        }

        /// <summary>
        /// Replaces the currently selected child-part (e.g. screw) with a newly
        /// generated variant that has <paramref name="newParam1Value"/> as its param1.
        ///
        /// The swap is wrapped in an undo/redo pair using a symmetric element design:
        /// each element stores the generator, the param value, and the world
        /// transform, and on Load() destroys the current object and recreates itself.
        /// The paired elements update each other's <c>objectToReplace</c> field so
        /// repeated undo/redo cycles always destroy the correct object.
        /// </summary>
        /// <param name="newParam2Value">The new param2 value, or null for single-param generators.</param>
        private void ApplyChildPartVariantChange(ChildPartResizeData cpd,
                                                  string newParam1Value, string newParam2Value) {
            var generator    = cpd.generator;
            Vector3    pos   = cpd.transform.position;
            Quaternion rot   = cpd.transform.rotation;
            string oldParam1 = cpd.currentParam1Value;
            string oldParam2 = cpd.currentParam2Value;

            // 1. Build the PRE (undo) element using the current (old) position and
            //    old params so that Undo recreates the original part in the right place.
            var preElem = new Protobot.StateSystems.ChildPartResizeElement(
                generator, oldParam1, oldParam2, pos, rot);

            // 2. Apply the new generator params NOW so that TryGetPartData returns
            //    the correct dimensions for the chosen variant, then compute the
            //    adjusted spawn position (keeps the connected end fixed in place).
            generator.param1.value = newParam1Value;
            if (generator.UsesTwoParams && newParam2Value != null)
                generator.param2.value = newParam2Value;
            Vector3 adjustedPos = CalcChildPartPosition(cpd, generator);

            // 3. Build the POST (redo) element using the adjusted position so that
            //    Redo regenerates the part at the same connected-end-anchored spot.
            var postElem = new Protobot.StateSystems.ChildPartResizeElement(
                generator, newParam1Value, newParam2Value, adjustedPos, rot);
            // Each element updates the other's objectToReplace after Load() so the
            // chain stays correct across multiple undo/redo invocations.
            preElem.pair  = postElem;
            postElem.pair = preElem;

            Protobot.StateSystems.StateSystem.AddElement(preElem);

            // 4. Generate the new part at the adjusted (anchor-aware) position.
            var newObj = generator.Generate(adjustedPos, rot);

            // 5. The pre-element destroys the new object if Undo fires.
            preElem.objectToReplace = newObj;

            // 6. Re-select the new part BEFORE destroying the old one.
            //    AvoidUISelectionCondition blocks SetCurrent() when the mouse is
            //    over a UI button (which it is when ◄/► are clicked), so assign
            //    the public field directly to bypass all selection conditions.
            //    This must happen before Destroy so current never holds a dead ref.
            if (_selectionManager != null)
                _selectionManager.current =
                    new ObjectSelection { gameObject = newObj, selector = null };

            // 7. Destroy the old part.
            Object.Destroy(cpd.gameObject);

            // 8. Push POST-resize snapshot so Ctrl+Y can redo.
            Protobot.StateSystems.StateSystem.AddState(postElem);
        }

        /// <summary>
        /// Computes the spawn position for a child-part variant swap so that the
        /// connected end stays fixed and only the free end moves as the part changes
        /// size (e.g. swapping a 1½" standoff for a 2" standoff while it is already
        /// snapped into an aluminium hole).
        ///
        /// When a standoff or spacer is snapped into an aluminium hole, one end of
        /// the part is "anchored" at that hole's world position.  Growing or shrinking
        /// the part should keep that anchor end in place and extend the other end.
        ///
        /// <b>Generator params must already be set to the new values</b> before
        /// calling this method, because <c>TryGetPartData</c> internally calls
        /// <c>GetChildObj()</c> which matches against both param1 and param2.
        ///
        /// Algorithm:
        ///   1. Find the first child HoleDetector that reports a live connection.
        ///   2. The nearest HoleCollider in that detector's list gives the anchor —
        ///      the world position of the aluminium hole the part is snapped into.
        ///   3. The direction from anchor → current center is the part's axis.
        ///   4. newCenter = anchor + axis × (PartData.PrimaryHoleDepth / 2).
        ///
        /// Returns the current center unchanged if the part is not connected to any
        /// hole (freely placed parts grow/shrink symmetrically from their center).
        /// </summary>
        private Vector3 CalcChildPartPosition(ChildPartResizeData cpd,
                                              ChildPartGenerator generator) {
            // Find the first HoleDetector on this part that has a live target hole
            // (i.e. the end of the standoff/spacer currently snapped into aluminium).
            HoleDetector connected = null;
            foreach (var hd in cpd.GetComponentsInChildren<HoleDetector>()) {
                if (hd.TargetHoleFound) { connected = hd; break; }
            }

            // Part is not connected — keep current center; part grows symmetrically.
            if (connected == null) return cpd.transform.position;

            // The nearest HoleCollider is the aluminium hole this end is snapped into.
            var orderedHoles = connected.GetOrderedHoles();
            if (orderedHoles.Count == 0) return cpd.transform.position;
            Vector3 anchor = orderedHoles[0].holeData.position;

            // Direction and distance from anchor to current part center.
            Vector3 toCenter = cpd.transform.position - anchor;
            if (toCenter.magnitude < 0.001f) return cpd.transform.position;

            // New half-length comes from the chosen variant's PartData.
            // TryGetPartData requires generator params to already be set to new values.
            float newHalfLength = toCenter.magnitude; // fallback: keep current size
            if (generator.TryGetPartData(out PartData pd) && pd.PrimaryHoleDepth > 0f)
                newHalfLength = pd.PrimaryHoleDepth * 0.5f;

            // New center = anchor + same axis direction × new half-length.
            return anchor + toCenter.normalized * newHalfLength;
        }

        // ── Preview helpers ───────────────────────────────────────────────────────

        // ── Shaft preview ─────────────────────────────────────────────────────────

        /// <summary>
        /// Shows a translucent ghost of only the DELTA section of the shaft — the
        /// portion that will be added (green) or removed (red) — positioned at the
        /// end being modified.
        ///
        /// Geometry derivation (shaft mesh centered at origin, spans ±(length/2)):
        ///   keepFarEnd=false (near end fixed, far end changes):
        ///     delta-section centre = shaft.position + forward × targetLength/2
        ///   keepFarEnd=true  (far  end fixed, near end changes):
        ///     delta-section centre = shaft.position − forward × targetLength/2
        ///   delta-section length = |targetLength − currentLength|
        /// </summary>
        private void ShowShaftPreview(ShaftResizeData srd, float targetLength) {
            ClearShaftPreview();
            float deltaLength = Mathf.Abs(targetLength - srd.length);
            if (deltaLength < 0.001f) return;

            var mf = srd.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;

            bool adding = targetLength > srd.length;

            // Centre of the added/removed section in world space (see doc-comment).
            Vector3 deltaCenter = shaftResizeFromFar
                ? srd.transform.position - srd.transform.forward * (targetLength * 0.5f)
                : srd.transform.position + srd.transform.forward * (targetLength * 0.5f);

            var ghost = new GameObject("ShaftResizePreview");
            ghost.transform.position = deltaCenter;
            ghost.transform.rotation = srd.transform.rotation;
            // Match the shaft's cross-section scale; only change the length (Z).
            // When removing, scale XY slightly larger (1.04×) so the ghost renders
            // on top of the actual shaft and avoids z-fighting.
            Vector3 scl = srd.transform.localScale;
            float xyScale = adding ? 1f : 1.04f;
            ghost.transform.localScale = new Vector3(scl.x * xyScale, scl.y * xyScale, deltaLength);

            var ghostMF = ghost.AddComponent<MeshFilter>();
            var ghostMR = ghost.AddComponent<MeshRenderer>();
            ghostMF.sharedMesh     = mf.sharedMesh;
            ghostMR.sharedMaterial = adding ? _previewAddMat : _previewRemoveMat;

            _shaftPreviewObjs.Add(ghost);
        }

        /// <summary>Destroys any active shaft preview ghost.</summary>
        private void ClearShaftPreview() {
            foreach (var obj in _shaftPreviewObjs)
                if (obj != null) Object.Destroy(obj);
            _shaftPreviewObjs.Clear();
        }

        // ── Plate preview ─────────────────────────────────────────────────────────

        /// <summary>
        /// Shows translucent ghosts of ONLY the delta sections — the strip(s) that
        /// will be added (green) or removed (red) — never overlapping the kept region
        /// and never overlapping each other, even when one axis adds while the other removes.
        ///
        /// Non-overlapping two-strip tiling:
        ///
        ///   X strip  (length delta):  deltaLen × newWid
        ///     Centre uses Q (new plate centre) offset by curLen's half-extent along R.
        ///     fromLeft=true  → Q − R × (curLen × 0.25)
        ///     fromLeft=false → Q + R × (curLen × 0.25)
        ///
        ///   Y strip  (width delta):   curLen × deltaWid
        ///     Centre uses P (current plate centre) offset by newWid's half-extent along U.
        ///     fromBottom=true  → P − U × (newWid × 0.25)
        ///     fromBottom=false → P + U × (newWid × 0.25)
        ///
        /// Proof that these tile exactly (no gap, no overlap):
        ///   deltaLen×newWid + curLen×deltaWid
        ///   = (newLen−curLen)×newWid + curLen×(newWid−curWid)
        ///   = newLen×newWid − curLen×curWid  (exact signed area difference) ✓
        /// </summary>
        private void ShowPlatePreview(PlateResizeData prd, int newLen, int newWid) {
            ClearPlatePreview();

            int deltaLen = Mathf.Abs(newLen - prd.length);
            int deltaWid = Mathf.Abs(newWid - prd.width);
            if (deltaLen == 0 && deltaWid == 0) return;

            // Q = where the plate centre will be after the resize.
            Vector3    Q   = PlateResizer.CalcNewPosition(
                prd.transform,
                prd.length, newLen, keepRightEdge: plateResizeLengthFromLeft,
                prd.width,  newWid, keepTopEdge:   plateResizeWidthFromBottom);
            Vector3    P   = prd.transform.position;
            Vector3    R   = prd.transform.right;
            Vector3    U   = prd.transform.up;
            Quaternion rot = prd.transform.rotation;
            Vector3    scl = prd.transform.localScale;

            // ── X strip: deltaLen × newWid, centred relative to Q ────────────────
            // Offset from Q by the OLD half-extent (curLen×0.25) toward the changing side.
            // This places the strip's inner edge at the old plate's changing edge, so it
            // never overlaps the kept region. The strip height matches newWid so there is
            // no corner overlap with the Y strip even in mixed add+remove cases.
            if (deltaLen > 0) {
                Vector3 xCentre = plateResizeLengthFromLeft
                    ? Q - R * (prd.length * 0.25f)   // left grows/shrinks: strip left of old left edge
                    : Q + R * (prd.length * 0.25f);   // right grows/shrinks: strip right of old right edge

                Mesh     xMesh = prd.generator.GetMesh(deltaLen, newWid);
                Material xMat  = (newLen > prd.length) ? _previewAddMat : _previewRemoveMat;
                SpawnPlateGhost(xCentre, rot, scl, xMesh, xMat);
            }

            // ── Y strip: curLen × deltaWid, centred relative to P ────────────────
            // Offset from P (not Q) by the NEW half-extent (newWid×0.25) toward the
            // changing side. Using P keeps this strip aligned with the original plate's
            // X centre, so it cannot extend into the X strip's territory.
            if (deltaWid > 0) {
                Vector3 yCentre = plateResizeWidthFromBottom
                    ? P - U * (newWid * 0.25f)   // bottom grows/shrinks: strip below new bottom edge
                    : P + U * (newWid * 0.25f);   // top    grows/shrinks: strip above new top edge

                Mesh     yMesh = prd.generator.GetMesh(prd.length, deltaWid);
                Material yMat  = (newWid > prd.width) ? _previewAddMat : _previewRemoveMat;
                SpawnPlateGhost(yCentre, rot, scl, yMesh, yMat);
            }
        }

        /// <summary>
        /// Instantiates a single translucent plate ghost and registers it for cleanup.
        /// </summary>
        private void SpawnPlateGhost(Vector3 pos, Quaternion rot, Vector3 scale,
                                     Mesh mesh, Material mat) {
            if (mesh == null) return;
            var ghost = new GameObject("PlateResizePreview");
            ghost.transform.position   = pos;
            ghost.transform.rotation   = rot;
            ghost.transform.localScale = scale;
            ghost.AddComponent<MeshFilter>().sharedMesh       = mesh;
            ghost.AddComponent<MeshRenderer>().sharedMaterial = mat;
            _platePreviewObjs.Add(ghost);
        }

        /// <summary>Destroys any active plate preview ghosts.</summary>
        private void ClearPlatePreview() {
            foreach (var obj in _platePreviewObjs)
                if (obj != null) Object.Destroy(obj);
            _platePreviewObjs.Clear();
        }

        // ── Hole-count preview ────────────────────────────────────────────────────

        /// <summary>
        /// Shows a 3-D preview of the pending hole-count change and sets the
        /// _holePendingChange flag so the input field isn't reset while it's active.
        ///
        /// REMOVAL: the actual part's MeshFilter is temporarily replaced with a
        ///   two-submesh mesh — submesh 0 (kept sections) uses the original material,
        ///   submesh 1 (removed sections) uses the red transparent material.  This
        ///   correctly highlights interior faces (e.g. inside a C-channel) that a
        ///   floating ghost overlay cannot reach.
        ///
        /// ADDITION: a green ghost mesh is placed adjacent to the modified end,
        ///   showing the sections that will be added.
        ///
        /// In both cases a blue sphere marks the edge being modified.
        /// </summary>
        private void ShowHolePreview(AluminumResizeData rd, int targetCount) {
            ClearHolePreview();
            _holePendingChange = true;

            bool removing = targetCount < rd.holeCount;
            int  absDelta = Mathf.Abs(targetCount - rd.holeCount);

            if (removing) {
                // ── REMOVAL: split the actual part into kept + removed submeshes ──────
                var part = rd.gameObject;
                var mf   = part.GetComponent<MeshFilter>();
                var mr   = part.GetComponent<MeshRenderer>();

                if (mf != null && mr != null && mr.sharedMaterials.Length > 0) {
                    _previewMeshFilter   = mf;
                    _previewMeshRenderer = mr;
                    _previewOriginalMesh = mf.sharedMesh;
                    _previewOriginalMats = mr.sharedMaterials; // returns a copy

                    // Each sub-mesh must sit at the correct position inside the part's
                    // local space.  GetMesh(n) always centers its mesh at the local
                    // origin, so we shift each one to line up with its actual end.
                    //
                    // resizeFromLeft = true  → remove from LEFT,  keep RIGHT edge
                    //   kept    shifts +absDelta * 0.25  (rightward)
                    //   removed shifts -targetCount * 0.25 (leftward)
                    //
                    // resizeFromLeft = false → remove from RIGHT, keep LEFT edge
                    //   kept    shifts -absDelta * 0.25  (leftward)
                    //   removed shifts +targetCount * 0.25 (rightward)
                    float sign         = resizeFromLeft ? 1f : -1f;
                    float keptShift    =  sign * absDelta    * 0.25f;
                    float removedShift = -sign * targetCount * 0.25f;

                    Mesh keptMesh    = rd.subParts.GetMesh(targetCount);
                    Mesh removedMesh = rd.subParts.GetMesh(absDelta);

                    var combine = new CombineInstance[] {
                        new CombineInstance {
                            mesh      = keptMesh,
                            transform = Matrix4x4.Translate(new Vector3(keptShift, 0f, 0f))
                        },
                        new CombineInstance {
                            mesh      = removedMesh,
                            transform = Matrix4x4.Translate(new Vector3(removedShift, 0f, 0f))
                        }
                    };

                    var previewMesh = new Mesh();
                    previewMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                    // mergeSubMeshes:false keeps each CombineInstance as its own
                    // submesh so we can apply a different material per section.
                    previewMesh.CombineMeshes(combine, mergeSubMeshes: false);
                    previewMesh.RecalculateNormals();

                    _previewDisplayMesh = previewMesh;
                    mf.sharedMesh = previewMesh;
                    // Material 0 → kept sections (original look)
                    // Material 1 → removed sections (red transparent)
                    mr.sharedMaterials = new[] { _previewOriginalMats[0], _previewRemoveMat };
                }

            } else {
                // ── ADDITION: green ghost placed adjacent to the modified end ─────────
                Mesh deltaMesh = rd.subParts.GetMesh(absDelta);

                Vector3 previewCenter = AluminumResizer.CalcPreviewCenter(
                    rd.transform, rd.holeCount, targetCount, modifyLeft: resizeFromLeft);

                var ghost = new GameObject("ResizePreviewMesh");
                ghost.transform.position  = previewCenter;
                ghost.transform.rotation  = rd.transform.rotation;
                ghost.transform.localScale = Vector3.one; // adjacent, no Z-fight risk
                var mfGhost = ghost.AddComponent<MeshFilter>();
                var mrGhost = ghost.AddComponent<MeshRenderer>();
                mfGhost.sharedMesh     = deltaMesh;
                mrGhost.sharedMaterial = _previewAddMat;
                _previewObjs.Add(ghost);
            }

            // Blue + symbol at the edge being modified (both removal and addition).
            // Built from two cube primitives arranged as a cross so it reads clearly
            // from any angle, unlike a sphere which has no obvious orientation cue.
            if (_previewArrow == null) {
                _previewArrow = new GameObject("ResizeEdgeIndicator");
                // Horizontal bar
                var hBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hBar.name = "HBar";
                Object.Destroy(hBar.GetComponent<Collider>());
                hBar.transform.SetParent(_previewArrow.transform, false);
                hBar.transform.localScale = new Vector3(1f, 0.22f, 0.22f);
                hBar.GetComponent<MeshRenderer>().material = _previewArrowMat;
                // Vertical bar
                var vBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vBar.name = "VBar";
                Object.Destroy(vBar.GetComponent<Collider>());
                vBar.transform.SetParent(_previewArrow.transform, false);
                vBar.transform.localScale = new Vector3(0.22f, 1f, 0.22f);
                vBar.GetComponent<MeshRenderer>().material = _previewArrowMat;
            }
            _previewArrow.transform.localScale = Vector3.one * 0.18f;
            _previewArrow.transform.position   =
                AluminumResizer.CalcArrowPos(rd.transform, rd.holeCount, resizeFromLeft);
            _previewArrow.SetActive(true);
        }

        /// <summary>
        /// Restores the part to its pre-preview state and destroys any addition
        /// ghost GameObjects.  Safe to call even when no preview is active.
        /// </summary>
        private void ClearHolePreview() {
            // ── Restore the actual part mesh (removal preview) ────────────────────
            // Only restore if the MeshFilter still holds the exact preview mesh we
            // assigned.  If something external (undo/redo) already swapped the mesh
            // out, skip the restore to avoid overwriting the correct external state.
            if (_previewMeshFilter != null && _previewDisplayMesh != null) {
                if (_previewMeshFilter.sharedMesh == _previewDisplayMesh) {
                    _previewMeshFilter.sharedMesh = _previewOriginalMesh;
                    Object.Destroy(_previewDisplayMesh); // free the temporary mesh
                }
            }
            if (_previewMeshRenderer != null && _previewOriginalMats != null)
                _previewMeshRenderer.sharedMaterials = _previewOriginalMats;

            _previewMeshFilter   = null;
            _previewMeshRenderer = null;
            _previewOriginalMesh = null;
            _previewOriginalMats = null;
            _previewDisplayMesh  = null;

            // ── Destroy addition ghost GameObjects ────────────────────────────────
            foreach (var obj in _previewObjs)
                if (obj != null) Object.Destroy(obj);
            _previewObjs.Clear();

            if (_previewArrow != null)
                _previewArrow.SetActive(false);

            // Allow RefreshHoleSection to sync the input field again.
            _holePendingChange = false;
        }

        // ── Preview material helper ───────────────────────────────────────────────

        /// <summary>Creates a Standard-shader material in Transparent mode.</summary>
        private static Material MakeTransparentMat(Color color) {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            return mat;
        }

        /// <summary>
        /// Creates a simple checkbox Toggle with a dark background and blue checkmark.
        /// </summary>
        private Toggle MakeToggle(string name, Transform parent) {
            var go = MakeEl(name, parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 16f);

            // Background
            var bgGO = MakeEl("Bg", go.transform);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            // Noticeably lighter than the panel background so the checkbox is easy
            // to see. InputBg (0.15) was too close to PanelBg (0.14).
            bgImg.color = new Color(0.35f, 0.35f, 0.38f, 1f);
            if (roundedSprite != null) {
                bgImg.sprite = roundedSprite;
                bgImg.type   = Image.Type.Sliced;
                bgImg.pixelsPerUnitMultiplier = 10f;
            }

            // Checkmark (blue filled square, shown when isOn)
            var ckGO = MakeEl("Ck", bgGO.transform);
            var ckRT = ckGO.GetComponent<RectTransform>();
            ckRT.anchorMin = Vector2.zero; ckRT.anchorMax = Vector2.one;
            ckRT.offsetMin = new Vector2(3f, 3f); ckRT.offsetMax = new Vector2(-3f, -3f);
            var ckImg = ckGO.AddComponent<Image>();
            ckImg.color = HeaderColor; // blue fill when checked
            if (roundedSprite != null) {
                ckImg.sprite = roundedSprite;
                ckImg.type   = Image.Type.Sliced;
                ckImg.pixelsPerUnitMultiplier = 10f;
            }

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic       = ckImg;
            toggle.isOn          = false;

            return toggle;
        }
    }
}
