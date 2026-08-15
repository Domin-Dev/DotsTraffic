
using System;
using System.Collections.Generic;
using UnityEngine;

public class RoadElement : MonoBehaviour
{
    public class RoadPoint
    {
        public Vector3 Position;
        public Vector3 HandleIn = Vector3.left;
        public Vector3 HandleOut = Vector3.right;
    }
    
    [Range(0,15)]
    public int ForwardLaneCount = 1;
    [Range(0,15)]
    public int BackwardLaneCount = 1;
    [Min(0)]
    public float MedianStripWidth;
    [Min(0.001f)]
    public float LaneWidth;

    public List<Vector3> Points = new();


    public float ForwardRoadwayWidth => ForwardLaneCount * LaneWidth;
    public float BackwardRoadwayWidth => BackwardLaneCount * LaneWidth;
    public float HalfMedianStripWidth => MedianStripWidth * 0.5f;
}