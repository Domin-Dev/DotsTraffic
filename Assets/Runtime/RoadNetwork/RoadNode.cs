
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class RoadNode 
{
    public Vector3 Position;
    [SerializeReference] public List<ConnectionBase> links = new ();

    public RoadNode(Vector3 position)
    {
        this.Position = position;
    }   
    public RoadNode(Vector3 nodePositionA, Vector3 nodePositionB)
    {
        this.Position = nodePositionA;
        ConnectRoad(new RoadNode(nodePositionB));
    } 
    public RoadNode(RoadNode roadNode)
    {
        this.Position = roadNode.Position;
    }
    public void ConnectRoad(RoadNode roadNode)
    {
        Connect(new RoadConnection(this,roadNode));
    }
    public void ConnectRoad(RoadNode roadNodeB,Vector3 handleA, Vector3 handleB)
    {
        Connect(new RoadConnection(this,roadNodeB,handleA,handleB));
    } 
    public void Connect(ConnectionBase roadConnection)
    {
        if(!roadConnection.Contains(this)) return;
        var roadNode = roadConnection.GetSecond(this);
    
        if(!links.Any(x => x.Contains(roadNode)))
            links.Add(roadConnection);

        if(!roadNode.links.Any(x => x.Contains(this)))
            roadNode.links.Add(roadConnection);
    }
    
    public (RoadConnection A, RoadConnection B) GetRoadConnections()
    {
        RoadConnection a = null;
        RoadConnection b = null;
        foreach (var link in links)
        {
            if (!(link is RoadConnection connection))
                continue;

            if (a == null)
                a = connection;
            else
            {
                b = connection;
                break;
            }
        }
        return (a, b);
    }
    public void VisitFirstLeaf(Action<RoadNode> action)
    {
        Visit((visited,stack,node) =>
        {
            if(node.links.Count <= 1)
            {
                action.Invoke(node);
                return true;
            }
            foreach (var link in node.links)
                stack.Push(link.GetSecond(node));
            return false;
        });
    }
    public void VisitNodes(Action<RoadNode> action)
    {
        Visit((visited,stack,node) =>
        {
            action.Invoke(node);
            foreach (var link in node.links)
                stack.Push(link.GetSecond(node));
            return false;
        });
    }
    public void VisitEdges(Action<ConnectionBase> action)
    {
        Visit((visited,stack,node) =>
        {
            foreach (var link in node.links)
            {
                var newNode = link.GetSecond(node);
                if(!visited.Contains(newNode))
                {
                    action.Invoke(link);
                    stack.Push(newNode);
                }
            }
            return false;
        });
    }
    private void Visit(Func<HashSet<RoadNode>,Stack<RoadNode>,RoadNode,bool> action)
    {
        if(action == null) return;
        var visited = new HashSet<RoadNode>();
        var stack = new Stack<RoadNode>();
        stack.Push(this);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!visited.Add(node))
                continue;       
            if(action.Invoke(visited,stack,node))
                return;
        }
    }
    public void SwitchConnections(RoadNode newNode = null)
    {
        foreach(var link in links)
        {
            RoadNode node = link.GetSecond(this);
            var list = node.links;
            var connection = list.FirstOrDefault(x => x.Contains(this));
            if(connection != null)
            {
                if(newNode == null || newNode == node)
                {
                    list.Remove(connection);
                }
                else
                {
                    connection.ReplaceNode(this,newNode);
                    newNode.links.Add(connection);
                }
            }
        } 
        links.Clear();
    }
    public void RemoveConnectionsWithNode(RoadNode node)
    {
        var list = links.FindAll( x => x.Contains(node));
        foreach(var element in list)
        {
            links.Remove(element);
            node.links.Remove(element);
        }
    }
}
