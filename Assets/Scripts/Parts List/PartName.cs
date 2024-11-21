using UnityEngine;

namespace Parts_List
{
    public class PartName : MonoBehaviour
    {
        public new string name;
        public float weightInGrams; // Weight
        public float GetWeight()
        {
            // Convert grams to pounds
            float weightInPounds = weightInGrams / 453.592f; // Convert grams to pounds

            return weightInPounds; // Return the weight in pounds
        }
    }
}
