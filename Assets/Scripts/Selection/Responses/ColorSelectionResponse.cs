using Models;
using UnityEngine;
using Protobot.Outlining;
using UnityEngine.UIElements;

namespace Protobot.SelectionSystem
{
    public class ColorSelectionResponse : SelectionResponse
    {
        public override bool RespondOnlyToSelectors => false;

        //This feels so wrong feel free to PR it if you have a better solution this is being called by HoleFaceResponseSelector.getresponseslection 
        public void HoleColliderException(ISelection sel)
        {
            OnSet(sel);
        }
        public override void OnSet(ISelection sel)
        {
            //this *shouldn't* have any issues but there may be an edge case where it could recolor a part that isn't supposed to be recolored
            //TODO: Add a check to be 100% that the part is supposed to be recolored
            if (sel.gameObject.TryGetComponent<Renderer>(out var component) ||
                sel.gameObject.transform.parent.gameObject.TryGetComponent<Renderer>(out component)) 
            {
                if (component.material.GetFloat("_Metallic") == .754f && ColorToolActiveCheck.colorToolActive)
                {
                    ColorTool.material = component.material;
                    component.material.color = ColorTool.colorToSet;
                }
            }
        }

        public override void OnClear(ClearInfo info)
        {
            info.selection.Deselect();
        }
    }
}