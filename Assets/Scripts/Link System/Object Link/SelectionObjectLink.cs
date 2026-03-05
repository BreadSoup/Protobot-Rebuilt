using UnityEngine;
using Protobot.SelectionSystem;

namespace Protobot {
    public class SelectionObjectLink : ObjectLink {
        public override GameObject obj => selectionManager.current?.gameObject;

        [SerializeField] private SelectionManager selectionManager;

        /// <summary>
        /// Exposes the SelectionManager this link tracks so external systems
        /// (e.g. PropertiesMenuUI) can update the correct manager directly.
        /// </summary>
        public SelectionManager Manager => selectionManager;
    }
}