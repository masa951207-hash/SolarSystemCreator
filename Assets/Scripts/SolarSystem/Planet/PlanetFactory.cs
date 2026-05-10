using UnityEngine;

/// <summary>
/// 天体生成ファクトリ。惑星は最大30個まで。
///
/// 【質量スケール】Sun = 100 を基準に実質量比を適用。
///   例) 木星 = 100 × 9.55e-4 ≈ 0.0955（現実の木星/太陽比を忠実に再現）
///   これにより惑星間の重力摂動が現実に近い大きさになる。
///
/// 【距離スケール】地球 = 15 ゲームユニット = 1 AU
///   内惑星（水星〜火星）は実比率。外惑星は視認性のため圧縮。
/// </summary>
public class PlanetFactory : MonoBehaviour
{
    public static PlanetFactory Instance { get; private set; }

    private const int MaxBodies = 30;
    private int createdCount = 0;

    // ② 各惑星の固有色
    public static readonly Color SunColor     = new Color(1.0f,  0.90f, 0.30f);
    public static readonly Color MercuryColor = new Color(0.62f, 0.60f, 0.57f);
    public static readonly Color VenusColor   = new Color(0.95f, 0.88f, 0.62f);
    public static readonly Color EarthColor   = new Color(0.22f, 0.53f, 0.95f);
    public static readonly Color MoonColor    = new Color(0.82f, 0.82f, 0.80f);
    public static readonly Color MarsColor    = new Color(0.85f, 0.32f, 0.15f);
    public static readonly Color JupiterColor = new Color(0.88f, 0.68f, 0.42f);
    public static readonly Color SaturnColor  = new Color(0.95f, 0.85f, 0.55f);
    public static readonly Color UranusColor  = new Color(0.48f, 0.88f, 0.92f);
    public static readonly Color NeptuneColor = new Color(0.20f, 0.42f, 0.95f);
    public static readonly Color HalleyColor  = new Color(0.75f, 0.92f, 1.00f);

    // ③ 太陽質量定数（これ以上の惑星は作れない）
    public const float SunMass = 100f;

    // 月 GameObject（リセット時に破棄するための参照）
    private GameObject moonObject;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ────────────────────────────────────────────
    //  リセット用クリア
    // ────────────────────────────────────────────
    public void ResetCount()
    {
        createdCount = 0;
        if (moonObject != null) Destroy(moonObject);
        moonObject = null;
    }

    // ────────────────────────────────────────────
    //  太陽生成
    // ────────────────────────────────────────────
    public CelestialBody CreateSun()
    {
        var data = new PlanetData
        {
            planetName    = "Sun",
            radius        = 3f,
            mass          = SunMass,
            color         = SunColor,
            planetType    = PlanetType.Gas,
            rotationSpeed = 0.5f
        };

        var obj = BuildSphere("Sun", Vector3.zero, data.radius, data.color, unlit: true);

        var light = obj.AddComponent<Light>();
        light.type      = LightType.Point;
        light.color     = new Color(1f, 0.95f, 0.82f);
        light.intensity = 5f;
        light.range     = 600f;

        var body = obj.AddComponent<CelestialBody>();
        body.Initialize(data, sun: true);

        GravitySystem.Instance.RegisterBody(body);
        return body;
    }

