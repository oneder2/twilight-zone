using Unity.VisualScripting;
using UnityEngine;

public class ItemCheckUp : Item
{
    public bool hasBeenChecked = false;
    
    public override void Interact()
    {
        GameManager.instance.isInteracting = true;  // 标记为交互中，但不暂停时间

        if (!hasBeenChecked)
        {
            DialogueGUI.Instance.ShowDialogue(itemData.commend);
            hasBeenChecked = true;
        }
        DialogueGUI.Instance.ShowDialogue("...");

        GameManager.instance.isInteracting = false;  // 交互立即结束
    }

    public override string GetDialogue()
    {
        return itemData.commend;
    }
}