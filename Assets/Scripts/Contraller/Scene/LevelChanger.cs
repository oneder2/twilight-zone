using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelChanger : MonoBehaviour
{
    [SerializeField] private LevelConnection _connection;
    [SerializeField] private string _targetSceneName;
    [SerializeField] private Transform _spawnpoint;
    [SerializeField] private string [] scenes = {};
    private Scene currentScene;
    private string currentSceneName;

    void Start()
    {
        if (_connection == LevelConnection.ActiveConnection)
        {
            FindObjectOfType<Player>().transform.position = _spawnpoint.position;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // Find player in collission list
        var player = other.collider.GetComponent<Player>();
        //  if player is making collision
        if (player != null)
        {
            Teleport(currentSceneName, _targetSceneName);
        }
    }

    private void Teleport(string from, string to)
    {
        // 获取当前场景
        currentScene = SceneManager.GetActiveScene();
        // 获取场景名称
        currentSceneName = currentScene.name;
        LevelConnection.ActiveConnection = _connection;
        StartCoroutine(TransformToScene(currentSceneName, _targetSceneName));
    }

    private IEnumerator TransformToScene(string from, string to)
    {
        Debug.Log("Changing");
        yield return SceneManager.LoadSceneAsync(to, LoadSceneMode.Additive);
        yield return SceneManager.UnloadSceneAsync(from);
        
        Scene newScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
        SceneManager.SetActiveScene(newScene);
        // EventHandler.CallBeforeSceneUnloadEvent();
        // EventHandler.CallAfterSceneUnloadEvent();
    }
} 
