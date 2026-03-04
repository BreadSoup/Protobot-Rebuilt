using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Protobot.StateSystems;
using Protobot.Outlining;

namespace Protobot {
    /// <summary>
    /// Self-spawning right-click radial context menu providing:
    ///   • Duplicate         — identical clone at the same position
    ///   • Mirror Duplicate  — mirrored clone (axis chosen via sub-menu)
    ///   • Mirror / Flip     — mirrors selection in place (axis via sub-menu)
    ///
    /// Items are greyed out when nothing is selected; active when a selection
    /// exists.  Hovering a mirror item opens its X / Y / Z axis sub-menu.
    /// Three translucent preview planes show the available mirror axes in 3D.
    ///
    /// No Inspector wiring is required — this component bootstraps itself via
    /// [RuntimeInitializeOnLoadMethod] after every scene load.
    ///
    /// Right-click opens the menu (short tap only — so camera-orbit right-drags
    /// are not intercepted).  Left-clicking a greyed item, pressing Escape, or
    /// right-clicking again closes the menu.
    /// </summary>
    public class RadialMenuUI : MonoBehaviour {

        // ── Constants ──────────────────────────────────────────────────────────

        private static readonly string[] MainLabels = { "Duplicate", "Mirror Dup", "Mirror / Flip" };
        private static readonly string[] AxisLabels = { "X Axis", "Y Axis", "Z Axis" };

        /// <summary>World-space mirror normals for X, Y, Z.</summary>
        private static readonly Vector3[] MirrorNormals = {
            Vector3.right,
            Vector3.up,
            Vector3.forward
        };

        // Pixel offsets of the three main buttons from the menu-open cursor position
        private static readonly Vector2[] ItemOffsets = {
            new Vector2(-90f, 55f),  // Duplicate   (left)
            new Vector2(  0f, 90f),  // Mirror Dup  (centre-top)
            new Vector2( 90f, 55f),  // Mirror/Flip (right)
        };

        // Pixel offsets of the three axis sub-buttons relative to their parent item
        private static readonly Vector2[] SubOffsets = {
            new Vector2(0f,  34f),  // X Axis
            new Vector2(0f,  65f),  // Y Axis
            new Vector2(0f,  96f),  // Z Axis
        };

        // Button dimensions
        private const float ItemW    = 112f;
        private const float ItemH    =  30f;
        private const float SubItemW =  84f;
        private const float SubItemH =  26f;

        // Colour palette
        private static readonly Color BgDisabled   = new Color(0.18f, 0.18f, 0.18f, 0.88f);
        private static readonly Color BgEnabled     = new Color(0.28f, 0.28f, 0.28f, 0.92f);
        private static readonly Color BgHover       = new Color(0.42f, 0.42f, 0.42f, 0.95f);
        private static readonly Color BgPressed     = new Color(0.18f, 0.44f, 0.95f, 1.00f);
        private static readonly Color BgSubNormal   = new Color(0.12f, 0.12f, 0.12f, 0.90f);
        private static readonly Color BgSubHover    = new Color(0.30f, 0.30f, 0.30f, 0.95f);
        private static readonly Color TextEnabled   = Color.white;
        private static readonly Color TextDisabled  = new Color(0.45f, 0.45f, 0.45f, 1.00f);

        /// <summary>
        /// Maximum time (seconds) a right-click may be held before it is treated
        /// as a camera-orbit drag rather than a tap to open the menu.
        /// </summary>
        private const float MaxTapSeconds = 0.20f;

        // ── UI References ─────────────────────────────────────────────────────

        private Canvas       _canvas;
        private GameObject   _menuRoot;

        private Image[]      _mainBg;     // [3] — one per main item
        private Text[]       _mainLabel;  // [3]

        // [itemIndex][axisIndex] — only populated for items 1 and 2 (mirror items)
        private Image[][]    _subBg;
        private Text[][]     _subLabel;
        private GameObject[] _subRoot;    // [3] sub-menu containers

        // ── Runtime State ─────────────────────────────────────────────────────

