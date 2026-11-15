using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct RoadElementComponent : IComponentData
{
    public int ForwardLaneCount;
    public int BackwardLaneCount;
    public float MedianStripWidth;
    public float LaneWidth;

    public RoadElementComponent(RoadConnectionSettings settings)
    {
        this.ForwardLaneCount = settings.ForwardLaneCount;
        this.BackwardLaneCount = settings.BackwardLaneCount;
        this.MedianStripWidth = settings.MedianStripWidth;
        this.LaneWidth = settings.LaneWidth;
    }
}