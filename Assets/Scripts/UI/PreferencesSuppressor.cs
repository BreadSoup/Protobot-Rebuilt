using Protobot.InputEvents;
using UnityEngine;

namespace Protobot {
    /// <summary>
    /// Attach this to the Preferences Canvas GameObject.
    ///
    /// When the Preferences panel opens (SetActive → true) this sets
    /// <see cref="InputEvent.SuppressAll"/> to true so that pressing any bound
    /// key inside the Preferences menu does not accidentally trigger in-scene
    /// actions (e.g. pressing B while viewing the Box Select row won't start a
    /// box select).
    ///
    /// When the panel closes (SetActive → false) suppression is lifted so
    /// normal keybinds work again immediately.
    /// </summary>
    public class PreferencesSuppressor : MonoBehaviour {
        private void OnEnable()  => InputEvent.SuppressAll = true;
        private void OnDisable() => InputEvent.SuppressAll = false;
    }
}
