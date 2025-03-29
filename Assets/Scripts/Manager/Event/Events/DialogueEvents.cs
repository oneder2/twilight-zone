public class DialogueStateChangedEvent
{
    public bool IsInDialogue { get; private set; }

    public DialogueStateChangedEvent(bool isInDialogue)
    {
        IsInDialogue = isInDialogue;
    }
}