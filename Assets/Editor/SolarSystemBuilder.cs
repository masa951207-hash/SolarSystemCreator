using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Tools > ☀ Solar System Creator > Build EXE (Desktop)
/// </summary>
public static class SolarSystemBuilder
{
    [MenuItem("Tools/☀ Solar System Creator/Build EXE (Desktop)")]
    public static void BuildFromMenu()
    {
        string desktopPath = System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.Desktop);
        string outputPath = System.IO.Path.Combine(
            desktopPath, "SolarSystemCreator", "SolarSystemCreator.exe");

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputPath));

        var options = new BuildPlayerOptions
        {
            scenes           = new[] { "Assets/Scenes/SolarSystem.unity" },
            locationPathName = outputPath,
            target           = BuildTarget.StandaloneWindows64,
            options          = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Build] 成功: {outputPath}");
            EditorUtility.RevealInFinder(outputPath);
            EditorUtility.DisplayDialog("ビルド完了！",
                "デスクトップの SolarSystemCreator フォルダに\nSolarSystemCreator.exe が出力されました！",
                "OK");
        }
        else
        {
            Debug.LogError($"[Build] 失敗: エラー数 {report.summary.totalErrors}");
            EditorUtility.DisplayDialog("ビルド失敗",
                "Consoleウィンドウのエラーを確認してください。", "OK");
        }
    }
}
