using System.Collections;
using UnityEngine;
// Import the URP namespace to access Light2D 
// 导入 URP 命名空间以访问 Light2D
using UnityEngine.Rendering.Universal; 

/// <summary>
/// Manages the global 2D lighting in the scene.
/// Uses a Singleton pattern for easy access.
/// 管理场景中的全局 2D 光照。
/// 使用单例模式方便访问。
/// </summary>
public class LightManager : Singleton<LightManager> // Ensure you have a valid Singleton base class
                                                     // 确保你有一个有效的 Singleton 基类
{
    // Serialized field to assign the global Light2D component in the Unity Inspector.
    // 序列化字段，用于在 Unity 检查器中指定全局 Light2D 组件。
    [SerializeField] 
    [Tooltip("Assign the global Light2D component here.")] // Tooltip for the Inspector
    // [Tooltip("请在此处指定全局 Light2D 组件。")] // 检查器提示
    private Light2D globalLight; 

    /// <summary>
    /// Public method to initiate a smooth transition of the global light settings.
    /// 公共方法，用于启动全局光照设置的平滑过渡。
    /// </summary>
    /// <param name="lightIntensity">The target intensity for the light. / 光照的目标强度。</param>
    /// <param name="lightColor">The target color for the light. / 光照的目标颜色。</param>
    public void UpdateLighting(float lightIntensity, Color lightColor)
    {
        // Stop any previously running lighting coroutine to avoid conflicts.
        // 停止任何先前运行的光照协程以避免冲突。
        StopAllCoroutines(); // Consider if stopping ALL coroutines is desired, or just the lighting one.
                             // 考虑是否需要停止所有协程，或者只停止光照相关的协程。

        // Start the coroutine to handle the smooth transition (Lerp).
        // 启动协程来处理平滑过渡（插值）。
        StartCoroutine(LerpLighting(lightIntensity, lightColor, 1f)); // Default duration is 1 second.
                                                                       // 默认过渡时间为 1 秒。
    }

    /// <summary>
    /// Coroutine to smoothly interpolate the light's intensity and color over a given duration.
    /// 协程，用于在给定时间内平滑地插值光照的强度和颜色。
    /// </summary>
    /// <param name="targetIntensity">The final intensity value. / 最终的强度值。</param>
    /// <param name="targetColor">The final color value. / 最终的颜色值。</param>
    /// <param name="duration">The time in seconds the transition should take. / 过渡所需的时间（秒）。</param>
    /// <returns>IEnumerator for the coroutine. / 用于协程的 IEnumerator。</returns>
    IEnumerator LerpLighting(float targetIntensity, Color targetColor, float duration)
    {
        // Check if globalLight is assigned to prevent NullReferenceException.
        // 检查 globalLight 是否已分配，以防止 NullReferenceException。
        if (globalLight == null)
        {
            Debug.LogError("[LightManager] Global Light2D is not assigned in the Inspector!");
            // Debug.LogError("[LightManager] 全局 Light2D 未在检查器中指定！");
            yield break; // Exit the coroutine if the light is missing.
                         // 如果光照丢失，则退出协程。
        }

        // Record the starting time and initial light properties.
        // 记录开始时间和初始的光照属性。
        float time = 0;
        float startIntensity = globalLight.intensity;
        Color startColor = globalLight.color;

        // Loop until the elapsed time reaches the specified duration.
        // 循环直到经过的时间达到指定的持续时间。
        while (time < duration)
        {
            // Increment the elapsed time by the time since the last frame.
            // 将经过的时间增加自上一帧以来的时间。
            time += Time.deltaTime;

            // Calculate the interpolation factor (0 to 1).
            // 计算插值因子（0 到 1）。
            float t = Mathf.Clamp01(time / duration); // Use Clamp01 to ensure t stays between 0 and 1.
                                                      // 使用 Clamp01 确保 t 保持在 0 和 1 之间。

            // Interpolate the intensity and color based on the factor t.
            // 根据因子 t 插值强度和颜色。
            globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            globalLight.color = Color.Lerp(startColor, targetColor, t);

            // Pause the coroutine and resume on the next frame.
            // 暂停协程并在下一帧恢复。
            yield return null; 
        }

        // Ensure the final values are set exactly at the end of the duration.
        // 确保在持续时间结束时精确设置最终值。
        globalLight.intensity = targetIntensity;
        globalLight.color = targetColor;
    }
}
