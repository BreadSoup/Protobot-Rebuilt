using Parts_List;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
        private ObjectLink selectedObject;
        private Font       uiFont;
        private Sprite     roundedSprite;

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

        // ── Preview objects (3-D overlays in world space) ────────────────────────
        // Red (removal) or green (addition) ghost mesh + blue edge indicator.
        private readonly System.Collections.Generic.List<GameObject> _previewObjs
            = new System.Collections.Generic.List<GameObject>();
        private GameObject _previewArrow;   // blue sphere at the modified edge
        private Material   _previewRemoveMat, _previewAddMat, _previewArrowMat;

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
                ClearHolePreview(); // remove 3-D overlays when nothing is selected
                return;
            }

            RefreshInfo();
            RefreshTransform();
            RefreshHoleSection();
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

            // Keep the displayed number in sync unless the user is typing.
            if (!holeCountInput.isFocused)
                holeCountInput.SetTextWithoutNotify(rd.holeCount.ToString());
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

            // 2. Move the part so the chosen edge stays world-fixed.
            rd.transform.position = AluminumResizer.CalcNewPosition(
                rd.transform, rd.holeCount, newCount, keepRightEdge: resizeFromLeft);

            // 3. Rebuild mesh + holes + metadata.
            AluminumResizer.ApplyResize(rd, newCount);

            // 4. Push POST-resize snapshot so Ctrl+Y can redo.
            Protobot.StateSystems.StateSystem.AddState(
                new Protobot.StateSystems.ResizeElement(rd));

            // 5. Clear the preview — the real mesh is now correct.
            ClearHolePreview();
            holeCountInput.SetTextWithoutNotify(newCount.ToString());
        }

        // ── Preview helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Creates (or refreshes) the 3-D red/green ghost mesh and blue edge
        /// indicator that show which sections will be removed or added.
        /// </summary>
        private void ShowHolePreview(AluminumResizeData rd, int targetCount) {
            ClearHolePreview();

            bool removing   = targetCount < rd.holeCount;
            int  absDelta   = Mathf.Abs(targetCount - rd.holeCount);
            Material mat    = removing ? _previewRemoveMat : _previewAddMat;

            // Build a mesh for exactly the changed sections using AluminumSubParts.
            Mesh deltaMesh = rd.subParts.GetMesh(absDelta);

            // World position for the delta-mesh ghost.
            Vector3 previewCenter = AluminumResizer.CalcPreviewCenter(
                rd.transform, rd.holeCount, targetCount, modifyLeft: resizeFromLeft);

            var ghost = new GameObject("ResizePreviewMesh");
            ghost.transform.position   = previewCenter;
            ghost.transform.rotation   = rd.transform.rotation;
            // Scale up slightly to prevent Z-fighting with the real part mesh.
            ghost.transform.localScale = Vector3.one * 1.005f;
            var mf = ghost.AddComponent<MeshFilter>();
            var mr = ghost.AddComponent<MeshRenderer>();
            mf.sharedMesh    = deltaMesh;
            mr.sharedMaterial = mat;
            _previewObjs.Add(ghost);

            // Blue sphere indicator at the modified edge.
            if (_previewArrow == null) {
                _previewArrow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _previewArrow.name = "ResizeEdgeIndicator";
                Object.Destroy(_previewArrow.GetComponent<Collider>());
                _previewArrow.GetComponent<MeshRenderer>().material = _previewArrowMat;
            }
            _previewArrow.transform.localScale = Vector3.one * 0.18f;
            _previewArrow.transform.position   =
                AluminumResizer.CalcArrowPos(rd.transform, rd.holeCount, resizeFromLeft);
            _previewArrow.SetActive(true);
        }

        /// <summary>Destroys all 3-D preview objects and hides the edge indicator.</summary>
        private void ClearHolePreview() {
            foreach (var obj in _previewObjs)
                if (obj != null) Object.Destroy(obj);
            _previewObjs.Clear();

            if (_previewArrow != null)
                _previewArrow.SetActive(false);
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
