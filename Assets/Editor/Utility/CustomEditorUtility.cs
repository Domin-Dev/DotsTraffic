using System;
using System.Linq;
using Unity.Mathematics;
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
    public static void IntSlider<T>(T[] values,string fieldName,Action<T,int> setValue,Func<T,int> getValue,int min,int max)
    {
        if(TryGetValue(values,getValue,out var oldValue))
            SetValue(values,setValue,oldValue,EditorGUILayout.IntSlider(fieldName,oldValue,min,max));
    }
    public static void FloatField<T>(T[] values,string fieldName,Action<T,float> setValue,Func<T,float> getValue,float min = float.MinValue,float max = float.MaxValue)
    {
        if(TryGetValue(values,getValue,out var oldValue))
            SetValue(values,setValue,oldValue,math.clamp(EditorGUILayout.FloatField(fieldName,oldValue),min,max));
    }
    private static bool TryGetValue<T,K>(T[] values,Func<T,K> getValue, out K value)
    {
        value = default;
        if(values == null || values.Length == 0) return false;
        K newValue = getValue(values[0]);
        EditorGUI.showMixedValue = !values.All(x => getValue(x).Equals(newValue)); 
        value = newValue;
        return true;
    }
    private static void SetValue<T,K>(T[] values,Action<T,K> setValue,K oldValue,K newValue)
    {
        if(!newValue.Equals(oldValue))
        {
            foreach(var i in values)
                setValue(i,newValue); 
        }
    }
}

