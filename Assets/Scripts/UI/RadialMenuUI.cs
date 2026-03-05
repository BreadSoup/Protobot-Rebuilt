using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Protobot.StateSystems;
using Protobot.Outlining;

namespace Protobot {
    /// <summary>
    /// Circular pie-slice radial menu that opens on a short right-click tap.
    ///
    /// Two slices:
    ///   RIGHT → "Copy"  — duplicates the selection in place.
    ///   LEFT  → "Flip"  — mirrors the selection along the chosen axis.
    ///                      Scroll wheel while hovering cycles X / Y / Z.
    ///
    /// Visual behaviour:
    ///   • Hovered slice smoothly pops outward (away from centre).
    ///   • Hover colour: blue accent.
    ///   • Greyed out when nothing is selected.
    ///   • Short right-click tap opens; another tap, Escape, or left-click
    ///     outside the ring closes.
    ///   • Long right-drags (camera orbit) are ignored.
    ///
    /// Self-spawning — no scene wiring needed.
    /// </summary>
    public class RadialMenuUI : MonoBehaviour {

        // ── Geometry ──────────────────────────────────────────────────────────
        private const float OuterR   = 130f; // outer radius of the pie disc (px)
        private const float CentreR  =  16f; // radius of the small centre circle
        private const float LabelR   =  82f; // radial distance to the label centres
        private const float PopDist  =  16f; // max pop-out distance on hover (px)
        private const float PopSpeed =  10f; // lerp speed for pop animation
        private const float MaxTap   = 0.20f;// right-click tap threshold (seconds)
        private const float ScrollCd = 0.20f;// minimum seconds between axis changes

        // ── Colours ───────────────────────────────────────────────────────────
        private static readonly Color CBack    = new Color(0.05f, 0.05f, 0.07f, 0.97f); // very dark bg
        private static readonly Color CSlice   = new Color(0.17f, 0.17f, 0.23f, 0.97f); // resting slice
        private static readonly Color CHover   = new Color(0.18f, 0.36f, 0.76f, 0.97f); // hovered slice
        private static readonly Color CDim     = new Color(0.09f, 0.09f, 0.12f, 0.97f); // no-selection
        private static readonly Color CSpoke   = new Color(0.03f, 0.03f, 0.04f, 1.00f); // divider line
        private static readonly Color CDot     = new Color(0.72f, 0.72f, 0.78f, 1.00f); // centre dot
        private static readonly Color CTextOn  = new Color(0.93f, 0.93f, 0.96f, 1.00f); // label enabled
        private static readonly Color CTextOff = new Color(0.32f, 0.32f, 0.36f, 1.00f); // label disabled

        // ── Axis colours (rich-text hex, no '#') ─────────────────────────────
        private static readonly string[] AxisHex  = { "FF4444", "44DD44", "4488FF" };
        private static readonly string[] AxisName = { "x", "y", "z" };

        // ── UI references ─────────────────────────────────────────────────────
        private Canvas        _canvas;
        private GameObject    _menuRoot;
        private Image         _imgRight, _imgLeft;       // the two pie-slice Images
        private RectTransform _rtRight,  _rtLeft;        // their RectTransforms (for pop)
        private Text          _lblRight, _lblLeft;       // labels
        private RectTransform _rtLblR,   _rtLblL;        // label RTs (follow the slice)

        // ── Runtime state ─────────────────────────────────────────────────────
        private bool    _open         = false;
        private Vector2 _center;                         // screen-space menu centre
        private float   _tapStart     = -1f;
        private int     _hovered      = -1;              // -1=none, 0=right, 1=left
        private float   _popR         = 0f;              // animated pop amounts
        private float   _popL         = 0f;
        private int     _axis         = 0;               // 0=X, 1=Y, 2=Z
        private float   _scrollTimer  = 0f;

        // ── Scene refs ────────────────────────────────────────────────────────
        private MovementManager   _mm;
        private AxisPreviewPlanes _previews;
        private Bounds            _bounds;

        // ── Shared sprite (generated once) ────────────────────────────────────
        private static Sprite _spr;

        // ── Bootstrap ─────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn() {
            var go = new GameObject("[RadialMenu]");
            DontDestroyOnLoad(go);
            go.AddComponent<RadialMenuUI>();
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake() {
            BuildCanvas();
            BuildMenu();

            var pg = new GameObject("AxisPreviews");
            pg.transform.SetParent(transform);
            _previews = pg.AddComponent<AxisPreviewPlanes>();
        }

