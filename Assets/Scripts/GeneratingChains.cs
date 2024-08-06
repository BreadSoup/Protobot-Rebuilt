using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;


public class GeneratingChains : MonoBehaviour{

    private ChainGenerator chainGenerator;

    // Start is called before the first frame update
    void Start()
    {
        // Add a SplineContainer component to this GameObject.
        var container = gameObject.AddComponent<SplineContainer>();

        // Create a new Spline on the SplineContainer.
        var spline = container.AddSpline();

        // Set some knot values.
        var knots = new BezierKnot[3];
        knots[0] = new BezierKnot(new float3(ChainGenerator.Point1x + 25,  ChainGenerator.Point1y, 0f));
        knots[1] = new BezierKnot(new float3(ChainGenerator.Point1x + 50,  ChainGenerator.Point1y, 0f));
        knots[2] = new BezierKnot(new float3(ChainGenerator.Point1x + 20, ChainGenerator.Point1y, 0f));
        spline.Knots = knots;

    }
}