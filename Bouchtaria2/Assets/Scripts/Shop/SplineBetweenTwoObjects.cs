using UnityEngine;
using UnityEngine.Splines;

public class SplineBetweenTwoObjects : MonoBehaviour
{
    public Transform startObject;
    public Transform endObject;

    public Vector3 startTangentOffset = new Vector3(2f, 0f, 0f);
    public Vector3 endTangentOffset = new Vector3(-2f, 0f, 0f);

    private SplineContainer splineContainer;

    void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();
    }

    void Update()
    {
        if (splineContainer == null || splineContainer.Splines.Count == 0)
            return;

        Spline spline = splineContainer.Splines[0];

        // --- Start knot ---
        BezierKnot startKnot = spline[0];
        startKnot.Position = startObject.position;
        spline[0] = startKnot;

        // --- End knot ---
        BezierKnot endKnot = spline[3];
        endKnot.Position = endObject.position;
        spline[3] = endKnot;

        // --- Start tangent ---
        BezierKnot startTangent = spline[1];
        startTangent.Position = startObject.position + startTangentOffset;
        spline[1] = startTangent;

        // --- End tangent ---
        BezierKnot endTangent = spline[2];
        endTangent.Position = endObject.position + endTangentOffset;
        spline[2] = endTangent;
    }
}
