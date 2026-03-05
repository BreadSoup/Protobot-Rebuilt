using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Protobot.StateSystems;
using Protobot.Outlining;
using Protobot.InputEvents;
using Protobot.SelectionSystem;

namespace Protobot {
    /// <summary>
    /// Circular 6-button radial context menu.
    ///
    /// Open/close: configurable keybind (default X).  Left-click executes,
    /// X or Escape closes without action.
    ///
    /// Button layout (6 equal 60° slices, clockwise from top):
    ///   0 = Flip       — 12 o'clock  — mirror selection in-place along chosen axis
    ///   1 = Copy       —  2 o'clock  — store selection in clipboard
    ///   2 = Paste      —  4 o'clock  — instantiate clipboard at cursor (greyed when empty)
    ///   3 = —          —  6 o'clock  — placeholder
    ///   4 = Mirror     —  8 o'clock  — mirror-duplicate; button splits into ±side sub-halves
    ///   5 = —          — 10 o'clock  — placeholder
    ///
    /// Mirror sub-halves (shown when hovering Mirror, based on current axis):
    ///   X axis: + / −   Y axis: Up / Down   Z axis: + / −
    ///   Hovering each half shows the preview plane and ghost on the corresponding side.
    ///
    /// To add more buttons: append to ButtonLabels, add a case to ExecuteButton.
    /// Geometry auto-adjusts to the array length.
    ///
    /// Self-spawning — no scene wiring needed.
    /// </summary>
    public class RadialMenuUI : MonoBehaviour {

        // ── Geometry ──────────────────────────────────────────────────────────
        private const float OuterR    = 65f;
        private const float InnerR    = 16f;
        private const float LabelR    = 44f;
        private const float MirrorGap = 0.5f;   // world-units gap between original and mirror copy

        // ── Button definitions ────────────────────────────────────────────────
        // Append here to add buttons; geometry auto-adjusts. Add a case to ExecuteButton.
        private static readonly string[] ButtonLabels = {
            "Flip",    // 0
            "Copy",    // 1
            "Paste",   // 2
            "—",       // 3  placeholder (was Duplicate)
            "Mirror",  // 4
            "—",       // 5  placeholder
        };

        private static int N => ButtonLabels.Length;

        // ── Colours (Protobot palette) ─────────────────────────────────────────
        private static readonly Color PanelBg    = new Color(0.14f, 0.14f, 0.14f, 0.92f);
        private static readonly Color HoverCol   = new Color(0.22f, 0.45f, 0.78f, 0.95f);
        private static readonly Color DisabledBg = new Color(0.14f, 0.14f, 0.14f, 0.45f);
        private static readonly Color RingBg     = new Color(0.07f, 0.07f, 0.08f, 0.95f);
        private static readonly Color CentreCol  = new Color(0.10f, 0.10f, 0.12f, 1.00f);
        private static readonly Color TextOn     = new Color(0.92f, 0.92f, 0.95f, 1.00f);
        private static readonly Color TextOff    = new Color(0.35f, 0.35f, 0.38f, 1.00f);

        // Axis accent colours (match Protobot ColorX/Y/Z)
        private static readonly string[] AxisHex   = { "BF3838", "33A633", "3366CC" };
        private static readonly string[] AxisNames  = { "X", "Y", "Z" };

        // ── Keybind (shared with RadialMenuKeybindRow) ─────────────────────────
        /// <summary>
        /// Shared RebindAction so the keybind settings row can read/update the binding.
        /// Default: X key.  Persisted via PlayerPrefs key "RadialMenu.Open".
        /// </summary>
        public static RebindAction SharedRebind { get; private set; }

        // ── UI References ─────────────────────────────────────────────────────
        private Canvas     _canvas;
        private GameObject _menuRoot;
        private Image[]    _sliceImgs;
        private Text[]     _labelTexts;

        // Mirror sub-slice UI (shown when hovering Mirror to pick ± side)
        private Image _mirrorSubImgA, _mirrorSubImgB;
        private Text  _mirrorSubLblA, _mirrorSubLblB;

