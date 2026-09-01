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
    private static bool showEditButtons = false;
    private static RoadPoint selectedPoint;
    private RoadElement road;


    private void OnEnable()
    {
        road = (RoadElement)target;
        OnTargetChanged?.Invoke(road);
        EditorWindow.windowFocusChanged += FocusChanged;

        RoadPointTools.OnChangePosition += ChangePosition;
        RoadPointTools.OnClickDeleteButton += DeletePoint;
        RoadPointTools.OnClickDuplicateButton += DuplicatePoint;
    }
    private void OnDisable()
    {
        EditorWindow.windowFocusChanged -= FocusChanged;

        RoadPointTools.OnChangePosition -= ChangePosition;
        RoadPointTools.OnClickDeleteButton -= DeletePoint;
        RoadPointTools.OnClickDuplicateButton -= DuplicatePoint;
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
        EditorOptions();
        EditorGUILayout.BeginHorizontal();
        
        if (CustomEditorUtility.SingleLineButton("One-way",CustomEditorIcons.UpArrow))
            RecordAction(road,"One-way",road.ChangeToOneWayRoad);
        
        if (CustomEditorUtility.SingleLineButton("Change Direction",CustomEditorIcons.ChangeDirection))
            RecordAction(road,"Change Direction",road.ChangeDirection);
 
        if (CustomEditorUtility.SingleLineButton("Project to Surface",CustomEditorIcons.ProjectToSurface))
            RecordAction(road,"Project to Surface",road.ProjectToSurface);
        

        EditorGUILayout.EndHorizontal();
        DrawPropertiesExcluding(serializedObject);

        if (CustomEditorUtility.SingleLineButton("Add Point",CustomEditorIcons.Add))
            RecordAction(road,"Add road point",road.AddPoint);

        serializedObject.ApplyModifiedProperties();
    }
   
    private void EditorOptions()
    {
        EditorGUILayout.BeginHorizontal();
        GUIStyle toggleButton = new GUIStyle(GUI.skin.button);

        showEditButtons = Toggle(toggleButton,"Show Edit Buttons",showEditButtons);
        showHandle = Toggle(toggleButton,"Show Position Handle",showHandle);

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();
    }

    private bool Toggle(GUIStyle toggleButton,string text,bool currentValue)
    {
        bool newValue = GUILayout.Toggle(currentValue,text, toggleButton);
        if(newValue != currentValue)
        {
            currentValue = newValue;
            SceneView.RepaintAll();
        }    
        return currentValue;
    }







    private void Events()
    {
        Event e = Event.current;
        if (e.type == EventType.ExecuteCommand)
        {
            switch(e.commandName)
            {
                case "Cut":
                case "SoftDelete" : 
                    DeletePoint();
                    e.Use();
                    break;
                case "Paste":
                case "Duplicate" :
                    DuplicatePoint();
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

            ShowEditButtons(i,middle);
            
            PrintArrows(road,a,nextNodeDirection,previousNodePosition - a,startOffset,startMedianOffset);
            previousNodePosition = a;
        }

        Vector3 last = road.transform.TransformPoint(road.Points[road.Points.Count -1].Position);
        Vector3 offset = Vector3.Cross(Vector3.up,last - previousNodePosition).normalized;
        PrintArrows(road,last,-(previousNodePosition - last),previousNodePosition - last,offset,offset * road.HalfMedianStripWidth);
    }
    private void ShowEditButtons(int currentIndexPoint,Vector3 middlePoint)
    {
        if(showEditButtons)
        {         
            Handles.BeginGUI(); 
            Vector2 guiPosition = HandleUtility.WorldToGUIPoint(middlePoint);
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            
            if (GUI.Button(new Rect(guiPosition.x - 12, guiPosition.y - 12, 24, 24),"+",style))
            {
                Vector3 localPoint = road.transform.InverseTransformPoint(middlePoint);
                RecordAction(road,"Add road point",() => {road.Points.Insert(currentIndexPoint + 1, new RoadPoint(localPoint));});
            }
            Handles.EndGUI();
        }
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
                    ChangePosition(road.transform.InverseTransformPoint(newWorldPoint));
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


    private void ChangePosition(RoadPoint roadPoint,Vector3 newPosition)
    {
        RecordAction(road,"Move road point",() =>
        { 
            if(roadPoint != null)
                roadPoint.Position = newPosition;
        });
        if(selectedPoint == roadPoint)
            OnSelectionChanged?.Invoke(selectedPoint);   

    }
    private void ChangePosition(Vector3 newPosition)
    {
        ChangePosition(selectedPoint,newPosition);
    }
    private void DeletePoint(RoadPoint roadPoint)
    {
        if(selectedPoint == roadPoint)
            Deselect();
        RecordAction(road,"Delete Point",() => road.DeletePoint(roadPoint));
    }  
    private void DeletePoint()
    {     
        DeletePoint(selectedPoint);
    } 
    private void DuplicatePoint(RoadPoint roadPoint)
    {
        RecordAction(road,"Duplicate Point",() => road.DuplicatePoint(roadPoint));
    }
    private void DuplicatePoint()
    {
        DuplicatePoint(selectedPoint);
    }
    private void RecordAction(RoadElement roadElement,string actionName, Action action)
    {
        Undo.RecordObject(roadElement, actionName);
        action();
        EditorUtility.SetDirty(roadElement);
    }
}