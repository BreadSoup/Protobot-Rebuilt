using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuickPrompt : MonoBehaviour
{
    public bool debug;
    public RectTransform rectTransform;
    
    // Start is called before the first frame update

    [Header("UI Reference")] 
    public Text displayText;

    public int response = 0;
    
    public IEnumerator Prompt(string text, Action<bool> callback) {
        displayText.text = text;
        displayText.enabled = true;
    
        // Move the UI up
        yield return StartCoroutine(MoveUI(rectTransform, -60));

        // Wait for user response
        yield return new WaitUntil(() => response > 0);

        // Move the UI back down based on the response
        yield return StartCoroutine(MoveUI(rectTransform, 150));

        // Store the result (true for response 1, false for response 2)
        bool result = (response == 1);

        // Call the callback function with the result
        callback?.Invoke(result);
    }

    public IEnumerator MoveUI(RectTransform quickPromptRectTransform, float destinationY)
    {
        float originY = quickPromptRectTransform.anchoredPosition.y;
        float totalMovementTime = 0.5f; // Total duration of the movement
        float currentMovementTime = 0f; // Time elapsed

        Vector3 startPosition = quickPromptRectTransform.anchoredPosition;

        while (currentMovementTime < totalMovementTime)
        {
            currentMovementTime += Time.deltaTime;
            float t = Mathf.Clamp01(currentMovementTime / totalMovementTime);
        
            // Using Sinusoidal ease-in-out for a smooth start and finish
            float easedT = Mathf.Sin(t * Mathf.PI * 0.5f); 

            quickPromptRectTransform.anchoredPosition = new Vector2(
                startPosition.x, 
                Mathf.Lerp(originY, destinationY, easedT)
            );

            yield return null;
        }

        quickPromptRectTransform.anchoredPosition = new Vector2(startPosition.x, destinationY); // Ensure final position is accurate
        Debug.Log($"Finished! Origin Y: {originY}, Destination Y: {destinationY}");
    }

    void Start()
    {
        
    }

    public void ChangeResponseValue(int value)
    {
        response = value;
    }
    // Update is called once per frame
    void Update()
    {
        //THIS IS DEBUG REMOVE LATER
        if (debug)
        {
            debug = false;
            
        }
    }
}
