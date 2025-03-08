using System;
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
            FindFirstObjectByType<Player>().transform.position = _spawnpoint.position;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // set level connection (red spot)
        LevelConnection.ActiveConnection = _connection;
        // Find player in collission list
        var player = other.collider.GetComponent<Player>();
        //  if player is making collision
        if (player != null)
        {
            currentSceneName = SceneManager.GetActiveScene().name;
            Debug.Log((currentSceneName, _targetSceneName));
            TransitionManager.Instance.Teleport(currentSceneName, _targetSceneName);
        }
    }
} 
