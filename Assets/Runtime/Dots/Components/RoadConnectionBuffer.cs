using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct RoadConnectionBuffer : IBufferElementData
{
    public float3 LocalHandleA;
    public float3 LocalHandleB;
    public Entity nodeA;
    public Entity nodeB;
}