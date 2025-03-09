using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
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
} 