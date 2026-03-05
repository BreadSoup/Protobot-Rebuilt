using UnityEngine;
using UnityEngine.EventSystems;

namespace Protobot.UI {
    /// <summary>
    /// Attach this to any UI element that has an Image (so the EventSystem can
    /// raycast it) to receive scroll-wheel events and fire step callbacks.
    ///
    /// Used on the child-part value display in the Properties Menu so the user
    /// can scroll over the size label to cycle screw / spacer sizes, exactly like
    /// scrolling over the numeric input fields changes hole count / shaft length.
    ///
    /// Usage:
    ///   var cycler = valGO.AddComponent&lt;ScrollableCycler&gt;();
    ///   cycler.onScrollUp   = () =&gt; OnChildPartCycle(+1);
    ///   cycler.onScrollDown = () =&gt; OnChildPartCycle(-1);
    /// </summary>
    public class ScrollableCycler : MonoBehaviour, IScrollHandler {
        /// <summary>Invoked when the scroll wheel moves up (away from user).</summary>
        public System.Action onScrollUp;

        /// <summary>Invoked when the scroll wheel moves down (toward user).</summary>
        public System.Action onScrollDown;

        /// <summary>
        /// Called by Unity's EventSystem when the scroll wheel moves over this element.
        /// </summary>
        public void OnScroll(PointerEventData eventData) {
            if (eventData.scrollDelta.y > 0)
                onScrollUp?.Invoke();
            else
                onScrollDown?.Invoke();
        }
    }
}
