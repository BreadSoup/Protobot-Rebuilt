using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Protobot.Transformations;

namespace Protobot {
    public class ScrewPlaceDisplacement : PlaceDisplacement {
        [SerializeField] private HoleFace currentHoleFace;
        public override bool ModifyRotation => true;

        public override bool TryGetDisplacement(PlacementData placementData, out Displacement displacement) {
            if (currentHoleFace.direction != Vector3.zero) // this has to return true if it doesn't this script wont do anything
            {
                //TODO: have a list of ids that are checked for screw placement instead of hardcoding it for better compatibility in the future
                if (placementData.objectId.Contains("SCRW") || placementData.objectId.Contains("RBMP")) // || is OR in C# so in coding if I have a integer and I want it to return true only if the value is 1 or 2 I would do if (value == 1 || value == 2)
                {
                    if (currentHoleFace.gameObject.activeInHierarchy)
                    {
                        displacement = new Displacement(currentHoleFace.position, currentHoleFace.LookRotation);
                        return true;
                    }
                }
            }

            displacement = null;
                return false;
        }
    }
}