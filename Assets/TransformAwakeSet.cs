using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformAwakeSet : MonoBehaviour
{
    [SerializeField] PositionManager positionManager;
    private void OnEnable()
    {
        positionManager.UpdateValues();
    }
}
