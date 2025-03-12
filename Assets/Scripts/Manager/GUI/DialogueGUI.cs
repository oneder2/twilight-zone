using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueGUI : Singleton<DialogueGUI>
{
    [SerializeField] private GameObject dialoguePanel;  // 对话框Panel
    [SerializeField] private TextMeshProUGUI dialogueText;  // 显示文字的Text组件

    private string[] dialogueLines;
    private int currentLine = 0;
    private bool canProceed = true;

    [SerializeField] private float displayTime = 3f;    // 文字显示的持续时间（秒）
    private Coroutine hideCoroutine; // 存储当前正在运行的隐藏协程

    public void ShowDialogue(string text)
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        dialogueText.text = text;           // 更新文字内容
        dialoguePanel.SetActive(true);      // 显示对话框

        hideCoroutine = StartCoroutine(HideDialogueAfterDelay());
    }

    public void ShowDialogue(string[] lines)
    {
        if (dialoguePanel.activeSelf)
        {
            Debug.LogWarning("对话框已在显示中");
            return;
        }

        dialogueLines = lines;
        currentLine = 0;
        dialoguePanel.SetActive(true);
        DisplayCurrentLine();
    }

    private void DisplayCurrentLine()
    {
        if (currentLine < dialogueLines.Length)
        {
            dialogueText.text = dialogueLines[currentLine];
        }
        else
        {
            HideDialogue();
        }
    }

    private void Update()
    {
        if (dialoguePanel.activeSelf && canProceed && Input.GetKeyDown(KeyCode.Space))
        {
            currentLine++;
            DisplayCurrentLine();
            canProceed = false;
            StartCoroutine(EnableProceedAfterDelay(0.2f));
        }
    }

    // 协程：延时隐藏对话框
    private IEnumerator HideDialogueAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);
        HideDialogue();
    }

    private IEnumerator EnableProceedAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canProceed = true;
    }

    private void HideDialogue()
    {
        dialoguePanel.SetActive(false);
        GameManager.instance.isInDialogue = false;
    }
}