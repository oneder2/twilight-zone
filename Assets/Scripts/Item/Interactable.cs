using UnityEngine;

public class Interactable : MonoBehaviour
{
    // 虚方法，供子类重写具体的交互逻辑
    public virtual void Interact()
    {
        Debug.Log("与 " + gameObject.name + " 交互");
    }
}