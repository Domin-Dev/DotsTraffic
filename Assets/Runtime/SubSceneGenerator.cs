using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SubSceneGenerator : MonoBehaviour
{
    [SerializeField] private string SubScenePath = "Assets/Scenes/TrafficSubScene.unity";
    public SubScene subScene;

    #if UNITY_EDITOR
    public void CreateRoadElement()
    {
        var gameObject = new GameObject("RoadElement",typeof(RoadElement));
        gameObject.transform.SetParent(transform);
        Selection.activeGameObject = gameObject;
        EditorGUIUtility.PingObject(gameObject);
    }
    public void GenerateSubScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        if(EditorSceneManager.SaveScene(scene,SubScenePath))
        {
            if(EditorSceneManager.CloseScene(scene,true))
            {
                if(subScene != null)
                    DestroyImmediate(subScene.gameObject);

                var subSceneGameObject = new GameObject("SubScene");
                subScene = subSceneGameObject.AddComponent<SubScene>();
                subScene.SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(SubScenePath);
            }
        }
        else
            EditorSceneManager.CloseScene(scene,true);
    }
    #endif
}

