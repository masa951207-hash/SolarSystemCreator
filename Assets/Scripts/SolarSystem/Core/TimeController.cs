using UnityEngine;

/// <summary>
/// シミュレーション速度倍率管理。Time.timeScale は使わず dt 倍率で制御する。
/// </summary>
public class TimeController : MonoBehaviour
{
    public static TimeController Instance { get; private set; }

    public static readonly float[] Speeds = { 1f, 10f, 100f, 1000f };
    public static readonly string[] SpeedLabels = { "1x", "10x", "100x", "1000x" };

    private int speedIndex = 0;

    public float SimulationSpeed => Speeds[speedIndex];
    public int   SpeedIndex      => speedIndex;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetSpeed(int index)
    {
        if (index >= 0 && index < Speeds.Length)
            speedIndex = index;
    }

    public void StepUp()   { if (speedIndex < Speeds.Length - 1) speedIndex++; }
    public void StepDown() { if (speedIndex > 0) speedIndex--; }
}