        private bool    _isOpen;
        private Vector2 _openPos;
        private float   _rightPressTime = -1f;

        // ── Scene References (lazily found) ──────────────────────────────────

        private MovementManager   _mm;
        private AxisPreviewPlanes _previews;
        private Bounds            _boundsAtOpen;

        // ── Bootstrap ─────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a single persistent RadialMenuUI instance after every scene load.
        /// DontDestroyOnLoad keeps it alive across scene transitions.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create() {
            var go = new GameObject("[RadialMenu]");
            DontDestroyOnLoad(go);
            go.AddComponent<RadialMenuUI>();
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake() {
            BuildCanvas();
            BuildMenu();

            // Create the preview planes object once; keep it alive with this script
            var planesGo = new GameObject("AxisPreviewPlanes");
            planesGo.transform.SetParent(transform);
            _previews = planesGo.AddComponent<AxisPreviewPlanes>();
        }

        private void Update() {
            if (Mouse.current == null) return;

            // ── Track right-press duration to distinguish tap from orbit-drag ──
            if (Mouse.current.rightButton.wasPressedThisFrame)
                _rightPressTime = Time.unscaledTime;

            if (Mouse.current.rightButton.wasReleasedThisFrame) {
                float held = (_rightPressTime >= 0f) ? (Time.unscaledTime - _rightPressTime) : float.MaxValue;
                _rightPressTime = -1f;

                if (held <= MaxTapSeconds) {
                    // Short tap → toggle the menu
                    if (_isOpen) CloseMenu();
                    else         OpenMenu(Mouse.current.position.ReadValue());
                    return;
                }
                // Long hold (camera orbit) → ignore
            }

            if (!_isOpen) return;

            // Escape closes the menu
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
                CloseMenu();
                return;
            }

            UpdateMenuState();
        }

        // ── Open / Close ──────────────────────────────────────────────────────

        private void OpenMenu(Vector2 screenPos) {
            // Lazily locate the MovementManager in the scene
            if (_mm == null)
                _mm = FindObjectOfType<MovementManager>();

            // Snapshot the selection bounds now so they don't drift if the user
            // moves something while the menu is open
            var selObjs      = MirrorSystem.GetSelectedObjects(_mm);
            _boundsAtOpen    = (selObjs != null && selObjs.Count > 0)
                             ? MirrorSystem.GetBounds(selObjs)
                             : new Bounds();

            _isOpen  = true;
            _openPos = screenPos;

            // Anchor the menu root to the click position
            var rootRect = _menuRoot.GetComponent<RectTransform>();
            rootRect.anchorMin        = Vector2.zero;
            rootRect.anchorMax        = Vector2.zero;
            rootRect.pivot            = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = screenPos;

            _menuRoot.SetActive(true);

            // Hide all sub-menus on open
            for (int i = 0; i < 3; i++)
                _subRoot[i].SetActive(false);

            // Show preview planes if there is a selection
            if (selObjs != null && selObjs.Count > 0)
                _previews.ShowAll(_boundsAtOpen);

            RefreshMainColors(HasSelection());
        }

        private void CloseMenu() {
            _isOpen = false;
            _menuRoot.SetActive(false);
            _previews.HideAll();
        }

        // ── Per-Frame Menu Logic ──────────────────────────────────────────────

