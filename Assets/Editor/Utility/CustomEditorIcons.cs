using UnityEditor;
using UnityEngine;

public static class CustomEditorIcons
{
    public static readonly Texture Add = LoadUnityIcon("CreateAddNew");
    public static readonly Texture UnityLogo = LoadUnityIcon("d_UnityLogo");
    public static readonly Texture UpArrow = LoadUnityIcon("UpArrow");
    public static readonly Texture ChangeDirection = LoadUnityIcon("RotateTool On");
    public static readonly Texture ProjectToSurface = LoadUnityIcon("Download-Available");
    public static readonly Texture Delete = LoadUnityIcon("TreeEditor.Trash");
    public static readonly Texture Duplicate = LoadUnityIcon("TreeEditor.Duplicate");
    public static readonly Texture Connection = LoadUnityIcon("d_EditCollider");
    public static readonly Texture Disconnection = LoadUnityIcon("d_UnityEditor.Graphs.AnimatorControllerTool");
    
    private static Texture LoadCustomIcon(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Textures/ToolIcons/{name}.png" );
    }
    private static Texture LoadUnityIcon(string name)
    {
        return EditorGUIUtility.IconContent(name).image;
    }
}

