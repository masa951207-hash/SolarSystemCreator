using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// N体重力シミュレーション。
/// Velocity Verlet (KDK Leapfrog) + サブステップで高速時の軌道崩れを防止。
/// 最大30天体まで対応。
/// </summary>
public class GravitySystem : MonoBehaviour
{
    public static GravitySystem Instance { get; private set; }

    [Tooltip("重力定数 G（ゲームスケール）")]
    public float GravitationalConstant = 1f;

    [Tooltip("ゼロ除算回避のソフトニング係数")]
    [SerializeField] private float softening = 0.3f;

    // サブステップ 1 回あたりの最大 dt。
    // 水星軌道(r=6, v≈4.08, T≈9.2)を最低50ステップ/周にするには dt ≤ 0.18 が必要。
    private const float MaxSubDt = 0.15f;

    private List<CelestialBody> bodies    = new List<CelestialBody>();
    private List<CelestialBody> toDestroy = new List<CelestialBody>();
    private bool gravityInitialized = false;

    // ────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // ────────────────────────────────────────────
    //  登録管理
    // ────────────────────────────────────────────
    public void RegisterBody(CelestialBody body)
    {
        if (body == null || bodies.Contains(body)) return;
        bodies.Add(body);
        gravityInitialized = false; // 新天体追加時は初期加速度を再計算
    }

    public void UnregisterBody(CelestialBody body) => bodies.Remove(body);

    /// <summary>全天体を破棄してリストを空にする（リセット用）</summary>
    public void Clear()
    {
        foreach (var body in bodies)
            if (body != null) Object.Destroy(body.gameObject);
        bodies.Clear();
        toDestroy.Clear();
        gravityInitialized = false;
    }

    public int                 GetBodyCount() => bodies.Count;
    public List<CelestialBody> GetBodies()    => new List<CelestialBody>(bodies);

    public CelestialBody FindByName(string bodyName)
    {
        foreach (var b in bodies)
            if (b != null && b.data.planetName == bodyName) return b;
        return null;
    }

    /// <summary>天体位置・速度を外部で変更した後に呼ぶ。次フレームで初期加速度を再計算させる。</summary>
    public void InvalidateAccelerations() => gravityInitialized = false;

    // ────────────────────────────────────────────
    //  物理ループ（Velocity Verlet / KDK Leapfrog）
    // ────────────────────────────────────────────
    void FixedUpdate()
    {
        if (SimulationManager.Instance != null && SimulationManager.Instance.IsPaused)
            return;

        float speed = TimeController.Instance != null
            ? TimeController.Instance.SimulationSpeed : 1f;
        float totalDt = Time.fixedDeltaTime * speed;

        // 初回フレームのみ初期加速度を計算（Verlet の起動条件）
        if (!gravityInitialized)
        {
            ClearAllAccelerations();
            ComputeGravity();
            gravityInitialized = true;
        }

        // サブステップ数を決定。最大 MaxSubDt を超えないよう分割。
        int   substeps = Mathf.Max(1, Mathf.CeilToInt(totalDt / MaxSubDt));
        float dt       = totalDt / substeps;

        for (int s = 0; s < substeps; s++)
        {
            // ── KDK Leapfrog (Velocity Verlet と等価) ──
            // 1. Half Kick:  v += 0.5 * a * dt  （前ステップで計算済みの a を使用）
            HalfKickAll(dt);
            // 2. Drift:      x += v * dt
            DriftAll(dt);
            // 3. 新位置で重力を再計算
            ClearAllAccelerations();
            ComputeGravity();
            // 4. Half Kick:  v += 0.5 * a_new * dt
            HalfKickAll(dt);
        }

        // トレイルと衝突処理はフレームに1回だけ
        UpdateTrails();
        ResolveCollisions();
        FlushDestroyList();
    }

    // ────────────────────────────────────────────
    //  Velocity Verlet ヘルパー
    // ────────────────────────────────────────────
    private void HalfKickAll(float dt)
    {
        foreach (var body in bodies)
            if (body != null) body.HalfKick(dt);
    }

    private void DriftAll(float dt)
    {
        foreach (var body in bodies)
            if (body != null) body.Drift(dt);
    }

    private void ClearAllAccelerations()
    {
        foreach (var body in bodies)
            if (body != null) body.ClearAcceleration();
    }

    // ────────────────────────────────────────────
    //  重力計算（ペアワイズ N²）
    // ────────────────────────────────────────────
    private void ComputeGravity()
    {
        float soft2 = softening * softening;

        for (int i = 0; i < bodies.Count; i++)
        {
            for (int j = i + 1; j < bodies.Count; j++)
            {
                var a = bodies[i];
                var b = bodies[j];
                if (a == null || b == null) continue;

                Vector3 diff  = b.transform.position - a.transform.position;
                float   dist2 = diff.sqrMagnitude + soft2;
                float   dist  = Mathf.Sqrt(dist2);
                Vector3 unit  = diff / dist;

                float gOvrR2 = GravitationalConstant / dist2;

                if (!a.isSun) a.AddAcceleration( unit * gOvrR2 * b.Mass);
                if (!b.isSun) b.AddAcceleration(-unit * gOvrR2 * a.Mass);
            }
        }
    }

    // ────────────────────────────────────────────
    //  軌道描画更新
    // ────────────────────────────────────────────
    private void UpdateTrails()
    {
        foreach (var body in bodies)
            if (body != null) body.UpdateOrbitTrail();
    }

    // ────────────────────────────────────────────
    //  衝突検出・合体
    // ────────────────────────────────────────────
    private void ResolveCollisions()
    {
        var absorbed = new HashSet<CelestialBody>();

        for (int i = 0; i < bodies.Count; i++)
        {
            for (int j = i + 1; j < bodies.Count; j++)
            {
                var a = bodies[i];
                var b = bodies[j];
                if (a == null || b == null)                          continue;
                if (absorbed.Contains(a) || absorbed.Contains(b))    continue;

                float dist      = Vector3.Distance(a.transform.position, b.transform.position);
                float threshold = a.data.radius + b.data.radius;
                if (dist >= threshold) continue;

                CelestialBody big, small;
                if      (a.isSun)          { big = a; small = b; }
                else if (b.isSun)          { big = b; small = a; }
                else if (a.Mass >= b.Mass) { big = a; small = b; }
                else                       { big = b; small = a; }

                big.MergeWith(small);
                absorbed.Add(small);
                toDestroy.Add(small);
            }
        }
    }

    private void FlushDestroyList()
    {
        foreach (var body in toDestroy)
        {
            bodies.Remove(body);
            if (body != null) Destroy(body.gameObject);
        }
        toDestroy.Clear();
    }
}
