using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }
    
    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Image fadeBackground;
    [SerializeField] private Color fadeColor = Color.black;
    
    private bool isFading = false;
    private Action onFadeComplete;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Make sure we have a canvas group for fading
        if (fadeCanvasGroup == null)
        {
            Debug.LogError("[GameSceneManager] No CanvasGroup assigned for fade effect!");
        }
        else
        {
            // Initialize fade canvas to be transparent
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            
            // Set the background color
            if (fadeBackground != null)
            {
                fadeBackground.color = fadeColor;
            }
        }
    }
    
    /// <summary>
    /// Loads a scene with a fade transition
    /// </summary>
    /// <param name="sceneName">The name of the scene to load</param>
    /// <param name="onComplete">Optional callback when scene load is complete</param>
    public void LoadScene(string sceneName, Action onComplete = null)
    {
        // Don't start another fade if we're already fading
        if (isFading)
        {
            Debug.LogWarning("[GameSceneManager] Scene transition already in progress!");
            return;
        }
        
        // Start fading out, then load the scene, then fade in
        StartCoroutine(FadeAndLoadScene(sceneName, onComplete));
    }
    
    /// <summary>
    /// Fades the screen to black, loads the scene, then fades back in
    /// </summary>
    private IEnumerator FadeAndLoadScene(string sceneName, Action onComplete)
    {
        isFading = true;
        
        // Fade out
        yield return StartCoroutine(FadeOut());
        
        // Load the scene (this will take some time)
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        // Small delay to allow scene to initialize
        yield return new WaitForSeconds(0.1f);
        
        // Fade in
        yield return StartCoroutine(FadeIn());
        
        // Invoke the completion callback
        onComplete?.Invoke();
        
        isFading = false;
    }
    
    /// <summary>
    /// Fade the screen to black
    /// </summary>
    private IEnumerator FadeOut()
    {
        // Make sure the canvas group blocks raycasts while fading
        fadeCanvasGroup.blocksRaycasts = true;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }
        
        // Ensure we end at fully opaque
        fadeCanvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// Fade from black back to the scene
    /// </summary>
    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsedTime / fadeDuration));
            yield return null;
        }
        
        // Ensure we end at fully transparent
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }
    
    /// <summary>
    /// Manually trigger a fade out
    /// </summary>
    /// <param name="onComplete">Action to call when fade is complete</param>
    public void FadeOut(Action onComplete = null)
    {
        if (isFading) return;
        
        isFading = true;
        onFadeComplete = onComplete;
        StartCoroutine(FadeOutCoroutine());
    }
    
    private IEnumerator FadeOutCoroutine()
    {
        yield return StartCoroutine(FadeOut());
        
        onFadeComplete?.Invoke();
        isFading = false;
    }
    
    /// <summary>
    /// Manually trigger a fade in
    /// </summary>
    /// <param name="onComplete">Action to call when fade is complete</param>
    public void FadeIn(Action onComplete = null)
    {
        if (isFading) return;
        
        isFading = true;
        onFadeComplete = onComplete;
        StartCoroutine(FadeInCoroutine());
    }
    
    private IEnumerator FadeInCoroutine()
    {
        yield return StartCoroutine(FadeIn());
        
        onFadeComplete?.Invoke();
        isFading = false;
    }
}