        // ── State ─────────────────────────────────────────────────────────────
        private bool    _open    = false;
        private Vector2 _openPos;
        private int     _hovered = -1;
        private int     _mirrorSide = -1;    // -1=none  0=positive side  1=negative side
        private int     _axis    = 0;        // 0=X  1=Y  2=Z
        private float   _scrollCd = 0f;
        private Bounds  _bounds;
        private List<GameObject> _selectedAtOpen;   // captured at OpenMenu for ghost preview

        // ── Clipboard ─────────────────────────────────────────────────────────
        private static readonly List<GameObject> _clipboard = new List<GameObject>();

        // ── Scene refs ────────────────────────────────────────────────────────
        private MovementManager   _mm;
        private SelectionManager  _sm;
        private AxisPreviewPlanes _preview;
        private GhostPreview      _ghost;

        // ── Shared sprite ─────────────────────────────────────────────────────
        private static Sprite _spr;

        // ═════════════════════════════════════════════════════════════════════
        // Bootstrap
        // ═════════════════════════════════════════════════════════════════════

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Spawn() {
            var go = new GameObject("[RadialMenu]");
            DontDestroyOnLoad(go);
            go.AddComponent<RadialMenuUI>();
        }

        // ═════════════════════════════════════════════════════════════════════
        // Lifecycle
        // ═════════════════════════════════════════════════════════════════════

        private void Awake() {
            // Set up configurable keybind (persisted via PlayerPrefs)
            SharedRebind = new RebindAction("RadialMenu.Open");
            if (SharedRebind.IsEmpty)
                SharedRebind.ManuelRebind("<Keyboard>/x");   // default: X key

            BuildCanvas();
            BuildMenu();

            var pg = new GameObject("AxisPreviews");
            pg.transform.SetParent(transform);
            _preview = pg.AddComponent<AxisPreviewPlanes>();

            var gg = new GameObject("GhostPreview");
            gg.transform.SetParent(transform);
            _ghost = gg.AddComponent<GhostPreview>();
        }

        private void Update() {
            if (Keyboard.current == null) return;

            // Check the configured keybind (falls back to X if no rebind saved)
            bool menuKeyDown = SharedRebind.action.WasPressedThisFrame();

            if (menuKeyDown) {
                if (_open) CloseMenu(false);
                else       OpenMenu();
                return;
            }

            if (!_open) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame) { CloseMenu(false); return; }

            UpdateHover();
            UpdateScrollAxis();
            RefreshColors();

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                CloseMenu(_hovered >= 0 && IsEnabled(_hovered, HasSel()));
        }

        // ═════════════════════════════════════════════════════════════════════
        // Open / Close
        // ═════════════════════════════════════════════════════════════════════

        private void OpenMenu() {
            if (_mm == null) _mm = FindObjectOfType<MovementManager>();

            var sel  = MirrorSystem.GetSelectedObjects(_mm);
            bool has = sel != null && sel.Count > 0;
            _bounds         = has ? MirrorSystem.GetBounds(sel) : new Bounds();
            _selectedAtOpen = has ? sel : null;

            _open       = true;
            _hovered    = -1;
            _mirrorSide = -1;
            _openPos    = Mouse.current != null
                        ? Mouse.current.position.ReadValue()
                        : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            var rt = _menuRoot.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = _openPos;
            _menuRoot.SetActive(true);

            ApplyMirrorSplitVisual();
            RefreshAxisLabels();
            RefreshColors();
        }

