using UnityEngine;
using UnityEngine.UI;

public class ChainGenerator : MonoBehaviour
{

    public Toggle toggle;

    void Start()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }
    }

    void OnToggleChanged(bool isOn)
    {
        if (isOn)
        {
            new GameObject("ChainGeneratorObject");
        }
    }
}