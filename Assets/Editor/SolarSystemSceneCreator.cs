using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// メニュー: Tools > ☀ Solar System Creator > Setup Scene
/// 実行すると SolarSystem.unity シーンを自動生成して開く。
/// </summary>
public static class SolarSystemSceneCreator
{
    private const string ScenePath = "Assets/Scenes/SolarSystem.unity";

    [MenuItem("Tools/☀ Solar System Creator/Setup Scene")]
    public static void SetupScene()
    {
        // 未保存の変更を確認
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // 空のシーンを新規作成
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);

        // GameManager オブジェクトを配置（これ1つで全システムが自動起動）
        var gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();

        // シーン保存
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        // Build Settings に追加（既存シーンを保持しつつ先頭に挿入）
        AddToBuildSettings(ScenePath);

        Debug.Log($"[Solar System] シーン作成完了 → {ScenePath}");
        EditorUtility.DisplayDialog(
            "Solar System Creator",
            $"シーンを作成しました！\n\n" +
            $"場所: {ScenePath}\n\n" +
            $"▶ ボタンを押すと起動します。\n\n" +
            $"操作:\n" +
            $"  右クリック+ドラッグ: カメラ回転\n" +
            $"  スクロール: ズーム\n" +
            $"  Space: ポーズ / 再開\n" +
            $"  1〜4: 速度切替\n" +
            $"  T: 惑星生成パネル開閉",
            "了解！");
    }

    [MenuItem("Tools/☀ Solar System Creator/Open Scene")]
    public static void OpenScene()
    {
        if (System.IO.File.Exists(
                System.IO.Path.Combine(Application.dataPath, "../", ScenePath)))
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }
        else
        {
            if (EditorUtility.DisplayDialog("シーンが見つかりません",
                    "SolarSystem.unity がまだ作成されていません。\n今すぐ作成しますか？",
                    "作成", "キャンセル"))
                SetupScene();
        }
    }

    private static void AddToBuildSettings(string path)
    {
        var existing = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);

        // 重複チェック
        foreach (var s in existing)
            if (s.path == path) return;

        existing.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = existing.ToArray();
    }
}
