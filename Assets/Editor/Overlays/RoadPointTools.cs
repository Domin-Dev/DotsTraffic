using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "Road Point Tools",defaultDockPosition = DockPosition.Bottom,defaultLayout = Layout.Panel,defaultDockZone = DockZone.RightToolbar)]
[Icon("Assets/Textures/ToolIcons/RoadPoint.png")]
public class RoadPointTools : Overlay, ITransientOverlay
{
    #region Events
    public static event Action<Vector3> OnChangePosition;
    public static event Action<RoadNode,Vector3,int> OnChangeHandlePosition;
    public static event Action<HandleMode> OnChangeHandleMode;
    public static event Action OnClickDeleteButton;
    public static event Action OnClickDuplicateButton;
    public static event Action OnClickConnectButton;
    public static event Action OnClickDisconnectButton;
    #endregion

    #region Targets
    private List<RoadNode> roadPoints;
    private RoadElement roadElement;
    #endregion

    #region Fields
    private Vector3Field positionField;
    private Button deleteButton;
    private Button duplicateButton;
    private Button connectButton;
    private Button disconnectButton;
    #endregion

    #region Groups
    private VisualElement buttons;
    private VisualElement positionFields;
    private VisualElement handleFields;
    private RadioButtonGroup handleModes;
    #endregion
    public bool visible
    {
        get
        {
            if(Selection.activeGameObject != null)
                return Selection.activeGameObject.GetComponent<RoadElement>() != null;
            else
                return false;
        }
    }
    public override VisualElement CreatePanelContent()
    {
        #region Title
        var title = new Label("Road Point Tools");
        #endregion

        #region Position Fields
        positionField = new Vector3Field("Position");
        positionField.RegisterValueChangedCallback((e) => {OnChangePosition?.Invoke(e.newValue); });    
        positionFields = new VisualElement();
        handleFields = new VisualElement();
        positionFields.Add(positionField);

        #endregion

        #region Handle Modes
        handleModes = new RadioButtonGroup("Handle Mode",new List<string>(Enum.GetNames(typeof(HandleMode))));
        handleModes.value = (int)RoadElementEditor.selectedHandleMode;
        handleModes.RegisterValueChangedCallback((e) => {OnChangeHandleMode?.Invoke((HandleMode)e.newValue); });
        #endregion

        #region Buttons  
        deleteButton = CustomEditorUtility.GetSingleLineButton("Delete",CustomEditorIcons.Delete);
        duplicateButton = CustomEditorUtility.GetSingleLineButton("Duplicate",CustomEditorIcons.Duplicate);
        connectButton = CustomEditorUtility.GetSingleLineButton("Connect",CustomEditorIcons.Connection);
        connectButton.style.display = DisplayStyle.None;
        disconnectButton = CustomEditorUtility.GetSingleLineButton("Disconnect",CustomEditorIcons.Disconnection);
       
        deleteButton.RegisterCallback<ClickEvent>((e) => { OnClickDeleteButton?.Invoke(); });
        duplicateButton.RegisterCallback<ClickEvent>((e) => { OnClickDuplicateButton?.Invoke(); });
        connectButton.RegisterCallback<ClickEvent>((e) => { OnClickConnectButton?.Invoke(); });
        disconnectButton.RegisterCallback<ClickEvent>((e) => { OnClickDisconnectButton?.Invoke(); });
       
        buttons = new VisualElement();
        buttons.style.flexDirection = FlexDirection.Row;
        buttons.Add(deleteButton);
        buttons.Add(duplicateButton);
        buttons.Add(connectButton);
        buttons.Add(disconnectButton);
        #endregion

        #region  root
        var root = new VisualElement();
        root.style.minWidth = 300;
        root.Add(title);
        root.Add(positionFields);
        root.Add(handleFields);
        root.Add(handleModes);
        root.Add(buttons);
        RefreshUI();
        return root;
        #endregion
    }
    public override void OnCreated()
    {
        RoadElementEditor.OnSelectionChanged += UpdateSelection;
        RoadElementEditor.OnTargetChanged += UpdateTarget;
    }
    public override void OnWillBeDestroyed()
    {
        RoadElementEditor.OnSelectionChanged -= UpdateSelection;
        RoadElementEditor.OnTargetChanged -= UpdateTarget;
    }
    private void UpdateSelection(List<RoadNode> roadPoints)
    {
        this.roadPoints = roadPoints;
        RefreshUI();
    }
    private void UpdateTarget(RoadElement roadElement)
    {
        this.roadElement = roadElement;
        RefreshUI();
    }
    
    #region RefreshUI
    private void RefreshUI()
    {
        if(positionFields == null) 
            return;

        if(roadPoints != null && roadPoints.Count > 0)
        {
            if(roadPoints.Count == 1)
                OneSelectedPoint();
            else
                MultipleSelectedPoints();
        }
        else 
            NoSelectedPoint();
    }
    private void NoSelectedPoint()
    {
        SetVisible(false);
        disconnectButton.style.display = DisplayStyle.None;
        connectButton.style.display = DisplayStyle.None;
        deleteButton.style.display = DisplayStyle.None;
        duplicateButton.style.display = DisplayStyle.None;
    }
    private void OneSelectedPoint()
    {
        SetVisible(true);
        disconnectButton.style.display = DisplayStyle.None;
        connectButton.style.display = DisplayStyle.None;
        deleteButton.style.display = DisplayStyle.Flex;
        duplicateButton.style.display = DisplayStyle.Flex;

        var selectedPoint = roadPoints[0];         
        if(handleFields.childCount != selectedPoint.links.Count)
        {
            handleFields.Clear();
            for(int i = 0; i < selectedPoint.links.Count; i++)
            {
                var connection = selectedPoint.links[i];
                var handle = new Vector3Field($"{i}.Handle position");
                var label = handle.Q<Label>();
                label.style.color = RoadElementEditor.roadEditorSettings.NextHandleColor(i);
                int index = i;
                handle.RegisterValueChangedCallback((e) => {OnChangeHandlePosition?.Invoke(selectedPoint,e.newValue,index); });
                handleFields.Add(handle);
            
            }
            positionField.SetValueWithoutNotify(selectedPoint.Position);
        }
        for(int i = 0; i < selectedPoint.links.Count; i++)
            (handleFields[i] as Vector3Field).SetValueWithoutNotify(selectedPoint.links[i].GetLocalHandle(selectedPoint)); 
}
    private void MultipleSelectedPoints()
    {
        SetVisible(false);
        disconnectButton.style.display = DisplayStyle.Flex;
        connectButton.style.display = DisplayStyle.Flex;
        deleteButton.style.display = DisplayStyle.Flex;
        duplicateButton.style.display = DisplayStyle.None;
    }
    private void SetVisible(bool value)
    {
        positionFields.visible = value;  
        handleModes.visible = value;
        handleFields.visible = value;
    }
    #endregion
}