
using Unity.Entities;
using UnityEngine;

public class RoadElementAuthoring : MonoBehaviour
{
    public class Baker : Baker<RoadElement>
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

            authoring.rootNode.VisitNodes(node =>
            {
                Entity entity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new RoadNodeComponent()
                {
                    Position = node.Position
                });
            });
        }
    }
}