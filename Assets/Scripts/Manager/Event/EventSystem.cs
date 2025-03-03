using System.Collections;
using UnityEngine;


public class EventSystem : MonoBehaviour
{
    public static EventSystem eventSystem { get; private set; }

    void Awake()
    {
        if (eventSystem == null)
        {
            eventSystem = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}