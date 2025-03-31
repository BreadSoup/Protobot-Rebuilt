using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using Protobot;
using Protobot.SelectionSystem;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PositionManager : MonoBehaviour
{
    [SerializeField] private GameObject transformTool;
    [SerializeField] private GameObject container;
    [SerializeField] private GameObject  selectionManager;
    [SerializeField] private Text partText;
    //TODO make this not formerly serialized
    [FormerlySerializedAs("PosXText")] [SerializeField] private InputField posXText;
    [FormerlySerializedAs("PosYText")] [SerializeField] private InputField posYText;
    [FormerlySerializedAs("PosZText")] [SerializeField] private InputField posZText;
    
    [FormerlySerializedAs("RotXText")] [SerializeField] private InputField rotXText;
    [FormerlySerializedAs("RotYText")] [SerializeField] private InputField rotYText;
    [FormerlySerializedAs("RotZText")] [SerializeField] private InputField rotZText;
    public GameObject obj = null;

    private string rotX;
    private string rotY;
    private string rotZ;
    private string posX;
    private string posY;
    private string posZ;
    

    void Update()
    {
        if (!transformTool.activeInHierarchy)
        {
            container.SetActive(false);
            return;
        }
        container.SetActive(true);
        obj = selectionManager.GetComponent<SelectionManager>().current.gameObject;
        //obj.transform.position = new Vector3(Convert.ToInt16(PosXText.text), Convert.ToInt16(PosYText.text), Convert.ToInt16(PosZText.text));
        //obj.transform.rotation = Quaternion.Euler(Convert.ToInt16(RotXText.text), Convert.ToInt16(RotYText.text), Convert.ToInt16(RotZText.text));
        /*obj.transform.position.Set(Convert.ToInt16(PosXText.text),  obj.transform.position.y, obj.transform.position.z);
        obj.transform.position.Set(obj.transform.position.x, Convert.ToInt16(PosYText.text), obj.transform.position.z);
        
        PosYText.text = obj.transform.position.y.ToString();
        PosZText.text = obj.transform.position.z.ToString();
        
        RotXText.text = obj.transform.rotation.eulerAngles.x.ToString();
        RotYText.text = obj.transform.rotation.eulerAngles.y.ToString();
        RotZText.text = obj.transform.rotation.eulerAngles.z.ToString();*/
        if (obj.CompareTag("Hole"))
        {
            obj = obj.GetComponent<HoleFace>().hole.part.gameObject;
        }
        partText.text = obj.name;
    }
    
    public void UpdateValues()
    {
        obj = selectionManager.GetComponent<SelectionManager>().current.gameObject;
        posXText.text = obj.transform.position.x.ToString();
        posX = obj.transform.position.x.ToString();
        posYText.text = obj.transform.position.y.ToString();
        posY = obj.transform.position.y.ToString();
        posZText.text = obj.transform.position.z.ToString();
        posZ = obj.transform.position.z.ToString();
        
        rotXText.text = obj.transform.rotation.eulerAngles.x.ToString();
        rotX = obj.transform.rotation.eulerAngles.x.ToString();
        rotYText.text = obj.transform.rotation.eulerAngles.y.ToString();
        rotY = obj.transform.rotation.eulerAngles.y.ToString();
        rotZText.text = obj.transform.rotation.eulerAngles.z.ToString();
        rotZ = obj.transform.rotation.eulerAngles.z.ToString();
    }

    public void ApplyValues(InputField input)
    {
        if (string.IsNullOrEmpty(posX) || string.IsNullOrEmpty(posY) || string.IsNullOrEmpty(posZ) ||
            string.IsNullOrEmpty(rotX) || string.IsNullOrEmpty(rotY) || string.IsNullOrEmpty(rotZ))
        {
            Debug.LogError("One or more input values are null or empty.");
            return;
            
        }
        
        Debug.Log(input.text);
        obj.transform.position = new Vector3(
            float.Parse(posX),
            float.Parse(posY),
            float.Parse(posZ)
        );

        obj.transform.rotation = Quaternion.Euler(
            float.Parse(rotX),
            float.Parse(rotY),
            float.Parse(rotZ)
        );
        
        obj.transform.position = obj.transform.position;
        obj.transform.rotation = obj.transform.rotation;
    }
    
    public void ChangeRotX(string newValue)
    {
        rotX = newValue;
        Debug.Log("rotX changed to: " + rotX);
    }

    public void ChangeRotY(string newValue)
    {
        rotY = newValue;
        Debug.Log("rotY changed to: " + rotY);
    }

    public void ChangeRotZ(string newValue)
    {
        rotZ = newValue;
        Debug.Log("rotZ changed to: " + rotZ);
    }

    public void ChangePosX(string newValue)
    {
        posX = newValue;
        Debug.Log("posX changed to: " + posX);
    }

    public void ChangePosY(string newValue)
    {
        posY = newValue;
        Debug.Log("posY changed to: " + posY);
    }

    public void ChangePosZ(string newValue)
    {
        posZ = newValue;
        Debug.Log("posZ changed to: " + posZ);
    }
}
