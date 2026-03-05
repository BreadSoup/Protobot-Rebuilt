using UnityEngine;
using UnityEngine.UI;
using Protobot.InputEvents;

namespace Protobot {
    /// <summary>
    /// Self-spawning MonoBehaviour that adds a "Selection" keybind row to the
    /// Quick Settings Menu at scene load.
    ///
    /// At runtime it:
    ///   1. Finds the "Quick Settings Menu" GameObject in the scene.
    ///   2. Locates its first VerticalLayoutGroup as the content container.
    ///   3. Injects a header ("Selection") and a keybind row:
    ///        [Radial Menu Key]  [current key]  [Rebind]  [Reset]
    ///
    /// If the Quick Settings Menu is not found (e.g., in a different scene), it
    /// fails silently — the keybind still works and persists via PlayerPrefs.
    ///
    /// The row uses RadialMenuUI.SharedRebind so it controls the same binding
    /// that RadialMenuUI checks every frame.
    /// </summary>
    public class RadialMenuKeybindRow : MonoBehaviour {

        // ── Colours (Protobot palette) ─────────────────────────────────────────
        private static readonly Color PanelBg  = new Color(0.14f, 0.14f, 0.14f, 1.00f);
        private static readonly Color TextOn   = new Color(0.92f, 0.92f, 0.95f, 1.00f);
        private static readonly Color TextDim  = new Color(0.55f, 0.55f, 0.58f, 1.00f);
        private static readonly Color HoverCol = new Color(0.22f, 0.45f, 0.78f, 1.00f);
        private static readonly Color HeaderCol = new Color(0.22f, 0.45f, 0.78f, 1.00f);

        private Text   _keyText;
        private Button _rebindBtn;
        private Button _resetBtn;

        // ── Bootstrap ─────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Spawn() {
            var go = new GameObject("[RadialMenuSettings]");
            DontDestroyOnLoad(go);
            go.AddComponent<RadialMenuKeybindRow>();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start() {
            // Wait one frame for the scene to fully initialise, then inject the row.
            // (RadialMenuUI.SharedRebind is set in Awake, so it's ready by Start.)
            Inject();
        }

        private void Inject() {
            // Try to find the Quick Settings Menu in the scene.
            var qsm = GameObject.Find("Quick Settings Menu");
            if (qsm == null) return;

            // Find the first VerticalLayoutGroup as the content container.
            var layout = qsm.GetComponentInChildren<VerticalLayoutGroup>();
            if (layout == null) return;

            Transform container = layout.transform;

            // ── "Selection" section header ────────────────────────────────────
            AddHeader("Selection", container);

            // ── Keybind row ───────────────────────────────────────────────────
            AddKeybindRow(container);

            // Subscribe to rebind events to keep the key display current.
            if (RadialMenuUI.SharedRebind != null) {
                RadialMenuUI.SharedRebind.OnCompleteRebind += UpdateKeyDisplay;
                RadialMenuUI.SharedRebind.OnResetRebinds   += UpdateKeyDisplay;
                UpdateKeyDisplay();
            }
        }

        // ── Row Builders ──────────────────────────────────────────────────────

        private void AddHeader(string title, Transform parent) {
            var go  = new GameObject($"Header_{title}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 24f);

            var bg = go.AddComponent<Image>();
            bg.color = HeaderCol;

            var lgo = MakeText(go.transform, title, 11, TextOn, TextAnchor.MiddleLeft);
            var lrt = lgo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(8f,  0f);
            lrt.offsetMax = new Vector2(-8f, 0f);
        }

        private void AddKeybindRow(Transform parent) {
            // Row container
            var row = new GameObject("Row_RadialMenuKey");
            row.transform.SetParent(parent, false);
            var rrt = row.AddComponent<RectTransform>();
            rrt.sizeDelta = new Vector2(0f, 28f);

            var rowBg = row.AddComponent<Image>();
            rowBg.color = PanelBg;

            var hLayout = row.AddComponent<HorizontalLayoutGroup>();
            hLayout.childControlWidth  = false;
            hLayout.childControlHeight = true;
            hLayout.spacing = 4f;
            hLayout.padding = new RectOffset(8, 4, 2, 2);

            // Label
            var lbl = MakeText(row.transform, "Radial Menu Key", 10, TextOn, TextAnchor.MiddleLeft);
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 0f);

            // Current key display
            var keyGO = MakeText(row.transform, "...", 10, HoverCol, TextAnchor.MiddleCenter);
            keyGO.GetComponent<RectTransform>().sizeDelta = new Vector2(44f, 0f);
            _keyText = keyGO.GetComponent<Text>();

            // Rebind button
            _rebindBtn = MakeButton(row.transform, "Rebind", 9, new Vector2(48f, 0f),
                                    () => {
                                        if (_keyText != null)
                                            _keyText.text = "…";
                                        RadialMenuUI.SharedRebind?.AttemptRebind();
                                    });

            // Reset button
            _resetBtn = MakeButton(row.transform, "Reset", 9, new Vector2(40f, 0f),
                                   () => RadialMenuUI.SharedRebind?.ResetRebinds());
        }

        // ── Display update ────────────────────────────────────────────────────

        private void UpdateKeyDisplay() {
            if (_keyText == null || RadialMenuUI.SharedRebind == null) return;

            var action = RadialMenuUI.SharedRebind.action;
            string display = "";
            foreach (var binding in action.bindings) {
                string path = string.IsNullOrEmpty(binding.overridePath)
                            ? binding.path : binding.overridePath;
                if (string.IsNullOrEmpty(path) || binding.isComposite || binding.isPartOfComposite)
                    continue;
                if (display != "") display += "+";
                display += binding.ToDisplayString(
                    UnityEngine.InputSystem.InputBinding.DisplayStringOptions
                        .DontUseShortDisplayNames);
            }
            _keyText.text = string.IsNullOrEmpty(display) ? "X" : display;

            if (_resetBtn != null)
                _resetBtn.interactable = !RadialMenuUI.SharedRebind.IsEmpty;
        }

        // ── UI Helpers ────────────────────────────────────────────────────────

        private static GameObject MakeText(Transform parent, string text, int fontSize,
                                           Color color, TextAnchor anchor) {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var t = go.AddComponent<Text>();
            t.text      = text;
            t.fontSize  = fontSize;
            t.color     = color;
            t.alignment = anchor;
            t.font      = GetFont();
            return go;
        }

        private static Button MakeButton(Transform parent, string label, int fontSize,
                                         Vector2 size, UnityEngine.Events.UnityAction onClick) {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;

            var bg  = go.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.22f, 0.26f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var cols = btn.colors;
            cols.highlightedColor = new Color(0.28f, 0.28f, 0.34f, 1f);
            cols.pressedColor     = new Color(0.18f, 0.18f, 0.22f, 1f);
            btn.colors = cols;

            btn.onClick.AddListener(onClick);

            var lgo = MakeText(go.transform, label, fontSize, TextOn, TextAnchor.MiddleCenter);
            var lrt = lgo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = Vector2.zero;

            return btn;
        }

        private static Font GetFont() {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
