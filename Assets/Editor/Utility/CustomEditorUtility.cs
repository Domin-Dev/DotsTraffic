using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
public static class CustomEditorUtility
{
    public static bool SingleLineButton(string text,Texture icon)
    {
        return GUILayout.Button(new GUIContent(text,icon), GUILayout.Height(EditorGUIUtility.singleLineHeight));
    }
    public static Button GetSingleLineButton(string text, Texture icon)
    {
        var button  = new Button() { text = text,iconImage = Background.FromTexture2D((Texture2D)icon)};
        button.style.maxHeight = EditorGUIUtility.singleLineHeight;
        button.style.height = EditorGUIUtility.singleLineHeight;
        return button;
    }
}

