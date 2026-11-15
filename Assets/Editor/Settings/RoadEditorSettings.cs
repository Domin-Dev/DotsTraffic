using UnityEngine;


[CreateAssetMenu(fileName = "RoadEditorSettings", menuName = "Traffic/Road Editor Settings")]
public class RoadEditorSettings : ScriptableObject
{
    [Header("Road")]
    public Color MedianStripColor = Color.red;
    public Color LaneColor = Color.white;
    public Color IntersectionConnectionColor = Color.green;
    public Color ForwardLaneColor =  Color.magenta;
    public Color BackwardLaneColor =  Color.cyan;

    [Header("Road Point")]
    public Color RoadPointColor = Color.blue;
    public Color SelectedRoadPointColor = Color.cyan;
    [Range(0.15f,2f)] public float RoadPointSize = 0.4f;
    [Header("Connection Point")]
    public Color ConnectionPointColor = Color.red;
    public Color SelectedConnectionPointColor = Color.magenta;
    [Range(0.1f,2f)] public float ConnectionPointSize = 0.2f;
    [Header("Handles")]
    public Color[] handleColors = GenerateColors(10,1f,0.8f);
    public Color HandleLineColor = Color.white;
    [Range(0.15f,2f)] public float handleSizeRelativeToRoadPoint = 0.75f;


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

    public static Color[] GenerateColors(int count,float saturation,float brightness)
    {
        Color[] colors = new Color[count];
        for (int i = 0; i < count; i++)
            colors[i] = Color.HSVToRGB((float)i/count,saturation,brightness);
        return colors;
    }
    public Color NextHandleColor(int index) => handleColors[index % handleColors.Length];
}