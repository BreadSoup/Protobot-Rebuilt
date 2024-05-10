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
                if (renderer.material.GetFloat("_Metallic") == .754f && ColorToolActiveCheck.colorToolActive)
                {
                    ColorTool.material = renderer.material;
                    renderer.material.color = ColorTool.colorToSet;
                }
            }
        }

        public override void OnClear(ClearInfo info)
        {
            info.selection.Deselect();
        }
    }
}