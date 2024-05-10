using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorToolActiveCheck : MonoBehaviour
{
    public static bool colorToolActive;

    private void OnEnable()
    {
        colorToolActive = true;
    }

    private void OnDisable()
    {
        colorToolActive = false;
    }
}
