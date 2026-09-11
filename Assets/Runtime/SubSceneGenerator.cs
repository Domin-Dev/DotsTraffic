using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SubSceneGenerator : MonoBehaviour
{
    [SerializeField] private string subScenePath = "Assets/Scenes/TrafficSubScene.unity";
    public SubScene subScene;
    public LayerMask surfaceLayer = 1 << 0;
    public RoadSpace roadSpace;
    
    #if UNITY_EDITOR
    public void CreateRoadElement()
    {
        var gameObject = new GameObject("RoadElement",typeof(RoadElement));
        gameObject.transform.SetParent(transform);

        var roadElement = gameObject.GetComponent<RoadElement>();
        roadElement.Points.Add(new RoadPoint(new Vector3(-1,0,0)));
        roadElement.Points.Add(new RoadPoint(new Vector3(1,0,0)));

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

    #endif
}

