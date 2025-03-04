using UnityEngine;

public class Interactable : MonoBehaviour
{
    // 虚方法，供子类重写具体的交互逻辑
    public virtual void Interact()
    {
        Debug.Log("与 " + gameObject.name + " 交互");
    }

    // 获取评价文字的虚拟方法
    public virtual string GetDialogue()
    {
        return "这是一个物体。";
    }
}