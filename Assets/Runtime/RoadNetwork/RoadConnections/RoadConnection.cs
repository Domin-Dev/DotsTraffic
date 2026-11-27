using UnityEngine;


public class RoadConnection : ConnectionBase
{   
    [SerializeReference] public RoadConnectionSettings settings;
    public RoadConnection(RoadNode nodeA,RoadNode nodeB) : base(nodeA,nodeB){}
    public RoadConnection(RoadNode nodeA, RoadNode nodeB, Vector3 handleA, Vector3 handleB) : base(nodeA,nodeB,handleA,handleB){}
} 