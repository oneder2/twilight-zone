using UnityEngine;
using UnityEngine.UI; // Required for Image, potentially Slider
using System.Collections;

/// <summary>
/// Manages UI elements specific to cutscenes, like letterbox bars and fullscreen images.
/// Assumed to be a Singleton within the GameRoot scene.
/// </summary>
public class CutsceneUIManager : Singleton<CutsceneUIManager> // Use your Singleton base
{
    [Header("Letterbox Elements")]
    [Tooltip("The UI element (Image/Panel) for the top letterbox bar.")]
    [SerializeField] private RectTransform topBar;
    [Tooltip("The UI element (Image/Panel) for the bottom letterbox bar.")]
    [SerializeField] private RectTransform bottomBar;
    [Tooltip("Animation speed/duration for the letterbox bars.")]
    [SerializeField] private float letterboxAnimDuration = 0.3f; // Changed to duration

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
    private Coroutine fullscreenFadeCoroutine = null;

    protected override void Awake()
    {
        base.Awake();
        // Initial setup - ensure elements are correctly positioned off-screen or hidden
        InitializeUIElements();
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

    /// <summary>
    /// Shows a fullscreen image, fading it in.
    /// </summary>
    /// <param name="imageSprite">The sprite to display.</param>
    /// <param name="fadeDuration">Duration of the fade-in.</param>
    public Coroutine ShowFullscreenImage(Sprite imageSprite, float fadeDuration = 0.5f)
    {
        if (fullscreenCgImage == null || fullscreenCgCanvasGroup == null) return null;
        if (imageSprite == null) { Debug.LogWarning("ShowFullscreenImage called with null sprite."); return null; }

        fullscreenCgImage.sprite = imageSprite;
        fullscreenCgImage.gameObject.SetActive(true); // Make sure it's active before fading
        if (fullscreenFadeCoroutine != null) StopCoroutine(fullscreenFadeCoroutine);
        fullscreenFadeCoroutine = StartCoroutine(FadeFullscreenImage(1f, fadeDuration));
        return fullscreenFadeCoroutine;
    }

    /// <summary>
    /// Hides the fullscreen image, fading it out.
    /// </summary>
    /// <param name="fadeDuration">Duration of the fade-out.</param>
    public Coroutine HideFullscreenImage(float fadeDuration = 0.5f)
    {
        // Don't hide if it's already hidden or hiding
        if (fullscreenCgImage == null || fullscreenCgCanvasGroup == null || !fullscreenCgImage.gameObject.activeSelf || fullscreenCgCanvasGroup.alpha == 0f)
        {
             return null;
        }

        if (fullscreenFadeCoroutine != null) StopCoroutine(fullscreenFadeCoroutine);
        // Pass true to disable GameObject after fade
        fullscreenFadeCoroutine = StartCoroutine(FadeFullscreenImage(0f, fadeDuration, true));
        return fullscreenFadeCoroutine;
    }


    // --- Private Animation Coroutines ---

    private Coroutine topBarAnimCoroutine;
    private Coroutine bottomBarAnimCoroutine;

    private void StopAllCoroutinesRelatedToLetterbox() {
        if(topBarAnimCoroutine != null) StopCoroutine(topBarAnimCoroutine);
        if(bottomBarAnimCoroutine != null) StopCoroutine(bottomBarAnimCoroutine);
    }
     private void StopAllCoroutinesRelatedToFullscreenImage() {
        if(fullscreenFadeCoroutine != null) StopCoroutine(fullscreenFadeCoroutine);
    }


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

    private IEnumerator FadeFullscreenImage(float targetAlpha, float duration, bool disableAfter = false)
    {
        if (fullscreenCgCanvasGroup == null) yield break;

        float startAlpha = fullscreenCgCanvasGroup.alpha;
        float timer = 0f;

        // Ensure object is active to fade (especially for fade in)
        if (targetAlpha > startAlpha && !fullscreenCgImage.gameObject.activeSelf) {
            fullscreenCgImage.gameObject.SetActive(true);
        }


        if (duration > 0.01f) {
            while(timer < duration)
            {
                timer += Time.deltaTime;
                fullscreenCgCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
                yield return null;
            }
        }

        fullscreenCgCanvasGroup.alpha = targetAlpha;

        if (disableAfter && targetAlpha < 0.1f) // Disable only if faded out
        {
            fullscreenCgImage.gameObject.SetActive(false);
        }
        fullscreenFadeCoroutine = null; // Clear coroutine reference
    }

    // TODO: Implement Slider-based animation if desired
    // public Coroutine ShowLetterboxWithSlider(bool animated = true) { ... }
    // public Coroutine HideLetterboxWithSlider(bool animated = true) { ... }
}
