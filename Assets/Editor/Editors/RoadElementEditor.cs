using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

[CustomEditor(typeof(RoadElement))]
public class RoadElementEditor : Editor
{   
    public static event Action<RoadPoint> OnSelectionChanged;
    public static event Action<RoadElement> OnTargetChanged;
    public static HandleMode selectedHandleMode { private set; get; } = HandleMode.Free;
    
    private static bool showHandle = true;
    private static bool showEditButtons = false;
    private static bool showRoadEditorSettings = false;

    private static RoadPoint selectedPoint;
    private static bool PointIsSelected => selectedPoint != null;
    private RoadElement road;
    private static Editor roadEditorSettingsEditor;
    private RoadEditorSettings roadEditorSettings;

    private static bool addNewPoint = false;

    private void OnEnable()
    {
        road = (RoadElement)target;
        OnTargetChanged?.Invoke(road);
        EditorWindow.windowFocusChanged += FocusChanged;
        roadEditorSettings = RoadEditorSettings.Load();

        RoadPointTools.OnChangePosition += ChangePosition;
        RoadPointTools.OnChangeHandleAPosition += ChangeHandleA;
        RoadPointTools.OnChangeHandleBPosition += ChangeHandleB;
        RoadPointTools.OnChangeHandleMode += ChangeHandleMode;
        
        RoadPointTools.OnClickDeleteButton += DeletePoint;
        RoadPointTools.OnClickDuplicateButton += DuplicatePoint;
    }
    private void OnDisable()
    {
        EditorWindow.windowFocusChanged -= FocusChanged;

        RoadPointTools.OnChangePosition -= ChangePosition;
        RoadPointTools.OnChangeHandleAPosition -= ChangeHandleA;
        RoadPointTools.OnChangeHandleBPosition -= ChangeHandleB;
        RoadPointTools.OnChangeHandleMode -= ChangeHandleMode;

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
        PrintLinesBezier(road);
    }
    public override void OnInspectorGUI()
    {        
        RoadElement road = (RoadElement)target;
        DrawEditorOptions();
        EditorGUILayout.BeginHorizontal();
        
        if (CustomEditorUtility.SingleLineButton("One-way",CustomEditorIcons.UpArrow))
            RecordAction(road,"One-way",road.ChangeToOneWayRoad);
        
        if (CustomEditorUtility.SingleLineButton("Change Direction",CustomEditorIcons.ChangeDirection))
            RecordAction(road,"Change Direction",road.ChangeDirection);
 
        if (CustomEditorUtility.SingleLineButton("Project to Surface",CustomEditorIcons.ProjectToSurface))
            RecordAction(road,"Project to Surface",road.ProjectToSurface);
        
        EditorGUILayout.EndHorizontal();
        DrawDefaultInspector();

        if (CustomEditorUtility.SingleLineButton("Add Point",CustomEditorIcons.Add))
            RecordAction(road,"Add road point",road.AddPoint);

        DrawRoadSettings();
    }
    private void DrawRoadSettings()
    {
        EditorGUILayout.Separator();
        using (var check = new EditorGUI.ChangeCheckScope())
        {
            showRoadEditorSettings = EditorGUILayout.InspectorTitlebar(showRoadEditorSettings, roadEditorSettings);
            if (showRoadEditorSettings)
            {
                CreateCachedEditor(roadEditorSettings, null, ref roadEditorSettingsEditor);
                roadEditorSettingsEditor.OnInspectorGUI();
            }
            if (check.changed)
            {
                SceneView.RepaintAll();
            }
        }
    }      
    private void DrawEditorOptions()
    {
        EditorGUILayout.BeginHorizontal();
        GUIStyle toggleButton = new GUIStyle(GUI.skin.button);

        showEditButtons = DrawToggle(toggleButton,"Show Edit Buttons",showEditButtons);
        showHandle = DrawToggle(toggleButton,"Show Position Handle",showHandle);

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();
    }
    private bool DrawToggle(GUIStyle toggleButton,string text,bool currentValue)
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

