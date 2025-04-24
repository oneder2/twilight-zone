using UnityEngine;
using TMPro; // Or UnityEngine.UI if using standard Text
using System.Collections;

public class DialogueGUI : Singleton<DialogueGUI> // Assuming it's a Singleton
{
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText; // Or Text
    [SerializeField] private float displayTimePerLine = 3f; // Time for single lines shown via ShowDialogue(string)

    // --- NEW: Flag to indicate if dialogue is currently active ---
    /// <summary>
    /// Gets whether the dialogue UI is currently active and potentially waiting for input or timer.
    /// </summary>
    public bool IsDialogueActive { get; private set; } = false;
    // --- End New Flag ---

    private Coroutine currentDialogueCoroutine = null;
    private string[] currentLines;
    private int currentLineIndex;
    private bool waitingForInput = false; // Flag for multi-line input wait


    /// <summary>
    /// Shows a single line of dialogue for a fixed duration.
    /// </summary>
    public void ShowDialogue(string text)
    {
        StopExistingDialogue(); // Stop previous dialogue if any

        currentDialogueCoroutine = StartCoroutine(ShowSingleLineCoroutine(text));
    }

    /// <summary>
    /// Shows multiple lines of dialogue, requiring input (e.g., Space) to proceed.
    /// </summary>
    public void ShowDialogue(string[] lines)
    {
         StopExistingDialogue();

         if (lines == null || lines.Length == 0) return;

         currentDialogueCoroutine = StartCoroutine(ShowMultipleLinesCoroutine(lines));
    }

    // --- Coroutine for single line ---
    private IEnumerator ShowSingleLineCoroutine(string text)
    {
        IsDialogueActive = true; // Mark as active
        dialogueText.text = text;
        dialoguePanel.SetActive(true);

        yield return new WaitForSeconds(displayTimePerLine); // Wait for display time

        HideDialogue(); // Automatically hide after duration
    }

    // --- Coroutine for multiple lines ---
    private IEnumerator ShowMultipleLinesCoroutine(string[] lines)
    {
         IsDialogueActive = true; // Mark as active
         currentLines = lines;
         currentLineIndex = 0;
         dialoguePanel.SetActive(true);
         DisplayCurrentLine();

         // Wait until all lines are shown (loop managed by Update checking 'waitingForInput')
         // This coroutine itself doesn't need to yield much here, the state is managed by Update
         // We just need to wait until IsDialogueActive becomes false again.
         yield return new WaitUntil(() => !IsDialogueActive);
    }


    // --- Handle input for multi-line dialogue ---
    void Update()
    {
         // Only check for input if multi-line dialogue is active and waiting
         if (IsDialogueActive && waitingForInput)
         {
              // Use your Input Manager or direct key check
              if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)) // Example proceed keys
              {
                   ProceedToNextLine();
              }
         }
    }

    private void DisplayCurrentLine()
    {
        if (currentLineIndex < currentLines.Length)
        {
            dialogueText.text = currentLines[currentLineIndex];
            waitingForInput = true; // Wait for input to proceed to next line
        }
        else
        {
            // All lines shown
            HideDialogue();
        }
    }

    private void ProceedToNextLine()
    {
         currentLineIndex++;
         waitingForInput = false; // Stop waiting for input briefly
         DisplayCurrentLine();
    }


    // --- Helper Methods ---
    private void StopExistingDialogue()
    {
        if (currentDialogueCoroutine != null)
        {
            StopCoroutine(currentDialogueCoroutine);
            currentDialogueCoroutine = null;
        }
        // Reset flags immediately if stopping
        // IsDialogueActive = false; // Let HideDialogue handle this
        // waitingForInput = false;
    }

    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
        IsDialogueActive = false; // Mark as inactive
        waitingForInput = false;
        currentLines = null;
        // Optional: Trigger an event indicating dialogue ended
        // EventManager.Instance?.TriggerEvent(new DialogueEndedEvent());
        // Note: GameRunManager state change should happen *after* cutscene coroutine finishes,
        // not necessarily right when dialogue ends unless dialogue IS the whole cutscene.
    }
}
