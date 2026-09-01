using UnityEngine;
using UnityEditor;

public static class TrafficTools
{
    [MenuItem("TrafficTools/AddTraffic")]
    private static void AddTraffic()
    {
        GameObject generator = new GameObject("TrafficGenerator",typeof(SubSceneGenerator));
    }

}