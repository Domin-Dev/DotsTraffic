using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using Unity.Scenes;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SubSceneGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct DebugNode
    {
        public float width;
        public Vector3 startPoint;
        public Vector3 endPoint;
        public Vector3 startHandler;
        public Vector3 endHandler;
    }

    [SerializeField] private string subScenePath = "Assets/Scenes/TrafficSubScene.unity";
    public SubScene subScene;
    public LayerMask surfaceLayer = 1 << 0;
    public RoadSpace roadSpace;
    [HideInInspector] public bool editMode = true;

    [Header("Vehicle Spawner Settings")]
    [SerializeField] SpawnerSettings spawnerSettings;

    [SerializeField][HideInInspector] private List<DebugNode> bakedRoadElements;
    
    #if UNITY_EDITOR
    public void CreateRoadElement()
    {
        var gameObject = new GameObject("RoadElement",typeof(RoadElement));
        gameObject.transform.SetParent(transform);
        Selection.activeGameObject = gameObject;
        EditorGUIUtility.PingObject(gameObject);
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
            sceneView.FrameSelected();  
    }
    public void GenerateSubScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
    
        var entitiesReferences = new GameObject("EntitiesReferences",typeof(EntitiesReferencesAuthoring));
        entitiesReferences.GetComponent<EntitiesReferencesAuthoring>().vehiclePrefab = spawnerSettings.carPrefab; 
        SceneManager.MoveGameObjectToScene(entitiesReferences, scene);

        if(EditorSceneManager.SaveScene(scene,subScenePath))
        {
            if(EditorSceneManager.CloseScene(scene,true))
            {
                if(subScene != null)
                    DestroyImmediate(subScene.gameObject);

                var subSceneGameObject = new GameObject("SubScene");
                subScene = subSceneGameObject.AddComponent<SubScene>();
                subScene.SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(subScenePath);
            }
        }
        else
            EditorSceneManager.CloseScene(scene,true);
    }
    public void BakeRoadNetwork()
    {
        if (subScene == null || subScene.SceneAsset == null) return;
        editMode = false;
    
        Scene scene = SceneManager.GetSceneByPath(AssetDatabase.GetAssetPath(subScene.SceneAsset));

        if (!scene.isLoaded)
            scene = EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(subScene.SceneAsset), OpenSceneMode.Additive);

        for(int i = transform.childCount -1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            child.SetParent(null);
            var roadElement = child.GetComponent<RoadElement>();
            roadElement.rootNode.VisitEdges(connection =>
            {
                bakedRoadElements.Add(new DebugNode()
                {
                    width = roadElement.Width, 
                    startPoint = child.transform.TransformPoint(connection.nodeA.Position),
                    endPoint = child.transform.TransformPoint(connection.nodeB.Position),
                    startHandler = child.transform.TransformPoint(connection.WorldHandleA),
                    endHandler = child.transform.TransformPoint(connection.WorldHandleB),
                });
            });
            child.AddComponent<RoadElementAuthoring>();
            child.GetComponent<RoadElementAuthoring>().settings = spawnerSettings;
            
            SceneManager.MoveGameObjectToScene(child.gameObject, scene);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, true);
    }
    public void OpenEditMode()
    {
        if (subScene == null || subScene.SceneAsset == null) return;
        editMode = true;
    
        Scene scene = SceneManager.GetSceneByPath(AssetDatabase.GetAssetPath(subScene.SceneAsset));

        if (!scene.isLoaded)
            scene = EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(subScene.SceneAsset), OpenSceneMode.Additive);

        Scene targetScene = gameObject.scene;
        var roadElements = FindObjectsByType<RoadElement>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach(var element in roadElements)
        {
            if (element.gameObject.scene != subScene.EditingScene)
                continue;
            element.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(element.gameObject, targetScene);
            element.transform.SetParent(transform);

            if(element.TryGetComponent<RoadElementAuthoring>(out var component))
                DestroyImmediate(component);
        }
        bakedRoadElements.Clear();

        EditorSceneManager.MarkSceneDirty(targetScene);
        EditorSceneManager.SaveScene(targetScene);      
        
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, true);  
    }
#endif


    public void OnDrawGizmos()
    {
        if(bakedRoadElements == null) return;
        foreach(var i in bakedRoadElements)
        {
            Handles.color = Color.white;
            Handles.DrawBezier(i.startPoint,i.endPoint, i.startHandler,i.endHandler, Color.white, null, 5f);   
            Handles.DrawAAPolyLine(BezierUtility.GetOffsetBezier(i.startPoint,i.startHandler, i.endHandler,i.endPoint,null,null, -i.width * 0.5f, 30).ToArray());
            Handles.DrawAAPolyLine(BezierUtility.GetOffsetBezier(i.startPoint,i.startHandler, i.endHandler,i.endPoint,null,null, i.width * 0.5f, 30).ToArray());
        }
    }
}

