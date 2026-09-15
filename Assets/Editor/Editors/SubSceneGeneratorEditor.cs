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
        
        if(generator.editMode)
        {
            if (CustomEditorUtility.SingleLineButton("Create Road Element",CustomEditorIcons.Add))
                generator.CreateRoadElement(); 

            if (CustomEditorUtility.SingleLineButton("Bake Road Network",CustomEditorIcons.Add))
                generator.BakeRoadNetwork(); 
        }
        else
        {
            if (CustomEditorUtility.SingleLineButton("Open Edit Mode",CustomEditorIcons.Add))
                generator.OpenEditMode(); 
        }
    }
} 