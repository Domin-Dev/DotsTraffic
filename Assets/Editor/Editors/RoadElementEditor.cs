using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.PackageManager;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
[CustomEditor(typeof(RoadElement))]
public class RoadElementEditor : Editor
{   
    public static event Action<RoadPoint> OnSelectionChanged;
    public static event Action<RoadElement> OnTargetChanged;
    private static bool showHandle = true;
    private static RoadPoint selectedPoint;
    private RoadElement road;


    private void OnEnable()
    {
        road = (RoadElement)target;
        OnTargetChanged?.Invoke(road);
        EditorWindow.windowFocusChanged += FocusChanged;
    }
    private void OnDisable()
    {
        EditorWindow.windowFocusChanged -= FocusChanged;
    }
    private void FocusChanged()
    {
        if(!(EditorWindow.focusedWindow is SceneView))
            Deselect();       
    }
    public void OnSceneGUI()
    { 
        Events();
        Tools.hidden = !showHandle; 
        PrintPointsHandles(road);
        PrintLines(road);
    }
    public override void OnInspectorGUI()
    {        
        RoadElement road = (RoadElement)target;
        bool newValue = EditorGUILayout.Toggle("Show Position Handle", showHandle);
        if(newValue != showHandle)
        {
            showHandle = newValue;
            SceneView.RepaintAll();
        }

        EditorGUILayout.BeginHorizontal();
        
        if (CustomEditorUtility.SingleLineButton("One-way",CustomEditorIcons.UpArrow))
        {
            Undo.RecordObject(road, "One-way");
            road.ChangeToOneWayRoad(); 
            EditorUtility.SetDirty(road); 
        }

        if (CustomEditorUtility.SingleLineButton("Change Direction",CustomEditorIcons.ChangeDirection))
        {
            Undo.RecordObject(road, "Change Direction");
            road.ChangeDirection(); 
            EditorUtility.SetDirty(road); 
        }      
        
        if (CustomEditorUtility.SingleLineButton("Project to Surface",CustomEditorIcons.ProjectToSurface))
        {
            Undo.RecordObject(road, "Project to Surface");
            road.ProjectToSurface();
            EditorUtility.SetDirty(road);
        }

        EditorGUILayout.EndHorizontal();
        DrawPropertiesExcluding(serializedObject);

        if (CustomEditorUtility.SingleLineButton("Add Point",CustomEditorIcons.Add))
        {
            Undo.RecordObject(road, "Add Road Point");
            road.Points.Add(new RoadPoint(road.transform.InverseTransformPoint(road.transform.position + new Vector3(0,0,0.5f))));
            EditorUtility.SetDirty(road);
        }

        serializedObject.ApplyModifiedProperties();
    }
   
    private void Events()
    {
        Event e = Event.current;
        if (e.type == EventType.ExecuteCommand)
        {
            switch(e.commandName)
            {
                case "SoftDelete" : 
                    DeletePoint(selectedPoint);
                    e.Use();
                    break;
                case "Duplicate" :
                    DuplicatePoint(selectedPoint);
                    e.Use();
                    break;
            }
            Debug.Log($"Validate: {e.commandName}");
        }
    }
    private void SelectPoint(RoadPoint roadPoint)
    {
        selectedPoint = roadPoint;
        OnSelectionChanged?.Invoke(selectedPoint);
    }
    private void Deselect()
    {
        selectedPoint = null;
        OnSelectionChanged?.Invoke(selectedPoint);
    }
    private void PrintLines(RoadElement road)
    {
        if(road.Points.Count == 0) return;

        Handles.color = Color.cyan;
        Vector3? nextStartOffset = null;
        Vector3 startNode = road.transform.TransformPoint(road.Points[0].Position);
        Vector3 previousNodePosition;

        if(road.Points.Count > 1) 
            previousNodePosition = startNode -(road.transform.TransformPoint(road.Points[1].Position) - startNode);
        else
            previousNodePosition = startNode + Vector3.right;

        for (int i = 0; i < road.Points.Count - 1; i++)
        {
            Vector3 a = road.transform.TransformPoint(road.Points[i].Position);
            Vector3 b = road.transform.TransformPoint(road.Points[i + 1].Position);
          
            Vector3 middle = (a + b) * 0.5f;
            float size = HandleUtility.GetHandleSize(middle) * 0.1f;   
            Vector3 nextNodeDirection = b - a;

            Vector3 offset1 = Vector3.Cross(Vector3.up, nextNodeDirection).normalized;
            Vector3 startOffset = nextStartOffset.HasValue ? nextStartOffset.Value : offset1;
            Vector3 endOffset = Vector3.zero;

            if(i < road.Points.Count - 2)
            {
                Vector3 c = road.transform.TransformPoint(road.Points[i + 2].Position);  
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
                road.Points.Insert( i + 1, new RoadPoint(localPoint));
                EditorUtility.SetDirty(road);
                GUI.changed = true;
            }
            Handles.EndGUI();

            
            PrintArrows(road,a,nextNodeDirection,previousNodePosition - a,startOffset,startMedianOffset);
            previousNodePosition = a;
        }

        Vector3 last = road.transform.TransformPoint(road.Points[road.Points.Count -1].Position);
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
            var roadPoint = road.Points[i];
            Vector3 worldPoint = road.transform.TransformPoint(roadPoint.Position);
            float size = HandleUtility.GetHandleSize(worldPoint) * 0.4f;

            if(roadPoint == selectedPoint)
            {
                Handles.color = Color.cyan;
                Handles.SphereHandleCap(0,worldPoint,Quaternion.identity,size,EventType.Repaint);

                EditorGUI.BeginChangeCheck();
                Vector3 newWorldPoint = Handles.PositionHandle(worldPoint,roadPoint.Rotation); 
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(road, "Move Road Point");
                    roadPoint.Position = road.transform.InverseTransformPoint(newWorldPoint);
                    OnSelectionChanged?.Invoke(roadPoint);
                    EditorUtility.SetDirty(road);
                }
            }
            else
            {
                Handles.color = Color.blue;
                if (Handles.Button(worldPoint, Quaternion.identity, size,size, Handles.SphereHandleCap))
                    SelectPoint(roadPoint);
            }
            Handles.Label(worldPoint + new Vector3(0,0,1) * size,$"Point {i}");
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

    private void DeletePoint(RoadPoint roadPoint)
    {
        if(road.Points.Contains(roadPoint))
        {
            Undo.RecordObject(road, "Delete Point");
            road.Points.Remove(roadPoint);
            EditorUtility.SetDirty(road);
        }
    }   
    private void DuplicatePoint(RoadPoint roadPoint)
    {
        int index = road.Points.IndexOf(roadPoint);
        if(index >= 0)
        {   
            Undo.RecordObject(road, "Duplicate Point");
            var newPoint = new RoadPoint(roadPoint);
            if(road.Points.Count == index + 1)
            {
                if(road.Points.Count == 1)
                    newPoint.Position += new Vector3(0,0,1);
                else
                    newPoint.Position += (roadPoint.Position - road.Points[index - 1].Position).normalized;
            }
            else
                newPoint.Position += (road.Points[index + 1].Position - roadPoint.Position).normalized;
            road.Points.Insert(index + 1,newPoint);
            EditorUtility.SetDirty(road);
        }
    }
}