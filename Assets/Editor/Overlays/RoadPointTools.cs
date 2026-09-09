using System;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "Road Point Tools",defaultDockPosition = DockPosition.Bottom,defaultLayout = Layout.Panel,defaultDockZone = DockZone.RightToolbar)]
[Icon("Assets/Textures/ToolIcons/RoadPoint.png")]
public class RoadPointTools : Overlay, ITransientOverlay
{
    public static event Action<Vector3> OnChangePosition;
    public static event Action<Vector3> OnChangeHandleAPosition;
    public static event Action<Vector3> OnChangeHandleBPosition;
    public static event Action OnClickDeleteButton;
    public static event Action OnClickDuplicateButton;
    
    private RoadPoint roadPoint;
    private RoadElement roadElement;

    private Vector3Field positionField;
    private Vector3Field HandleAPositionField;
    private Vector3Field HandleBPositionField;
    private Button deleteButton;
    private Button duplicateButton;



    private VisualElement buttons;
    private VisualElement positionFields;

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
        // title
        var title = new Label("Road Point Tools");

        // Position Fields
        positionField = new Vector3Field("Position");
        positionField.RegisterValueChangedCallback((e) => {OnChangePosition?.Invoke(e.newValue); });
        HandleAPositionField = new Vector3Field("HandleA position");
        HandleAPositionField.RegisterValueChangedCallback((e) => {OnChangeHandleAPosition?.Invoke(e.newValue); });
        HandleBPositionField = new Vector3Field("HandleB position");
        HandleBPositionField.RegisterValueChangedCallback((e) => {OnChangeHandleBPosition?.Invoke(e.newValue); });

        positionFields = new VisualElement();
        positionFields.Add(positionField);
        positionFields.Add(HandleAPositionField);
        positionFields.Add(HandleBPositionField);

        // Buttons
        deleteButton = CustomEditorUtility.GetSingleLineButton("Delete point",CustomEditorIcons.Delete);
        duplicateButton = CustomEditorUtility.GetSingleLineButton("Duplicate point",CustomEditorIcons.Duplicate);
        deleteButton.RegisterCallback<ClickEvent>((e) => { OnClickDeleteButton?.Invoke(); });
        duplicateButton.RegisterCallback<ClickEvent>((e) => { OnClickDuplicateButton?.Invoke(); });
       
        buttons = new VisualElement();
        buttons.style.flexDirection = FlexDirection.Row;
        buttons.Add(deleteButton);
        buttons.Add(duplicateButton);

        // root
        var root = new VisualElement();
        root.style.minWidth = 300;

        root.Add(title);
        root.Add(positionFields);
        root.Add(buttons);

        RefreshUI();
        return root;
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
    private void UpdateSelection(RoadPoint roadPoint)
    {
        this.roadPoint = roadPoint;
        RefreshUI();
    }
    private void UpdateTarget(RoadElement roadElement)
    {
        this.roadElement = roadElement;
        RefreshUI();
    }
    private void RefreshUI()
    {
        if(positionFields == null) 
            return;

        if(roadPoint != null)
        {
            positionFields.visible = true;
            buttons.visible = true;

            positionField.value = roadPoint.Position; 
            HandleAPositionField.value = roadPoint.LocalPositionHandleA; 
            HandleBPositionField.value = roadPoint.LocalPositionHandleB; 
        }
        else 
        {
            positionFields.visible = false;
            buttons.visible = false;   
        }
    }
}