    // ────────────────────────────────────────────
    //  ① 太陽系フルセット生成（8惑星 + 視覚的な月）
    //
    //  質量: Sun=100 基準の実際の比率
    //    Mercury  1.66e-7 × 100 = 0.0000166
    //    Venus    2.45e-6 × 100 = 0.000245
    //    Earth    3.00e-6 × 100 = 0.0003
    //    Mars     3.23e-7 × 100 = 0.0000323
    //    Jupiter  9.55e-4 × 100 = 0.0955
    //    Saturn   2.86e-4 × 100 = 0.0286
    //    Uranus   4.37e-5 × 100 = 0.00437
    //    Neptune  5.15e-5 × 100 = 0.00515
    //
    //  距離: 地球=15 (1AU) 基準
    //    内惑星は実比率。外惑星は視認性のため圧縮。
    //    Jupiter: 5.2AU × 15 = 78 (実値通り)
    //    Saturn:  9.5AU → 120 (実値143を圧縮)
    //    Uranus: 19.2AU → 170 (実値288を圧縮)
    //    Neptune:30.1AU → 220 (実値451を圧縮)
    // ────────────────────────────────────────────
    public void CreateSolarSystem()
    {
        // 水星（Mercury）
        CreatePlanet(new PlanetData {
            planetName = "Mercury", radius = 0.38f, mass = 0.0000166f,
            color = MercuryColor, planetType = PlanetType.Rocky,
            orbitDistance = 6f, orbitSpeed = CalcOrbitalSpeed(6f), rotationSpeed = 3f
        });

        // 金星（Venus）
        CreatePlanet(new PlanetData {
            planetName = "Venus", radius = 0.90f, mass = 0.000245f,
            color = VenusColor, planetType = PlanetType.Rocky,
            orbitDistance = 11f, orbitSpeed = CalcOrbitalSpeed(11f), rotationSpeed = -2f
        });

        // ④ 地球（Earth）
        var earth = CreatePlanet(new PlanetData {
            planetName = "Earth", radius = 1.00f, mass = 0.0003f,
            color = EarthColor, planetType = PlanetType.Rocky,
            orbitDistance = 15f, orbitSpeed = CalcOrbitalSpeed(15f), rotationSpeed = 15f
        });

        // ④ 月（視覚専用、N体物理には参加しない）
        if (earth != null) CreateMoonVisual(earth);

        // 火星（Mars）
        CreatePlanet(new PlanetData {
            planetName = "Mars", radius = 0.53f, mass = 0.0000323f,
            color = MarsColor, planetType = PlanetType.Rocky,
            orbitDistance = 23f, orbitSpeed = CalcOrbitalSpeed(23f), rotationSpeed = 14f
        });

        // 木星（Jupiter） — 実距離比率 5.2AU
        CreatePlanet(new PlanetData {
            planetName = "Jupiter", radius = 2.50f, mass = 0.0955f,
            color = JupiterColor, planetType = PlanetType.Gas,
            orbitDistance = 78f, orbitSpeed = CalcOrbitalSpeed(78f), rotationSpeed = 35f
        });

        // 土星（Saturn）
        CreatePlanet(new PlanetData {
            planetName = "Saturn", radius = 2.20f, mass = 0.0286f,
            color = SaturnColor, planetType = PlanetType.Gas,
            orbitDistance = 120f, orbitSpeed = CalcOrbitalSpeed(120f), rotationSpeed = 32f
        });

        // 天王星（Uranus）
        CreatePlanet(new PlanetData {
            planetName = "Uranus", radius = 1.60f, mass = 0.00437f,
            color = UranusColor, planetType = PlanetType.Gas,
            orbitDistance = 170f, orbitSpeed = CalcOrbitalSpeed(170f), rotationSpeed = -20f
        });

        // 海王星（Neptune）
        CreatePlanet(new PlanetData {
            planetName = "Neptune", radius = 1.50f, mass = 0.00515f,
            color = NeptuneColor, planetType = PlanetType.Gas,
            orbitDistance = 220f, orbitSpeed = CalcOrbitalSpeed(220f), rotationSpeed = 22f
        });

        // ハレー彗星（Halley's Comet）— 逆行楕円軌道
        CreateHalleysComet();
    }

    // ────────────────────────────────────────────
    //  ハレー彗星生成（逆行楕円軌道）
    //
    //  実数値（1AU = 15 units に対応して圧縮）:
    //    近日点 0.586AU → 8.8 units
    //    遠日点 35.1AU → 256 units（海王星220との比率で圧縮）
    //    軌道周期 約75-76年  ※ゲーム内では速度×でほぼそれに対応
    //    逆行軌道（clockwise from above）
    // ────────────────────────────────────────────
    public CelestialBody CreateHalleysComet()
    {
        const float perihelion = 8.8f;
        const float aphelion   = 256f;
        float a = (perihelion + aphelion) * 0.5f;  // 半長軸

        float G = GravitySystem.Instance != null
            ? GravitySystem.Instance.GravitationalConstant : 1f;
        float M = GetSunMass();

        // 近日点速度: vis-viva  v = sqrt(G*M*(2/r - 1/a))
        float vPeri = Mathf.Sqrt(G * M * (2f / perihelion - 1f / a));

        var data = new PlanetData
        {
            planetName    = "Halley",
            radius        = 0.15f,
            mass          = 1e-6f,          // 惑星への重力影響なし
            color         = HalleyColor,
            planetType    = PlanetType.Rocky,
            rotationSpeed = 5f
        };

        // 近日点位置（+X軸上）
        var obj  = BuildSphere("Halley", new Vector3(perihelion, 0f, 0f), data.radius, data.color);
        var body = obj.AddComponent<CelestialBody>();
        body.Initialize(data);

        // 逆行（clockwise → 近日点で -Z 方向）
        body.SetVelocity(new Vector3(0f, 0f, -vPeri));

        GravitySystem.Instance.RegisterBody(body);
        return body;
    }

