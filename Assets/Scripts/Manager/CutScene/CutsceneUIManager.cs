using UnityEngine;
using UnityEngine.UI; // Required for Image, potentially Slider
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages UI elements specific to cutscenes, like letterbox bars (vertical slide animation) and fullscreen images (crossfade).
/// Assumed to be a Singleton within the GameRoot scene.
/// 管理过场动画特有的 UI 元素，如信箱黑边（垂直滑动动画）和全屏图像（交叉淡入淡出）。
/// 假设是 GameRoot 场景中的 Singleton。
/// </summary>
public class CutsceneUIManager : Singleton<CutsceneUIManager> // Use your Singleton base
{
    #region Letterbox Variables
    [Header("信箱黑边元素 (Letterbox Elements)")]
    [Tooltip("顶部黑边的 UI 元素 (RectTransform)。锚点应设为顶部中心。")]
    // [Tooltip("UI element (RectTransform) for the top letterbox bar. Anchor should be top-center.")]
    [SerializeField] private RectTransform topBar;

    [Tooltip("底部黑边的 UI 元素 (RectTransform)。锚点应设为底部中心。")]
    // [Tooltip("UI element (RectTransform) for the bottom letterbox bar. Anchor should be bottom-center.")]
    [SerializeField] private RectTransform bottomBar;

    [Tooltip("信箱黑边动画的时长（秒）。")]
    // [Tooltip("Animation duration for the letterbox bars (seconds).")]
    [SerializeField] private float letterboxAnimDuration = 0.3f;

    // Store the initial positions or heights for animation
    // 存储动画的初始位置或高度
    private float topBarHiddenY; // Calculated in Initialize
    private float topBarShownY;  // Calculated in Initialize
    private float bottomBarHiddenY; // Calculated in Initialize
    private float bottomBarShownY; // Calculated in Initialize
    private bool areBarsVisible = false;
    private Coroutine letterboxCoroutine = null;
    #endregion

    #region Fullscreen CG Variables
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

    [System.Serializable]
    public class CGEntry
    {
        public string identifier;
        public Sprite sprite;
    }
    [SerializeField] private List<CGEntry> cgLibrary = new List<CGEntry>();
    private Dictionary<string, Sprite> cgLookup = new Dictionary<string, Sprite>();

    private bool isForegroundCgActive = false;
    private Coroutine cgFadeCoroutine = null;
    #endregion


    protected override void Awake()
    {
        base.Awake();
        InitializeUIElements();
        InitializeCGLibrary();
    }

    private void InitializeUIElements()
    {
         // Letterbox check and initialization for vertical sliding
         // 检查信箱黑边并为垂直滑动进行初始化
         if (topBar == null || bottomBar == null) {
              Debug.LogError("[CutsceneUIManager] Letterbox bars not assigned!", this);
         } else {
              // Assuming top bar anchored top-center, bottom bar anchored bottom-center
              // 假设顶部黑边锚定在顶部中心，底部黑边锚定在底部中心
              // Calculate hidden positions based on their height
              // 根据它们的高度计算隐藏位置
              topBarShownY = topBar.anchoredPosition.y; // Assumes initial Inspector position is the 'shown' position relative to anchor
                                                        // 假设初始 Inspector 位置是相对于锚点的“显示”位置
              topBarHiddenY = topBarShownY + topBar.rect.height; // Position above the screen edge
                                                                 // 屏幕边缘上方的位置
              topBar.anchoredPosition = new Vector2(topBar.anchoredPosition.x, topBarHiddenY); // Start hidden

              bottomBarShownY = bottomBar.anchoredPosition.y;
              bottomBarHiddenY = bottomBarShownY - bottomBar.rect.height; // Position below the screen edge
                                                                         // 屏幕边缘下方的位置
              bottomBar.anchoredPosition = new Vector2(bottomBar.anchoredPosition.x, bottomBarHiddenY); // Start hidden

              topBar.gameObject.SetActive(false); // Start inactive
              bottomBar.gameObject.SetActive(false);
              areBarsVisible = false;
         }

         // CG Background check
         if (cgBackgroundImage == null || cgForegroundImage == null || cgBackgroundCanvasGroup == null || cgForegroundCanvasGroup == null) {
              Debug.LogError("[CutsceneUIManager] Fullscreen CG elements (Images/CanvasGroups) not assigned!", this);
              return;
         }
         // Initialize CG state
         cgBackgroundCanvasGroup.alpha = 0f;
         cgForegroundCanvasGroup.alpha = 0f;
         cgBackgroundImage.gameObject.SetActive(false);
         cgForegroundImage.gameObject.SetActive(false);
         isForegroundCgActive = false;
    }