        private void Update() {
            if (Mouse.current == null) return;

            // Track right-button press time to separate "tap" from "orbit-drag"
            if (Mouse.current.rightButton.wasPressedThisFrame)
                _tapStart = Time.unscaledTime;

            if (Mouse.current.rightButton.wasReleasedThisFrame) {
                float held = (_tapStart >= 0f) ? Time.unscaledTime - _tapStart : 99f;
                _tapStart = -1f;
                if (held <= MaxTap) {
                    if (_open) CloseMenu();
                    else       OpenMenu(Mouse.current.position.ReadValue());
                    return;
                }
            }

            if (!_open) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
                CloseMenu();
                return;
            }

            UpdateHover();
            UpdateScroll();
            AnimatePop();
            PaintSlices();

            if (Mouse.current.leftButton.wasPressedThisFrame) OnClick();
        }

        // ── Open / Close ──────────────────────────────────────────────────────

        private void OpenMenu(Vector2 screenPos) {
            if (_mm == null) _mm = FindObjectOfType<MovementManager>();

            var sel = MirrorSystem.GetSelectedObjects(_mm);
            _bounds = (sel != null && sel.Count > 0)
                    ? MirrorSystem.GetBounds(sel)
                    : new Bounds();

            _open    = true;
            _center  = screenPos;
            _hovered = -1;
            _popR = _popL = 0f;

            // Anchor the root to the click position in canvas/screen space
            var rt              = _menuRoot.GetComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.zero;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = screenPos;
            _menuRoot.SetActive(true);

            if (sel != null && sel.Count > 0)
                _previews.ShowAll(_bounds);

            SetFlipText();
            PaintSlices();
        }

        private void CloseMenu() {
            _open = false;
            _menuRoot.SetActive(false);
            _previews.HideAll();
        }

        // ── Per-frame ─────────────────────────────────────────────────────────

        private void UpdateHover() {
            Vector2 d    = Mouse.current.position.ReadValue() - _center;
            float   dist = d.magnitude;
            // Consider the menu hittable even slightly beyond the outer edge
            // (so the popped-out slice is still hoverable)
            _hovered = (dist < CentreR || dist > OuterR + PopDist + 6f)
                     ? -1
                     : (d.x >= 0f ? 0 : 1);
        }

        private void UpdateScroll() {
            _scrollTimer -= Time.deltaTime;
            if (_hovered != 1 || _scrollTimer > 0f) return;

            float s = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(s) < 0.01f) return;

            _axis        = (_axis + 3 + (s > 0f ? 1 : -1)) % 3;
            _scrollTimer = ScrollCd;
            SetFlipText();

