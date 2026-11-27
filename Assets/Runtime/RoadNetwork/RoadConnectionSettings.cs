using UnityEngine;

[System.Serializable]
public class RoadConnectionSettings
{
    [Range(0,20)]
    public int ForwardLaneCount = 1;
    [Range(0,20)]
    public int BackwardLaneCount = 1;
    [Min(0)]
    public float MedianStripWidth = 0.1f;
    [Min(0.0001f)]
    public float LaneWidth = 0.5f;

    public float ForwardRoadwayWidth => ForwardLaneCount * LaneWidth;
    public float BackwardRoadwayWidth => BackwardLaneCount * LaneWidth;
    public float HalfMedianStripWidth => MedianStripWidth * 0.5f;
    public float MedianStripOffset => (BackwardLaneCount - ForwardLaneCount) * LaneWidth;
    public float Width => MedianStripWidth + LaneWidth * (ForwardLaneCount + BackwardLaneCount);

    public RoadConnectionSettings(RoadConnectionSettings settings)
    {
        this.ForwardLaneCount = settings.ForwardLaneCount;
        this.BackwardLaneCount = settings.BackwardLaneCount;
        this.MedianStripWidth = settings.MedianStripWidth;
        this.LaneWidth = settings.LaneWidth;
    }
    public void ChangeToOneWayRoad()
    {
        ForwardLaneCount += BackwardLaneCount;
        BackwardLaneCount = 0;
        MedianStripWidth = 0;
    }
    public void ChangeDirection()
    {
        int temp = BackwardLaneCount;;
        BackwardLaneCount = ForwardLaneCount;
        ForwardLaneCount = temp;
    }
}