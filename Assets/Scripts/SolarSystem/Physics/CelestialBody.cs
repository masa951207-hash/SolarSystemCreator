using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 天体コンポーネント。
/// Rigidbodyは使わず手動でVelocity Verlet積分する。
/// </summary>
public class CelestialBody : MonoBehaviour
{
    public PlanetData data;
    public bool isSun = false;

    public Vector3 Velocity      { get; private set; }
    public float   Mass          => data.mass;
    public float   RotationCount { get; private set; } = 0f;

    private Vector3           acceleration;
    private LineRenderer      orbitLine;
    private Queue<Vector3>    orbitHistory = new Queue<Vector3>();
    private Vector3           lastTrailPos;
    private bool              hasTrailPos  = false;

    private const int   MaxOrbitPoints = 500;
    // 軌道線の解像度（この距離以上移動したときだけ点を追加）
    private const float TrailMinStep   = 0.35f;

    // ────────────────────────────────────────────
    //  初期化
    // ────────────────────────────────────────────
    public void Initialize(PlanetData planetData, bool sun = false)
    {
        data  = planetData;
        isSun = sun;

        transform.localScale = Vector3.one * data.radius * 2f;

        if (!isSun)
            SetupOrbitLine();
    }

    public void SetVelocity(Vector3 v) => Velocity = v;

    // ────────────────────────────────────────────
    //  Velocity Verlet 積分メソッド（GravitySystem から呼ぶ）
    // ────────────────────────────────────────────

    public void AddAcceleration(Vector3 acc) => acceleration += acc;

    public void ClearAcceleration() => acceleration = Vector3.zero;

    /// <summary>Half kick: v += 0.5 * a * dt</summary>
    public void HalfKick(float dt)
    {
        if (isSun) return;
        Velocity += acceleration * (0.5f * dt);
    }

    /// <summary>Drift: x += v * dt（XZ平面固定）</summary>
    public void Drift(float dt)
    {
        if (isSun) return;
        Vector3 pos = transform.position + Velocity * dt;
        pos.y = 0f;
        transform.position = pos;
    }

    // ────────────────────────────────────────────
    //  軌道トレイル
    // ────────────────────────────────────────────
    public void UpdateOrbitTrail()
    {
        if (orbitLine == null) return;

        Vector3 pos = transform.position;

        // 一定距離以上移動したときだけ点を追加（高速時に点が詰まるのを防ぐ）
        if (hasTrailPos && Vector3.Distance(pos, lastTrailPos) < TrailMinStep)
            return;

        hasTrailPos = true;
        lastTrailPos = pos;

        orbitHistory.Enqueue(pos);
        if (orbitHistory.Count > MaxOrbitPoints)
            orbitHistory.Dequeue();

        var pts = new Vector3[orbitHistory.Count];
        orbitHistory.CopyTo(pts, 0);
        orbitLine.positionCount = pts.Length;
        orbitLine.SetPositions(pts);
    }

    public void ClearTrail()
    {
        orbitHistory.Clear();
        hasTrailPos = false;
        if (orbitLine != null) orbitLine.positionCount = 0;
    }

    // ────────────────────────────────────────────
    //  衝突合体
    // ────────────────────────────────────────────
    public void MergeWith(CelestialBody other)
    {
        float totalMass = data.mass + other.data.mass;

        Velocity = (Velocity * data.mass + other.Velocity * other.data.mass) / totalMass;

        data.mass = totalMass;
        float newRadius = Mathf.Pow(
            Mathf.Pow(data.radius, 3f) + Mathf.Pow(other.data.radius, 3f),
            1f / 3f);
        data.radius = newRadius;
        transform.localScale = Vector3.one * data.radius * 2f;
    }

    // ────────────────────────────────────────────
    //  自転
    // ────────────────────────────────────────────
    void Update()
    {
        if (data != null)
        {
            float delta = data.rotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, delta, Space.World);
            if (!isSun)
                RotationCount += Mathf.Abs(delta) / 360f;
        }
    }

    // ────────────────────────────────────────────
    //  軌道線セットアップ
    // ────────────────────────────────────────────
    private void SetupOrbitLine()
    {
        orbitLine = gameObject.AddComponent<LineRenderer>();
        orbitLine.useWorldSpace = true;
        orbitLine.positionCount = 0;
        orbitLine.startWidth    = 0.18f;
        orbitLine.endWidth      = 0.04f;

        // ② 惑星色と一致したグラデーション（古い点=透明→新しい点=不透明）
        Color c = data.color;
        orbitLine.colorGradient = MakeGradient(c);
        orbitLine.startColor    = new Color(c.r, c.g, c.b, 0f);
        orbitLine.endColor      = new Color(c.r, c.g, c.b, 0.9f);

        // 頂点カラーを確実に反映するシェーダーを選択
        // Sprites/Default はすべての Unity ビルドに含まれる
        ApplyLineShader(orbitLine, c);
    }

    private static void ApplyLineShader(LineRenderer lr, Color c)
    {
        // URP/Unlit → Sprites/Default → Unlit/Color の順でフォールバック
        string[] candidates = {
            "Universal Render Pipeline/Particles/Unlit",
            "Sprites/Default",
            "Unlit/Color"
        };
        foreach (var name in candidates)
        {
            var shader = Shader.Find(name);
            if (shader == null) continue;
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", c);
            mat.SetColor("_Color",     c);
            lr.material = mat;
            return;
        }
        // フォールバック失敗時は既存マテリアルの色だけ変更
        lr.material.SetColor("_BaseColor", c);
        lr.material.SetColor("_Color",     c);
    }

    // グラデーション：古い点(位置0)が透明 → 新しい点(位置1=惑星付近)が不透明
    private static Gradient MakeGradient(Color c)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 1f) }
        );
        return g;
    }
}