        private void UpdateMenuState() {
            Vector2 mp         = Mouse.current.position.ReadValue();
            bool    hasSel     = HasSelection();
            int     hovMain    = HoveredMain(mp);
            int     hovSub     = -1;

            // Show sub-menu only when hovering a mirror item AND there is a selection
            for (int i = 0; i < 3; i++) {
                bool showSub = (i > 0) && (hovMain == i) && hasSel;
                _subRoot[i].SetActive(showSub);
            }

            // Detect sub-item hover and update preview highlight
            for (int i = 1; i < 3; i++) {
                if (!_subRoot[i].activeSelf) continue;
                for (int j = 0; j < 3; j++) {
                    if (_subBg[i][j] == null) continue;
                    if (IsHovering(_subBg[i][j].rectTransform, mp)) {
                        hovSub = j;
                        _previews.HighlightAxis(j);
                        break;
                    }
                }
            }

            // If not hovering any sub-item, restore all-planes view
            if (hovSub < 0 && hasSel && _boundsAtOpen.size != Vector3.zero)
                _previews.ShowAll(_boundsAtOpen);

            // ── Main item colours ──
            for (int i = 0; i < 3; i++) {
                bool hov = (hovMain == i);
                _mainBg[i].color    = !hasSel  ? BgDisabled
                                    : hov       ? BgHover
                                                : BgEnabled;
                _mainLabel[i].color = hasSel ? TextEnabled : TextDisabled;
            }

            // ── Sub-item colours ──
            for (int i = 1; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    if (_subBg[i][j] == null) continue;
                    _subBg[i][j].color = (hovMain == i && hovSub == j)
                                       ? BgSubHover
                                       : BgSubNormal;
                }
            }

            // ── Click handling ──
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            if (hovMain == 0 && hasSel) {
                // Duplicate — no axis needed
                PerformDuplicate();
                CloseMenu();
            } else if (hovMain > 0 && hovSub >= 0 && hasSel) {
                // Mirror action with the chosen axis
                if (hovMain == 1) PerformMirrorDuplicate(hovSub);
                if (hovMain == 2) PerformMirror(hovSub);
                CloseMenu();
            } else if (hovMain >= 0 && !hasSel) {
                // Clicked a greyed item — just close (nothing to do)
                CloseMenu();
            } else if (hovMain < 0) {
                // Clicked outside the menu
                CloseMenu();
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private bool HasSelection() {
            if (_mm == null) _mm = FindObjectOfType<MovementManager>();
            return _mm != null && _mm.MovingObj != null;
        }

        /// <summary>Returns the index (0..2) of the main item the cursor is over, or -1.</summary>
        private int HoveredMain(Vector2 screenPos) {
            for (int i = 0; i < 3; i++)
                if (_mainBg[i] != null && IsHovering(_mainBg[i].rectTransform, screenPos))
                    return i;
            return -1;
        }

        private static bool IsHovering(RectTransform rect, Vector2 screenPos) {
            // null camera = correct for Screen Space Overlay
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, null);
        }

        private void RefreshMainColors(bool hasSel) {
            for (int i = 0; i < 3; i++) {
                _mainBg[i].color    = hasSel ? BgEnabled : BgDisabled;
                _mainLabel[i].color = hasSel ? TextEnabled : TextDisabled;
            }
        }

        // ── Actions ───────────────────────────────────────────────────────────

        /// <summary>
        /// Duplicates the current selection in place (no mirror).
        /// Follows the same pattern as ObjectDuplicator.
        /// </summary>
        private void PerformDuplicate() {
            if (_mm == null) return;
            var objs = MirrorSystem.GetSelectedObjects(_mm);
            if (objs == null || objs.Count == 0) return;

            var clones = new List<GameObject>();
            foreach (var obj in objs) {
                var clone = Instantiate(obj, obj.transform.position, obj.transform.rotation);
                clone.DisableOutline();

                var rend     = clone.GetComponent<Renderer>();
                var origRend = obj.GetComponent<Renderer>();
                if (rend != null && origRend != null)
                    rend.material = new Material(origRend.material);

                clones.Add(clone);
            }

            // Pre-state: record clones as "not yet existing" so UNDO hides them
            var preElems = new List<IElement>();
            foreach (var clone in clones) {
                var pre = new ObjectElement(clone);
                pre.existing = false;
                preElems.Add(pre);
            }
            StateSystem.AddElements(preElems);

            // Post-state: clones exist at their current positions
            var postElems = new List<IElement>();
            foreach (var clone in clones)
                postElems.Add(new ObjectElement(clone));
            StateSystem.AddState(new State(postElems));
        }

        private void PerformMirrorDuplicate(int axisIndex) {
            if (_mm == null) return;
            var objs = MirrorSystem.GetSelectedObjects(_mm);
            if (objs == null || objs.Count == 0) return;
            MirrorSystem.MirrorDuplicate(objs, _boundsAtOpen.center, MirrorNormals[axisIndex]);
        }

