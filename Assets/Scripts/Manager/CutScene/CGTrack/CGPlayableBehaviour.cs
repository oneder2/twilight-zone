using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Runtime logic for a CG clip. Interacts with CutsceneUIManager
/// to show and hide the specified CG using its crossfade methods.
/// CG片段的运行时逻辑。与 CutsceneUIManager 交互
/// 使用其交叉淡入淡出方法显示和隐藏指定的CG。
/// </summary>
public class CGPlayableBehaviour : PlayableBehaviour
{
    // --- Data from CGPlayableAsset ---
    public string CgIdentifier { get; set; }
    public float FadeInDuration { get; set; }
    public float FadeOutDuration { get; set; }

    // --- Runtime Reference ---
    // Use the concrete type from your project
    // 使用项目中的具体类型
    public CutsceneUIManager CGManager { get; set; } // Set by the CGTrackMixerBehaviour

    private bool isFirstFrame = true;
    private bool fadeOutTriggered = false; // Ensure fade out is triggered only once

    // --- PlayableBehaviour Lifecycle ---

    public override void OnGraphStart(Playable playable)
    {
        isFirstFrame = true;
        fadeOutTriggered = false;
    }

     public override void OnGraphStop(Playable playable)
    {
        // Optional: Add cleanup logic if needed when graph stops abruptly
        // 可选：如果图突然停止，根据需要添加清理逻辑
        isFirstFrame = true;
        fadeOutTriggered = false;
    }


    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        isFirstFrame = true;
        fadeOutTriggered = false;
    }

    /// <summary>
    /// Called when the behaviour becomes inactive (playable time leaves the clip duration).
    /// Triggers the fade-out of all CGs if fade-out hasn't been triggered already.
    /// 当行为变为非活动状态时调用（播放时间离开片段持续时间）。
    /// 如果尚未触发淡出，则触发所有CG的淡出。
    /// </summary>
    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (CGManager != null && !fadeOutTriggered)
        {
            // If the clip ends or is interrupted before fade-out started naturally near the end.
            Debug.Log($"[CGPlayableBehaviour] Paused/Ended. Triggering fade out for all CGs via OnBehaviourPause (using duration {FadeOutDuration}).");
            // Call the method to hide all CGs using the fade duration from the asset
            // 调用隐藏所有CG的方法，使用资产中的淡出时长
            CGManager.HideAllFullscreenCGs(FadeOutDuration);
            fadeOutTriggered = true; // Mark as triggered
        }
        isFirstFrame = true; // Reset for next time
    }

    /// <summary>
    /// Called every frame the playable is active, before ProcessFrame.
    /// Initiates showing the specific CG on the first active frame.
    /// 在 ProcessFrame 之前，每个 Playable 活动的帧都会调用。
    /// 在第一个活动帧上启动显示特定CG。
    /// </summary>
    public override void PrepareFrame(Playable playable, FrameData info)
    {
        if (CGManager == null) return;

        // Trigger show/crossfade only on the very first frame the behaviour is active.
        if (isFirstFrame && info.weight > 0f)
        {
            isFirstFrame = false;
            fadeOutTriggered = false; // Reset fade out state

            Debug.Log($"[CGPlayableBehaviour] Starting. Triggering show for '{CgIdentifier}' with duration {FadeInDuration}.");
            // Tell the CutsceneUIManager to show this specific CG
            // 告知 CutsceneUIManager 显示此特定CG
            CGManager.ShowFullscreenCG(CgIdentifier, FadeInDuration);
        }
    }

     /// <summary>
    /// Called every frame the playable is active.
    /// Can be used to trigger fade-out slightly before the clip actually ends.
    /// 每个 Playable 活动的帧都会调用。
    /// 可用于在片段实际结束前稍微触发淡出。
    /// </summary>
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (CGManager == null || fadeOutTriggered) return;

        double time = playable.GetTime();
        double duration = playable.GetDuration();
        // Calculate when fade-out should start based on clip duration and fade-out time
        double fadeOutStartTime = duration - FadeOutDuration;

        // Check if it's time to start fading out (and duration is long enough for fade out)
        if (FadeOutDuration >= 0 && duration > FadeOutDuration && time >= fadeOutStartTime) // Ensure fadeOutDuration is valid
        {
            Debug.Log($"[CGPlayableBehaviour] Nearing end. Triggering fade out for all CGs via ProcessFrame (using duration {FadeOutDuration}).");
            // Call the method to hide all CGs using the fade duration from the asset
            CGManager.HideAllFullscreenCGs(FadeOutDuration);
            fadeOutTriggered = true; // Mark as triggered so OnPause doesn't do it again
        }
    }
}
