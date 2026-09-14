using UnityEngine;

[System.Serializable]
public class RoadConnection : ConnectionBase
{   
    public RoadConnection(RoadNode nodeA,RoadNode nodeB) : base(nodeA,nodeB){}
    public RoadConnection(RoadNode nodeA, RoadNode nodeB, Vector3 handleA, Vector3 handleB) : base(nodeA,nodeB,handleA,handleB){}
}