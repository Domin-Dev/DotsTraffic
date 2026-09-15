
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class RoadElementBaker : Baker<RoadElement>
{
    public override void Bake(RoadElement authoring)
    {    
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);    
        AddComponent(entity,new RoadElementComponent()
        {
            BackwardLaneCount = authoring.BackwardLaneCount,
            ForwardLaneCount = authoring.ForwardLaneCount,
            MedianStripWidth = authoring.MedianStripWidth,
            LaneWidth = authoring.LaneWidth,
        });

        Dictionary<RoadNode,Entity> entities = new Dictionary<RoadNode, Entity>();
        int index = 0;
        authoring.rootNode.VisitNodes(node =>
        {
            Entity entity = CreateAdditionalEntity(TransformUsageFlags.Dynamic,false,$"{index}_RoadNode");
            AddComponent(entity, new RoadNodeComponent()
            {
                Position = node.Position,
                RoadElement = entity
            });
            AddComponent(entity,LocalTransform.FromPosition(node.Position));
            AddBuffer<RoadConnectionBuffer>(entity);
            entities.Add(node,entity);
            index++;
        });

        index = 0;
        authoring.rootNode.VisitEdges(connection =>
        {
            Entity connectionEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic,false,$"{index}_RoadConnection");
            Entity nodeA = entities[connection.nodeA];
            Entity nodeB = entities[connection.nodeB];
            AddComponent(connectionEntity, new RoadConnectionComponent()
            {
                LocalHandleA = connection.LocalHandleA,
                LocalHandleB = connection.LocalHandleB,
                nodeA = nodeA,
                nodeB = nodeB
            });
            AddBuffer<ConnectionPointsBufferA>(connectionEntity);
            AddBuffer<ConnectionPointsBufferB>(connectionEntity);
            AppendToBuffer(nodeA,new RoadConnectionBuffer() { Connection = connectionEntity });
            AppendToBuffer(nodeB,new RoadConnectionBuffer() { Connection = connectionEntity });

            Vector3 startPoint = authoring.transform.TransformPoint(connection.nodeA.Position);
            Vector3 endPoint = authoring.transform.TransformPoint(connection.nodeB.Position);
            Vector3 startHandler = authoring.transform.TransformPoint(connection.WorldHandleA);
            Vector3 endHandler = authoring.transform.TransformPoint(connection.WorldHandleB);

            var pointsA = BezierUtility.GetOffsetBezier(startPoint, startHandler, endHandler, endPoint,null,null, -authoring.Width * 0.5f, 30);
            var pointsB = BezierUtility.GetOffsetBezier(startPoint, startHandler, endHandler, endPoint,null,null, authoring.Width * 0.5f, 30);
           
            foreach(var point in pointsA)
                AppendToBuffer(connectionEntity, new ConnectionPointsBufferA() { Position = point});    
            foreach(var point in pointsB)
                AppendToBuffer(connectionEntity, new ConnectionPointsBufferB() { Position = point});       
            index++;
        });
    }
}
