
using System.Collections.Generic;
using System.Linq;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
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
    [HideInInspector] [SerializeReference] public RoadNode rootNode = new RoadNode(Vector3.right,Vector3.left);
    public float ForwardRoadwayWidth => ForwardLaneCount * LaneWidth;
    public float BackwardRoadwayWidth => BackwardLaneCount * LaneWidth;
    public float HalfMedianStripWidth => MedianStripWidth * 0.5f;
    public float MedianStripOffset => (BackwardLaneCount - ForwardLaneCount) * LaneWidth;
    public float Width => MedianStripWidth + LaneWidth * (ForwardLaneCount + BackwardLaneCount);

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
            rootNode.VisitNodes(node =>
            {
                Ray ray = new Ray(transform.TransformPoint(node.Position + Vector3.up),Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit,10000f,(int)generator.surfaceLayer))
                    node.Position = transform.InverseTransformPoint(hit.point);
            });
        }
    }
    public void DuplicatePoint(RoadNode roadPoint)
    {
        if(roadPoint == null) return;
        if(roadPoint.links.Count <= 1)
        {
            var link = roadPoint.links.FirstOrDefault();
            Vector3 localHandle = link == null ? Vector3.right : link.GetLocalHandle(roadPoint);
            roadPoint.ConnectRoad(new RoadNode(roadPoint.Position - localHandle * 2f),-localHandle,localHandle);  
        }
        else
            roadPoint.ConnectRoad(new RoadNode(roadPoint));

                //     if(Points.Count == 1)
                //     {
                //         var direction = roadPoint.LocalPositionHandleA.normalized + roadPoint.LocalPositionHandleB.normalized;
                //         if (direction.sqrMagnitude < 0.1f)
                //             direction = Vector3.Cross(roadPoint.LocalPositionHandleA, Vector3.up).normalized;  
                //         float length = roadPoint.LocalPositionHandleA.magnitude + roadPoint.LocalPositionHandleB.magnitude;
                //         newPoint = new RoadNode(roadPoint.Position + direction * length,-roadPoint.LocalPositionHandleA,-roadPoint.LocalPositionHandleB); 
                //     }
                //     else
                //         newPoint = new RoadNode((roadPoint.HandleA + roadPoint.HandleB) * 0.5f,-roadPoint.LocalPositionHandleA,-roadPoint.LocalPositionHandleA);  
                // // }
                // // else
                // // {
                // //     newPoint = new RoadNode(roadPoint.Position + roadPoint.LocalPositionHandleA * 2f,roadPoint.LocalPositionHandleA,-roadPoint.LocalPositionHandleA);  
                // // }     
          //  }
          //  else
          //      newPoint = new RoadNode(roadPoint);
             //   newPoint.Position += (Points[index + 1].Position - roadPoint.Position).normalized;                   
        //}
    }  
    public void DeletePoint(RoadNode roadPoint)
    {
        if(roadPoint == rootNode && roadPoint.links.Count > 0)
            rootNode = roadPoint.links[0].GetSecond(roadPoint);
        roadPoint.SwitchConnections(roadPoint.links.Count > 0 ? roadPoint.links[0].GetSecond(roadPoint) : null);
    }
    public void AddPoint()
    {
        rootNode.VisitFirstLeaf(node => DuplicatePoint(node));
    }
    public void CreateIntersection(RoadNode node)
    {
        (RoadConnection connectionA, RoadConnection connectionB) = node.GetRoadConnections();
        if(connectionA != null && connectionB != null)
        {           
            var handleA = connectionA.GetLocalHandle(node);
            var handleB = connectionB.GetLocalHandle(node);
            RoadNode roadNodeA = new RoadNode(node.Position + handleA.normalized);
            RoadNode roadNodeB = new RoadNode(node.Position + handleB.normalized);
            var connectionNodeA = connectionA.GetSecond(node);
            var connectionNodeB = connectionB.GetSecond(node);

            if(node == rootNode)
                rootNode = roadNodeA;

            connectionNodeA.RemoveConnectionsWithNode(node);
            connectionNodeA.ConnectRoad(roadNodeA,connectionA.GetLocalHandle(connectionNodeA),handleA);
            connectionNodeB.RemoveConnectionsWithNode(node);
            connectionNodeB.ConnectRoad(roadNodeB,connectionB.GetLocalHandle(connectionNodeB),handleB);
            RoadNode roadNode = new RoadNode((roadNodeA.Position + roadNodeB.Position) / 2f + Vector3.Cross(Vector3.up,roadNodeA.Position - roadNodeB.Position).normalized);

            roadNodeA.Connect(new IntersectionConnection(roadNodeA,roadNodeB));
            roadNodeA.Connect(new IntersectionConnection(roadNodeA,roadNode));
            roadNodeB.Connect(new IntersectionConnection(roadNodeB,roadNode));
        }
    }    
}

