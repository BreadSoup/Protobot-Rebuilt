using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using SFB;
using Protobot.Builds.Windows;
using Protobot.Builds;
using DG.Tweening.Plugins.Core.PathCore;
using System.Linq;
using System.Security.Cryptography;
using Parts_List;

public class PartListOutput : MonoBehaviour
{
    [SerializeField] string partsList;
    [SerializeField] static Dictionary<string, int> partCount = new Dictionary<string, int>();
    public void OutputPartsList()
    {
        //finds all of parts loaded in the scene
        PartName[] parts = FindObjectsOfType<PartName>();

        //checks for the amount of duplicate parts and creates a dictonary allowing for parts to be listed
        //"part name" x2
        //instead of
        //"part name"
        //"part name"
        partCount = new Dictionary<string, int>();
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
        }

        //sorts the dictionary in decending order based on the amount of the part
        var sortedDictLinq = from entry in partCount orderby entry.Value descending select entry;
        var sortedDict = sortedDictLinq.ToDictionary(pair => pair.Key, pair => pair.Value);

        //edits the output string in order to add a disclaimer about certain things 
        //for example clarifying that HS is an abbrievation of High Strength

        partsList = "======DISCLAIMER======\nHS is an abbreviation of High Strength\nNumbers inside of () are used the represent the hole count, for example C-Channel 1x2x1 (25) means the C-Channel is 25 holes long\n======================\n\n======PARTS LIST======\n";
        //makes the dictionary into a string that is used as the input for txt file output 
        foreach (string key in sortedDict.Keys)
        {
            partsList = partsList + key + " x" + sortedDict[key] + "\n";
        }


        //writes the partlist string into a txt file instead of having an in-program ui

        string fileLocation = GetFileLocation();

        if (!(fileLocation == ""))
        {
            File.WriteAllText(fileLocation, partsList);
        }
    }

    public string GetFileLocation()
    {
        //allows the user to specify the file path to save the txt file to
        string fullPath = StandaloneFileBrowser.SaveFilePanel("Save Parts List", "", "Parts", "txt");
        return fullPath;
    }
}
