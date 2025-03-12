using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    void Update()
    {
        if (Input.GetButtonDown("Interact") && !GameManager.instance.isInDialogue)
        {
            // Raycast或OverlapCircle查找可交互对象
            // 调用其Interact方法
        }
    }
}