    private void InitializeCGLibrary()
    {
        cgLookup.Clear();
        foreach(var entry in cgLibrary)
        {
            if (!string.IsNullOrEmpty(entry.identifier) && entry.sprite != null)
            {
                if (!cgLookup.ContainsKey(entry.identifier)) { cgLookup.Add(entry.identifier, entry.sprite); }
                else { Debug.LogWarning($"[CutsceneUIManager] Duplicate CG identifier found: {entry.identifier}"); }
            }
        }
        // Debug.Log($"[CutsceneUIManager] CG Library initialized with {cgLookup.Count} entries.");
    }

    #region Event Listeners
    void OnEnable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<ShowCGRequestedEvent>(HandleShowCGRequest);
            EventManager.Instance.AddListener<HideAllCGsRequestedEvent>(HandleHideCGRequest);
            // Debug.Log("[CutsceneUIManager] Subscribed to CG request events.");
        } else { Debug.LogError("[CutsceneUIManager] EventManager not found on Enable!"); }
    }

    void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<ShowCGRequestedEvent>(HandleShowCGRequest);
            EventManager.Instance.RemoveListener<HideAllCGsRequestedEvent>(HandleHideCGRequest);
            // Debug.Log("[CutsceneUIManager] Unsubscribed from CG request events.");
        }
        StopAllCoroutines();
        cgFadeCoroutine = null;
        letterboxCoroutine = null;
    }

    private void HandleShowCGRequest(ShowCGRequestedEvent eventData)
    {
        if (eventData == null || string.IsNullOrEmpty(eventData.CgIdentifier)) return;
        if (!cgLookup.TryGetValue(eventData.CgIdentifier, out Sprite spriteToShow))
        {
            Debug.LogError($"[CutsceneUIManager] CG identifier '{eventData.CgIdentifier}' not found!");
            return;
        }
        float duration = (eventData.FadeDuration >= 0) ? eventData.FadeDuration : defaultCgFadeDuration;
        ShowFullscreenCGInternal(spriteToShow, duration);
    }

    private void HandleHideCGRequest(HideAllCGsRequestedEvent eventData)
    {
        if (eventData == null) return;
        float duration = (eventData.FadeDuration >= 0) ? eventData.FadeDuration : defaultCgFadeDuration;
        HideAllFullscreenCGsInternal(duration);
    }
    #endregion

    #region Public CG Methods
    public void ShowFullscreenCG(string cgIdentifier, float fadeDuration = -1f)
    {
        if (!cgLookup.TryGetValue(cgIdentifier, out Sprite spriteToShow))
        {
            Debug.LogError($"[CutsceneUIManager] CG identifier '{cgIdentifier}' not found!");
            return;
        }
        float duration = (fadeDuration >= 0) ? fadeDuration : defaultCgFadeDuration;
        ShowFullscreenCGInternal(spriteToShow, duration);
    }

    public void HideAllFullscreenCGs(float fadeDuration = -1f)
    {
        float duration = (fadeDuration >= 0) ? fadeDuration : defaultCgFadeDuration;
        HideAllFullscreenCGsInternal(duration);
    }
    #endregion

    #region Internal CG Logic
    private void ShowFullscreenCGInternal(Sprite spriteToShow, float duration)
    {
        // Start Letterbox Show Animation (Vertical)
        // 启动信箱黑边显示动画（垂直）
        ShowLetterbox(true, letterboxAnimDuration);

        // Start CG Crossfade
        // 启动 CG 交叉淡入淡出
        if (cgFadeCoroutine != null) StopCoroutine(cgFadeCoroutine);
        cgFadeCoroutine = StartCoroutine(CrossfadeCGCoroutine(spriteToShow, duration));
    }

    private void HideAllFullscreenCGsInternal(float duration)
    {
        if (cgBackgroundCanvasGroup.alpha < 0.01f && cgForegroundCanvasGroup.alpha < 0.01f && !isForegroundCgActive)
        {
            if (areBarsVisible) HideLetterbox(true, letterboxAnimDuration);
            return;
        }

        // Start Letterbox Hide Animation (Vertical)
        // 启动信箱黑边隐藏动画（垂直）
        HideLetterbox(true, letterboxAnimDuration);

        // Start CG Fade Out
        // 启动 CG 淡出
        if (cgFadeCoroutine != null) StopCoroutine(cgFadeCoroutine);
        cgFadeCoroutine = StartCoroutine(FadeOutAllCGsCoroutine(duration));
    }
    #endregion

    #region Letterbox Control Methods (Using Vertical Slide)
    /// <summary>
    /// Shows the letterbox bars using vertical sliding animation.
    /// 使用垂直滑动动画显示信箱黑边。
    /// </summary>
    public Coroutine ShowLetterbox(bool animated = true, float duration = -1f)
    {
        if (areBarsVisible) return null;
        if (topBar == null || bottomBar == null) return null;

        areBarsVisible = true;
        float animDuration = (duration >= 0) ? duration : letterboxAnimDuration;

        // Ensure bars are active before animation
        topBar.gameObject.SetActive(true);
        bottomBar.gameObject.SetActive(true);

        if (animated && animDuration > 0)
        {
            if (letterboxCoroutine != null) StopCoroutine(letterboxCoroutine);
            // Start animation from hidden Y to shown Y
            // 从隐藏 Y 位置向显示 Y 位置开始动画
            letterboxCoroutine = StartCoroutine(AnimateBarsVertical(topBarShownY, bottomBarShownY, animDuration));
            return letterboxCoroutine;
        }
        else
        {
            // Show instantly at shown position
            // 立即在显示位置显示
            topBar.anchoredPosition = new Vector2(topBar.anchoredPosition.x, topBarShownY);
            bottomBar.anchoredPosition = new Vector2(bottomBar.anchoredPosition.x, bottomBarShownY);
            return null;
        }
    }

    /// <summary>
    /// Hides the letterbox bars using vertical sliding animation.
    /// 使用垂直滑动动画隐藏信箱黑边。
    /// </summary>
    public Coroutine HideLetterbox(bool animated = true, float duration = -1f)
    {
        if (!areBarsVisible) return null;
        if (topBar == null || bottomBar == null) return null;

        areBarsVisible = false; // Set flag immediately
        float animDuration = (duration >= 0) ? duration : letterboxAnimDuration;

        if (animated && animDuration > 0)
        {
            if (letterboxCoroutine != null) StopCoroutine(letterboxCoroutine);
            // Start animation from shown Y to hidden Y, disable after
            // 从显示 Y 位置向隐藏 Y 位置开始动画，之后禁用
            letterboxCoroutine = StartCoroutine(AnimateBarsVertical(topBarHiddenY, bottomBarHiddenY, animDuration, true));
            return letterboxCoroutine;
        }
        else
        {
            // Hide instantly
            topBar.anchoredPosition = new Vector2(topBar.anchoredPosition.x, topBarHiddenY);
            bottomBar.anchoredPosition = new Vector2(bottomBar.anchoredPosition.x, bottomBarHiddenY);
            topBar.gameObject.SetActive(false);
            bottomBar.gameObject.SetActive(false);
            return null;
        }
    }
    #endregion

    #region Coroutines (CG Fades & Letterbox Vertical Slide)

    /// <summary>
    /// Animates the vertical position of the letterbox bars.
    /// 为信箱黑边的垂直位置设置动画。
    /// </summary>
    private IEnumerator AnimateBarsVertical(float targetTopY, float targetBottomY, float duration, bool disableAfter = false)
    {
        if (topBar == null || bottomBar == null) yield break;

        Vector2 startTopPos = topBar.anchoredPosition;
        Vector2 startBottomPos = bottomBar.anchoredPosition;
        Vector2 targetTopPos = new Vector2(startTopPos.x, targetTopY);
        Vector2 targetBottomPos = new Vector2(startBottomPos.x, targetBottomY);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // Use unscaledDeltaTime for smooth animation during pause
            float progress = Mathf.Clamp01(timer / duration);
            // Apply easing if desired: progress = Mathf.SmoothStep(0.0f, 1.0f, progress);
            topBar.anchoredPosition = Vector2.Lerp(startTopPos, targetTopPos, progress);
            bottomBar.anchoredPosition = Vector2.Lerp(startBottomPos, targetBottomPos, progress);
            yield return null;
        }

        // Ensure final position
        topBar.anchoredPosition = targetTopPos;
        bottomBar.anchoredPosition = targetBottomPos;

        if (disableAfter)
        {
            topBar.gameObject.SetActive(false);
            bottomBar.gameObject.SetActive(false);
        }
        letterboxCoroutine = null; // Clear coroutine reference
    }


    // --- CG Fade Coroutines remain the same ---
    // --- CG 淡入淡出协程保持不变 ---
    private IEnumerator CrossfadeCGCoroutine(Sprite newSprite, float duration)
    {
        Image targetImage; CanvasGroup targetGroup;
        Image currentImage; CanvasGroup currentGroup;

        if (isForegroundCgActive)
        {
            targetImage = cgBackgroundImage; targetGroup = cgBackgroundCanvasGroup;
            currentImage = cgForegroundImage; currentGroup = cgForegroundCanvasGroup;
        }
        else
        {
            targetImage = cgForegroundImage; targetGroup = cgForegroundCanvasGroup;
            currentImage = cgBackgroundImage; currentGroup = cgBackgroundCanvasGroup;
        }

        targetImage.sprite = newSprite;
        targetImage.gameObject.SetActive(true); // Activate target image GO / 激活目标图像 GO
        targetGroup.alpha = 0f;
        targetGroup.blocksRaycasts = true; // Block raycasts while fading in / 淡入时阻止射线投射

        float timer = 0f;
        float currentStartAlpha = currentGroup.alpha;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            targetGroup.alpha = Mathf.Lerp(0f, 1f, progress);
            currentGroup.alpha = Mathf.Lerp(currentStartAlpha, 0f, progress);
            yield return null;
        }

        targetGroup.alpha = 1f;
        targetGroup.blocksRaycasts = true; // Ensure target blocks when fully visible / 确保目标完全可见时阻止
        currentGroup.alpha = 0f;
        currentGroup.blocksRaycasts = false; // --- FIX: Disable raycasts on faded out group --- / --- 修复：禁用淡出组的射线投射 ---
        currentImage.gameObject.SetActive(false); // Disable faded out image GO / 禁用淡出的图像 GO

        isForegroundCgActive = (targetImage == cgForegroundImage);
        cgFadeCoroutine = null;
    }

    private IEnumerator FadeOutAllCGsCoroutine(float duration)
    {
        float timer = 0f;
        float startAlphaBg = cgBackgroundCanvasGroup.alpha;
        float startAlphaFg = cgForegroundCanvasGroup.alpha;

        // Ensure both groups block raycasts during fade out (might not be necessary, but safe)
        // 确保两个组在淡出期间都阻止射线投射（可能没必要，但安全）
        cgBackgroundCanvasGroup.blocksRaycasts = true;
        cgForegroundCanvasGroup.blocksRaycasts = true;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            cgBackgroundCanvasGroup.alpha = Mathf.Lerp(startAlphaBg, 0f, progress);
            cgForegroundCanvasGroup.alpha = Mathf.Lerp(startAlphaFg, 0f, progress);
            yield return null;
        }

        cgBackgroundCanvasGroup.alpha = 0f;
        cgForegroundCanvasGroup.alpha = 0f;
        // --- FIX: Disable raycasts when fully faded out ---
        // --- 修复：完全淡出时禁用射线投射 ---
        cgBackgroundCanvasGroup.blocksRaycasts = false;
        cgForegroundCanvasGroup.blocksRaycasts = false;
        // --- END FIX ---
        cgBackgroundImage.gameObject.SetActive(false);
        cgForegroundImage.gameObject.SetActive(false);
        isForegroundCgActive = false;
        cgFadeCoroutine = null;
        // Debug.Log("[CutsceneUIManager] Fade out all CGs complete.");
    }
    #endregion
}
