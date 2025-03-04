using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueGUI : MonoBehaviour
{
    // 单例模式，方便全局访问
    public static DialogueGUI Instance;

    [SerializeField] private GameObject dialoguePanel;  // 对话框Panel
    [SerializeField] private TextMeshProUGUI dialogueText;         // 显示文字的Text组件
    [SerializeField] private float displayTime = 3f;    // 文字显示的持续时间（秒）

    private Coroutine hideCoroutine; // 存储当前正在运行的隐藏协程

    private void Awake()
    {
        // 设置单例
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 显示对话框并设置文字
    public void ShowDialogue(string text)
    {
        // 如果有正在运行的隐藏协程，先停止它
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        dialogueText.text = text;           // 更新文字内容
        dialoguePanel.SetActive(true);      // 显示对话框

        // 启动新的隐藏协程
        hideCoroutine = StartCoroutine(HideDialogueAfterDelay());
    }

    // 协程：延时隐藏对话框
    private IEnumerator HideDialogueAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);
        HideDialogue();
    }

    // 隐藏对话框
    private void HideDialogue()
    {
        dialoguePanel.SetActive(false);
        hideCoroutine = null; // 重置协程引用
    }
}