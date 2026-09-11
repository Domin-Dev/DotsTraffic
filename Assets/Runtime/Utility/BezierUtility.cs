
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class BezierUtility
{
    public static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return u*u*u*p0 + 3f*u*u*t*p1 + 3f*u*t*t*p2+ t*t*t*p3;
    }
    public static Vector3 Tangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return 3f*u*u*(p1 - p0) + 6f*u*t*(p2 - p1) + 3f*t*t*(p3 - p2);
    }   
    public static float EstimateBezierLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int samples = 10)
    {
        float length = 0f;
        Vector3 previous = p0;

        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector3 current = BezierUtility.Bezier(p0, p1, p2, p3, t);
            length += Vector3.Distance(previous, current);
            previous = current;
        }
        return length;
    }
    public static Vector3 GetSpaceUp(this RoadSpace space)
    {
        return space switch
        {
            RoadSpace.XZ => Vector3.up,      
            RoadSpace.XY  => Vector3.forward,
            RoadSpace.YZ  => Vector3.right,  
            RoadSpace.XYZ     => Vector3.up,    
            _ => Vector3.up
        };
    }
    public static List<Vector3> GetOffsetBezier( Vector3 startPoint, Vector3 startHandler, Vector3 endHandler, Vector3 endPoint,
    Vector3? prevStartHandler, Vector3? nextEndHandler, float offset, int segments = 30)
    {
        List<Vector3> rawOffsetPoints = new List<Vector3>();

        for (int i = 0; i <= segments; i++) 
        {
            float t = i / (float)segments;
            Vector3 pos = Bezier(startPoint, startHandler, endHandler, endPoint, t);
            Vector3 tangent = Tangent(startPoint, startHandler, endHandler, endPoint, t).normalized;

            if (i == 0 && prevStartHandler.HasValue)
            {
                Vector3 prevTangent = (startPoint - prevStartHandler.Value).normalized;
                tangent = (prevTangent + tangent) * 0.5f;
            }
            else if (i == segments && nextEndHandler.HasValue)
            {
                Vector3 nextTangent = (nextEndHandler.Value - endPoint).normalized;
                tangent = (tangent + nextTangent) * 0.5f;
            }

            Vector3 offsetDirection = math.normalizesafe(math.cross(tangent.normalized, Vector3.up));
            rawOffsetPoints.Add(pos + offsetDirection * offset);
        }

        return RemoveSelfIntersections(rawOffsetPoints);
    }

    public static List<Vector3> RemoveSelfIntersections(List<Vector3> points)
    {
        int count = points.Count;
        int cutStart = -1;
        int cutEnd = -1;
        Vector3 intersectionPoint = Vector3.zero;

        for (int i = 0; i < count - 2; i++)
        {
            for (int j = i + 2; j < count - 1; j++)
            {
                if (TryGetLineIntersection(points[i], points[i + 1], points[j], points[j + 1], out Vector3 intersect))
                {
                    cutStart = i;
                    cutEnd = j;
                    intersectionPoint = intersect;
                    break;
                }
            }
            if (cutStart != -1) break;
        }

        if (cutStart != -1)
        {
            List<Vector3> cleaned = new List<Vector3>();
            for (int i = 0; i <= cutStart; i++) cleaned.Add(points[i]);
            cleaned.Add(intersectionPoint);
            for (int i = cutEnd + 1; i < count; i++) cleaned.Add(points[i]);
            return cleaned;
        }
        return points;
    }
    public static bool TryGetLineIntersection(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, out Vector3 intersection)
    {
        intersection = Vector3.zero;

        float d = (p2.x - p1.x) * (p4.z - p3.z) - (p2.z - p1.z) * (p4.x - p3.x);
        if (Mathf.Abs(d) < 0.0001f) return false;

        float u = ((p3.x - p1.x) * (p4.z - p3.z) - (p3.z - p1.z) * (p4.x - p3.x)) / d;
        float v = ((p3.x - p1.x) * (p2.z - p1.z) - (p3.z - p1.z) * (p2.x - p1.x)) / d;

        if (u >= 0 && u <= 1 && v >= 0 && v <= 1)
        {
            intersection = new Vector3(p1.x + u * (p2.x - p1.x), p1.y, p1.z + u * (p2.z - p1.z));
            return true;
        }
        return false;
    }

}