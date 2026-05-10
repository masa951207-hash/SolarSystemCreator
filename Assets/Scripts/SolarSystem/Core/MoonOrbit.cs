using UnityEngine;

/// <summary>
/// 月の視覚的な軌道スクリプト。
/// 現実質量では地球のヒル圏が軌道半径より小さいため物理N体には参加しない。
/// 代わりに地球の位置を追従しながら単純回転で軌道を表現する。
/// </summary>
public class MoonOrbit : MonoBehaviour
{
    private Transform earthTransform;
    private float orbitRadius;
    private float angle;

    // 現実の月の公転周期 = 地球公転周期 * 0.0747
    // シム内 Earth 周期 ≈ 36.5 sim-sec → Moon 周期 ≈ 2.73 sim-sec
    // ω = 2π / 2.73 ≈ 2.30 rad/sim-sec
    private const float AngularSpeed = 2.30f;

    public void Initialize(Transform earth, float radius, float startAngle = 0f)
    {
        earthTransform = earth;
        orbitRadius    = radius;
        angle          = startAngle;
    }

    void Update()
    {
        if (earthTransform == null) return;
        if (SimulationManager.Instance != null && SimulationManager.Instance.IsPaused) return;

        float speed = TimeController.Instance != null
            ? TimeController.Instance.SimulationSpeed : 1f;

        angle += AngularSpeed * speed * Time.deltaTime;

        transform.position = earthTransform.position + new Vector3(
            Mathf.Cos(angle) * orbitRadius,
            0f,
            Mathf.Sin(angle) * orbitRadius);
    }
}
