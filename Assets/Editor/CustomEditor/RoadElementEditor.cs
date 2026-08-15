using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[CustomEditor(typeof(RoadElement))]
public class RoadElementEditor : Editor
{
    private bool showHandle = true;
    private void OnSceneGUI()
    {
        Tools.hidden = !showHandle;

        RoadElement road = (RoadElement)target;
        EditorGUI.BeginChangeCheck();
        PrintPointsHandles(road);
        PrintLines(road);
    }

    private void PrintLines(RoadElement road)
    {
        if(road.Points.Count == 0) return;

        Handles.color = Color.cyan;
        Vector3? nextStartOffset = null;
        Vector3 startNode = road.transform.TransformPoint(road.Points[0]);
        Vector3 previousNodePosition;

        if(road.Points.Count > 1) 
            previousNodePosition = startNode -(road.transform.TransformPoint(road.Points[1]) - startNode);
        else
            previousNodePosition = startNode + Vector3.right;

        for (int i = 0; i < road.Points.Count - 1; i++)
        {
            Vector3 a = road.transform.TransformPoint(road.Points[i]);
            Vector3 b = road.transform.TransformPoint(road.Points[i + 1]);
          
            Vector3 middle = (a + b) * 0.5f;
            float size = HandleUtility.GetHandleSize(middle) * 0.1f;   
            Vector3 nextNodeDirection = b - a;

            Vector3 offset1 = Vector3.Cross(Vector3.up, nextNodeDirection).normalized;
            Vector3 startOffset = nextStartOffset.HasValue ? nextStartOffset.Value : offset1;
            Vector3 endOffset = Vector3.zero;

            if(i < road.Points.Count - 2)
            {
                Vector3 c = road.transform.TransformPoint(road.Points[i + 2]);  
                Vector3 offset2 = Vector3.Cross(Vector3.up, c - b).normalized;
                if(LineIntersectionXZ(a + startOffset,b + offset1,b + offset2,c + offset2,out var news))
                {
                    news.y = b.y;
                    endOffset = news - b; 
                    nextStartOffset = endOffset;
                }
            }
            else
                endOffset = offset1;


            startOffset = startOffset.normalized;
            endOffset = endOffset.normalized;

            Vector3 startMedianOffset = startOffset * road.HalfMedianStripWidth;
            Vector3 endMedianOffset = endOffset * road.HalfMedianStripWidth;

            Handles.color = Color.red;
            Handles.DrawLine(a + startMedianOffset,b + endMedianOffset);
            Handles.DrawLine(a - startMedianOffset,b - endMedianOffset);

            Handles.color = Color.white;
            Handles.DrawLine(a + startMedianOffset + road.ForwardRoadwayWidth * startOffset,b + endMedianOffset  + road.ForwardRoadwayWidth * endOffset);
            Handles.DrawLine(a - startMedianOffset - road.BackwardRoadwayWidth * startOffset,b - endMedianOffset  - road.BackwardRoadwayWidth * endOffset);


            Handles.BeginGUI();
            Vector2 guiPosition = HandleUtility.WorldToGUIPoint(middle);
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            
            if (GUI.Button(new Rect(guiPosition.x - 12, guiPosition.y - 12, 24, 24),"+",style))
            {
                Undo.RecordObject(road,"Add Road Point");
                Vector3 localPoint = road.transform.InverseTransformPoint(middle);
                road.Points.Insert( i + 1, localPoint);
                EditorUtility.SetDirty(road);
                GUI.changed = true;
            }
            Handles.EndGUI();

            
            PrintArrows(road,a,nextNodeDirection,previousNodePosition - a,startOffset,startMedianOffset);
            previousNodePosition = a;
        }

        Vector3 last = road.transform.TransformPoint(road.Points[road.Points.Count -1]);
        Vector3 offset = Vector3.Cross(Vector3.up,last - previousNodePosition).normalized;
        PrintArrows(road,last,-(previousNodePosition - last),previousNodePosition - last,offset,offset * road.HalfMedianStripWidth);
    }
    private void PrintArrows(RoadElement road,Vector3 nodePosition,Vector3 nextNodeDir,Vector3 previousNodeDir,Vector3 offsetDirection,Vector3 medianOffset)
    {
        float size = HandleUtility.GetHandleSize(nodePosition) * 0.4f;
        Handles.color = Color.magenta;
        for(int i = 0; i < road.ForwardLaneCount; i++)
            Handles.ArrowHandleCap(0,nodePosition + medianOffset + offsetDirection * (i + 0.5f) * road.LaneWidth , Quaternion.LookRotation(nextNodeDir),size, EventType.Repaint); 
        
        Handles.color = Color.cyan;
        for(int i = 0; i < road.BackwardLaneCount; i++)
            Handles.ArrowHandleCap(0,nodePosition - medianOffset - offsetDirection * (i + 0.5f) * road.LaneWidth , Quaternion.LookRotation(previousNodeDir),size, EventType.Repaint);       
    }
    private void PrintPointsHandles(RoadElement road)
    {
        for (int i = 0; i < road.Points.Count; i++)
        {
            Vector3 worldPoint = road.transform.TransformPoint(road.Points[i]);
            EditorGUI.BeginChangeCheck();
            Vector3 newWorldPoint = Handles.PositionHandle(worldPoint,Quaternion.identity); 
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(road, "Move Road Point");
                road.Points[i] = road.transform.InverseTransformPoint(newWorldPoint);
                EditorUtility.SetDirty(road);
            }
            Handles.Label(worldPoint,$"Point {i}");
        }
    }
    public override void OnInspectorGUI()
    {
        showHandle = EditorGUILayout.Toggle("Show Position Handle", showHandle);
        DrawDefaultInspector();
        RoadElement road = (RoadElement)target;

        if (GUILayout.Button("Add Point"))
        {
            Undo.RecordObject(road, "Add Road Point");
            road.Points.Add(road.transform.InverseTransformPoint(road.transform.position + new Vector3(0,0,0.5f)));
            EditorUtility.SetDirty(road);
        }
    }
    private bool LineIntersectionXZ(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4,out Vector3 intersection)
    {
        intersection = Vector3.zero;
        Vector2 a = new Vector2(p1.x, p1.z);
        Vector2 b = new Vector2(p2.x, p2.z);
        Vector2 c = new Vector2(p3.x, p3.z);
        Vector2 d = new Vector2(p4.x, p4.z);
        Vector2 r = b - a;
        Vector2 s = d - c;

        float cross = r.x * s.y - r.y * s.x;

        if (Mathf.Abs(cross) < 0.0001f)
            return false;

        Vector2 ca = c - a;
        float t = (ca.x * s.y - ca.y * s.x) / cross;
        Vector2 result = a + r * t;

        intersection = new Vector3(
            result.x,
            p1.y,
            result.y
        );
        return true;
    }
}