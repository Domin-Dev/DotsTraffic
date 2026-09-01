
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


    // [UnityEditor.MenuItem("RoadElement/DeletePoint _DEL",true, 1000)]
    // public static bool DeletePoiknt()
    // {
    //     return true; 
    // }   
}