            if (_bounds.size != Vector3.zero)
                _previews.HighlightAxis(_axis);
        }

        private void AnimatePop() {
            bool hasSel = HasSel();
            _popR = Mathf.Lerp(_popR, (hasSel && _hovered == 0) ? PopDist : 0f, Time.deltaTime * PopSpeed);
            _popL = Mathf.Lerp(_popL, (hasSel && _hovered == 1) ? PopDist : 0f, Time.deltaTime * PopSpeed);

            // Move slices outward from centre
            _rtRight.anchoredPosition = new Vector2( _popR, 0f);
            _rtLeft.anchoredPosition  = new Vector2(-_popL, 0f);

            // Labels ride with their slice
            _rtLblR.anchoredPosition  = new Vector2( LabelR + _popR, 0f);
            _rtLblL.anchoredPosition  = new Vector2(-LabelR - _popL, 0f);
        }

        private void PaintSlices() {
            bool hasSel = HasSel();

            _imgRight.color = !hasSel      ? CDim
                            : _hovered == 0 ? CHover
                                            : CSlice;

            _imgLeft.color  = !hasSel      ? CDim
                            : _hovered == 1 ? CHover
                                           : CSlice;

            _lblRight.color = hasSel ? CTextOn : CTextOff;
            _lblLeft.color  = hasSel ? CTextOn : CTextOff;
        }

        private void OnClick() {
            if (!HasSel() || _hovered < 0) { CloseMenu(); return; }
            if (_hovered == 0) DoCopy();
            else               DoFlip();
            CloseMenu();
        }

        // ── Actions ───────────────────────────────────────────────────────────

        private void DoCopy() {
            if (_mm == null) return;
            var objs = MirrorSystem.GetSelectedObjects(_mm);
            if (objs == null || objs.Count == 0) return;

            var clones = new List<GameObject>();
            foreach (var obj in objs) {
                var c = Instantiate(obj, obj.transform.position, obj.transform.rotation);
                c.DisableOutline();
                var r  = c.GetComponent<Renderer>();
                var or = obj.GetComponent<Renderer>();
                if (r != null && or != null) r.material = new Material(or.material);
                clones.Add(c);
            }
            var pre = new List<IElement>();
            foreach (var c in clones) { var e = new ObjectElement(c); e.existing = false; pre.Add(e); }
            StateSystem.AddElements(pre);
            var post = new List<IElement>();
            foreach (var c in clones) post.Add(new ObjectElement(c));
            StateSystem.AddState(new State(post));
        }

        private void DoFlip() {
            if (_mm == null) return;
            var objs = MirrorSystem.GetSelectedObjects(_mm);
            if (objs == null || objs.Count == 0) return;
            Vector3 n = _axis == 0 ? Vector3.right : _axis == 1 ? Vector3.up : Vector3.forward;
            MirrorSystem.MirrorObjects(objs, _bounds.center, n);
        }

        private bool HasSel() {
            if (_mm == null) _mm = FindObjectOfType<MovementManager>();
            return _mm != null && _mm.MovingObj != null;
        }

        private void SetFlipText() {
            var p = new string[3];
            for (int i = 0; i < 3; i++)
                p[i] = i == _axis
                     ? $"<b><color=#{AxisHex[i]}>{AxisName[i]}</color></b>"
                     : $"<color=#484850>{AxisName[i]}</color>";
            _lblLeft.text = "Flip\n" + p[0] + ",  " + p[1] + ",  " + p[2];
        }

        // ── UI Construction ───────────────────────────────────────────────────

        private void BuildCanvas() {
            var cg            = new GameObject("RadialCanvas");
            cg.transform.SetParent(transform);
            _canvas           = cg.AddComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 999;
            cg.AddComponent<CanvasScaler>();
            cg.AddComponent<GraphicRaycaster>();
        }

        private void BuildMenu() {
            float d = OuterR * 2f; // full disc diameter (260 px)

            // Root positioned at the cursor when the menu opens.
            // It has zero size; all children use it as their anchor.
            var rootGO         = new GameObject("MenuRoot");
            rootGO.transform.SetParent(_canvas.transform, false);
            var rootRT         = rootGO.AddComponent<RectTransform>();
            rootRT.sizeDelta   = Vector2.zero;
            rootRT.anchorMin   = rootRT.anchorMax = new Vector2(0.5f, 0.5f);
            _menuRoot          = rootGO;
            _menuRoot.SetActive(false);

            // ── Layer 1: dark background disc ────────────────────────────────
            // Slightly larger than the slices so a thin dark ring shows around them.
            MakeCircle("Bg", _menuRoot.transform, Vector2.zero, d + 6f, CBack);

            // ── Layer 2: pie slices ───────────────────────────────────────────
            // Each is a full 260×260 circle image, filled as a half-wedge via
            // Radial360.  Origin=Top means filling starts at 12 o'clock.
            //   clockwise=true  → right half (Copy)
            //   clockwise=false → left  half (Flip)
            var rightGO    = MakeCircle("SliceR", _menuRoot.transform, Vector2.zero, d, CSlice);
            _imgRight      = rightGO.GetComponent<Image>();
            _imgRight.type          = Image.Type.Filled;
            _imgRight.fillMethod    = Image.FillMethod.Radial360;
            _imgRight.fillOrigin    = (int)Image.Origin360.Top;
            _imgRight.fillClockwise = true;
            _imgRight.fillAmount    = 0.5f;
            _rtRight       = rightGO.GetComponent<RectTransform>();

            var leftGO     = MakeCircle("SliceL", _menuRoot.transform, Vector2.zero, d, CSlice);
            _imgLeft       = leftGO.GetComponent<Image>();
            _imgLeft.type          = Image.Type.Filled;
            _imgLeft.fillMethod    = Image.FillMethod.Radial360;
            _imgLeft.fillOrigin    = (int)Image.Origin360.Top;
            _imgLeft.fillClockwise = false;
            _imgLeft.fillAmount    = 0.5f;
            _rtLeft        = leftGO.GetComponent<RectTransform>();

            // ── Layer 3: spoke divider (vertical line between slices) ─────────
            MakeRect("Spoke", _menuRoot.transform, Vector2.zero, new Vector2(3f, d + 8f), CSpoke);

            // ── Layer 4: centre circle (covers the middle, gives "pie" look) ──
            MakeCircle("Centre", _menuRoot.transform, Vector2.zero, CentreR * 2f, CDot);

            // ── Layer 5: labels (children of root, not of slices) ─────────────
            // They are moved manually in AnimatePop() to follow their slice.
            var lr         = MakeLabel("LblR", _menuRoot.transform,
                                       new Vector2(LabelR, 0f), new Vector2(108f, 58f),
                                       "Copy", 14);
            _lblRight      = lr.GetComponent<Text>();
            _rtLblR        = lr.GetComponent<RectTransform>();

            var ll         = MakeLabel("LblL", _menuRoot.transform,
                                       new Vector2(-LabelR, 0f), new Vector2(108f, 58f),
                                       "", 12);
            _lblLeft       = ll.GetComponent<Text>();
            _lblLeft.supportRichText = true;
            _rtLblL        = ll.GetComponent<RectTransform>();
            SetFlipText();
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        /// <summary>Creates a centred circular Image GameObject.</summary>
        private static GameObject MakeCircle(string name, Transform parent,
                                             Vector2 pos, float diameter, Color col) {
            var go  = MakeGO(name, parent, pos, Vector2.one * diameter);
            var img = go.AddComponent<Image>();
            img.sprite = GetCircleSprite();
            img.color  = col;
            img.type   = Image.Type.Simple;
            return go;
        }

        /// <summary>Creates a centred plain (non-circular) Image GameObject.</summary>
        private static void MakeRect(string name, Transform parent,
                                     Vector2 pos, Vector2 size, Color col) {
            var go  = MakeGO(name, parent, pos, size);
            var img = go.AddComponent<Image>();
            img.color = col;
        }

        /// <summary>Creates a centred Text label GameObject.</summary>
        private static GameObject MakeLabel(string name, Transform parent,
                                            Vector2 pos, Vector2 size,
                                            string text, int fontSize) {
            var go = MakeGO(name, parent, pos, size);
            var t  = go.AddComponent<Text>();
            t.text            = text;
            t.font            = GetFont();
            t.fontSize        = fontSize;
            t.color           = CTextOff;
            t.alignment       = TextAnchor.MiddleCenter;
            t.supportRichText = true;
            return go;
        }

        /// <summary>
        /// Creates a RectTransform GameObject centred under <paramref name="parent"/>.
        /// </summary>
        private static GameObject MakeGO(string name, Transform parent,
                                         Vector2 pos, Vector2 size) {
            var go              = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt              = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            return go;
        }

        // ── Circle sprite ─────────────────────────────────────────────────────

        /// <summary>
        /// Generates a smooth anti-aliased white circle sprite (256×256) once.
        /// All circular UI elements share this sprite; colour is applied via Image.color.
        ///
        /// When used with Image.fillMethod = Radial360, the sprite becomes a pie-wedge.
        /// </summary>
        private static Sprite GetCircleSprite() {
            if (_spr != null) return _spr;

            const int   N  = 256;
            const float R  = N * 0.5f;
            var tex = new Texture2D(N, N, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < N; y++) {
                for (int x = 0; x < N; x++) {
                    float dx = x - R + 0.5f;
                    float dy = y - R + 0.5f;
                    float d  = Mathf.Sqrt(dx * dx + dy * dy);
                    // Smooth anti-aliased edge over 2 pixels
                    float a  = Mathf.Clamp01(1f - (d - (R - 2f)) / 2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();

            _spr = Sprite.Create(
                tex,
                new Rect(0f, 0f, N, N),
                new Vector2(0.5f, 0.5f),
                100f);   // 100 px per unit — standard for UI sprites
            return _spr;
        }

        // ── Font ─────────────────────────────────────────────────────────────

        private static Font GetFont() {
            // Try Unity 2021.3's built-in font; fall back to Arial for other versions.
            // Note: using explicit null check because Unity overrides == but not ??.
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
