using UnityEngine;

namespace Parts_List
{
    /// <summary>
    /// Stores display information about a placed part for use in the UI.
    /// Added to every part when it is generated and placed in the scene.
    /// </summary>
    public class PartName : MonoBehaviour
    {
        public new string name;
        public float weightInGrams;

        [Header("Properties Menu Display")]
        [Tooltip("Label for the first parameter row, e.g. 'Profile', 'Type', 'Length'.")]
        public string param1Label;

        [Tooltip("Value for the first parameter row, e.g. '1x2', 'Normal', '4 holes'.")]
        public string param1Display;

        [Tooltip("Label for the second parameter row, e.g. 'Length', 'Width'. Leave blank to hide the row.")]
        public string param2Label;

        [Tooltip("Value for the second parameter row, e.g. '20 holes', '12\"', '3'.")]
        public string param2Display;

        /// <summary>
        /// Returns the part weight converted from grams to pounds.
        /// </summary>
        public float GetWeight()
        {
            float weightInPounds = weightInGrams / 453.592f;
            return weightInPounds;
        }
    }
}

