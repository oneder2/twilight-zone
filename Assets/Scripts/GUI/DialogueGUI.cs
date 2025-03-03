using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueGUI : MonoBehaviour
{
    // 单例模式，方便全局访问
    public static DialogueGUI canvas { get; private set; }

    [SerializeField] private GameObject dialoguePanel;  // 对话框Panel
    [SerializeField] private TextMeshProUGUI dialogueText;         // 显示文字的Text组件
    [SerializeField] private float displayTime = 3f;    // 文字显示的持续时间（秒）

    private void Awake()
    {
        // 设置单例
        if (canvas == null)
        {
            canvas = this;
            DontDestroyOnLoad(gameObject); // 可选：跨场景保留
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 显示对话框并设置文字
    public void ShowDialogue(string text)
    {
        dialogueText.text = text;           // 更新文字内容
        dialoguePanel.SetActive(true);      // 显示对话框
        Invoke("HideDialogue", displayTime); // 延时隐藏
    }

    // 隐藏对话框
    private void HideDialogue()
    {
        dialoguePanel.SetActive(false);
    }
}
