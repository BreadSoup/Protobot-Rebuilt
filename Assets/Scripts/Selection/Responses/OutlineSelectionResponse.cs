using UnityEngine;
using Protobot.Outlining;
using UnityEngine.UIElements;

namespace Protobot.SelectionSystem {
    public class OutlineSelectionResponse : SelectionResponse {
        public override bool RespondOnlyToSelectors => false;

        [SerializeField] private int colorIndex = 0;
        [SerializeField] private string ignoredTags;
        public override void OnSet(ISelection sel) {
            if (!sel.gameObject.tag.Contains(ignoredTags)) {
                sel.gameObject.EnableOutline(colorIndex, 1, 0.15f);
            }
        }

        public override void OnClear(ClearInfo info) {
            var clearedObj = info.selection.gameObject;

            if (!clearedObj.tag.Contains(ignoredTags)) {
                clearedObj.DisableOutline();
            }
        }
    }
}