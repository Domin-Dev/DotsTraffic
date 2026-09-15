using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct RoadConnectionBuffer : IBufferElementData
{
    public Entity Connection;
}