    // ────────────────────────────────────────────
    //  ④ 月（視覚専用）生成
    //  現実質量では地球のヒル圏(≈0.15 unit)が軌道半径(2.0 unit)より
    //  はるかに小さいため N体物理には参加せず視覚専用にする。
    // ────────────────────────────────────────────
    public void CreateMoonVisual(CelestialBody earth)
    {
        if (earth == null) return;

        float moonOrbitRadius = 2.0f;
        var moonData = new PlanetData
        {
            planetName = "Moon", radius = 0.27f,
            color = MoonColor, planetType = PlanetType.Rocky
        };

        moonObject = BuildSphere("Moon",
            earth.transform.position + Vector3.right * moonOrbitRadius,
            moonData.radius, moonData.color);

        var moonOrbit = moonObject.AddComponent<MoonOrbit>();
        moonOrbit.Initialize(earth.transform, moonOrbitRadius,
            startAngle: Random.Range(0f, Mathf.PI * 2f));
    }

    // ────────────────────────────────────────────
    //  汎用惑星生成（ユーザー操作用）
    // ────────────────────────────────────────────
    public CelestialBody CreatePlanet(PlanetData data)
    {
        if (GravitySystem.Instance.GetBodyCount() >= MaxBodies)
        {
            Debug.LogWarning("[PlanetFactory] 惑星上限 (30) に達しました。");
            return null;
        }

        // ③ 太陽以上の質量は作れない
        float currentSunMass = GetSunMass();
        if (data.mass >= currentSunMass)
        {
            Debug.LogWarning($"[PlanetFactory] 質量が太陽 ({currentSunMass:F0}) 以上のため作成不可。");
            return null;
        }

        createdCount++;
        if (string.IsNullOrEmpty(data.planetName) || data.planetName == "Planet")
            data.planetName = $"Planet {createdCount}";

        float   angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 pos   = new Vector3(
            Mathf.Cos(angle) * data.orbitDistance,
            0f,
            Mathf.Sin(angle) * data.orbitDistance);

        var obj  = BuildSphere(data.planetName, pos, data.radius, data.color);
        var body = obj.AddComponent<CelestialBody>();
        body.Initialize(data);

        Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        body.SetVelocity(tangent * data.orbitSpeed);

        GravitySystem.Instance.RegisterBody(body);
        return body;
    }

    // ────────────────────────────────────────────
    //  現在の太陽質量を取得
    // ────────────────────────────────────────────
    public static float GetSunMass()
    {
        if (GravitySystem.Instance != null)
            foreach (var b in GravitySystem.Instance.GetBodies())
                if (b != null && b.isSun) return b.Mass;
        return SunMass;
    }

    // ────────────────────────────────────────────
    //  円軌道速度計算  v = sqrt(G * M_sun / r)
    // ────────────────────────────────────────────
    public static float CalcOrbitalSpeed(float distance, float sunMass = -1f)
    {
        if (sunMass < 0f) sunMass = GetSunMass();
        float G = GravitySystem.Instance != null
            ? GravitySystem.Instance.GravitationalConstant : 1f;
        return Mathf.Sqrt(G * sunMass / Mathf.Max(distance, 0.1f));
    }

    // ────────────────────────────────────────────
    //  球体 GameObject 生成
    // ────────────────────────────────────────────
    private static GameObject BuildSphere(string objName, Vector3 position,
        float radius, Color color, bool unlit = false)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = objName;
        obj.transform.position   = position;
        obj.transform.localScale = Vector3.one * radius * 2f;
        Object.Destroy(obj.GetComponent<SphereCollider>());

        var renderer = obj.GetComponent<Renderer>();

        // Shader.Find はビルド環境によって失敗するため、
        // まず Shader.Find を試み、失敗した場合は renderer.material
        // （CreatePrimitive のデフォルトマテリアルのインスタンスコピー）を使う。
        // デフォルトマテリアルのシェーダはビルドに必ず含まれるため安全。

        if (unlit)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetColor("_BaseColor", color * 2f);
                mat.SetColor("_Color",     color * 2f);
                renderer.material = mat;
                return obj;
            }
            // フォールバック: 既存マテリアルにEmissionを付与して明るく見せる
            var m = renderer.material;
            m.color = color * 1.5f;
            m.SetColor("_BaseColor", color * 1.5f);
            m.SetColor("_Color",     color * 1.5f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color);
            return obj;
        }

        // 通常惑星: renderer.material (sharedMaterial の新インスタンス) に直接色を設定
        var mat2 = renderer.material;
        mat2.color = color;
        mat2.SetColor("_BaseColor", color);
        mat2.SetColor("_Color",     color);
        return obj;
    }
}
