using UnityEngine;

/// <summary>
/// シミュレーション状態管理（ポーズ・再開・統計）
/// </summary>
public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    public bool IsPaused { get; private set; } = false;

    public float SimulationTime { get; private set; } = 0f;   // 累積シミュレーション秒数

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (!IsPaused && TimeController.Instance != null)
            SimulationTime += Time.fixedDeltaTime * TimeController.Instance.SimulationSpeed;
    }

    public void Pause()       => IsPaused = true;
    public void Resume()      => IsPaused = false;
    public void TogglePause() => IsPaused = !IsPaused;
    public void ResetTime()   => SimulationTime = 0f;

    public int PlanetCount => GravitySystem.Instance != null
        ? GravitySystem.Instance.GetBodyCount() : 0;

    /// <summary>シミュレーション経過時間を人間可読な文字列に変換</summary>
    public string GetFormattedTime()
    {
        float t = SimulationTime;
        if (t < 3600f)   return $"{t:F0} sec";
        if (t < 86400f)  return $"{t / 3600f:F1} hr";
        if (t < 2592000f) return $"{t / 86400f:F1} days";
        return $"{t / 2592000f:F1} months";
    }
}
