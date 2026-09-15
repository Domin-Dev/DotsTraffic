
using Unity.Entities;

public struct VehicleComponent : IComponentData
{
    public int PointIndex;
    public Entity CurrentRoadConnection;
    public bool directionA;
}
public struct VehicleStats : IComponentData
{
    public float Speed;
}