        if(addNewPoint)
        {
            Debug.Log(e.mousePosition);
            addNewPoint = false;
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
    private void PrintLinesBezier(RoadElement roadElement)
    {
        for (int i = 0; i < road.Points.Count - 1; i++)
        {
            RoadPoint currentPoint = road.Points[i];
            RoadPoint nextPoint = road.Points[i+1];
            DrawLine(currentPoint,nextPoint);
        }
        if(roadElement.Loop && road.Points.Count > 1)
        
            DrawLine(road.Points[road.Points.Count-1],road.Points[0]);
    }




private void DrawLine(RoadPoint currentPoint, RoadPoint nextPoint, float offset = 2.0f)
{
    Vector3 startPoint = road.transform.TransformPoint(currentPoint.Position);
    Vector3 endPoint = road.transform.TransformPoint(nextPoint.Position);
    Vector3 startHandler = road.transform.TransformPoint(currentPoint.HandleA);
    Vector3 endHandler = road.transform.TransformPoint(nextPoint.HandleB);

    Vector3? prevStartHandler = road.transform.TransformPoint(currentPoint.HandleB);
    Vector3? nextEndHandler = road.transform.TransformPoint(nextPoint.HandleA);

    Handles.color = roadEditorSettings.HandleLineColor;
    Handles.DrawBezier(startPoint, endPoint, startHandler, endHandler, Color.white, null, 5f);   

    Handles.DrawAAPolyLine(BezierUtility.GetOffsetBezier(startPoint, startHandler, endHandler, endPoint,prevStartHandler,nextEndHandler, -offset, 30).ToArray());
    Handles.DrawAAPolyLine(BezierUtility.GetOffsetBezier(startPoint, startHandler, endHandler, endPoint,prevStartHandler,nextEndHandler, offset, 30).ToArray());
}





    private void PrintLines(RoadElement road)
    {
        if(road.Points.Count == 0) return;
        
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
                // if(LineIntersectionXZ(a + startOffset,b + offset1,b + offset2,c + offset2,out var news))
                // {
                //     news.y = b.y;
                //     endOffset = news - b; 
                //     nextStartOffset = endOffset;
                // }
            }
            else
                endOffset = offset1;


            startOffset = startOffset.normalized;
            endOffset = endOffset.normalized;

            Vector3 startMedianOffset = startOffset * road.HalfMedianStripWidth;
            Vector3 endMedianOffset = endOffset * road.HalfMedianStripWidth;

            Handles.color = roadEditorSettings.MedianStripColor;
            Handles.DrawLine(a + startMedianOffset,b + endMedianOffset);
            Handles.DrawLine(a - startMedianOffset,b - endMedianOffset);

            Handles.color = roadEditorSettings.LaneColor;
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
        Handles.color = roadEditorSettings.ForwardLaneColor;
        for(int i = 0; i < road.ForwardLaneCount; i++)
            Handles.ArrowHandleCap(0,nodePosition + medianOffset + offsetDirection * (i + 0.5f) * road.LaneWidth , Quaternion.LookRotation(nextNodeDir),size, EventType.Repaint); 
        
        Handles.color = roadEditorSettings.BackwardLaneColor;
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
                Vector3 handleA = road.transform.TransformPoint(roadPoint.HandleA);
                Vector3 handleB = road.transform.TransformPoint(roadPoint.HandleB);

                Handles.color = roadEditorSettings.SelectedRoadPointColor;
                Handles.SphereHandleCap(0,worldPoint,Quaternion.identity,size,EventType.Repaint);
                
                Handles.color = roadEditorSettings.HandleAColor;
                Handles.SphereHandleCap(0,handleA,Quaternion.identity,size,EventType.Repaint);
                
                Handles.color = roadEditorSettings.HandleBColor;
                Handles.SphereHandleCap(0,handleB,Quaternion.identity,size,EventType.Repaint);

                Handles.color = roadEditorSettings.HandleLineColor;
                Handles.DrawLine(worldPoint,handleA);
                Handles.DrawLine(worldPoint,handleB);

                EditorGUI.BeginChangeCheck();
                Vector3 newWorldPoint = road.transform.InverseTransformPoint(Handles.PositionHandle(worldPoint,Quaternion.identity)); 
                if (EditorGUI.EndChangeCheck()) ChangePosition(road.transform.InverseTransformPoint(newWorldPoint));
                
                EditorGUI.BeginChangeCheck();
                Vector3 newHandleA = road.transform.InverseTransformPoint(Handles.PositionHandle(handleA,Quaternion.identity)); 
                if(EditorGUI.EndChangeCheck()) ChangeHandleA(newHandleA-newWorldPoint);
               
                EditorGUI.BeginChangeCheck();
                Vector3 newHandleB = road.transform.InverseTransformPoint(Handles.PositionHandle(handleB,Quaternion.identity)); 
                if(EditorGUI.EndChangeCheck()) ChangeHandleB(newHandleB-newWorldPoint);
            }
            else
            {
                Handles.color = roadEditorSettings.RoadPointColor;
                if (Handles.Button(worldPoint, Quaternion.identity, size,size, Handles.SphereHandleCap))
                    SelectPoint(roadPoint);
            }
            Handles.Label(worldPoint + new Vector3(0,0,1) * size,$"Point {i}");
        }
    }



    #region  Handles
    private void ChangeHandleMode(HandleMode handleMode)
    {
        selectedHandleMode = handleMode;
    }
    private void ChangeHandleA(Vector3 handleA)
    {
        RecordAction(road,"Move road point",() =>
        { 
            if(selectedPoint != null)
            {
                selectedPoint.LocalPositionHandleA = handleA;
                selectedPoint.LocalPositionHandleB = CalculateOppositeHandle(handleA, selectedPoint.LocalPositionHandleB);
            }
        });
        OnSelectionChanged?.Invoke(selectedPoint);
    }
    private void ChangeHandleB(Vector3 handleB)
    {
        RecordAction(road,"Move road point",() =>
        { 
            if(selectedPoint != null)
            {
                selectedPoint.LocalPositionHandleB = handleB;
                selectedPoint.LocalPositionHandleA = CalculateOppositeHandle(handleB, selectedPoint.LocalPositionHandleA);
            }
        });
        OnSelectionChanged?.Invoke(selectedPoint);
    }
    private Vector3 CalculateOppositeHandle(Vector3 changedHandle1,Vector3 currentHandle2)
    {
        switch(selectedHandleMode)
        {
            case HandleMode.Free:
                return currentHandle2;
            case HandleMode.Mirrored:
                return -changedHandle1;
            case HandleMode.Aligned:
                float length = currentHandle2.magnitude;
                return -math.normalizesafe(changedHandle1.normalized) * length;
        }
        return currentHandle2;
    }
    #endregion
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

    [Shortcut("TrafficTools/AddRoadPoint",typeof(SceneView), KeyCode.Mouse0, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
    public static void DoSomething()
    {
        Debug.Log(Event.current.mousePosition);
        addNewPoint =  true;
    }
}