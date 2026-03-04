using System.Collections;
using UnityEngine;
using EPOOutline;

namespace Protobot.Outlining {
    public static class Outliner {
        public static void EnableOutline(this GameObject obj, int colorIndex, int layer = 0, float fillAlpha = 0.1f) {
            Renderer renderer = obj.GetComponent<Renderer>();

            if (renderer != null) {
                Outlinable outline = obj.GetComponent<Outlinable>();

                if (outline == null) {
                    outline = obj.AddComponent<Outlinable>();
                    outline.AddAllChildRenderersToRenderingList();
                    //outline.OutlineParameters.FillPass.Shader = Resources.Load<Shader>("Easy performant Outline/Shaders/Fills/ColorFill");
                }

                outline.enabled = true;
                outline.OutlineLayer = layer;
                outline.OutlineParameters.Color = OutlineSettings.GetColor(colorIndex);

                //var fillColor = OutlineSettings.GetColor(colorIndex);
                //fillColor.a = fillAlpha;
                //outline.OutlineParameters.FillPass.SetColor("_PublicColor", fillColor);
            }

            for (int i = 0; i < obj.transform.childCount; i++)
                EnableOutline(obj.transform.GetChild(i).gameObject, colorIndex);
        }

        public static void DisableOutline(this GameObject obj) {
            if (obj == null) return; // guard against destroyed objects (e.g. HoleColliders removed during resize)
            Outlinable outline = obj.GetComponent<Outlinable>();

            if (outline != null) {
                //if (outline.persistent)
                //    return;
                //else
                    outline.enabled = false;
            }

            for (int i = 0; i < obj.transform.childCount; i++)
                DisableOutline(obj.transform.GetChild(i).gameObject);
        }
    }
}