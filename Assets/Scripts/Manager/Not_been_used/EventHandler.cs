using System;
using UnityEngine;

public static class EventHandler
{
    public static event Action<Item, int> UpdateUIEvent; 
    public static void CallUpdateUIEvent(Item item, int index)
    {
        UpdateUIEvent?.Invoke(item, index);
    }
    
    public static event Action BeforeSceneUnloadEvent;
    public static void CallBeforeSceneUnloadEvent()
    {
        BeforeSceneUnloadEvent?.Invoke();
    }

    public static event Action AfterSceneUnloadEvent;
    public static void CallAfterSceneUnloadEvent()
    {
        AfterSceneUnloadEvent?.Invoke();
    }


}