using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{    
    public GUIManager guiManager;
    public EventManager eventManager;
    public GameSceneManager sceneManager; // 修正为 GameSceneManager

    public static GameManager instance;
    
    public bool isInDialogue = false;  // NPC对话状态，暂停时间
    public bool isInteracting = false; // 一般交互状态，不暂停时间

    public bool IsInDialogue { get { return isInDialogue; } }
    public void SetDialogueState(bool state) { isInDialogue = state; }

    override protected void Awake() { instance = this; }
}