        private void CloseMenu(bool fireAction) {
            // Hide the UI first so AvoidUISelectionCondition sees overUI=false
            // when ExecuteButton → TrySelectClones runs.
            _open = false;
            _menuRoot.SetActive(false);
            _preview.HideAll();
            _ghost.HideGhosts();

            if (fireAction) ExecuteButton(_hovered);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Per-Frame
        // ═════════════════════════════════════════════════════════════════════

        private void UpdateHover() {
            if (Mouse.current == null) { SetHovered(-1); return; }

            Vector2 local = Mouse.current.position.ReadValue() - _openPos;
            float   dist  = local.magnitude;

            if (dist < InnerR || dist > OuterR + 12f) { SetHovered(-1); return; }

            float angleDeg  = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
            float cwFromTop = (90f - angleDeg + 360f) % 360f;
            float sliceDeg  = 360f / N;
            int   idx = (int)((cwFromTop + sliceDeg * 0.5f) % 360f / sliceDeg);
            SetHovered(idx);

            // If hovering Mirror, also track which sub-half
            if (idx == 4) {
                // Mirror slice spans 210-270° CW; split at 240°
                int newSide = (cwFromTop < 240f) ? 0 : 1;
                if (newSide != _mirrorSide) {
                    _mirrorSide = newSide;
                    ApplyMirrorSplitVisual();
                    ApplyPreview(4);
                }
            }
        }

        private void SetHovered(int idx) {
            if (idx == _hovered) return;
            int prev = _hovered;
            _hovered = idx;
            if (idx != 4) {
                _mirrorSide = -1;
                if (prev == 4) ApplyMirrorSplitVisual();
            }
            ApplyPreview(idx);
        }

        private void UpdateScrollAxis() {
            if (Mouse.current == null) return;
            _scrollCd -= Time.deltaTime;
            if (_scrollCd > 0f) return;
            if (_hovered != 0 && _hovered != 4) return;

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            _axis = (_axis + 3 + (scroll > 0f ? 1 : -1)) % 3;
            _scrollCd = 0.15f;
            RefreshAxisLabels();
            ApplyMirrorSplitVisual();   // update sub-slice labels for new axis
            ApplyPreview(_hovered);
        }

        private void RefreshColors() {
            bool hasSel = HasSel();
            for (int i = 0; i < N; i++) {
                bool enabled = IsEnabled(i, hasSel);
                _sliceImgs[i].color  = !enabled    ? DisabledBg
                                     : i == _hovered ? HoverCol
                                                     : PanelBg;
                _labelTexts[i].color = enabled ? TextOn : TextOff;
            }

            // Sub-slice colours
            if (_hovered == 4 && HasSel()) {
                _mirrorSubImgA.color = _mirrorSide == 0 ? HoverCol : PanelBg;
                _mirrorSubImgB.color = _mirrorSide == 1 ? HoverCol : PanelBg;
            }
        }

        // ── Mirror sub-slice visual ────────────────────────────────────────────
        private void ApplyMirrorSplitVisual() {
            bool showSplit = _open && _hovered == 4;

            // Toggle: main Mirror slice vs. two sub-slices
            _sliceImgs[4].gameObject.SetActive(!showSplit);
            _mirrorSubImgA.gameObject.SetActive(showSplit);
            _mirrorSubImgB.gameObject.SetActive(showSplit);

            // Sub-slice labels are separate GOs — must be toggled independently
            _mirrorSubLblA.gameObject.SetActive(showSplit);
            _mirrorSubLblB.gameObject.SetActive(showSplit);

            if (showSplit) {
                _mirrorSubLblA.text = SubLabelA(_axis);
                _mirrorSubLblB.text = SubLabelB(_axis);
            }
        }

        // Y axis: Sub-A (225° CW = lower on screen) = Down = -Y
        //         Sub-B (255° CW = higher on screen) = Up   = +Y
        // X/Z:   Sub-A = positive (+), Sub-B = negative (−)
        private static string SubLabelA(int axis) => axis == 1 ? "Down" : "+";
        private static string SubLabelB(int axis) => axis == 1 ? "Up"   : "–";

        // ═════════════════════════════════════════════════════════════════════
        // Preview planes & ghost
        // ═════════════════════════════════════════════════════════════════════

        private void ApplyPreview(int btn) {
            bool hasBounds = _open && _bounds.size != Vector3.zero;
            if (!hasBounds || btn < 0) { _preview.HideAll(); _ghost.HideGhosts(); return; }

            switch (btn) {
                case 0:
                    _ghost.HideGhosts();
                    _preview.ShowFlipPlane(_bounds, _axis);
                    break;

                case 4:
                    if (_mirrorSide < 0) { _preview.HideAll(); _ghost.HideGhosts(); break; }
                    // Y axis: Sub-A=Down=-Y, Sub-B=Up=+Y → sign is flipped vs X/Z
                    int   sign = (_axis == 1) ? (_mirrorSide == 0 ? -1 : 1)
                                              : (_mirrorSide == 0 ?  1 : -1);
                    var   n    = AxisNormal(_axis) * sign;
                    float half = Mathf.Abs(Vector3.Dot(_bounds.extents, n));
                    var   ctr  = _bounds.center + n * (half + MirrorGap * 0.5f);
                    _preview.ShowMirrorPlaneAt(ctr, _bounds, _axis);
                    if (_selectedAtOpen != null)
                        _ghost.ShowGhosts(_selectedAtOpen, ctr, n);
                    break;

                default:
                    _preview.HideAll();
                    _ghost.HideGhosts();
                    break;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // Button state & actions
        // ═════════════════════════════════════════════════════════════════════

        private static bool IsEnabled(int btn, bool hasSel) {
            if (btn == 3 || btn == 5) return false;          // placeholders
            if (btn == 2) return _clipboard.Count > 0;       // Paste: needs clipboard
            return hasSel;
        }

        private void ExecuteButton(int btn) {
            if (!IsEnabled(btn, HasSel())) return;
            switch (btn) {
                case 0: DoFlip();      break;
                case 1: DoCopy();      break;
                case 2: DoPaste();     break;
                case 4: DoMirrorDup(); break;
            }
        }

        // ── Flip ──────────────────────────────────────────────────────────────
        private void DoFlip() {
            var objs = GetObjs(); if (objs == null) return;
            MirrorSystem.MirrorObjects(objs, _bounds.center, AxisNormal(_axis));
        }

        // ── Copy ──────────────────────────────────────────────────────────────
        private void DoCopy() {
            var objs = GetObjs(); if (objs == null) return;
            _clipboard.Clear();
            _clipboard.AddRange(objs);
        }

        // ── Paste at cursor (world position where menu was opened) ─────────────
        private void DoPaste() {
            _clipboard.RemoveAll(o => o == null);
            if (_clipboard.Count == 0) return;

            var clones = Clones(_clipboard);

            // Project the screen position where M was pressed onto a horizontal world
            // plane at the average Y of the clipboard items, then offset clones there.
            var cam = Camera.main;
            if (cam != null) {
                float avgY = _clipboard.Average(o => o.transform.position.y);
                var worldPlane = new Plane(Vector3.up, new Vector3(0f, avgY, 0f));
                Ray ray = cam.ScreenPointToRay(_openPos);
                if (worldPlane.Raycast(ray, out float t)) {
                    Vector3 pasteCenter = ray.GetPoint(t);
                    Vector3 srcCenter   = MirrorSystem.GetBounds(_clipboard).center;
                    Vector3 offset      = new Vector3(
                        pasteCenter.x - srcCenter.x, 0f,
                        pasteCenter.z - srcCenter.z);
                    foreach (var c in clones) c.transform.position += offset;
                }
            }
            RegisterWithUndo(clones);
        }

        // ── Mirror Duplicate (with ± side from _mirrorSide) ───────────────────
        private void DoMirrorDup() {
            var objs = GetObjs(); if (objs == null) return;
            // Y axis: Sub-A=Down=-Y, Sub-B=Up=+Y → sign flipped vs X/Z
            int   sign = (_axis == 1) ? (_mirrorSide == 0 ? -1 : 1)
                                      : (_mirrorSide == 0 ?  1 : -1);
            var   n    = AxisNormal(_axis) * sign;
            float half = Mathf.Abs(Vector3.Dot(_bounds.extents, n));
            var clones = MirrorSystem.MirrorDuplicate(
                objs,
                _bounds.center + n * (half + MirrorGap * 0.5f),
                n);
            // Select the new mirrored parts so they can be moved immediately
            TrySelectClones(clones);
        }

        /// <summary>
        /// Starts a one-frame-deferred coroutine to highlight <paramref name="clones"/>
        /// after a mirror-duplicate operation.
        ///
        /// Why deferred?  The radial menu canvas is still active when ExecuteButton runs.
        /// Waiting one frame lets the EventSystem clear the UI-hover flag so unrelated
        /// conditions (AvoidUISelectionCondition) do not interfere.
        /// </summary>
        private void TrySelectClones(List<GameObject> clones) {
            if (clones == null || clones.Count == 0) return;
            StartCoroutine(SelectNextFrame(clones));
        }

        /// <summary>
        /// Highlights the mirror-duplicate clones with the selection outline until the
        /// user hovers over a different part, so they can see which parts were just created.
        ///
        /// Why not SetCurrent?
        ///   HoverSelector fires clearEvent every frame when the mouse is not over any
        ///   collider.  That immediately calls ClearCurrent → DisableOutline, wiping the
        ///   outline in Frame+1.  We suppress only the per-frame clear (not hover-set)
        ///   via HoverSelector.SuppressClear so the outline stays visible during the hold
        ///   period while still allowing normal hover-highlighting of other parts.
        ///
        ///   We deliberately do not force-assign sm.current: the clone's tag fails
        ///   TagSelectionCondition, and if allowClearing=false on that condition, a
        ///   forced assignment would permanently jam the clearing pipeline.
        /// </summary>
        private IEnumerator SelectNextFrame(List<GameObject> clones) {
            yield return null;   // one frame — canvas hidden, EventSystem updates overUI

            clones.RemoveAll(c => c == null);
            if (clones.Count == 0) yield break;

            if (_sm == null) _sm = FindObjectOfType<SelectionManager>();

            // Clear the previously-selected part's outline so ONLY the clone glows.
            _sm?.ClearCurrent();

            // Suppress HoverSelector's per-frame "mouse over nothing → clear" event so
            // the outline we apply here is not immediately wiped on the next Update().
            HoverSelector.SuppressClear = true;

            foreach (var c in clones)
                c.EnableOutline(0, 1, 0.15f);

            // Keep the highlight until the user hovers over a part that is NOT one of
            // the clones (or a child collider thereof).  A large safety cap (3600 frames
            // ≈ 60 s) prevents the coroutine from leaking if nothing is ever hovered.
            for (int i = 0; i < 3600; i++) {
                yield return null;
                clones.RemoveAll(c => c == null);
                if (clones.Count == 0) break;
                // sm.current is set when HoverSelector finds a part under the mouse.
                // If it's NOT one of our clones (or a child thereof) the user has moved
                // to something else — stop holding the highlight.
                if (_sm != null && _sm.current?.gameObject != null
                    && !IsCloneOrDescendant(clones, _sm.current.gameObject))
                    break;
            }

            // Remove the highlight when the hold ends.
            foreach (var c in clones)
                if (c != null) c.DisableOutline();

            HoverSelector.SuppressClear = false;
        }

        /// <summary>
        /// Returns true if <paramref name="obj"/> is one of <paramref name="clones"/>
        /// or a child (any depth) of one of them.  Needed because HoverSelector returns
        /// a child collider object, not the part root.
        /// </summary>
        private static bool IsCloneOrDescendant(List<GameObject> clones, GameObject obj) {
            for (Transform t = obj.transform; t != null; t = t.parent)
                if (clones.Contains(t.gameObject)) return true;
            return false;
        }

        // ── Shared helpers ────────────────────────────────────────────────────
        private List<GameObject> GetObjs() {
            if (_mm == null) _mm = FindObjectOfType<MovementManager>();
            return MirrorSystem.GetSelectedObjects(_mm);
        }

        private bool HasSel() {
            if (_mm == null) _mm = FindObjectOfType<MovementManager>();
            return _mm != null && _mm.MovingObj != null;
        }

        private static Vector3 AxisNormal(int a) =>
            a == 0 ? Vector3.right : a == 1 ? Vector3.up : Vector3.forward;

        private static List<GameObject> Clones(List<GameObject> srcs) {
            var list = new List<GameObject>(srcs.Count);
            foreach (var src in srcs) {
                if (src == null) continue;
                var c = Instantiate(src, src.transform.position, src.transform.rotation);
                c.DisableOutline();
                var r = c.GetComponent<Renderer>(); var sr = src.GetComponent<Renderer>();
                if (r != null && sr != null) r.material = new Material(sr.material);
                list.Add(c);
            }
            return list;
        }

        private static void RegisterWithUndo(List<GameObject> clones) {
            var pre = new List<IElement>(clones.Count);
            foreach (var c in clones) { var e = new ObjectElement(c); e.existing = false; pre.Add(e); }
            StateSystem.AddElements(pre);
            var post = new List<IElement>(clones.Count);
            foreach (var c in clones) post.Add(new ObjectElement(c));
            StateSystem.AddState(new State(post));
        }

        // ── Axis label helpers ────────────────────────────────────────────────
        private void RefreshAxisLabels() {
            string line = BuildAxisLine();
            if (N > 0) _labelTexts[0].text = "Flip\n" + line;
            // Mirror button (4) has no axis text — the sub-slice labels show the direction
        }

        private string BuildAxisLine() {
            var p = new string[3];
            for (int i = 0; i < 3; i++)
                p[i] = i == _axis
                     ? $"<b><color=#{AxisHex[i]}>{AxisNames[i]}</color></b>"
                     : $"<color=#484850>{AxisNames[i]}</color>";
            return string.Join(" ", p);
        }

        // ═════════════════════════════════════════════════════════════════════
        // UI Construction
        // ═════════════════════════════════════════════════════════════════════

        private void BuildCanvas() {
            var cg = new GameObject("RadialCanvas");
            cg.transform.SetParent(transform);
            _canvas = cg.AddComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 999;
            cg.AddComponent<CanvasScaler>();
            cg.AddComponent<GraphicRaycaster>();
        }

        private void BuildMenu() {
            int   n    = N;
            float deg  = 360f / n;
            float outerD = OuterR * 2f;

            var rootGO = new GameObject("MenuRoot");
            rootGO.transform.SetParent(_canvas.transform, false);
            var rootRT = rootGO.AddComponent<RectTransform>();
            rootRT.sizeDelta = Vector2.zero;
            rootRT.anchorMin = rootRT.anchorMax = new Vector2(0.5f, 0.5f);
            _menuRoot = rootGO;
            _menuRoot.SetActive(false);

            // Background disc
            CircleGO("Bg", _menuRoot.transform, Vector2.zero, outerD + 6f, RingBg);

            // Main pie slices
            _sliceImgs  = new Image[n];
            _labelTexts = new Text[n];

            for (int i = 0; i < n; i++) {
                float stdRad = (90f - i * deg) * Mathf.Deg2Rad;
                var sg  = CircleGO($"Slice{i}", _menuRoot.transform, Vector2.zero, outerD, PanelBg);
                var img = sg.GetComponent<Image>();
                img.type          = Image.Type.Filled;
                img.fillMethod    = Image.FillMethod.Radial360;
                img.fillOrigin    = (int)Image.Origin360.Top;
                img.fillClockwise = true;
                img.fillAmount    = 1f / n;
                sg.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, 30f - i * deg);
                _sliceImgs[i] = img;

                var lp  = new Vector2(Mathf.Cos(stdRad) * LabelR, Mathf.Sin(stdRad) * LabelR);
                bool rt = i == 0 || i == 4;
                var lgo = LabelGO($"Lbl{i}", _menuRoot.transform, lp,
                                  new Vector2(60f, 42f), ButtonLabels[i], 10, rt);
                _labelTexts[i] = lgo.GetComponent<Text>();
            }

            // Mirror sub-slices (each 30°, hidden until Mirror is hovered)
            // Mirror is slice 4, centred at 240° CW.
            // Sub-A centred at 225° CW, Sub-B at 255° CW.
            // fillAmount = 1/12  |  rotation = 15 - centreCW
            // Sub-A label centred in its wedge (225° CW), Sub-B centred in its wedge (255° CW).
            // Using the centre angle keeps labels 15° clear of every spoke line.
            BuildMirrorSubSlice(225f, out _mirrorSubImgA, out _mirrorSubLblA, "SubA");
            BuildMirrorSubSlice(255f, out _mirrorSubImgB, out _mirrorSubLblB, "SubB");

            // Spoke dividers
            for (int i = 0; i < n; i++) {
                float cwBound = i * deg + deg * 0.5f;
                SpokeGO($"Spoke{i}", _menuRoot.transform, OuterR, 1.5f, RingBg, cwBound);
            }

            // Centre cap
            CircleGO("Centre", _menuRoot.transform, Vector2.zero, InnerR * 2f + 4f, CentreCol);

            RefreshAxisLabels();
        }

        /// <summary>
        /// Creates one 30° sub-slice (1/12 fill) centred at <paramref name="centreCW"/>
        /// degrees clockwise from top, plus a small label at the same angle pushed out
        /// to ~80 % of OuterR.  Using the centre angle (not a corner) keeps the label
        /// 15° clear of the nearest spoke line on either side.
        ///
        /// Both the slice image and the label start hidden; ApplyMirrorSplitVisual
        /// shows/hides them together when Mirror is hovered.
        /// </summary>
        private void BuildMirrorSubSlice(float centreCW,
                                         out Image img, out Text lbl, string name) {
            float outerD = OuterR * 2f;
            var go = CircleGO($"Mirror{name}", _menuRoot.transform, Vector2.zero, outerD, PanelBg);
            var i  = go.GetComponent<Image>();
            i.type          = Image.Type.Filled;
            i.fillMethod    = Image.FillMethod.Radial360;
            i.fillOrigin    = (int)Image.Origin360.Top;
            i.fillClockwise = true;
            i.fillAmount    = 1f / 12f;
            // rotation = 15 - centreCW  (15° is natural offset for a 30° slice)
            go.GetComponent<RectTransform>().localRotation =
                Quaternion.Euler(0f, 0f, 15f - centreCW);
            img = i;

            // Label at centre angle, pushed to ~80 % of OuterR — outer half of the wedge,
            // well clear of any spoke line (spokes sit at the 60° slice boundaries).
            float stdRad      = (90f - centreCW) * Mathf.Deg2Rad;
            float labelRadius = OuterR * 0.80f;   // ≈ 52 px
            var   lp = new Vector2(Mathf.Cos(stdRad) * labelRadius,
                                   Mathf.Sin(stdRad) * labelRadius);
            var lgo = LabelGO($"Lbl{name}", _menuRoot.transform, lp,
                              new Vector2(24f, 20f), "", 11, false);
            lbl = lgo.GetComponent<Text>();
            lgo.SetActive(false);   // hidden until Mirror is hovered

            go.SetActive(false);
        }

        // ── UI element factories ──────────────────────────────────────────────

        private static GameObject CircleGO(string name, Transform parent,
                                           Vector2 pos, float diameter, Color col) {
            var go  = BaseGO(name, parent, pos, Vector2.one * diameter);
            var img = go.AddComponent<Image>();
            img.sprite = GetCircleSprite();
            img.type   = Image.Type.Simple;
            img.color  = col;
            return go;
        }

        private static void SpokeGO(string name, Transform parent,
                                    float length, float width, Color col, float cwDeg) {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(width, length);
            rt.localRotation    = Quaternion.Euler(0f, 0f, -cwDeg);
            go.AddComponent<Image>().color = col;
        }

        private static GameObject LabelGO(string name, Transform parent,
                                          Vector2 pos, Vector2 size,
                                          string text, int fontSize, bool richText) {
            var go = BaseGO(name, parent, pos, size);
            var t  = go.AddComponent<Text>();
            t.text            = text;
            t.font            = GetFont();
            t.fontSize        = fontSize;
            t.color           = TextOff;
            t.alignment       = TextAnchor.MiddleCenter;
            t.supportRichText = richText;
            return go;
        }

        private static GameObject BaseGO(string name, Transform parent, Vector2 pos, Vector2 size) {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        // ── Shared circle sprite ──────────────────────────────────────────────

        private static Sprite GetCircleSprite() {
            if (_spr != null) return _spr;
            const int W = 256; const float R = W * 0.5f;
            var tex = new Texture2D(W, W, TextureFormat.ARGB32, false) {
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < W; y++)
            for (int x = 0; x < W; x++) {
                float dx = x - R + 0.5f, dy = y - R + 0.5f;
                float a  = Mathf.Clamp01(1f - (Mathf.Sqrt(dx*dx + dy*dy) - (R - 2f)) / 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            _spr = Sprite.Create(tex, new Rect(0, 0, W, W), new Vector2(0.5f, 0.5f), 100f);
            return _spr;
        }

        // ── Font ──────────────────────────────────────────────────────────────

        private static Font GetFont() {
            // Explicit null check — ?? does not work with Unity's overridden == operator.
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
