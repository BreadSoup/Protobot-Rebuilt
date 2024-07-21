using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class GenerateLoopedSpline : MonoBehaviour
{
    public Vector3 point1;
    public Vector3 point2;
    public float radius1;
    public float radius2;

    void Start()
    {
        // Add a SplineContainer component to this GameObject.
        var container = gameObject.AddComponent<SplineContainer>();

        // Create a new Spline on the SplineContainer.
        var spline = container.AddSpline();

        // Calculate the positions of the knots based on the points and radii.
        var knot1 = point1 + (point2 - point1).normalized * radius1;
        var knot2 = point2 + (point1 - point2).normalized * radius2;
        var knot3 = point1 - (point2 - point1).normalized * radius1;
        var knot4 = point2 - (point1 - point2).normalized * radius2;

        // Create the knots for the spline.
        var knots = new BezierKnot[4];
        knots[0] = new BezierKnot(new float3(knot1.x, knot1.y, knot1.z));
        knots[1] = new BezierKnot(new float3(knot2.x, knot2.y, knot2.z));
        knots[2] = new BezierKnot(new float3(knot3.x, knot3.y, knot3.z));
        knots[3] = new BezierKnot(new float3(knot4.x, knot4.y, knot4.z));

        // Set the knots for the spline.
        spline.Knots = knots;
    }
}