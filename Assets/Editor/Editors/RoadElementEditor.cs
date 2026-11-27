using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
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
 
    #region EditorSettings
    private static bool openEditorSettings = false;
    private static Editor roadEditorSettingsEditor;
    public static RoadEditorSettings roadEditorSettings;
    #endregion

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
    
    #region Scene GUI
    public void OnSceneGUI()
    { 
        Events();
        Tools.hidden = !showPositionHandle;
        List<Vector3> prevPoints = new List<Vector3>();

        if(showConnectionHandles)
            road.rootNode.VisitEdges((ConnectionBase prev,RoadNode node,ConnectionBase connection) => 
            {
                DrawCurve(prevPoints,prev,node,connection);
                DrawConnectionHandlers(connection);
            });
        else
            road.rootNode.VisitEdges((ConnectionBase prev,RoadNode node,ConnectionBase connection) => DrawCurve(prevPoints,prev,node,connection));
        
        if(showNodeHandles) road.rootNode.VisitNodes((RoadNode node) => DrawNodeHandles(node));

        int temp;
        do
        {
            temp = prevPoints.Count;
            prevPoints = BezierUtility.RemoveSelfIntersections(prevPoints);
        } 
        while(temp != prevPoints.Count);
        Handles.DrawAAPolyLine((prevPoints).ToArray());
    }
    private void FocusChanged()
    {
        if(EditorWindow.focusedWindow?.GetType().Name != "InspectorWindow")
            Deselect();       
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
    private void DrawNodeHandles(RoadNode roadPoint)
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
    private void DrawConnectionHandlers(ConnectionBase connectionBase)
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
    #endregion

    #region Inspector GUI
    public override void OnInspectorGUI()
    {      
        RoadElement road = (RoadElement)target;
        DrawEditorOptions();
        DrawProperties();
      //  DrawDefaultInspector();

        if (CustomEditorUtility.SingleLineButton("Add Point",CustomEditorIcons.Add))
            RecordAction(road,"Add road point",road.AddPoint);
        DrawEditorSettings();
    }
    private void DrawProperties()
    {
        serializedObject.Update();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        DrawProperties("Road Element",road.settings);
        if(CustomEditorUtility.SingleLineButton("Apply to all connections",CustomEditorIcons.ProjectToSurface))
            RecordAction(road,"Apply to all connections",road.ApplyToAllConnections);
        EditorGUILayout.EndVertical();

        DrawConnectionSettings();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RegisterCompleteObjectUndo(road,"Value Changed");
            EditorUtility.SetDirty(target);
        }
        serializedObject.ApplyModifiedProperties();
    }
    private void DrawConnectionSettings()
    {
        if(IsSelected && selectedType == typeof(RoadConnection))
        {
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var connections = selectedObjects.Cast<RoadConnection>();
            bool isNull = connections.First().settings == null;
            bool mixedValue = connections.Any(x => (x.settings == null) != isNull);
            EditorGUI.showMixedValue = mixedValue;

            EditorGUI.BeginChangeCheck();
            bool toggle = EditorGUILayout.ToggleLeft(new GUIContent("Override Road Element Settings"),!isNull);
            if(EditorGUI.EndChangeCheck())
            {
                if(toggle && !mixedValue)
                {
                    foreach(var item in connections)
                        if(item.settings == null) item.settings = new RoadConnectionSettings(road.settings);
                }
                else
                {
                    foreach(var item in connections)
                        item.settings = null;
                }
            }

            EditorGUI.showMixedValue = false;
            DrawProperties("Selected Connection",connections.Where(x => x.settings != null).Select(x => x.settings).ToArray());
            EditorGUILayout.EndVertical();
        }
    }
    private void DrawProperties(string labelField,params RoadConnectionSettings[] connectionSettings )
    {
        EditorGUILayout.LabelField(labelField,EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        if(connectionSettings == null || connectionSettings.Length == 0)
        {
            EditorGUI.BeginDisabledGroup(true);
            connectionSettings = new []{road.settings};
        }

        if (CustomEditorUtility.SingleLineButton("One-way",CustomEditorIcons.UpArrow))
            RecordAction(road,"One-way",() => { foreach (var item in connectionSettings)item.ChangeToOneWayRoad();});
        
        if (CustomEditorUtility.SingleLineButton("Change Direction",CustomEditorIcons.ChangeDirection))
            RecordAction(road,"Change Direction",() => { foreach (var item in connectionSettings)item.ChangeDirection();});
 
     //   if (CustomEditorUtility.SingleLineButton("Project to Surface",CustomEditorIcons.ProjectToSurface))
       //     RecordAction(road,"Project to Surface",road.ProjectToSurface);
       
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

        EditorGUI.BeginChangeCheck();

        CustomEditorUtility.IntSlider(connectionSettings,
        "Forward Lane Count",(x,v) => x.ForwardLaneCount = v,(x) => x.ForwardLaneCount,0,20);
        CustomEditorUtility.IntSlider(connectionSettings,
        "Backward Lane Count",(x,v) => x.BackwardLaneCount = v,(x) => x.BackwardLaneCount,0,20);
        
        CustomEditorUtility.FloatField(connectionSettings,
        "Median Strip Width",(x,v) => x.MedianStripWidth = v,(x) => x.MedianStripWidth,0);        
        CustomEditorUtility.FloatField(connectionSettings,
        "Lane Width",(x,v) => x.LaneWidth = v,(x) => x.LaneWidth,0.00001f);

        EditorGUI.EndDisabledGroup();
        EditorGUI.showMixedValue = false;
    }
    private void DrawEditorSettings()
    {
        EditorGUILayout.Separator();
        using (var check = new EditorGUI.ChangeCheckScope())
        {
            openEditorSettings = EditorGUILayout.InspectorTitlebar(openEditorSettings, roadEditorSettings);
            if (openEditorSettings)
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

        EditorGUI.BeginChangeCheck();
        showPositionHandle = GUILayout.Toggle(showPositionHandle,"Show Position Handle",toggleButton);
        showConnectionHandles = GUILayout.Toggle(showConnectionHandles,"Show Connection Handles",toggleButton);
        showNodeHandles = GUILayout.Toggle(showNodeHandles,"Show Node Handles",toggleButton);
        if(EditorGUI.EndChangeCheck())
            SceneView.RepaintAll();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();
    }
    #endregion

    #region Selection
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
    #endregion

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
    
    #region  Actions
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
    #endregion
}