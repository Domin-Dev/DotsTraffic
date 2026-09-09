using UnityEngine;


[CreateAssetMenu(fileName = "RoadEditorSettings", menuName = "Traffic/Road Editor Settings")]
public class RoadEditorSettings : ScriptableObject
{
    [Header("Road")]
    public Color MedianStripColor = Color.red;
    public Color LaneColor = Color.white;
    public Color ForwardLaneColor =  Color.magenta;
    public Color BackwardLaneColor =  Color.cyan;
    [Header("Road Point")]
    public Color RoadPointColor = Color.blue;
    public Color SelectedRoadPointColor = Color.cyan;
    [Header("Handles")]
    public Color HandleAColor = Color.red;
    public Color HandleBColor = Color.green;
    public Color HandleLineColor = Color.white;

    #if UNITY_EDITOR
        public static RoadEditorSettings Load() {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:RoadEditorSettings");
            if (guids.Length == 0)
            {
                Debug.LogWarning("Road editor settings not found - default settings will be used");
                return ScriptableObject.CreateInstance<RoadEditorSettings>();
            }
            else
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<RoadEditorSettings>(path);
            }
        }
    #endif
}