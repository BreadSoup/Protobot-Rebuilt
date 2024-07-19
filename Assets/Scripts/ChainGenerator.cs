using UnityEngine;

public class ChainGenerator : MonoBehaviour
{
    public Transform line; // Assign the "Line" object in the Inspector
    public GameObject chainLinkPrefab; // Assign the "ChainLink" object in the Inspector
    public float linkSpacing = 0.1f; // Set the distance between each chain link

    private void Start()
    {
        GenerateChain();
    }

    private void GenerateChain()
    {
        // Calculate the line's start and end points
        Vector3 lineStart = line.position - line.localScale.z * 0.5f * Vector3.forward;
        Vector3 lineEnd = line.position + line.localScale.z * 0.5f * Vector3.forward;

        // Calculate the line's length
        float lineLength = Vector3.Distance(lineStart, lineEnd);

        // Generate the chain links along the line
        Vector3 currentPosition = lineStart;
        while (currentPosition.z < lineEnd.z)
        {
            GameObject chainLink = Instantiate(chainLinkPrefab, currentPosition, Quaternion.Euler(0, 90, 0));
            chainLink.transform.parent = transform;
            currentPosition += linkSpacing * Vector3.forward;
        }
    }
}