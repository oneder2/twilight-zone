using System.Collections;
using UnityEngine;


public class StageLighting : MonoBehaviour
{
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D globalLight;

    public static StageLighting stageLighting;

    void Awake()
    {
        if (stageLighting != null && stageLighting != this)
        {
            Debug.LogWarning("发现重复的StageLighting实例，正在销毁");
            Destroy(gameObject); // 销毁重复实例
            return;
        }
    }

    void Start()
    {
        EventManager.Instance.AddListener<StageChangeEvent>(OnStageChanged);
        UpdateLighting(StageManager.Instance.GetCurrentStageData());
    }

    void OnDestroy()
    {
        EventManager.Instance.RemoveListener<StageChangeEvent>(OnStageChanged);
    }

    void OnStageChanged(StageChangeEvent stageEvent)
    {
        StageData data = StageManager.Instance.GetCurrentStageData();
        UpdateLighting(data);
    }

    void UpdateLighting(StageData data)
    {
        StartCoroutine(LerpLighting(data.lightIntensity, data.lightColor, 1f));
    }

    IEnumerator LerpLighting(float targetIntensity, Color targetColor, float duration)
    {
        float time = 0;
        float startIntensity = globalLight.intensity;
        Color startColor = globalLight.color;
        while (time < duration)
        {
            time += Time.deltaTime;
            globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, time / duration);
            globalLight.color = Color.Lerp(startColor, targetColor, time / duration);
            yield return null;
        }
    }
    
}