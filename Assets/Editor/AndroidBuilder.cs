using UnityEditor;
using UnityEngine;

/// <summary>
/// 命令行（CI）构建入口：Unity.exe -batchmode -quit -executeMethod AndroidBuilder.PerformBuild
/// 用于 MCP 不可用时构建 Android APK；日志见 -logFile 指定文件。
/// </summary>
public static class AndroidBuilder
{
    public static void PerformBuild()
    {
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = "Builds/Android/doudou-survivor.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(opts);
        var s = report.summary;
        Debug.Log("[CI Build] result=" + s.result
            + " size=" + (s.totalSize / 1048576f).ToString("F1") + "MB"
            + " errors=" + s.totalErrors
            + " output=" + s.outputPath);
    }
}
