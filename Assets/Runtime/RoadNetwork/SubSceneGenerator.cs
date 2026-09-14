using Unity.Scenes;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SubSceneGenerator : MonoBehaviour
{
    [SerializeField] private string subScenePath = "Assets/Scenes/TrafficSubScene.unity";
    public SubScene subScene;
    public LayerMask surfaceLayer = 1 << 0;
    public RoadSpace roadSpace;
    [HideInInspector] public bool editMode = true;
    
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
        if (subScene == null || subScene.SceneAsset == null)return;
        Scene scene = SceneManager.GetSceneByPath(AssetDatabase.GetAssetPath(subScene.SceneAsset));

        if (!scene.isLoaded)
            scene = EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(subScene.SceneAsset), OpenSceneMode.Additive);

        for(int i = transform.childCount -1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            child.SetParent(null);
           // child.AddComponent<RoadElement>();
            SceneManager.MoveGameObjectToScene(child.gameObject, scene);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, true);
    }
    #endif
}

