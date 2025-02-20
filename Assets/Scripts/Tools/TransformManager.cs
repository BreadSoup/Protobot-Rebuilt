using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using Protobot;
using Protobot.StateSystems;
using Protobot.InputEvents;
using Protobot.Transformations;
using UnityEngine.InputSystem;

public class TransformManager : MonoBehaviour
{
    [SerializeField]
    private MovementManager movementManager;

    [SerializeField]
    private GameObject transformMenu;

    [SerializeField]
    private InputField x;
    [SerializeField]
    private InputField y;
    [SerializeField]
    private InputField z;

    private Vector3 _lastPosition;
    private GameObject _movingObject;
    private CanvasGroup _canvasGroup;

    // Start is called before the first frame update
    void Start()
    {
        _movingObject = movementManager.MovingObj;
        _canvasGroup = transformMenu.GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
        {
            _canvasGroup = transformMenu.AddComponent<CanvasGroup>();
        }

        if (_movingObject != null)
        {
            _lastPosition = _movingObject.transform.position;
            UpdateTextFields(_lastPosition);
            FadeInTransformMenu();
        }
        else
        {
            FadeOutTransformMenu();
        }
    }

    // Update is called once per frame
    void Update()
    {
        GameObject movingObject = movementManager.MovingObj;

        if (movingObject != _movingObject)
        {
            _movingObject = movingObject;

            if (_movingObject != null)
            {
                _lastPosition = _movingObject.transform.position;
                UpdateTextFields(_lastPosition);
                FadeInTransformMenu();
            }
            else
            {
                FadeOutTransformMenu();
            }
        }

        if (movingObject != null)
        {
            if (movingObject.transform.position != _lastPosition)
            {
                _lastPosition = movingObject.transform.position;
                UpdateTextFields(_lastPosition);
            }
        }
    }

    private void UpdateTextFields(Vector3 position)
    {
        x.text = position.x.ToString() + "in";
        y.text = position.y.ToString() + "in";
        z.text = position.z.ToString() + "in";
    }

    public void SetXPosition(string xPos)
    {
        xPos = xPos.Replace("in", "");
        if (_movingObject == null) return;

        Debug.Log(_movingObject);

        if (float.TryParse(xPos, out float xValue))
        {
            Vector3 newPosition = _movingObject.transform.position;
            newPosition.x = xValue;
            movementManager.MoveTo(newPosition);
            Debug.Log(newPosition);
            _lastPosition = newPosition;
        }
        UpdateTextFields(_lastPosition);
    }

    public void SetYPosition(string yPos)
    {
        yPos = yPos.Replace("in", "");
        if (_movingObject == null) return;

        if (float.TryParse(yPos, out float yValue))
        {
            Vector3 newPosition = _movingObject.transform.position;
            newPosition.y = yValue;
            movementManager.MoveTo(newPosition);
            _lastPosition = newPosition;
        }
        UpdateTextFields(_lastPosition);
    }

    public void SetZPosition(string zPos)
    {
        zPos = zPos.Replace("in", "");
        if (_movingObject == null) return;

        if (float.TryParse(zPos, out float zValue))
        {
            Vector3 newPosition = _movingObject.transform.position;
            newPosition.z = zValue;
            movementManager.MoveTo(newPosition);
            _lastPosition = newPosition;
        }
        UpdateTextFields(_lastPosition);
    }

    private void FadeInTransformMenu()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.DOFade(1f, 0.5f).SetEase(Ease.OutQuad);
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }
    }

    private void FadeOutTransformMenu()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.DOFade(0f, 0.5f).SetEase(Ease.OutQuad);
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }
}
