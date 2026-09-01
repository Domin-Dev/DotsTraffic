
using System;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class RoadPoint 
{
    public Vector3 Position;
    public Quaternion Rotation;

    public RoadPoint(Vector3 position,Quaternion rotation)
    {
        this.Position = position;
        this.Rotation = rotation;
    }
    public RoadPoint(Vector3 position)
    {
        this.Position = position;
        this.Rotation = Quaternion.identity;
    }
    public RoadPoint(RoadPoint roadPoint)
    {
        this.Position = roadPoint.Position;
        this.Rotation = roadPoint.Rotation;
    }
}

