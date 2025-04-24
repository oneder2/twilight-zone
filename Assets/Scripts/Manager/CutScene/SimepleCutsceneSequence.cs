using UnityEngine;
using System; // Required for Action

/// <summary>
/// Defines the data for a simple Cutscene sequence using fullscreen CGs and dialogue.
/// 用于定义使用全屏 CG 和对话的简单过场动画序列的数据。
/// </summary>
[CreateAssetMenu(fileName = "New Simple CG Sequence", menuName = "Game/Simple CG Sequence")]
public class SimpleCGSequence : ScriptableObject
{
    [System.Serializable]
    public struct CGStep // Represents one image and its associated dialogue // 代表一个图像及其关联的对话
    {
        [Tooltip("The fullscreen CG image to display for this step.")] // 此步骤要显示的全屏 CG 图像。
        public Sprite cgSprite;
        [Tooltip("The dialogue lines to show while this CG is displayed.")] // 显示此 CG 时要显示的对话行。
        [TextArea(3, 10)] // Makes it easier to edit lines in the inspector // 使在检查器中编辑行更容易
        public string[] dialogueLines;
    }

    [Tooltip("The list of CG steps in this sequence.")] // 此序列中的 CG 步骤列表。
    public CGStep[] steps;

    [Tooltip("Optional identifier for logic to run after the sequence finishes (e.g., setting game flags).")] // 用于在序列结束后运行逻辑的可选标识符（例如，设置游戏标志）。
    public string postSequenceLogicKey = ""; // Example: "CrushSuccessOutcome" // 示例：“CrushSuccessOutcome”

    // Note: We removed the direct Action reference from the SO.
    // The action will be passed in when starting the sequence.
    // 注意：我们从 SO 中移除了直接的 Action 引用。
    // 操作将在启动序列时传入。
}
