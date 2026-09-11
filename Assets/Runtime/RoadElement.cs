
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.InputSystem;


public class RoadElement : MonoBehaviour
{    

    public bool Loop;
    [Range(0,15)]
    public int ForwardLaneCount = 1;
    [Range(0,15)]
    public int BackwardLaneCount = 1;
    [Min(0)]
    public float MedianStripWidth = 0.1f;
    [Min(0.001f)]
    public float LaneWidth = 0.5f;

    public List<RoadPoint> Points = new();


    public float ForwardRoadwayWidth => ForwardLaneCount * LaneWidth;
    public float BackwardRoadwayWidth => BackwardLaneCount * LaneWidth;
    public float HalfMedianStripWidth => MedianStripWidth * 0.5f;


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
    public void ProjectToSurface()
    {
        if(transform.parent.TryGetComponent<SubSceneGenerator>(out var generator))
        {
            for(int i = 0; i < Points.Count; i++)
            {
                Ray ray = new Ray(transform.TransformPoint(Points[i].Position + Vector3.up),Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit,10000f,(int)generator.surfaceLayer))
                {
                    Points[i].Position = transform.InverseTransformPoint(hit.point);
                }
            }
        }
    }



    public void DuplicatePoint(RoadPoint roadPoint)
    {
        int index = Points.IndexOf(roadPoint);
        if(index >= 0)
        {   
            RoadPoint newPoint;
            if(Points.Count == index + 1 || index == 0)
            {
                if(Loop)
                {
                    if(Points.Count == 1)
                    {
                        var direction = roadPoint.LocalPositionHandleA.normalized + roadPoint.LocalPositionHandleB.normalized;
                        if (direction.sqrMagnitude < 0.1f)
                            direction = Vector3.Cross(roadPoint.LocalPositionHandleA, Vector3.up).normalized;  
                        float length = roadPoint.LocalPositionHandleA.magnitude + roadPoint.LocalPositionHandleB.magnitude;
                        newPoint = new RoadPoint(roadPoint.Position + direction * length,-roadPoint.LocalPositionHandleA,-roadPoint.LocalPositionHandleB); 
                    }
                    else
                        newPoint = new RoadPoint((roadPoint.HandleA + roadPoint.HandleB) * 0.5f,-roadPoint.LocalPositionHandleA,-roadPoint.LocalPositionHandleA);  
                }
                else
                {
                    newPoint = new RoadPoint(roadPoint.Position + roadPoint.LocalPositionHandleA * 2f,roadPoint.LocalPositionHandleA,-roadPoint.LocalPositionHandleA);  
                }
//Vector3 position = roadPoint.Position + roadPoint.LocalPositionHandleA * 2f;
              //  Vector3 handleA = Loop ? Points[0].HandleB - position: -roadPoint.LocalPositionHandleA;        
            }
            else
                newPoint = new RoadPoint(roadPoint);
             //   newPoint.Position += (Points[index + 1].Position - roadPoint.Position).normalized;
            Points.Insert(index + 1,newPoint);
        }
    }  
    public void DeletePoint(RoadPoint roadPoint)
    {
        Points.Remove(roadPoint);
    }
    public void AddPoint()
    {
        if(Points.Count > 0)
            DuplicatePoint(Points[Points.Count -1]);
        else  
            Points.Add(new RoadPoint(Vector3.zero));
    }
    
}

