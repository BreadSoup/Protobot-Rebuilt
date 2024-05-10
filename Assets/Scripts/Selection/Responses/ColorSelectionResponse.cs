using UnityEngine;
using Protobot.Outlining;
using UnityEngine.UIElements;

namespace Protobot.SelectionSystem
{
    public class ColorSelectionResponse : SelectionResponse
    {
        public override bool RespondOnlyToSelectors => false;
        public override void OnSet(ISelection sel)
        {
            if (sel.gameObject.TryGetComponent<Renderer>(out Renderer renderer))
            {
                ColorTool.material = renderer.material;
            }
        }

        public override void OnClear(ClearInfo info)
        {
            ColorTool.material = ColorTool.placeholder;
        }
    }
}