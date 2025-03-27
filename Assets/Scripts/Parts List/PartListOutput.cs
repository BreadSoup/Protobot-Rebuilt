using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using SFB;
using Protobot.Builds.Windows;
using Protobot.Builds;
using DG.Tweening.Plugins.Core.PathCore;
using System.Security.Cryptography;
using Parts_List;

public class PartListOutput : MonoBehaviour
{
    [SerializeField] string partsList;
    [SerializeField] static Dictionary<string, int> partCount = new Dictionary<string, int>();
    [SerializeField] private WeightDisplay weightDisplay; // Reference to WeightDisplay component
    private float totalWeight = 0f; // Total weight of parts

    // Event to notify weight change
    public delegate void WeightUpdated(float newWeight);
    public event WeightUpdated OnWeightUpdated;

    public void CalculatePartsList()
    {
        //finds all of parts loaded in the scene
        PartName[] parts = FindObjectsOfType<PartName>();

        //checks for the amount of duplicate parts and creates a dictonary allowing for parts to be listed
        //"part name" x2
        //instead of
        //"part name"
        //"part name"
        partCount = new Dictionary<string, int>();
        totalWeight = 0f;

        foreach (PartName part in parts)
        {
            if (partCount.ContainsKey(part.name))
            {
                partCount[part.name]++;
            }
            else
            {
                partCount[part.name] = 1;
            }

            // Calculate total weight
            totalWeight += part.GetWeight(); // Add the weight of this part
        }
        // Notify subscribers about the weight update
        OnWeightUpdated?.Invoke(totalWeight);

        // Build the parts list string
        BuildPartsListString();
    }
        // New method to build the parts list string
        private void BuildPartsListString()
        {
            // Sorts the dictionary in descending order based on the amount of the part
            var sortedDictLinq = from entry in partCount orderby entry.Value descending select entry;
            var sortedDict = sortedDictLinq.ToDictionary(pair => pair.Key, pair => pair.Value);

            // Edits the output string in order to add a disclaimer about certain things 
            partsList = "======DISCLAIMER======\nHS is an abbreviation of High Strength\nNumbers inside of () are used to represent the hole count, for example C-Channel 1x2x1 (25) means the C-Channel is 25 holes long\n======================\n";
        
            // Add total weight to the output
            partsList += $"\nTotal Estimated Weight: {totalWeight} lbs\n\n======PARTS LIST======\n"; // Display total weight

            // Makes the dictionary into a string that is used as the input for txt file output 
            foreach (string key in sortedDict.Keys)
            {
                // Find the weight of the part by getting the PartName component
                PartName part = FindObjectsOfType<PartName>().FirstOrDefault(p => p.name == key);
                if (part.GetWeight() != 0)
                {
                    float partWeight = part.GetWeight(); // Get the weight in pounds
                    partsList += $"{key} x{sortedDict[key]} ({(partWeight * sortedDict[key]):F6} lbs)\n"; // Include total weight for this part type
                }
                else
                {
                    partsList += $"{key} x{sortedDict[key]} (Weight not found)\n"; // Handle case where part is not found
                }
            }
        }
        
        // New method to save the parts list to a file
        public void SavePartsListToFile()
        {
            string fileLocation = GetFileLocation();

            if (!string.IsNullOrEmpty(fileLocation))
            {
                File.WriteAllText(fileLocation, partsList);
            }
        }

        public float GetTotalWeight()
        {
            return totalWeight;
        }

        public string GetFileLocation()
        {
            //allows the user to specify the file path to save the txt file to
            return StandaloneFileBrowser.SaveFilePanel("Save Parts List", "", "Parts", "txt");
        }
}
