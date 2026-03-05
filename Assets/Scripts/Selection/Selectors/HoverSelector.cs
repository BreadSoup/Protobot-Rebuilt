using System;
using UnityEngine;

namespace Protobot.SelectionSystem {
    public class HoverSelector : Selector {
        public override event Action<ISelection> setEvent;
        public override event Action clearEvent;
        [SerializeField] private MouseCast mouseCast = null;
        [SerializeField] private bool checkPrevObj;

        private GameObject prevObj;

        /// <summary>
        /// When true, the per-frame clear-event is not fired while the mouse
        /// is over empty space.  Set by RadialMenuUI to keep a post-mirror
        /// outline visible for a short hold period without immediately being
        /// wiped by the hover-nothing path.
        /// </summary>
        public static bool SuppressClear { get; set; } = false;

        public void Update() {
            GameObject mouseCastObj = mouseCast.gameObject;

            if (mouseCastObj != null) {
                if (prevObj != mouseCastObj || !checkPrevObj) {
                    var selection = new ObjectSelection {
                        gameObject = mouseCastObj,
                        selector = this
                    };

                    setEvent?.Invoke(selection);
                }
            }
            else if (!SuppressClear)
                clearEvent?.Invoke();

            prevObj = mouseCastObj;
        }
    }
}