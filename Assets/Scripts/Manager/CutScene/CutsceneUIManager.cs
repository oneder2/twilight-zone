using UnityEngine;
using UnityEngine.UI; // Required for Image, potentially Slider
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages UI elements specific to cutscenes, like letterbox bars and fullscreen images.
/// Assumed to be a Singleton within the GameRoot scene.
/// </summary>
public class CutsceneUIManager : Singleton<CutsceneUIManager> // Use your Singleton base
{
    // Letter box
    #region letter box
    [Header("Letterbox Elements")]
    [Tooltip("The UI element (Image/Panel) for the top letterbox bar.")]
    [SerializeField] private RectTransform topBar;
    [Tooltip("The UI element (Image/Panel) for the bottom letterbox bar.")]
    [SerializeField] private RectTransform bottomBar;
    [Tooltip("Animation speed/duration for the letterbox bars.")]
    [SerializeField] private float letterboxAnimDuration = 0.3f; // Changed to duration
    #endregion

    // --- 新增：全屏CG交叉淡入淡出 ---
    #region full screen CG
    // --- NEW: Fullscreen CG Crossfade ---
    [Header("全屏CG背景 (Fullscreen CG Background)")]
    [Tooltip("用于显示CG的背景Image组件 (Background Image component for CGs)")]
    [SerializeField] private Image cgBackgroundImage;
    [Tooltip("用于显示CG的前景Image组件 (Foreground Image component for CGs)")]
    [SerializeField] private Image cgForegroundImage;
    [Tooltip("背景Image的CanvasGroup (CanvasGroup for the background Image)")]
    [SerializeField] private CanvasGroup cgBackgroundCanvasGroup;
    [Tooltip("前景Image的CanvasGroup (CanvasGroup for the foreground Image)")]
    [SerializeField] private CanvasGroup cgForegroundCanvasGroup;

    [Tooltip("默认的CG交叉淡入淡出时长 (Default duration for CG crossfades)")]
    [SerializeField] private float defaultCgFadeDuration = 0.5f;

    // 存储CG标识符到Sprite的映射 (Store mapping from CG identifier to Sprite)
    // 你可以在 Inspector 中填充这个列表
    // You can populate this list in the Inspector
    [System.Serializable]
    public class CGEntry
    {
        public string identifier;
        public Sprite sprite;
    }
    [SerializeField] private List<CGEntry> cgLibrary = new List<CGEntry>();
    private Dictionary<string, Sprite> cgLookup = new Dictionary<string, Sprite>();

    private bool isForegroundCgActive = false; // 标记当前前景CG是否可见 (Flag indicating if the foreground CG is currently active/visible)
    private Coroutine cgFadeCoroutine = null; // 用于管理当前的淡入淡出协程 (To manage the current fade coroutine)
    // --- 结束新增部分 ---
    #endregion


    // Store the initial positions or heights for animation
    private float topBarHiddenY = 300f; // Example value, adjust based on screen height/bar height
    private float topBarShownY = 0f;
    private float bottomBarHiddenY = -300f;// Example value
    private float bottomBarShownY = 0f;
    private bool areBarsVisible = false;
    private Coroutine letterboxCoroutine = null;


    [Header("Fullscreen CG Elements")]
    [Tooltip("Image component used to display fullscreen CGs.")]
    [SerializeField] private Image fullscreenCgImage;
    [Tooltip("CanvasGroup for fading the fullscreen image.")]
    [SerializeField] private CanvasGroup fullscreenCgCanvasGroup;

    protected override void Awake()
    {
        base.Awake();
        // Initial setup - ensure elements are correctly positioned off-screen or hidden
        InitializeUIElements();
        InitializeCGLibrary(); // 初始化CG库
    }

    private void InitializeUIElements()
    {
         if (topBar == null || bottomBar == null) { Debug.LogError("Letterbox bars not assigned!", this); return; }
         if (fullscreenCgImage == null || fullscreenCgCanvasGroup == null) { Debug.LogError("Fullscreen CG elements not assigned!", this); return; }

         // Assuming top bar anchored top-center, bottom bar anchored bottom-center
         // Calculate hidden positions more reliably based on their height
         topBarShownY = topBar.anchoredPosition.y; // Assumes initial Inspector position is the 'shown' position relative to anchor
         topBarHiddenY = topBarShownY + topBar.rect.height;
         topBar.anchoredPosition = new Vector2(topBar.anchoredPosition.x, topBarHiddenY);

         bottomBarShownY = bottomBar.anchoredPosition.y;
         bottomBarHiddenY = bottomBarShownY - bottomBar.rect.height;
         bottomBar.anchoredPosition = new Vector2(bottomBar.anchoredPosition.x, bottomBarHiddenY);

         topBar.gameObject.SetActive(false);
         bottomBar.gameObject.SetActive(false);
         fullscreenCgImage.gameObject.SetActive(false);
         fullscreenCgCanvasGroup.alpha = 0f;
         areBarsVisible = false;
    }

