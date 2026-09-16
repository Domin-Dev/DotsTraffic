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
    #region Events
    public static event Action<List<RoadNode>> OnSelectionChanged;
    public static event Action<RoadElement> OnTargetChanged;
    #endregion

    #region Shows
    private static bool showHandle = true;
   // private static bool showEditButtons = false;
    private static bool showRoadEditorSettings = false;
    #endregion

    #region Selected
    public static HandleMode selectedHandleMode { private set; get; } = HandleMode.Free; 
    private static RoadNode selectedPoint => selectedPoints[0];
    private static List<RoadNode> selectedPoints = new List<RoadNode>();
    private static bool PointIsSelected => selectedPoints.Count > 0;   
    private static RoadElement road;
    #endregion
 
    private static Editor roadEditorSettingsEditor;
    public static RoadEditorSettings roadEditorSettings;
    private static bool addNewPoint = false;

    private void OnEnable()
    {
        road = (RoadElement)target;
        OnTargetChanged?.Invoke(road);
        EditorWindow.windowFocusChanged += FocusChanged;
        roadEditorSettings = RoadEditorSettings.Load();

        RoadPointTools.OnChangePosition += ChangePosition;
        RoadPointTools.OnChangeHandlePosition += ChangeHandle;
        RoadPointTools.OnChangeHandleMode += ChangeHandleMode; 

        RoadPointTools.OnClickDeleteButton += DeletePoints;
        RoadPointTools.OnClickDuplicateButton += DuplicatePoint;
        RoadPointTools.OnClickConnectButton += ConnectNodes;
        RoadPointTools.OnClickDisconnectButton += DisconnectNodes;

        Undo.undoRedoPerformed += UndoRedo;
    }
    private void OnDisable()
    {
        EditorWindow.windowFocusChanged -= FocusChanged;

        RoadPointTools.OnChangePosition -= ChangePosition;
        RoadPointTools.OnChangeHandlePosition -= ChangeHandle;
        RoadPointTools.OnChangeHandleMode -= ChangeHandleMode;

        RoadPointTools.OnClickDeleteButton -= DeletePoints;
        RoadPointTools.OnClickDuplicateButton -= DuplicatePoint;
        RoadPointTools.OnClickConnectButton -= ConnectNodes;
        RoadPointTools.OnClickDisconnectButton -= DisconnectNodes;

        Undo.undoRedoPerformed -= UndoRedo;
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
        road.rootNode.VisitEdges((ConnectionBase connection) => DrawCurve(connection));
        road.rootNode.VisitNodes((RoadNode node) => PrintHandles(node));
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

      //  showEditButtons = DrawToggle(toggleButton,"Show Edit Buttons",showEditButtons);
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
                    DeletePoints();
                    e.Use();
                    break;
                case "Paste":
                case "Duplicate" :
                    DuplicatePoint();
                    e.Use();
                    break;
            }
        }

     //   if(addNewPoint)
      //  {
            //addNewPoint = false;
        //}
    }
    private void SelectPoint(RoadNode roadNode)
    {
        selectedPoints.Clear();
        selectedPoints.Add(roadNode);
        OnSelectionChanged?.Invoke(selectedPoints);
    }
    private void SelectMultiplePoints(RoadNode roadNode)
    {
        selectedHandleMode = HandleMode.Free;
        selectedPoints.Add(roadNode);
        OnSelectionChanged?.Invoke(selectedPoints);
    }
    private void Deselect()
    {
        selectedPoints.Clear();
        OnSelectionChanged?.Invoke(selectedPoints);
    }
    private void DrawCurve(ConnectionBase connection,float offset = 2.0f)
    {
        Vector3 startPoint = road.transform.TransformPoint(connection.nodeA.Position);
        Vector3 endPoint = road.transform.TransformPoint(connection.nodeB.Position);

        Vector3 startHandler = road.transform.TransformPoint(connection.WorldHandleA);
        Vector3 endHandler = road.transform.TransformPoint(connection.WorldHandleB);

        Vector3? prevStartHandler = null;// road.transform.TransformPoint(currentPoint.HandleB);
        Vector3? nextEndHandler = null;//road.transform.TransformPoint(nextPoint.HandleA);

        Handles.color = connection is RoadConnection ? roadEditorSettings.HandleLineColor : roadEditorSettings.IntersectionConnectionColor;
        Handles.DrawBezier(startPoint, endPoint, startHandler, endHandler, Color.white, null, 5f);   
        Handles.DrawAAPolyLine(BezierUtility.GetOffsetBezier(startPoint, startHandler, endHandler, endPoint,prevStartHandler,nextEndHandler, -road.Width * 0.5f, 30).ToArray());
        Handles.DrawAAPolyLine(BezierUtility.GetOffsetBezier(endPoint, endHandler, startHandler, startPoint,prevStartHandler,nextEndHandler, -road.Width * 0.5f, 30).ToArray());
    
      //  Handles.DrawAAPolyLine(BezierUtility.GetOffsetBezier(startPoint, startHandler, endHandler, endPoint,prevStartHandler,nextEndHandler,0, 30).ToArray());
        Handles.color = roadEditorSettings.MedianStripColor;
        
       // Handles.DrawAAPolyLine(BezierUtility.GetOffsetBezier(startPoint, startHandler, endHandler, endPoint,prevStartHandler,nextEndHandler, -road.HalfMedianStripWidth , 30).ToArray());
      //  Handles.DrawAAPolyLine(BezierUtility.GetOffsetBezier(startPoint, startHandler, endHandler, endPoint,prevStartHandler,nextEndHandler, road.HalfMedianStripWidth, 30).ToArray());
    }
    private void PrintHandles(RoadNode roadPoint)
    {    
        Vector3 worldPoint = road.transform.TransformPoint(roadPoint.Position);
        float size = HandleUtility.GetHandleSize(worldPoint) * 0.4f;

        if(selectedPoints.Contains(roadPoint))
        {
            Handles.color = roadEditorSettings.SelectedRoadPointColor;
            Handles.SphereHandleCap(0,worldPoint,Quaternion.identity,size,EventType.Repaint);
    
            EditorGUI.BeginChangeCheck();
            Vector3 newWorldPoint = road.transform.InverseTransformPoint(Handles.PositionHandle(worldPoint,Quaternion.identity)); 
            if (EditorGUI.EndChangeCheck()) ChangePosition(roadPoint,newWorldPoint);

            int index = 0;      
            foreach(ConnectionBase connection in roadPoint.links)
            {
                Vector3 handlePosition = road.transform.TransformPoint(roadPoint.Position + connection.GetLocalHandle(roadPoint));                
                Handles.color = roadEditorSettings.NextHandleColor(index);
                Handles.SphereHandleCap(0,handlePosition,Quaternion.identity,size * roadEditorSettings.handleSizeRelativeToRoadPoint,EventType.Repaint);
                Handles.DrawLine(worldPoint,handlePosition,5f);      
                
                EditorGUI.BeginChangeCheck();
                Vector3 newHandle = road.transform.InverseTransformPoint(Handles.PositionHandle(handlePosition,Quaternion.identity)); 
                if(EditorGUI.EndChangeCheck())
                    ChangeHandle(roadPoint,newHandle-newWorldPoint,index);         
                index++;
            }
        }
        else
        {
            Handles.color = roadEditorSettings.RoadPointColor;
            if (Handles.Button(worldPoint, Quaternion.identity, size,size, Handles.SphereHandleCap))
            {
                if(Event.current.control)
                    SelectMultiplePoints(roadPoint);
                else
                    SelectPoint(roadPoint);
            }
        }
    }
    private void UndoRedo() => OnSelectionChanged?.Invoke(selectedPoints);

    // private void PrintLines(RoadElement road)
    // {
    //     if(road.Points.Count == 0) return;
        
    //     Vector3? nextStartOffset = null;
    //     Vector3 startNode = road.transform.TransformPoint(road.Points[0].Position);
    //     Vector3 previousNodePosition;

    //     if(road.Points.Count > 1) 
    //         previousNodePosition = startNode -(road.transform.TransformPoint(road.Points[1].Position) - startNode);
    //     else
    //         previousNodePosition = startNode + Vector3.right;

    //     for (int i = 0; i < road.Points.Count - 1; i++)
    //     {
    //         Vector3 a = road.transform.TransformPoint(road.Points[i].Position);
    //         Vector3 b = road.transform.TransformPoint(road.Points[i + 1].Position);
          
    //         Vector3 middle = (a + b) * 0.5f;
    //         float size = HandleUtility.GetHandleSize(middle) * 0.1f;   
    //         Vector3 nextNodeDirection = b - a;

    //         Vector3 offset1 = Vector3.Cross(Vector3.up, nextNodeDirection).normalized;
    //         Vector3 startOffset = nextStartOffset.HasValue ? nextStartOffset.Value : offset1;
    //         Vector3 endOffset = Vector3.zero;

    //         if(i < road.Points.Count - 2)
    //         {
    //             Vector3 c = road.transform.TransformPoint(road.Points[i + 2].Position);  
    //             Vector3 offset2 = Vector3.Cross(Vector3.up, c - b).normalized;
    //             // if(LineIntersectionXZ(a + startOffset,b + offset1,b + offset2,c + offset2,out var news))
    //             // {
    //             //     news.y = b.y;
    //             //     endOffset = news - b; 
    //             //     nextStartOffset = endOffset;
    //             // }
    //         }
    //         else
    //             endOffset = offset1;


    //         startOffset = startOffset.normalized;
    //         endOffset = endOffset.normalized;

    //         Vector3 startMedianOffset = startOffset * road.HalfMedianStripWidth;
    //         Vector3 endMedianOffset = endOffset * road.HalfMedianStripWidth;

    //         Handles.color = roadEditorSettings.MedianStripColor;
    //         Handles.DrawLine(a + startMedianOffset,b + endMedianOffset);
    //         Handles.DrawLine(a - startMedianOffset,b - endMedianOffset);

    //         Handles.color = roadEditorSettings.LaneColor;
    //         Handles.DrawLine(a + startMedianOffset + road.ForwardRoadwayWidth * startOffset,b + endMedianOffset  + road.ForwardRoadwayWidth * endOffset);
    //         Handles.DrawLine(a - startMedianOffset - road.BackwardRoadwayWidth * startOffset,b - endMedianOffset  - road.BackwardRoadwayWidth * endOffset);

    //         ShowEditButtons(i,middle);
            
    //         PrintArrows(road,a,nextNodeDirection,previousNodePosition - a,startOffset,startMedianOffset);
    //         previousNodePosition = a;
    //     }

    //     Vector3 last = road.transform.TransformPoint(road.Points[road.Points.Count -1].Position);
    //DoSomething     Vector3 offset = Vector3.Cross(Vector3.up,last - previousNodePosition).normalized;
    //     PrintArrows(road,last,-(previousNodePosition - last),previousNodePosition - last,offset,offset * road.HalfMedianStripWidth);
    // }
    // private void ShowEditButtons(int currentIndexPoint,Vector3 middlePoint)
    // {
    //     if(showEditButtons)
    //     {         
    //         Handles.BeginGUI(); 
    //         Vector2 guiPosition = HandleUtility.WorldToGUIPoint(middlePoint);
    //         GUIStyle style = new GUIStyle(GUI.skin.button)
    //         {
    //             fontSize = 14,
    //             fontStyle = FontStyle.Bold
    //         };
            
    //         if (GUI.Button(new Rect(guiPosition.x - 12, guiPosition.y - 12, 24, 24),"+",style))
    //         {
    //             Vector3 localPoint = road.transform.InverseTransformPoint(middlePoint);
    //             RecordAction(road,"Add road point",() => {road.Points.Insert(currentIndexPoint + 1, new RoadNode(localPoint));});
    //         }
    //         Handles.EndGUI();
    //     }
    // }
    // private void PrintArrows(RoadElement road,Vector3 nodePosition,Vector3 nextNodeDir,Vector3 previousNodeDir,Vector3 offsetDirection,Vector3 medianOffset)
    // {
    //     float size = HandleUtility.GetHandleSize(nodePosition) * 0.4f;
    //     Handles.color = roadEditorSettings.ForwardLaneColor;
    //     for(int i = 0; i < road.ForwardLaneCount; i++)
    //         Handles.ArrowHandleCap(0,nodePosition + medianOffset + offsetDirection * (i + 0.5f) * road.LaneWidth , Quaternion.LookRotation(nextNodeDir),size, EventType.Repaint); 
        
    //     Handles.color = roadEditorSettings.BackwardLaneColor;
    //     for(int i = 0; i < road.BackwardLaneCount; i++)
    //         Handles.ArrowHandleCap(0,nodePosition - medianOffset - offsetDirection * (i + 0.5f) * road.LaneWidth , Quaternion.LookRotation(previousNodeDir),size, EventType.Repaint);       
    // }




    #region  Handles
    private void ChangeHandleMode(HandleMode handleMode)
    {
        selectedHandleMode = handleMode;
    }
    private void ChangeHandle(RoadNode node,Vector3 newPosition,int index)
    {
        RecordAction(road,"Move road point",() =>
        { 
            if(node != null)
            {
                node.links[index].SetLocalHandle(node,newPosition);
                for(int i = 0; i < node.links.Count; i++)
                {
                    if(i == index) continue;
                    var connection = node.links[i];
                    connection.SetLocalHandle(node,CalculateOppositeHandle(newPosition,connection.GetLocalHandle(node)));
                }
            }
        });
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
    private void ChangePosition(RoadNode roadPoint,Vector3 newPosition)
    {
        RecordAction(road,"Move road point",() =>
        { 
            if(roadPoint != null)
                roadPoint.Position = newPosition;
        });
    }
    private void ChangePosition(Vector3 newPosition)
    {
        if(PointIsSelected)
            ChangePosition(selectedPoint,newPosition);
    }
    private void DeletePoints()
    {
        if(PointIsSelected)
        {
            RecordAction(road,"Delete Points",() =>
            {
                foreach(var selected in selectedPoints)
                    road.DeletePoint(selected);
            });
            Deselect();
        }
    } 
    private void DuplicatePoint(RoadNode roadPoint)
    {
        RecordAction(road,"Duplicate Point",() => road.DuplicatePoint(roadPoint));
    }
    private void DuplicatePoint()
    {
        DuplicatePoint(selectedPoint);
    }
    private void RecordAction(RoadElement roadElement,string actionName, Action action)
    {
        Undo.RegisterCompleteObjectUndo(roadElement, actionName);
        action();
        EditorUtility.SetDirty(roadElement);
        OnSelectionChanged?.Invoke(selectedPoints);
    }

    [Shortcut("TrafficTools/ConnectNodes",typeof(SceneView), KeyCode.Q, ShortcutModifiers.Control)]
    private static void ConnectNodes()
    {
        if(selectedPoints.Count > 1)
        {
            selectedPoints[0].ConnectRoad(selectedPoints[1]);
        }
    }

    [Shortcut("TrafficTools/DisconnectNodes",typeof(SceneView), KeyCode.W, ShortcutModifiers.Control)]
    private static void DisconnectNodes()
    {
        if(selectedPoints.Count > 1)
        {
            for (int i = 0; i < selectedPoints.Count; i++)
                for (int k = i + 1; k < selectedPoints.Count; k++)
                    selectedPoints[i].RemoveConnectionsWithNode(selectedPoints[k]);
        }
    }

    [Shortcut("TrafficTools/CreateIntersection",typeof(SceneView), KeyCode.E, ShortcutModifiers.Control)]
    private static void CreateIntersection()
    {
        if(selectedPoints.Count == 1)
        {
            road.CreateIntersection(selectedPoints[0]);
        }
    }
}