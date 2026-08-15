using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SubSceneGenerator))]
public class WeatherAuthoringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SubSceneGenerator generator =
            (SubSceneGenerator)target;

        GUILayout.Space(40); 

        if (GUILayout.Button("Generate Sub Scene"))
            generator.GenerateSubScene();
        
        if (GUILayout.Button("Create Road Element"))
            generator.CreateRoadElement();
    }
}