    private void InitializeCGLibrary()
    // 将列表转换为字典以便快速查找
    // Convert the list to a dictionary for faster lookup
    {
        cgLookup.Clear();
        foreach(var entry in cgLibrary)
        {
            if (!string.IsNullOrEmpty(entry.identifier) && entry.sprite != null)
            {
                if (!cgLookup.ContainsKey(entry.identifier))
                {
                    cgLookup.Add(entry.identifier, entry.sprite);
                } else {
                    Debug.LogWarning($"[CutsceneUIManager] Duplicate CG identifier found in library: {entry.identifier}");
                }
            }
        }
        Debug.Log($"[CutsceneUIManager] CG Library initialized with {cgLookup.Count} entries.");
    }


    // --- 事件监听注册/取消注册 ---
    // --- Event Listener Registration/Unregistration ---
    void OnEnable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<ShowCGRequestedEvent>(HandleShowCGRequest);
            EventManager.Instance.AddListener<HideAllCGsRequestedEvent>(HandleHideCGRequest);
            Debug.Log("[CutsceneUIManager] Subscribed to CG request events.");
        } else {
            Debug.LogError("[CutsceneUIManager] EventManager not found on Enable! Cannot subscribe to CG events.");
        }
    }

    void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<ShowCGRequestedEvent>(HandleShowCGRequest);
            EventManager.Instance.RemoveListener<HideAllCGsRequestedEvent>(HandleHideCGRequest);
            Debug.Log("[CutsceneUIManager] Unsubscribed from CG request events.");
        }
    }


    // --- 事件处理器 ---
    // --- Event Handlers ---
    #region Event Handler
    private void HandleShowCGRequest(ShowCGRequestedEvent eventData)
    {
        if (eventData == null || string.IsNullOrEmpty(eventData.CgIdentifier))
        {
            Debug.LogWarning("[CutsceneUIManager] Received invalid ShowCGRequestedEvent.");
            return;
        }

        if (!cgLookup.TryGetValue(eventData.CgIdentifier, out Sprite spriteToShow))
        {
            Debug.LogError($"[CutsceneUIManager] CG with identifier '{eventData.CgIdentifier}' not found in library! Cannot process request.");
            return;
        }

        float duration = (eventData.FadeDuration >= 0) ? eventData.FadeDuration : defaultCgFadeDuration;
        Debug.Log($"[CutsceneUIManager] Handling ShowCGRequest. CG: '{eventData.CgIdentifier}', Duration: {duration}");

        // 停止任何正在进行的CG淡入淡出
        if (cgFadeCoroutine != null) StopCoroutine(cgFadeCoroutine);
        cgFadeCoroutine = StartCoroutine(CrossfadeCGCoroutine(spriteToShow, duration));
    }

    private void HandleHideCGRequest(HideAllCGsRequestedEvent eventData)
    {
        if (eventData == null) return;

        // 检查是否有CG正在显示
        if (cgBackgroundCanvasGroup.alpha == 0f && cgForegroundCanvasGroup.alpha == 0f)
        {
            Debug.Log("[CutsceneUIManager] Received HideAllCGsRequest, but no CGs are visible.");
            return;
        }

        float duration = (eventData.FadeDuration >= 0) ? eventData.FadeDuration : defaultCgFadeDuration;
        Debug.Log($"[CutsceneUIManager] Handling HideAllCGsRequest. Duration: {duration}");

        // 停止任何正在进行的CG淡入淡出
        if (cgFadeCoroutine != null) StopCoroutine(cgFadeCoroutine);
        cgFadeCoroutine = StartCoroutine(FadeOutAllCGsCoroutine(duration));
    }
    #endregion


    // --- Letter box ---
    #region Letter box
    /// <summary>
    /// Shows the letterbox bars.
    /// </summary>
    /// <param name="animated">Play an animation?</param>
    /// <returns>Coroutine for waiting (null if not animated or already visible).</returns>
    public Coroutine ShowLetterbox(bool animated = true)
    {
        if (areBarsVisible) return null;
        areBarsVisible = true;

        if (topBar) topBar.gameObject.SetActive(true);
        if (bottomBar) bottomBar.gameObject.SetActive(true);

        if (animated && letterboxAnimDuration > 0)
        {
            if (letterboxCoroutine != null) StopCoroutine(letterboxCoroutine);
            letterboxCoroutine = StartCoroutine(AnimateBars(topBarShownY, bottomBarShownY));
            return letterboxCoroutine;
        }
        else
        {
            if (topBar) topBar.anchoredPosition = new Vector2(topBar.anchoredPosition.x, topBarShownY);
            if (bottomBar) bottomBar.anchoredPosition = new Vector2(bottomBar.anchoredPosition.x, bottomBarShownY);
            return null;
        }
    }

    /// <summary>
    /// Hides the letterbox bars.
    /// </summary>
    /// <param name="animated">Play an animation?</param>
    /// <returns>Coroutine for waiting (null if not animated or already hidden).</returns>
    public Coroutine HideLetterbox(bool animated = true)
    {
        if (!areBarsVisible) return null;
        // Set flag immediately even if animating out
        areBarsVisible = false;

        if (animated && letterboxAnimDuration > 0)
        {
            if (letterboxCoroutine != null) StopCoroutine(letterboxCoroutine);
            // Pass true to disable GameObjects after animation
            letterboxCoroutine = StartCoroutine(AnimateBars(topBarHiddenY, bottomBarHiddenY, true));
            return letterboxCoroutine;
        }
        else
        {
            if (topBar) { topBar.anchoredPosition = new Vector2(topBar.anchoredPosition.x, topBarHiddenY); topBar.gameObject.SetActive(false); }
            if (bottomBar) { bottomBar.anchoredPosition = new Vector2(bottomBar.anchoredPosition.x, bottomBarHiddenY); bottomBar.gameObject.SetActive(false); }
            return null;
        }
    }

    // --- Private Animation Coroutines ---
    private IEnumerator AnimateBars(float targetTopY, float targetBottomY, bool disableAfter = false)
    {
        // Simple Lerp animation
        float timer = 0f;
        Vector2 startTopPos = topBar ? topBar.anchoredPosition : Vector2.zero;
        Vector2 startBottomPos = bottomBar ? bottomBar.anchoredPosition : Vector2.zero;
        Vector2 targetTopPos = topBar ? new Vector2(startTopPos.x, targetTopY) : Vector2.zero;
        Vector2 targetBottomPos = bottomBar ? new Vector2(startBottomPos.x, targetBottomY) : Vector2.zero;

        if (letterboxAnimDuration > 0.01f) {
             while (timer < letterboxAnimDuration)
             {
                  timer += Time.deltaTime;
                  float progress = Mathf.Clamp01(timer / letterboxAnimDuration);
                  // Could apply easing here, e.g., progress = Mathf.SmoothStep(0.0f, 1.0f, progress);
                  if (topBar) topBar.anchoredPosition = Vector2.Lerp(startTopPos, targetTopPos, progress);
                  if (bottomBar) bottomBar.anchoredPosition = Vector2.Lerp(startBottomPos, targetBottomPos, progress);
                  yield return null;
             }
        }

        // Ensure final position
        if (topBar) topBar.anchoredPosition = targetTopPos;
        if (bottomBar) bottomBar.anchoredPosition = targetBottomPos;

        if (disableAfter)
        {
            if (topBar) topBar.gameObject.SetActive(false);
            if (bottomBar) bottomBar.gameObject.SetActive(false);
        }
        letterboxCoroutine = null; // Clear coroutine reference
    }
    #endregion


    // --- 全屏CG控制方法 (Fullscreen CG Control Methods) ---
    #region Full screen method
    /// <summary>
    /// 通过标识符查找并显示全屏CG，使用交叉淡入淡出效果。
    /// Finds and shows a fullscreen CG by identifier, using a crossfade effect.
    /// </summary>
    /// <param name="cgIdentifier">在CG库中定义的CG标识符 (The CG identifier defined in the CG library)</param>
    /// <param name="fadeDuration">淡入淡出时长（秒），如果小于0则使用默认值 (Fade duration in seconds, uses default if < 0)</param>
    public void ShowFullscreenCG(string cgIdentifier, float fadeDuration = -1f)
    {
        if (!cgLookup.TryGetValue(cgIdentifier, out Sprite spriteToShow))
        {
            Debug.LogError($"[CutsceneUIManager] CG with identifier '{cgIdentifier}' not found in library!");
            return;
        }

        float duration = (fadeDuration >= 0) ? fadeDuration : defaultCgFadeDuration;
        Debug.Log($"[CutsceneUIManager] Showing CG: '{cgIdentifier}' with fade duration: {duration}");

        // 停止任何正在进行的CG淡入淡出
        // Stop any ongoing CG fade
        if (cgFadeCoroutine != null)
        {
            StopCoroutine(cgFadeCoroutine);
        }

        cgFadeCoroutine = StartCoroutine(CrossfadeCGCoroutine(spriteToShow, duration));
    }

    /// <summary>
    /// 隐藏所有当前显示的全屏CG，使用淡出效果。
    /// Hides all currently displayed fullscreen CGs using a fade-out effect.
    /// </summary>
    /// <param name="fadeDuration">淡出时长（秒），如果小于0则使用默认值 (Fade duration in seconds, uses default if < 0)</param>
    public void HideAllFullscreenCGs(float fadeDuration = -1f)
    {
        // 检查是否有CG正在显示
        if (cgBackgroundCanvasGroup.alpha == 0f && cgForegroundCanvasGroup.alpha == 0f)
        {
            Debug.Log("[CutsceneUIManager] No CGs currently visible to hide.");
            return; // 没有可见的CG，无需隐藏
        }


        float duration = (fadeDuration >= 0) ? fadeDuration : defaultCgFadeDuration;
        Debug.Log($"[CutsceneUIManager] Hiding all CGs with fade duration: {duration}");

        // 停止任何正在进行的CG淡入淡出
        if (cgFadeCoroutine != null)
        {
            StopCoroutine(cgFadeCoroutine);
        }

        cgFadeCoroutine = StartCoroutine(FadeOutAllCGsCoroutine(duration));
    }
    #endregion


    // --- CG 淡入淡出协程 (CG Fade Coroutines) ---
    #region CG Fade in/out
    private IEnumerator CrossfadeCGCoroutine(Sprite newSprite, float duration)
    {
        Image targetImage;
        CanvasGroup targetGroup;
        Image currentImage;
        CanvasGroup currentGroup;

        // 确定哪个是目标（隐藏的），哪个是当前（可见的）
        // Determine which is the target (hidden) and which is current (visible)
        if (isForegroundCgActive)
        {
            targetImage = cgBackgroundImage;
            targetGroup = cgBackgroundCanvasGroup;
            currentImage = cgForegroundImage;
            currentGroup = cgForegroundCanvasGroup;
        }
        else
        {
            targetImage = cgForegroundImage;
            targetGroup = cgForegroundCanvasGroup;
            currentImage = cgBackgroundImage;
            currentGroup = cgBackgroundCanvasGroup;
        }

        // 设置新图像并确保目标是激活的
        // Set the new sprite and ensure the target is active
        targetImage.sprite = newSprite;
        targetImage.gameObject.SetActive(true);
        targetGroup.alpha = 0f; // 确保从完全透明开始 (Ensure starting from fully transparent)

        // 同时开始淡入目标和淡出当前
        // Start fading in the target and fading out the current simultaneously
        float timer = 0f;
        float currentStartAlpha = currentGroup.alpha; // 当前可能不是完全不透明 (Current might not be fully opaque)

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // 使用 unscaledDeltaTime 保证暂停时也能渐变
            float progress = Mathf.Clamp01(timer / duration);

            targetGroup.alpha = Mathf.Lerp(0f, 1f, progress);
            currentGroup.alpha = Mathf.Lerp(currentStartAlpha, 0f, progress);

            yield return null;
        }

        // 确保最终状态
        // Ensure final state
        targetGroup.alpha = 1f;
        currentGroup.alpha = 0f;
        currentImage.gameObject.SetActive(false); // 禁用不再可见的图像 (Disable the no longer visible image)

        // 更新激活状态标记
        // Update the active state flag
        isForegroundCgActive = (targetImage == cgForegroundImage);

        cgFadeCoroutine = null; // 标记协程结束 (Mark coroutine as finished)
        Debug.Log($"[CutsceneUIManager] Crossfade complete. Foreground active: {isForegroundCgActive}");
    }

    private IEnumerator FadeOutAllCGsCoroutine(float duration)
    {
        float timer = 0f;
        float startAlphaBg = cgBackgroundCanvasGroup.alpha;
        float startAlphaFg = cgForegroundCanvasGroup.alpha;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            cgBackgroundCanvasGroup.alpha = Mathf.Lerp(startAlphaBg, 0f, progress);
            cgForegroundCanvasGroup.alpha = Mathf.Lerp(startAlphaFg, 0f, progress);

            yield return null;
        }

        // 确保最终状态并禁用
        // Ensure final state and disable
        cgBackgroundCanvasGroup.alpha = 0f;
        cgForegroundCanvasGroup.alpha = 0f;
        cgBackgroundImage.gameObject.SetActive(false);
        cgForegroundImage.gameObject.SetActive(false);
        isForegroundCgActive = false; // 重置激活标记

        cgFadeCoroutine = null; // 标记协程结束
        Debug.Log("[CutsceneUIManager] Fade out all CGs complete.");
    }
    #endregion
}
