using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Protobot.StateSystems;
using Protobot.Outlining;

namespace Protobot {
    /// <summary>
    /// Circular radial context menu — press M to open, left-click to execute, M or Escape to close.
    ///
    /// Button layout (6 equal 60° slices, clockwise from top):
    ///   0 = Flip       — 12 o'clock  — mirrors selection in-place along chosen axis
    ///   1 = Copy       —  2 o'clock  — stores selection in clipboard
    ///   2 = Paste      —  4 o'clock  — instantiates clipboard (greyed when empty)
    ///   3 = Duplicate  —  6 o'clock  — clones selection in-place
    ///   4 = Mirror     —  8 o'clock  — mirror-duplicate, offset so clone doesn't overlap
    ///   5 = —          — 10 o'clock  — placeholder (always disabled)
    ///
    /// To add more buttons:
    ///   1. Append a string to <see cref="ButtonLabels"/> — geometry auto-adjusts.
    ///   2. Add a matching case to <see cref="ExecuteButton"/>.
    ///
    /// Self-spawning via RuntimeInitializeOnLoadMethod — no scene wiring needed.
    /// </summary>
    public class RadialMenuUI : MonoBehaviour {

        // ── Geometry ──────────────────────────────────────────────────────────
        private const float OuterR    = 65f;   // outer radius (pixels)
        private const float InnerR    = 16f;   // dead-zone radius (pixels)
        private const float LabelR    = 44f;   // distance from centre to label midpoint
        private const float MirrorGap = 0.5f;  // world-units gap between original and mirror copy

        // ── Button definitions — extend this array to add buttons ─────────────
        private static readonly string[] ButtonLabels = {
            "Flip",       // 0  top
            "Copy",       // 1  top-right
            "Paste",      // 2  bottom-right
            "Duplicate",  // 3  bottom
            "Mirror",     // 4  bottom-left
            "—",          // 5  top-left (placeholder)
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

        // Axis accent colours (match Protobot's ColorX/Y/Z)
        private static readonly string[] AxisHex   = { "BF3838", "33A633", "3366CC" };
        private static readonly string[] AxisNames  = { "X", "Y", "Z" };

        // ── UI references ─────────────────────────────────────────────────────
        private Canvas     _canvas;
        private GameObject _menuRoot;
        private Image[]    _sliceImgs;
        private Text[]     _labelTexts;

        // ── State ─────────────────────────────────────────────────────────────
        private bool    _open    = false;
        private Vector2 _openPos;
        private int     _hovered = -1;
        private int     _axis    = 0;          // 0=X  1=Y  2=Z
        private float   _scrollCd = 0f;
        private Bounds  _bounds;

        // ── Clipboard ─────────────────────────────────────────────────────────
        private static readonly List<GameObject> _clipboard = new List<GameObject>();

        // ── Scene refs ────────────────────────────────────────────────────────
        private MovementManager   _mm;
        private AxisPreviewPlanes _preview;

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
            BuildCanvas();
            BuildMenu();
            var pg = new GameObject("AxisPreviews");
            pg.transform.SetParent(transform);
            _preview = pg.AddComponent<AxisPreviewPlanes>();
        }

        private void Update() {
            if (Keyboard.current == null) return;

            // M key: toggle menu
            if (Keyboard.current.mKey.wasPressedThisFrame) {
                if (_open) CloseMenu(false);
                else       OpenMenu();
                return;
            }

            if (!_open) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame) { CloseMenu(false); return; }

            UpdateHover();
            UpdateScrollAxis();
            RefreshColors();

            // Left-click to execute
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
            _bounds  = has ? MirrorSystem.GetBounds(sel) : new Bounds();

            _open    = true;
            _hovered = -1;
            _openPos = Mouse.current != null
                     ? Mouse.current.position.ReadValue()
                     : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            var rt = _menuRoot.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = _openPos;
            _menuRoot.SetActive(true);

            RefreshAxisLabels();
            RefreshColors();
        }

        private void CloseMenu(bool fireAction) {
            if (fireAction) ExecuteButton(_hovered);
            _open = false;
            _menuRoot.SetActive(false);
            _preview.HideAll();
        }

        // ═════════════════════════════════════════════════════════════════════
        // Per-Frame
        // ═════════════════════════════════════════════════════════════════════

        private void UpdateHover() {
            if (Mouse.current == null) { SetHovered(-1); return; }

            Vector2 local = Mouse.current.position.ReadValue() - _openPos;
            float   dist  = local.magnitude;

            if (dist < InnerR || dist > OuterR + 12f) { SetHovered(-1); return; }

            // Convert screen angle → clockwise degrees from top → slice index.
            float angleDeg  = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
            float cwFromTop = (90f - angleDeg + 360f) % 360f;
            float sliceDeg  = 360f / N;
            // +half-slice offset centres each slice at i*sliceDeg rather than i*sliceDeg+sliceDeg/2
            int   idx = (int)((cwFromTop + sliceDeg * 0.5f) % 360f / sliceDeg);

            SetHovered(idx);
        }

        private void SetHovered(int idx) {
            if (idx == _hovered) return;
            _hovered = idx;
            ApplyPreview(idx);
        }

        private void UpdateScrollAxis() {
            if (Mouse.current == null) return;
            _scrollCd -= Time.deltaTime;
            if (_scrollCd > 0f) return;
            if (_hovered != 0 && _hovered != 4) return;   // only Flip/Mirror use axis

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            _axis = (_axis + 3 + (scroll > 0f ? 1 : -1)) % 3;
            _scrollCd = 0.15f;
            RefreshAxisLabels();
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
        }

