using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Protobot.UI {
    public class TooltipUI : MonoBehaviour {
        public enum Direction {
            Up,
            Down,
            Left,
            Right
        };
        
        private RectTransform rectTransform;
        private Image image;
        
        [SerializeField] private Text toolTipText;
        [SerializeField] private bool showing;
        private float hoverTimer;
        [SerializeField] private float waitDuration;

        private Vector3 pos;

        void Start() {
            rectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            HideToolTip();
        }

        void Update() {
            if (showing) {
                hoverTimer += Time.deltaTime;

                if (hoverTimer >= waitDuration) {
                    image.enabled = true;
                    toolTipText.enabled = true;

                    rectTransform.sizeDelta = new Vector2(toolTipText.preferredWidth + 5, toolTipText.preferredHeight+ 5);

                    rectTransform.position = pos;
                }
            }
        }

        public Vector3 CalculatePosition(RectTransform otherRect, Direction dir) {
            Vector3 newPos = otherRect.position;

            var sizeDelta = rectTransform.sizeDelta;
            var otherSizeDelta = otherRect.sizeDelta;
            
            Vector2 offset = (otherSizeDelta / 2) + sizeDelta;
            
            Vector3 xPosShift = new Vector3(offset.x, 0, 0);
            Vector3 yPosShift = new Vector3(0, offset.y, 0);

            if (dir == Direction.Up) newPos += yPosShift;
            if (dir == Direction.Down) newPos -= yPosShift;
            if (dir == Direction.Right) newPos += xPosShift;
            if (dir == Direction.Left) newPos -= xPosShift;

            return newPos;
        }
        private bool MouseWithinRect(RectTransform rect) {
            Vector2 localMousePosition = Mouse.current.position.ReadValue();
            Vector3[] worldCorners = new Vector3[4];
            rect.GetWorldCorners(worldCorners);
            Rect worldRect = new Rect(
                worldCorners[0].x, 
                worldCorners[0].y, 
                worldCorners[2].x - worldCorners[0].x, 
                worldCorners[2].y - worldCorners[0].y);
            return worldRect.Contains(localMousePosition);
        }

        public void ShowTooltip(string text, RectTransform rect, Direction dir) {
            if (text == "") return;
            if (!MouseWithinRect(rect)) return;
            toolTipText.text = text;
            hoverTimer = 0;
            pos = CalculatePosition(rect, dir);
            showing = true;
        }

        public void HideToolTip() {
            image.enabled = false;
            toolTipText.enabled = false;
            hoverTimer = 0;
            showing = false;
        }
    }
}
