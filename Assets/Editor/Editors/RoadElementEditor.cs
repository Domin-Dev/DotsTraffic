using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

[CustomEditor(typeof(RoadElement))]
public class RoadElementEditor : Editor
{   
    #region Events
    public static event Action<List<ISelectable>,Type> OnSelectionChanged;
    public static event Action<RoadElement> OnTargetChanged;
    #endregion

    #region Shows
    private static bool showPositionHandle = true;
    private static bool showConnectionHandles = true;
    private static bool showNodeHandles = true;
    #endregion

    #region Selected
    public static HandleMode selectedHandleMode { private set; get; } = HandleMode.Free; 
    
    private static ISelectable selected => selectedObjects[0];
    private static List<ISelectable> selectedObjects = new List<ISelectable>();
    private static Type selectedType;
    private static bool IsSelected => selectedObjects.Count > 0; 

    private static RoadElement road;
    #endregion
 
    private static bool openRoadEditorSettings = false;
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

        Undo.undoRedoPerformed += UpdateSelection;
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

        Undo.undoRedoPerformed -= UpdateSelection;
    }
    private void FocusChanged()
    {
        if(EditorWindow.focusedWindow?.GetType().Name != "InspectorWindow")
            Deselect();       
    }
    public void OnSceneGUI()
    { 
        Events();
        Tools.hidden = !showPositionHandle;
        List<Vector3> prevPoints = new List<Vector3>();

        if(showConnectionHandles)
            road.rootNode.VisitEdges((ConnectionBase prev,RoadNode node,ConnectionBase connection) => 
            {
                DrawCurve(prevPoints,prev,node,connection);
                DisplayConnectionHandlers(connection);
            });
        else
            road.rootNode.VisitEdges((ConnectionBase prev,RoadNode node,ConnectionBase connection) => DrawCurve(prevPoints,prev,node,connection));
        
        if(showNodeHandles) road.rootNode.VisitNodes((RoadNode node) => DisplayNodeHandles(node));

        int temp;
        do
        {
            temp = prevPoints.Count;
            prevPoints = BezierUtility.RemoveSelfIntersections(prevPoints);
        } 
        while(temp != prevPoints.Count);
        Handles.DrawAAPolyLine((prevPoints).ToArray());
    }
    public override void OnInspectorGUI()
    {      
        RoadElement road = (RoadElement)target;
        DrawEditorOptions();
        DrawProperties();
      //  DrawDefaultInspector();

        if (CustomEditorUtility.SingleLineButton("Add Point",CustomEditorIcons.Add))
            RecordAction(road,"Add road point",road.AddPoint);
        DrawRoadSettings();
    }
    private void DrawProperties()
    {
        serializedObject.Update();

        bool selectedInEdit = IsSelected && selectedType == typeof(RoadConnection);
        RoadConnectionSettings[] settings = selectedInEdit ? 
            selectedObjects.Cast<RoadConnection>().Select(x => x.settings).ToArray() : 
            new []{road.settings};

        EditorGUILayout.BeginHorizontal();
        if (CustomEditorUtility.SingleLineButton("One-way",CustomEditorIcons.UpArrow))
            RecordAction(road,"One-way",road.settings.ChangeToOneWayRoad);
        
        if (CustomEditorUtility.SingleLineButton("Change Direction",CustomEditorIcons.ChangeDirection))
            RecordAction(road,"Change Direction",road.settings.ChangeDirection);
 
        if (CustomEditorUtility.SingleLineButton("Project to Surface",CustomEditorIcons.ProjectToSurface))
            RecordAction(road,"Project to Surface",road.ProjectToSurface);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

        EditorGUI.BeginChangeCheck();

        EditorGUI.showMixedValue = settings.All(x => x.ForwardLaneCount == settings[0].ForwardLaneCount);
        //EditorGUILayout.IntSlider("Forward Lane Count",settings[0].ForwardLaneCount,0,20));
        
        //settings.BackwardLaneCount = EditorGUILayout.IntSlider("Backward Lane Count",settings.BackwardLaneCount,0,20);
        //settings.MedianStripWidth = Math.Max(0,EditorGUILayout.FloatField("Median Strip Width",settings.MedianStripWidth));
      //  settings.LaneWidth = Math.Max(0.00001f,EditorGUILayout.FloatField("Lane Width",settings.LaneWidth));
        
        EditorGUI.showMixedValue = false;

        EditorGUILayout.Separator();
        if(!selectedInEdit)
        {
            if (CustomEditorUtility.SingleLineButton("Apply to all connections",CustomEditorIcons.ProjectToSurface))
                RecordAction(road,"Apply to all connections",road.ProjectToSurface);
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RegisterCompleteObjectUndo(road,"Value Changed");
            EditorUtility.SetDirty(target);
        }

        serializedObject.ApplyModifiedProperties();
    }
    private void DrawRoadSettings()
    {
        EditorGUILayout.Separator();
        using (var check = new EditorGUI.ChangeCheckScope())
        {
            openRoadEditorSettings = EditorGUILayout.InspectorTitlebar(openRoadEditorSettings, roadEditorSettings);
            if (openRoadEditorSettings)
            {
                CreateCachedEditor(roadEditorSettings, null, ref roadEditorSettingsEditor);
                roadEditorSettingsEditor.OnInspectorGUI();
            }
            if(check.changed)
            {
                SceneView.RepaintAll();
            }
        }
    }      
    private void DrawEditorOptions()
    {
        EditorGUILayout.BeginHorizontal();
        GUIStyle toggleButton = new GUIStyle(GUI.skin.button);

        showPositionHandle = DrawToggle(toggleButton,"Show Position Handle",showPositionHandle);
        showConnectionHandles = DrawToggle(toggleButton,"Show Connection Handles",showConnectionHandles);
        showNodeHandles = DrawToggle(toggleButton,"Show Node Handles",showNodeHandles);

        EditorGUILayout.EndHorizontal();
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
        if(e != null && e.type == EventType.ExecuteCommand)
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
    private void SelectObject(ISelectable obj)
    {
        selectedObjects.Clear();
        selectedObjects.Add(obj);
        selectedType = obj.GetType();
        UpdateSelection();
        EditorUtility.SetDirty(target);
    }

    private void SelectMultipleObjects(ISelectable obj)
    {
        selectedHandleMode = HandleMode.Free;
        var newType = obj.GetType();
        if(newType != selectedType)
        {
            selectedObjects.Clear();
            selectedType = newType;
        }
        selectedObjects.Add(obj);
        UpdateSelection();
        EditorUtility.SetDirty(target);
    }
    private void Deselect()
    {
        selectedObjects.Clear();
        selectedType = null;
        UpdateSelection();
        EditorUtility.SetDirty(target);
    }
    private void UpdateSelection()
    {
        OnSelectionChanged?.Invoke(selectedObjects,selectedType);
    }
    private void DrawCurve(List<Vector3> prevPoints,ConnectionBase prev,RoadNode node,ConnectionBase connection,float offset = 2.0f)
    {
        var second = connection.GetSecond(node);

        Vector3 startPoint = road.transform.TransformPoint(node.Position);
        Vector3 endPoint = road.transform.TransformPoint(second.Position);

        Vector3 startHandler = road.transform.TransformPoint(connection.GetWorldHandle(node));
        Vector3 endHandler = road.transform.TransformPoint(connection.GetWorldHandle(second));

        Vector3? prevStartHandler = prev != null ? road.transform.TransformPoint(prev.GetWorldHandle(node)) : null;
        Vector3? nextEndHandler = second.links.Count > 1 ? road.transform.TransformPoint(second.GetNextConnection(connection).GetWorldHandle(second)) : null;

        Handles.color = connection is RoadConnection ? roadEditorSettings.HandleLineColor : roadEditorSettings.IntersectionConnectionColor;
        Handles.DrawBezier(startPoint, endPoint, startHandler, endHandler, Color.white, null, 5f);   
        var result = BezierUtility.GetOffsetBezier(startPoint, startHandler, endHandler, endPoint,prevStartHandler,nextEndHandler, road.settings.Width * 0.5f, 50);
        prevPoints.AddRange(result);

        //Handles.DrawAAPolyLine(BezierUtility.GetOffsetBezier(endPoint, endHandler, startHandler, startPoint,prevStartHandler,nextEndHandler, -road.Width * 0.5f, 30).ToArray());
    
      //  Handles.DrawAAPolyLine(BezierUtility.GetOffsetBezier(startPoint, startHandler, endHandler, endPoint,prevStartHandler,nextEndHandler,0, 30).ToArray());
        Handles.color = roadEditorSettings.MedianStripColor;
        
       // Handles.DrawAAPolyLine(BezierUtility.GetOffsetBezier(startPoint, startHandler, endHandler, endPoint,prevStartHandler,nextEndHandler, -road.HalfMedianStripWidth , 30).ToArray());
      //  Handles.DrawAAPolyLine(BezierUtility.GetOffsetBezier(startPoint, startHandler, endHandler, endPoint,prevStartHandler,nextEndHandler, road.HalfMedianStripWidth, 30).ToArray());
    }
    private void DisplayNodeHandles(RoadNode roadPoint)
    {    
        Vector3 worldPoint = road.transform.TransformPoint(roadPoint.Position);
        float size = HandleUtility.GetHandleSize(worldPoint) * roadEditorSettings.RoadPointSize;

        if(selectedObjects.Contains(roadPoint))
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
                    SelectMultipleObjects(roadPoint);
                else
                    SelectObject(roadPoint);
            }
        }
    }
    private void DisplayConnectionHandlers(ConnectionBase connectionBase)
    {
        Vector3 worldPoint = connectionBase.Bezier(road,0.5f);
        float size = HandleUtility.GetHandleSize(worldPoint) * roadEditorSettings.ConnectionPointSize;


        if(selectedObjects.Contains(connectionBase))
        {
            Handles.color = roadEditorSettings.ConnectionPointColor;
            Handles.CubeHandleCap(0,worldPoint,Quaternion.identity,size,EventType.Repaint);
        }
        else
        {
            Handles.color = roadEditorSettings.SelectedConnectionPointColor;
            if (Handles.Button(worldPoint, Quaternion.identity, size,size, Handles.CubeHandleCap))
            {
                if(Event.current.control)
                    SelectMultipleObjects(connectionBase);
                else
                    SelectObject(connectionBase);
            }
        }
        
    }

    

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
        if(IsSelected && selectedType == typeof(RoadNode))
            ChangePosition(selected as RoadNode,newPosition);
    }
    private void DeletePoints()
    {
        if(IsSelected && selectedType == typeof(RoadNode))
        {
            RecordAction(road,"Delete Points",() =>
            {
                foreach(var selected in selectedObjects)
                    road.DeletePoint(selected as RoadNode);
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
        DuplicatePoint(selected as RoadNode);
    }
    private void RecordAction(RoadElement roadElement,string actionName, Action action)
    {
        Undo.RegisterCompleteObjectUndo(roadElement, actionName);
        action();
        EditorUtility.SetDirty(roadElement);
        UpdateSelection();
    }

    [Shortcut("TrafficTools/ConnectNodes",typeof(SceneView), KeyCode.Q, ShortcutModifiers.Control)]
    private static void ConnectNodes()
    {
        if(selectedType == typeof(RoadNode) && selectedObjects.Count > 1)
            (selectedObjects[0] as RoadNode).ConnectRoad(selectedObjects[1] as RoadNode);
    }

    [Shortcut("TrafficTools/DisconnectNodes",typeof(SceneView), KeyCode.W, ShortcutModifiers.Control)]
    private static void DisconnectNodes()
    {
        if(selectedType == typeof(RoadNode) && selectedObjects.Count > 1)
        {
            for (int i = 0; i < selectedObjects.Count; i++)
                for (int k = i + 1; k < selectedObjects.Count; k++)
                    (selectedObjects[i] as RoadNode).RemoveConnectionsWithNode(selectedObjects[k] as RoadNode);
        }
    }

    [Shortcut("TrafficTools/CreateIntersection",typeof(SceneView), KeyCode.E, ShortcutModifiers.Control)]
    private static void CreateIntersection()
    {
        if(selectedObjects.Count == 1 && selectedType == typeof(RoadNode))
            road.CreateIntersection(selectedObjects[0] as RoadNode);
    }
}