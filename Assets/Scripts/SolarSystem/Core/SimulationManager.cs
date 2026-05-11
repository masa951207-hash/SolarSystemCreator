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

    // T = 2π√(r³/GM) = 2π√(15³/100) ≈ 36.51 sim秒 = 1地球年
    public const float EarthYearSeconds = 36.51f;

    /// <summary>シミュレーション経過時間を人間可読な文字列に変換（年優先）</summary>
    public string GetFormattedTime()
    {
        float years = SimulationTime / EarthYearSeconds;
        if (years >= 1f)
        {
            if (years < 1000f) return $"{years:F1} 年";
            return $"{years:F0} 年";
        }
        float days = SimulationTime / (EarthYearSeconds / 365.25f);
        if (days >= 1f) return $"{days:F0} 日";
        return $"{SimulationTime:F0} 秒";
    }
}
