using UnityEngine;

public class CanvasPixelScaler : MonoBehaviour
{
    public Canvas canvas;
    public float baseWidth = 320;  // 设计分辨率的宽度（像素）
    public float baseHeight = 180; // 设计分辨率的高度（像素）
    public Camera mainCamera;

    void Start()
    {
        AdjustCanvasToScreen();
    }

    void AdjustCanvasToScreen()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float targetAspect = baseWidth / baseHeight;
        float screenAspect = screenWidth / screenHeight;

        // 计算缩放比例
        float scale;
        if (screenAspect >= targetAspect)
        {
            // 屏幕比设计分辨率宽，按高度适配
            scale = screenHeight / baseHeight;
        }
        else
        {
            // 屏幕比设计分辨率窄，按宽度适配
            scale = screenWidth / baseWidth;
        }

        // 调整相机大小
        mainCamera.orthographicSize = baseHeight / (2 * scale * 32); // 32 是 Pixels Per Unit，根据你的设置调整
        canvas.transform.localScale = new Vector3(scale, scale, 1);
    }
}