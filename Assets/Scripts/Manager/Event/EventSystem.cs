using System.Collections;
using UnityEngine;


public class EventSystem : MonoBehaviour
{
    public static EventSystem Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}