using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        EnsureSystems();
    }

    void Start()
    {
        SetupCamera();

        try
        {
            InitializeScene();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManager] 初期化エラー: {e}");
        }
    }

    private void EnsureSystems()
    {
        EnsureComponent<GravitySystem>("GravitySystem");
        EnsureComponent<PlanetFactory>("PlanetFactory");
        EnsureComponent<TimeController>("TimeController");
        EnsureComponent<SimulationManager>("SimulationManager");
    }

    private void EnsureComponent<T>(string goName) where T : MonoBehaviour
    {
        if (FindFirstObjectByType<T>() == null)
            new GameObject(goName).AddComponent<T>();
    }

    private void InitializeScene()
    {
        PlanetFactory.Instance.CreateSun();
        PlanetFactory.Instance.CreateSolarSystem();

        SetupLighting();
        BuildUI();
    }

    // ────────────────────────────────────────────
    //  ④ ライティング設定（惑星が鮮明に見える明るさに）
    // ────────────────────────────────────────────
    private void SetupLighting()
    {
        // アンビエントライト：惑星の色が識別できる十分な明るさ
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.52f);

        // 既存のDirectional Light を強化。なければ新規作成。
        Light dirLight = FindFirstObjectByType<Light>();
        if (dirLight != null && dirLight.type == LightType.Directional)
        {
            dirLight.color     = new Color(0.95f, 0.90f, 0.85f);
            dirLight.intensity = 0.7f;
            dirLight.transform.rotation = Quaternion.Euler(45f, 30f, 0f);
        }
        else
        {
            // Directional Light を自動生成
            var dlGo = new GameObject("DirectionalLight");
            dirLight = dlGo.AddComponent<Light>();
            dirLight.type      = LightType.Directional;
            dirLight.color     = new Color(0.95f, 0.90f, 0.85f);
            dirLight.intensity = 0.7f;
            dirLight.transform.rotation = Quaternion.Euler(45f, 30f, 0f);
        }
    }

    // ────────────────────────────────────────────
    //  カメラセットアップ
    // ────────────────────────────────────────────
    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);
        cam.farClipPlane    = 3000f;   // 海王星(220)まで確実に描画
        cam.fieldOfView     = 60f;
        // 初期位置は CameraMode の orbitDist に合わせる（後で上書きされる）
        cam.transform.position = new Vector3(0f, 100f, -30f);
        cam.transform.rotation = Quaternion.Euler(72f, 0f, 0f);

        // URP カメラ設定
        var urpData = cam.gameObject.GetComponent<UniversalAdditionalCameraData>();
        if (urpData == null)
            urpData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderShadows        = false;
        urpData.antialiasing         = AntialiasingMode.None;
        urpData.renderPostProcessing = false;

        if (cam.GetComponent<CameraMode>() == null)
            cam.gameObject.AddComponent<CameraMode>();
    }

    private void BuildUI()
    {
        var uiRoot = new GameObject("UIRoot");

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            esGo.transform.SetParent(uiRoot.transform);
        }

        new GameObject("TimeUI",            typeof(TimeUI)).transform.SetParent(uiRoot.transform);
        new GameObject("CreatePlanetPanel", typeof(CreatePlanetPanel)).transform.SetParent(uiRoot.transform);
        new GameObject("PlanetListUI",      typeof(PlanetListUI)).transform.SetParent(uiRoot.transform);
    }

    // ────────────────────────────────────────────
    //  惑星直列：全惑星を太陽から同一方向に整列
    // ────────────────────────────────────────────
    public static void AlignPlanets()
    {
        if (GravitySystem.Instance == null) return;

        foreach (var body in GravitySystem.Instance.GetBodies())
        {
            if (body == null || body.isSun) continue;

            float dist = body.transform.position.magnitude;
            if (dist < 0.001f) continue;

            // +X 軸上に再配置
            body.transform.position = new Vector3(dist, 0f, 0f);

            // 円軌道速度（反時計回り = +Z 方向）で再設定
            float speed = PlanetFactory.CalcOrbitalSpeed(dist);
            body.SetVelocity(new Vector3(0f, 0f, speed));

            body.ClearTrail();
        }

        // 位置変更後に加速度を再計算させる
        GravitySystem.Instance.InvalidateAccelerations();
    }

    // ────────────────────────────────────────────
    //  リセット：全天体を破棄して太陽系を再生成
    // ────────────────────────────────────────────
    public static void ResetSimulation()
    {
        // カメラモードをTopDownに戻す
        Camera.main?.GetComponent<CameraMode>()?.OnReset();

        // 全天体（CelestialBody）を破棄してGravitySystemをクリア
        GravitySystem.Instance?.Clear();

        // 月（視覚専用）も破棄
        var moonGo = GameObject.Find("Moon");
        if (moonGo != null) Destroy(moonGo);

        // カウンターと経過時間をリセット
        PlanetFactory.Instance?.ResetCount();
        SimulationManager.Instance?.ResetTime();
        SimulationManager.Instance?.Resume();

        // 太陽系を再生成
        if (PlanetFactory.Instance != null)
        {
            PlanetFactory.Instance.CreateSun();
            PlanetFactory.Instance.CreateSolarSystem();
        }
    }
}
