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
            if (sel == null)  
                return;
            GameObject targetGameObject = sel.gameObject;
            Renderer component;
            if (targetGameObject != null)
            {
                targetGameObject.TryGetComponent<Renderer>(out component);

                if (component == null)
                {
                    if (targetGameObject.transform.parent != null)
                    {
                        targetGameObject.transform.parent.gameObject.TryGetComponent<Renderer>(out component);
                        if (targetGameObject.transform.parent.gameObject != null)
                        {
                            targetGameObject.transform.parent.gameObject.TryGetComponent<Renderer>(out component);
                        }
                    }
                }
                if (component == null)
                    return;
            }
            if (sel.gameObject.TryGetComponent<Renderer>(out component) ||
                sel.gameObject.transform.parent.gameObject.TryGetComponent<Renderer>(out component))
            {
                if (component == null)
                    return;
                if (component.material.GetFloat("_Metallic") == .754f)
                {
                    ColorTool.Material = component.material;
                    if (ColorToolActiveCheck.colorToolActive)
                        component.material.color = ColorTool.ColorToSet;
                }
            }
        }

        public override void OnClear(ClearInfo info)
        {
            info.selection.Deselect();
        }
    }
}