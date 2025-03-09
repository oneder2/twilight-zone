using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

// <summary>
// LedderLevelChanger araise API of TransitionManager, do teleport when player collided on it
// Inherite from LevelChanger, find the position of Player
// </summary>

public class LedderLevelChanger : MonoBehaviour
{    
    [SerializeField] protected SceneConnection connection;
    [SerializeField] protected string targetSceneName;
    [SerializeField] protected Transform spawnpoint;
    protected Scene currentScene;
    protected string currentSceneName;

    void Start()
    {
        if (connection == SceneConnection.ActiveConnection)
        {
            FindFirstObjectByType<Player>().transform.position = spawnpoint.position;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // set level connection (red spot)
        SceneConnection.ActiveConnection = connection;
        // Find player in collission list
        var player = other.collider.GetComponent<Player>();
        //  if player is making collision
        if (player != null && !TransitionManager.Instance.isFade)
        {
            currentSceneName = SceneManager.GetActiveScene().name;
            TransitionManager.Instance.Teleport(currentSceneName, targetSceneName);
        }
    }
} 
