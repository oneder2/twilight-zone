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

    void Start()
    {
        if (_connection == LevelConnection.ActiveConnection)
        {
            FindObjectOfType<Player>().transform.position = _spawnpoint.position;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        Debug.Log("Hovered");
        var player = other.collider.GetComponent<Player>();
        if (player != null)
        {
            LevelConnection.ActiveConnection = _connection;
            SceneManager.LoadScene(_targetSceneName);
        }
    }    
} 
