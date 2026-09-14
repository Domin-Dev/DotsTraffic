using UnityEngine;

[System.Serializable]
public class IntersectionConnection : ConnectionBase
{   
    public IntersectionConnection(RoadNode nodeA,RoadNode nodeB) : base(nodeA,nodeB){}
    public IntersectionConnection(RoadNode nodeA, RoadNode nodeB, Vector3 handleA, Vector3 handleB) : base(nodeA,nodeB,handleA,handleB){}
}