        // ═════════════════════════════════════════════════════════════════════
        // Preview planes
        // ═════════════════════════════════════════════════════════════════════

        private void ApplyPreview(int btn) {
            bool hasBounds = _open && _bounds.size != Vector3.zero;
            if (!hasBounds || btn < 0) { _preview.HideAll(); return; }

            switch (btn) {
                case 0: _preview.ShowFlipPlane(_bounds, _axis);   break;
                case 4: _preview.ShowMirrorPlane(_bounds, _axis); break;
                default: _preview.HideAll();                      break;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // Button state & actions
        // ═════════════════════════════════════════════════════════════════════

        private static bool IsEnabled(int btn, bool hasSel) {
            if (btn == 5) return false;                  // placeholder
            if (btn == 2) return _clipboard.Count > 0;  // Paste needs clipboard
            return hasSel;
        }

        private void ExecuteButton(int btn) {
            if (!IsEnabled(btn, HasSel())) return;
            switch (btn) {
                case 0: DoFlip();      break;
                case 1: DoCopy();      break;
                case 2: DoPaste();     break;
                case 3: DoDuplicate(); break;
                case 4: DoMirrorDup(); break;
                // 5 = placeholder, no-op
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

        // ── Paste ─────────────────────────────────────────────────────────────
        private void DoPaste() {
            _clipboard.RemoveAll(o => o == null);
            if (_clipboard.Count == 0) return;
            RegisterWithUndo(Clones(_clipboard));
        }

        // ── Duplicate ─────────────────────────────────────────────────────────
        private void DoDuplicate() {
            var objs = GetObjs(); if (objs == null) return;
            RegisterWithUndo(Clones(objs));
        }

        // ── Mirror Duplicate ──────────────────────────────────────────────────
        // Places the mirror plane at the bounding-box edge + half of MirrorGap,
        // so the cloned copy ends up exactly MirrorGap units away from the original.
        private void DoMirrorDup() {
            var objs = GetObjs(); if (objs == null) return;
            Vector3 n       = AxisNormal(_axis);
            float   halfExt = Mathf.Abs(Vector3.Dot(_bounds.extents, n));
            Vector3 center  = _bounds.center + n * (halfExt + MirrorGap * 0.5f);
            MirrorSystem.MirrorDuplicate(objs, center, n);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
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
            if (N > 0) _labelTexts[0].text = "Flip\n"   + line;
            if (N > 4) _labelTexts[4].text = "Mirror\n" + line;
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

            // Root — zero-size anchor, repositioned each time the menu opens
            var rootGO = new GameObject("MenuRoot");
            rootGO.transform.SetParent(_canvas.transform, false);
            var rootRT = rootGO.AddComponent<RectTransform>();
            rootRT.sizeDelta = Vector2.zero;
            rootRT.anchorMin = rootRT.anchorMax = new Vector2(0.5f, 0.5f);
            _menuRoot = rootGO;
            _menuRoot.SetActive(false);

            // Background disc (6 px wider = dark ring visible between slices)
            CircleGO("Bg", _menuRoot.transform, Vector2.zero, outerD + 6f, RingBg);

            // Pie slices — one full-circle Radial360 image per button, rotated to position
            _sliceImgs  = new Image[n];
            _labelTexts = new Text[n];

            for (int i = 0; i < n; i++) {
                // Standard-math angle for this slice's centre (CCW from right)
                float stdRad = (90f - i * deg) * Mathf.Deg2Rad;

                // Slice image: full circle, Radial360 fill = 1/N
                // Rotating by (30 - i*deg)° centres the filled wedge at i*deg CW from top
                var sg  = CircleGO($"Slice{i}", _menuRoot.transform, Vector2.zero, outerD, PanelBg);
                var img = sg.GetComponent<Image>();
                img.type          = Image.Type.Filled;
                img.fillMethod    = Image.FillMethod.Radial360;
                img.fillOrigin    = (int)Image.Origin360.Top;
                img.fillClockwise = true;
                img.fillAmount    = 1f / n;
                sg.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, 30f - i * deg);
                _sliceImgs[i] = img;

                // Label centred on this slice
                var lp  = new Vector2(Mathf.Cos(stdRad) * LabelR, Mathf.Sin(stdRad) * LabelR);
                bool rt = i == 0 || i == 4;   // Flip and Mirror need rich text for axis colours
                var lgo = LabelGO($"Lbl{i}", _menuRoot.transform, lp,
                                  new Vector2(60f, 42f), ButtonLabels[i], 10, rt);
                _labelTexts[i] = lgo.GetComponent<Text>();
            }

            // Thin spoke dividers at each slice boundary
            for (int i = 0; i < n; i++) {
                float cwBound = i * deg + deg * 0.5f;   // boundary between slice i and i+1
                SpokeGO($"Spoke{i}", _menuRoot.transform, OuterR, 1.5f, RingBg, cwBound);
            }

            // Centre cap over the dead zone
            CircleGO("Centre", _menuRoot.transform, Vector2.zero, InnerR * 2f + 4f, CentreCol);

            RefreshAxisLabels();
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

        // Spoke: pivots from the menu centre outward, so rotation is around the centre.
        private static void SpokeGO(string name, Transform parent,
                                    float length, float width, Color col, float cwDeg) {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0f);   // pivot at bottom = menu centre
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
            // Explicit null check — ?? doesn't work with Unity's overridden == operator.
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
