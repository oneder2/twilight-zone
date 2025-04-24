using UnityEngine;


public class GameManager : Singleton<GameManager>
{    
    private bool isInDialogue = false;  // NPC对话状态，暂停时间
    public bool isInteracting = false; // 一般交互状态，不暂停时间

    public bool IsInDialogue
    {
        get => isInDialogue;
        private set
        {
            if (isInDialogue != value) // 仅在状态变化时触发事件
            {
                isInDialogue = value;
                EventManager.Instance.TriggerEvent(new DialogueStateChangedEvent(isInDialogue));
                Debug.Log($"对话状态变为: {isInDialogue}");
            }
        }
    }
    public void SetDialogueState(bool state) { isInDialogue = state; }

    // 开始对话
    public void StartDialogue()
    {
        IsInDialogue = true;
    }

    // 结束对话
    public void EndDialogue()
    {
        IsInDialogue = false;
    }
}