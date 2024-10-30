using System;
using UnityEngine;

namespace Protobot.SelectionSystem {
    public class HoleFaceResponseSelector : MonoBehaviour, IResponseSelector {
        [SerializeField] private HoleFace hoverHoleFace = null;
        [SerializeField] private HoleFace selectHoleFace = null;

        public ISelection GetResponseSelection(ISelection incomingSelection) {
            if (incomingSelection.gameObject.CompareTag("HoleCollider")) {
                selectHoleFace.gameObject.SetActive(true);
                selectHoleFace.Set(hoverHoleFace);
                ColorSelectionResponse colorSelectionResponse = selectHoleFace.gameObject.GetComponent<ColorSelectionResponse>();
                if (colorSelectionResponse == null)
                {
                    colorSelectionResponse = selectHoleFace.gameObject.AddComponent<ColorSelectionResponse>();
                }
                colorSelectionResponse.HoleColliderException(incomingSelection);
                return new HoleSelection {
                    gameObject = selectHoleFace.gameObject,
                    selector = incomingSelection.selector,
                    holeData = selectHoleFace.hole,
                    holeFace = selectHoleFace,
                    faceDirection = selectHoleFace.direction
                };
            }
            return null;
        }
    }
    
}