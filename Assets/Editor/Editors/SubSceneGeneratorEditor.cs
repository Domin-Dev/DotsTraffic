using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SubSceneGenerator))]
public class WeatherAuthoringEditor : Editor
{
    private const string createRoadIcon = "Assets/Textures/ToolIcons/RoadPoint.png";
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SubSceneGenerator generator =
            (SubSceneGenerator)target;

        GUILayout.Space(40); 


        if (CustomEditorUtility.SingleLineButton("Generate Sub Scene",CustomEditorIcons.UnityLogo))
            generator.GenerateSubScene();
        
        if (CustomEditorUtility.SingleLineButton("Create Road Element",CustomEditorIcons.Add))
            generator.CreateRoadElement(); 
    }
} 