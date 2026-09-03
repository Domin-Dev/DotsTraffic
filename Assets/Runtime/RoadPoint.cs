
using System;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class RoadPoint 
{
    public Vector3 Position;
    public Vector3 LocalPositionHandleA;
    public Vector3 LocalPositionHandleB;




    public Vector3 HandleA => Position + LocalPositionHandleA;
    public Vector3 HandleB => Position + LocalPositionHandleB;

    public Quaternion Rotation;

    public RoadPoint(Vector3 position,Quaternion rotation)
    {
        this.Position = position;
        this.Rotation = rotation;
        this.LocalPositionHandleA = -Vector3.forward;
        this.LocalPositionHandleB = Vector3.forward;
    }
    public RoadPoint(Vector3 position,Vector3 handleA,Vector3 HandleB)
    {
        this.LocalPositionHandleA = handleA;
        this.LocalPositionHandleB = HandleB;
        this.Position = position;
    }
    public RoadPoint(Vector3 position)
    {
        this.Position = position;
        this.Rotation = Quaternion.identity;
        this.LocalPositionHandleA = -Vector3.forward;
        this.LocalPositionHandleB = Vector3.forward;
    }
    public RoadPoint(RoadPoint roadPoint)
    {
        this.Position = roadPoint.Position;
        this.Rotation = roadPoint.Rotation; 
        this.LocalPositionHandleA = roadPoint.LocalPositionHandleA;
        this.LocalPositionHandleB = roadPoint.LocalPositionHandleB;
    }
}

