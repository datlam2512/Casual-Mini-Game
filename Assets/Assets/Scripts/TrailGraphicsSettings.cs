using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal static class TrailGraphicsSettings
{
    private const string Prefix = "TinyAdventure.Graphics.";
    private static readonly int[] FrameLimits = { 30, 60, 120, -1 };
    private static UniversalRenderPipelineAsset runtimePipeline;

    public static int Quality => Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Quality", 2), 0, 2);
    public static int FrameLimit => Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "FrameLimit", 1), 0, 3);
    public static bool Fullscreen => PlayerPrefs.GetInt(Prefix + "Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySaved()
    {
        ApplyQuality(Quality);
        ApplyFrameLimit(FrameLimit);
        if (PlayerPrefs.HasKey(Prefix + "Fullscreen")) ApplyFullscreen(Fullscreen);
    }

    public static void SetQuality(int value)
    {
        ApplyQuality(value);
        Save("Quality", value);
    }

    public static void SetFrameLimit(int value)
    {
        ApplyFrameLimit(value);
        Save("FrameLimit", value);
    }

    public static void SetFullscreen(bool value)
    {
        ApplyFullscreen(value);
        Save("Fullscreen", value ? 1 : 0);
    }

    private static void ApplyQuality(int value)
    {
        if (runtimePipeline == null)
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset source) return;
            runtimePipeline = Object.Instantiate(source);
            runtimePipeline.name = "Tiny Adventure Runtime Graphics";
            runtimePipeline.hideFlags = HideFlags.HideAndDontSave;
            QualitySettings.renderPipeline = runtimePipeline;
        }

        runtimePipeline.renderScale = value == 0 ? .70f : value == 1 ? .85f : 1f;
        runtimePipeline.shadowDistance = value == 0 ? 20f : value == 1 ? 35f : 50f;
    }

    private static void ApplyFrameLimit(int value)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = FrameLimits[value];
    }

    private static void ApplyFullscreen(bool value)
    {
#if !UNITY_EDITOR
        Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
#endif
    }

    private static void Save(string key, int value)
    {
        PlayerPrefs.SetInt(Prefix + key, value);
        PlayerPrefs.Save();
    }
}
