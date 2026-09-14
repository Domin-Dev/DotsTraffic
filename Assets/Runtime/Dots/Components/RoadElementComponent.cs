using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct RoadElementComponent : IComponentData
{
    public int ForwardLaneCount;
    public int BackwardLaneCount;
    public float MedianStripWidth;
    public float LaneWidth;
}