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
    public static event Action OnClickDeleteButton;
    public static event Action OnClickDuplicateButton;
    
    private RoadPoint roadPoint;
    private RoadElement roadElement;

    private Vector3Field positionField;
    private Button deleteButton;
    private Button duplicateButton;
    private VisualElement buttons;

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
        var root = new VisualElement();
        root.style.minWidth = 300;

        var title = new Label("Road Point Tools");
        positionField = new Vector3Field("Position");
        positionField.RegisterValueChangedCallback((e) => {OnChangePosition?.Invoke(e.newValue); });

        deleteButton = CustomEditorUtility.GetSingleLineButton("Delete point",CustomEditorIcons.Delete);
        duplicateButton = CustomEditorUtility.GetSingleLineButton("Duplicate point",CustomEditorIcons.Duplicate);

        deleteButton.RegisterCallback<ClickEvent>((e) => { OnClickDeleteButton?.Invoke(); });
        duplicateButton.RegisterCallback<ClickEvent>((e) => { OnClickDuplicateButton?.Invoke(); });

        buttons = new VisualElement();
        buttons.style.flexDirection = FlexDirection.Row;
        buttons.Add(deleteButton);
        buttons.Add(duplicateButton);

        root.Add(title);
        root.Add(positionField);
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
        if(positionField == null) 
            return;

        if(roadPoint != null)
        {
            positionField.visible = true;
            buttons.visible = true;
            positionField.value = roadPoint.Position; 
        }
        else 
        {
            positionField.visible = false;
            buttons.visible = false;
        }
    }
}