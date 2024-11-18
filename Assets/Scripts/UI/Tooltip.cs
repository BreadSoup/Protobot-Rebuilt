using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Protobot.UI {
    [RequireComponent(typeof(RectTransform))]
    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        private RectTransform rect;
        [SerializeField] private TooltipUI.Direction dir;
        [SerializeField] private TooltipUI tooltipRef;
        public string text;

        private void Start() {
            rect = GetComponent<RectTransform>(); // since this is only getting the rect transform of the object this script is attached to
        }

        public void OnPointerEnter(PointerEventData eventData) {
            tooltipRef.HideToolTip();
            tooltipRef.ShowTooltip(text, rect, dir);
        }

        public void OnPointerExit(PointerEventData eventData) {
            tooltipRef.HideToolTip();
        }

        public void OnDisable() {
            tooltipRef.HideToolTip();
        }
    }
}