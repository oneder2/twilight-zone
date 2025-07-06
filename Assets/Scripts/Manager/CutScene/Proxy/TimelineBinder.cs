using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline; // Required for TimelineAsset and TrackAsset types

/// <summary>
/// Dynamically binds Timeline tracks to persistent Singleton managers after the scene loads.
/// Handles binding for both DialogueTrack (to DialogueManager) and CGTrack (to CutsceneUIManager).
/// 场景加载后动态地将 Timeline 轨道绑定到持久化的 Singleton 管理器。
/// 处理 DialogueTrack (到 DialogueManager) 和 CGTrack (到 CutsceneUIManager) 的绑定。
/// Attach this component to the GameObject containing the PlayableDirector for the cutscene.
/// 将此组件附加到包含过场动画 PlayableDirector 的 GameObject 上。
/// </summary>
public class TimelineBinder : MonoBehaviour
{
    [Tooltip("The PlayableDirector component for this cutscene's Timeline.")]
    [SerializeField] private PlayableDirector director;

    void Start()
    {
        // Attempt to get the director component if not assigned
        // 如果未分配，尝试获取 director 组件
        if (director == null)
        {
            director = GetComponent<PlayableDirector>();
        }

        if (director == null)
        {
            Debug.LogError("[TimelineBinder] PlayableDirector component not found on this GameObject. Binding cannot proceed.", this);
            return;
        }

        // Get the TimelineAsset associated with the director
        // 获取与 director 关联的 TimelineAsset
        TimelineAsset timelineAsset = director.playableAsset as TimelineAsset;
        if (timelineAsset == null)
        {
             Debug.LogError("[TimelineBinder] PlayableDirector does not have a valid TimelineAsset assigned. Binding cannot proceed.", director);
             return;
        }

        Debug.Log($"[TimelineBinder] Starting binding process for Timeline: {timelineAsset.name}");

        // Get references to the persistent managers (assuming Singleton pattern)
        // 获取对持久化管理器的引用（假设为 Singleton 模式）
        DialogueManager dialogueManagerInstance = DialogueManager.Instance;
        CutsceneUIManager cgManagerInstance = CutsceneUIManager.Instance; // Use your actual class name / 使用你的实际类名

        bool dialogueManagerBound = false;
        bool cgManagerBound = false;

        // Iterate through all tracks in the Timeline asset
        // 遍历 Timeline 资源中的所有轨道
        foreach (var track in timelineAsset.GetOutputTracks())
        {
            // --- Check for DialogueTrack ---
            // --- 检查 DialogueTrack ---
            if (track is DialogueTrack dialogueTrack) // Use 'is' for type checking and casting / 使用 'is' 进行类型检查和转换
            {
                if (dialogueManagerInstance != null)
                {
                    Debug.Log($"[TimelineBinder] Found DialogueTrack: '{dialogueTrack.name}'. Binding to DialogueManager instance.");
                    // Bind the DialogueTrack to the DialogueManager instance
                    // 将 DialogueTrack 绑定到 DialogueManager 实例
                    director.SetGenericBinding(dialogueTrack, dialogueManagerInstance);
                    dialogueManagerBound = true; // Mark as bound / 标记为已绑定
                }
                else
                {
                    // Log error only if DialogueManager is expected but not found
                    // 仅当期望 DialogueManager 但未找到时记录错误
                    Debug.LogError($"[TimelineBinder] Found DialogueTrack '{dialogueTrack.name}' but DialogueManager.Instance is null! Cannot bind.", this);
                }
                // Continue to next track even if bound, in case of multiple tracks (though unlikely needed)
                // 即使已绑定也继续到下一个轨道，以防有多个轨道（虽然不太可能需要）
            }

            // --- Check for CGTrack ---
            // --- 检查 CGTrack ---
            else if (track is CGTrack cgTrack) // Use 'is' for type checking and casting / 使用 'is' 进行类型检查和转换
            {
                if (cgManagerInstance != null)
                {
                    Debug.Log($"[TimelineBinder] Found CGTrack: '{cgTrack.name}'. Binding to CutsceneUIManager instance.");
                    // Bind the CGTrack to the CutsceneUIManager instance
                    // 将 CGTrack 绑定到 CutsceneUIManager 实例
                    director.SetGenericBinding(cgTrack, cgManagerInstance);
                    cgManagerBound = true; // Mark as bound / 标记为已绑定
                }
                else
                {
                    // Log error only if CutsceneUIManager is expected but not found
                    // 仅当期望 CutsceneUIManager 但未找到时记录错误
                    Debug.LogError($"[TimelineBinder] Found CGTrack '{cgTrack.name}' but CutsceneUIManager.Instance is null! Cannot bind.", this);
                }
                // Continue to next track
                // 继续到下一个轨道
            }
        }

        // Optional: Log summary after checking all tracks
        // 可选：检查完所有轨道后记录摘要
        if (!dialogueManagerBound && timelineAsset.GetOutputTracks().Any(t => t is DialogueTrack)) {
             Debug.LogWarning($"[TimelineBinder] Finished binding process. A DialogueTrack exists but DialogueManager was not found or bound.");
        }
        if (!cgManagerBound && timelineAsset.GetOutputTracks().Any(t => t is CGTrack)) {
             Debug.LogWarning($"[TimelineBinder] Finished binding process. A CGTrack exists but CutsceneUIManager was not found or bound.");
        }
         Debug.Log("[TimelineBinder] Binding process complete.");
    }
}
