using UnityEngine;

[System.Serializable]
public abstract class ConnectionBase
{
    public Vector3 LocalHandleA;
    public Vector3 LocalHandleB;
    [SerializeReference] public RoadNode nodeA;
    [SerializeReference] public RoadNode nodeB;
    public Vector3 WorldHandleA => LocalHandleA + nodeA.Position;
    public Vector3 WorldHandleB => LocalHandleB + nodeB.Position;

    public ConnectionBase(RoadNode nodeA,RoadNode nodeB)
    {
        LocalHandleA = (nodeB.Position - nodeA.Position).normalized;
        LocalHandleB = (nodeA.Position - nodeB.Position).normalized;
        this.nodeA = nodeA;
        this.nodeB = nodeB;
    }
    public ConnectionBase(RoadNode nodeA, RoadNode nodeB, Vector3 handleA, Vector3 handleB)
    {
        this.nodeA = nodeA;
        this.nodeB = nodeB;
        this.LocalHandleA = handleA;
        this.LocalHandleB = handleB;
    }

       
    public Vector3 GetLocalHandle(RoadNode node)
    {
        if(node == nodeA)
            return LocalHandleA;
        else if(node == nodeB)
            return LocalHandleB;
        return Vector3.zero;
    }
    public void SetLocalHandle(RoadNode node,Vector3 handle)
    {
        if(node == nodeA)
            LocalHandleA = handle;
        else if(node == nodeB)
            LocalHandleB = handle;    
    }
    public bool Contains(RoadNode node)
    {
        return node == nodeA || node == nodeB;
    }
    public RoadNode GetSecond(RoadNode node)
    {
        if(nodeA == node)
            return nodeB;
        if(nodeB == node)
            return nodeA;
        return null;
    }
    public Vector3 GetSecondLocalHandle(RoadNode node)
    {
        return GetLocalHandle(GetSecond(node));
    }
    public void ReplaceNode(RoadNode node,RoadNode newNode)
    {
        if(nodeA == node)
            nodeA = newNode;
        else if(nodeB == node)
            nodeB = newNode;
    }
}