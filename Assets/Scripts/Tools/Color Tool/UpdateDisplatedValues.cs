using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UpdateDisplatedValues : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI m_TextMeshProUGUI;
    [SerializeField] Slider m_Slider;

    private void Awake()
    {
        m_Slider = GetComponent<Slider>();
    }

    public void UpdateText(string color)
    {
        m_TextMeshProUGUI.text = color + m_Slider.value;
    }

}
