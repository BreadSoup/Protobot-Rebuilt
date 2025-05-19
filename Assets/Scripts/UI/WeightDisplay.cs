using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Make sure to include this if you're using UI Text
using TMPro; // Uncomment this if you're using TextMeshPro

public class WeightDisplay : MonoBehaviour
{
    /// <summary>
    /// public Text weightText; // For regular UI Text
    /// </summary>
    public TextMeshProUGUI weightText; // For TextMesh Pro

    private float estimatedWeight = 0; // This is your variable

    void Start()
    {
        // Find the PartListOutput instance and subscribe to the OnWeightUpdated event
        PartListOutput partListOutput = FindObjectOfType<PartListOutput>();
        if (partListOutput != null)
        {
            partListOutput.OnWeightUpdated += UpdateWeightDisplay;
        }

        UpdateWeightDisplay(estimatedWeight); // Initial display
    }

    // Method to update the weight display
    public void UpdateWeightDisplay(float newWeight)
    {
        estimatedWeight = newWeight; // Update the estimated weight variable
        weightText.text = $"Total Weight: {estimatedWeight:F2} lbs"; // Update the UI text
    }
}