        private void PerformMirror(int axisIndex) {
            if (_mm == null) return;
            var objs = MirrorSystem.GetSelectedObjects(_mm);
            if (objs == null || objs.Count == 0) return;
            MirrorSystem.MirrorObjects(objs, _boundsAtOpen.center, MirrorNormals[axisIndex]);
        }

        // ── UI Construction ───────────────────────────────────────────────────

        private void BuildCanvas() {
            var canvasGo = new GameObject("RadialCanvas");
            canvasGo.transform.SetParent(transform);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 999;   // render on top of everything

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        private void BuildMenu() {
            // Invisible root used only to show/hide the whole menu and to anchor
            // all items relative to the cursor click position
            _menuRoot = new GameObject("MenuRoot");
            _menuRoot.transform.SetParent(_canvas.transform, false);
            var rootRect = _menuRoot.AddComponent<RectTransform>();
            rootRect.sizeDelta = Vector2.zero;
            _menuRoot.SetActive(false);

            _mainBg    = new Image[3];
            _mainLabel = new Text[3];
            _subRoot   = new GameObject[3];
            _subBg     = new Image[3][];
            _subLabel  = new Text[3][];

            // Background panel behind the menu items (slight dark shade)
            var panelGo   = new GameObject("Panel");
            panelGo.transform.SetParent(_menuRoot.transform, false);
            var panelRect  = panelGo.AddComponent<RectTransform>();
            panelRect.anchoredPosition = new Vector2(0f, 62f);
            panelRect.sizeDelta        = new Vector2(310f, 128f);
            var panelImg  = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.08f, 0.08f, 0.70f);

            var font = GetFont();

            for (int i = 0; i < 3; i++) {
                // ── Main item button ──
                var itemGo = MakeButton($"Main_{i}", _menuRoot.transform,
                                        ItemOffsets[i], ItemW, ItemH,
                                        BgDisabled, font, MainLabels[i],
                                        out _mainBg[i], out _mainLabel[i]);

                // ── Sub-menu container (hidden by default) ──
                var subGo   = new GameObject($"Sub_{i}");
                subGo.transform.SetParent(itemGo.transform, false);
                subGo.AddComponent<RectTransform>().sizeDelta = Vector2.zero;
                _subRoot[i] = subGo;
                _subRoot[i].SetActive(false);

                _subBg[i]    = new Image[3];
                _subLabel[i] = new Text[3];

                // Only mirror items (1 and 2) have axis sub-menus
                if (i > 0) {
                    for (int j = 0; j < 3; j++) {
                        MakeButton($"Sub_{i}_{j}", subGo.transform,
                                   SubOffsets[j], SubItemW, SubItemH,
                                   BgSubNormal, font, AxisLabels[j],
                                   out _subBg[i][j], out _subLabel[i][j]);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a labelled rectangular button and returns the parent GameObject.
        /// </summary>
        private static GameObject MakeButton(string name, Transform parent,
                                             Vector2 offset, float w, float h,
                                             Color bgColor, Font font, string label,
                                             out Image bg, out Text text) {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta        = new Vector2(w, h);
            rect.anchoredPosition = offset;

            bg       = go.AddComponent<Image>();
            bg.color = bgColor;

            var textGo   = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);

            var textRect          = textGo.AddComponent<RectTransform>();
            textRect.anchorMin    = Vector2.zero;
            textRect.anchorMax    = Vector2.one;
            textRect.offsetMin    = new Vector2(3f, 0f);
            textRect.offsetMax    = new Vector2(-3f, 0f);

            text           = textGo.AddComponent<Text>();
            text.text      = label;
            text.font      = font;
            text.fontSize  = 11;
            text.color     = TextDisabled;
            text.alignment = TextAnchor.MiddleCenter;

            return go;
        }

        /// <summary>
        /// Returns a built-in Unity font, trying the names used in Unity 2021.3.
        /// </summary>
        private static Font GetFont() {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null)
                f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
