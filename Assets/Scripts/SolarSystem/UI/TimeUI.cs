using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 時間制御UI + モード表示 + 統計。
/// ① 画面上部中央に現在のモード（地動説/天動説/地球視点）を大きく表示。
/// ② ボタンを大きめにしてスマホでも判読できるよう調整。
/// </summary>
public class TimeUI : MonoBehaviour
{
    private Canvas   canvas;
    private Text     infoText;
    private Text     modeText;       // ① 上部中央モード表示
    private Button[] speedButtons = new Button[4];
    private Button   pauseButton;
    private Text     pauseLabel;
    private Button   camModeButton;
    private Text     camModeLabel;
    private Button   geoModeButton;
    private Text     geoModeLabel;
    private Button   resetButton;
    private Button   alignButton;
    private Button   clearTrailButton;

    private Font uiFont;

    private static readonly Color NormalColor     = new Color(0.15f, 0.15f, 0.20f, 0.88f);
    private static readonly Color SelectedColor   = new Color(0.15f, 0.55f, 0.85f, 0.95f);
    private static readonly Color PauseColor      = new Color(0.70f, 0.30f, 0.10f, 0.92f);
    private static readonly Color EarthViewColor  = new Color(0.10f, 0.50f, 0.25f, 0.92f);
    private static readonly Color GeocentricColor = new Color(0.55f, 0.35f, 0.10f, 0.92f);
    private static readonly Color ResetColor      = new Color(0.70f, 0.15f, 0.15f, 0.95f);
    private static readonly Color AlignColor      = new Color(0.20f, 0.55f, 0.30f, 0.95f);
    private static readonly Color ClearTrailColor = new Color(0.25f, 0.35f, 0.60f, 0.95f);

    // ① モード名マッピング
    private static readonly string[] ModeNames = {
        "地動説  ( 太陽中心 )",
        "地球視点  ( 地上観測 )",
        "天動説  ( 地球中心 )"
    };

    void Start()
    {
        uiFont = GetFont();
        BuildCanvas();
    }

    void Update()
    {
        RefreshHighlight();
        UpdateInfoText();
        UpdateModeText();

        if (Input.GetKeyDown(KeyCode.Space))  SimulationManager.Instance?.TogglePause();
        if (Input.GetKeyDown(KeyCode.Alpha1)) TimeController.Instance?.SetSpeed(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TimeController.Instance?.SetSpeed(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TimeController.Instance?.SetSpeed(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TimeController.Instance?.SetSpeed(3);

        if (Input.GetKeyDown(KeyCode.E))
            Camera.main?.GetComponent<CameraMode>()?.ToggleMode();
        if (Input.GetKeyDown(KeyCode.G))
            Camera.main?.GetComponent<CameraMode>()?.ToggleGeocentricMode();
        if (Input.GetKeyDown(KeyCode.R))
            GameManager.ResetSimulation();
        if (Input.GetKeyDown(KeyCode.A))
            GameManager.AlignPlanets();
        if (Input.GetKeyDown(KeyCode.C))
            GameManager.ClearAllOrbitTrails();
    }

    // ────────────────────────────────────────────
    //  Canvas 構築
    // ────────────────────────────────────────────
    private void BuildCanvas()
    {
        var go = new GameObject("TimeCanvas");
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0f;
        go.AddComponent<GraphicRaycaster>();

        // ── ① 上部中央モードテキスト ─────────────────
        modeText = MakeText(go, "地動説  ( 太陽中心 )",
            new Vector2(0f, -10f),
            new Vector2(600f, 50f),
            new Vector2(0.5f, 1f),
            TextAnchor.MiddleCenter, 24, bold: true);
        modeText.color = new Color(0.95f, 0.92f, 0.75f);

        // ────────────────────────────────────────────
        // ② ボタン — 行1（右上）: 速度 + ポーズ
        //    ボタンサイズを大きくし、日本語が収まるよう幅確保
        // ────────────────────────────────────────────
        float bH = 44f;

        // 速度ボタン群（右から: PAUSE → 1000x → 100x → 10x → 1x）
        pauseButton = MakeButton(go, "▶ PLAY",
            new Vector2(-10f, -10f), new Vector2(100f, bH), new Vector2(1f, 1f));
        pauseButton.onClick.AddListener(() => SimulationManager.Instance?.TogglePause());
        pauseLabel = pauseButton.GetComponentInChildren<Text>();

        float[] speedW = { 55f, 60f, 65f, 75f };
        float   sx     = -115f;
        for (int i = 3; i >= 0; i--)
        {
            int idx = i;
            speedButtons[i] = MakeButton(go,
                TimeController.SpeedLabels[i],
                new Vector2(sx, -10f),
                new Vector2(speedW[i], bH),
                new Vector2(1f, 1f));
            speedButtons[i].onClick.AddListener(() => TimeController.Instance?.SetSpeed(idx));
            sx -= speedW[i] + 5f;
        }

        // ── 行2（右上 2段目）: カメラモード + リセット + 直列 ──
        float y2 = -10f - bH - 6f;

        resetButton = MakeButton(go, "[R] リセット",
            new Vector2(-10f, y2), new Vector2(120f, bH), new Vector2(1f, 1f));
        resetButton.onClick.AddListener(GameManager.ResetSimulation);
        resetButton.GetComponent<Image>().color = ResetColor;

        alignButton = MakeButton(go, "[A] 直列",
            new Vector2(-135f, y2), new Vector2(100f, bH), new Vector2(1f, 1f));
        alignButton.onClick.AddListener(GameManager.AlignPlanets);
        alignButton.GetComponent<Image>().color = AlignColor;

        // 軌道クリアボタン
        clearTrailButton = MakeButton(go, "[C] 軌道クリア",
            new Vector2(-240f, y2), new Vector2(120f, bH), new Vector2(1f, 1f));
        clearTrailButton.onClick.AddListener(GameManager.ClearAllOrbitTrails);
        clearTrailButton.GetComponent<Image>().color = ClearTrailColor;

        camModeButton = MakeButton(go, "[E] 地球視点",
            new Vector2(-365f, y2), new Vector2(135f, bH), new Vector2(1f, 1f));
        camModeButton.onClick.AddListener(() =>
            Camera.main?.GetComponent<CameraMode>()?.ToggleMode());
        camModeLabel = camModeButton.GetComponentInChildren<Text>();

        geoModeButton = MakeButton(go, "[G] 天動説",
            new Vector2(-505f, y2), new Vector2(125f, bH), new Vector2(1f, 1f));
        geoModeButton.onClick.AddListener(() =>
            Camera.main?.GetComponent<CameraMode>()?.ToggleGeocentricMode());
        geoModeLabel = geoModeButton.GetComponentInChildren<Text>();

        // ── 統計テキスト ─────────────────────────
        float y3 = y2 - bH - 6f;
        infoText = MakeText(go, "",
            new Vector2(-10f, y3),
            new Vector2(250f, 120f),
            new Vector2(1f, 1f),
            TextAnchor.UpperRight, 13);

        RefreshHighlight();
    }

    // ────────────────────────────────────────────
    //  毎フレーム更新
    // ────────────────────────────────────────────
    private void RefreshHighlight()
    {
        if (TimeController.Instance == null) return;

        int cur = TimeController.Instance.SpeedIndex;
        for (int i = 0; i < speedButtons.Length; i++)
        {
            if (speedButtons[i] == null) continue;
            var img = speedButtons[i].GetComponent<Image>();
            if (img != null) img.color = (i == cur) ? SelectedColor : NormalColor;
        }

        if (pauseLabel == null || SimulationManager.Instance == null) return;
        bool paused = SimulationManager.Instance.IsPaused;
        pauseLabel.text = paused ? "▶ PLAY" : "⏸ PAUSE";
        var pb = pauseButton.GetComponent<Image>();
        if (pb != null) pb.color = paused ? PauseColor : NormalColor;

        var camMode = Camera.main?.GetComponent<CameraMode>();
        if (camMode != null)
        {
            bool isEarth = camMode.CurrentMode == CameraMode.Mode.EarthView;
            if (camModeLabel != null)
                camModeLabel.text = isEarth ? "[E] 俯瞰" : "[E] 地球視点";
            var cb = camModeButton?.GetComponent<Image>();
            if (cb != null) cb.color = isEarth ? EarthViewColor : NormalColor;

            bool isGeo = camMode.CurrentMode == CameraMode.Mode.Geocentric;
            if (geoModeLabel != null)
                geoModeLabel.text = isGeo ? "[G] 俯瞰" : "[G] 天動説";
            var gb = geoModeButton?.GetComponent<Image>();
            if (gb != null) gb.color = isGeo ? GeocentricColor : NormalColor;
        }
    }

    // ① 上部中央のモードラベル更新
    private void UpdateModeText()
    {
        if (modeText == null) return;
        var camMode = Camera.main?.GetComponent<CameraMode>();
        if (camMode == null) return;

        modeText.text = camMode.CurrentMode switch
        {
            CameraMode.Mode.EarthView  => "◉  地球視点  ( 地上観測 )",
            CameraMode.Mode.Geocentric => "◉  天動説  ( 地球中心 )",
            _                          => "◉  地動説  ( 太陽中心 )"
        };
    }

    // ⑤ 統計テキスト（経過年数含む）
    private void UpdateInfoText()
    {
        if (infoText == null) return;
        var sim  = SimulationManager.Instance;
        var grav = GravitySystem.Instance;
        if (sim == null || grav == null) return;

        infoText.text =
            $"天体数 : {grav.GetBodyCount()}/30\n" +
            $"経過   : {sim.GetFormattedTime()}\n" +
            $"速度   : {TimeController.Instance?.SimulationSpeed:F0}x";
    }

    // ────────────────────────────────────────────
    //  UI ヘルパー
    // ────────────────────────────────────────────
    private Button MakeButton(GameObject parent, string label,
        Vector2 anchoredPos, Vector2 size, Vector2 anchor)
    {
        var obj = new GameObject(label + "_Btn");
        obj.transform.SetParent(parent.transform, false);

        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img = obj.AddComponent<Image>();
        img.color = NormalColor;

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;

        var txtObj = new GameObject("Label");
        txtObj.transform.SetParent(obj.transform, false);
        var txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        txtRt.offsetMin = new Vector2(4, 2);
        txtRt.offsetMax = new Vector2(-4, -2);

        var txt = txtObj.AddComponent<Text>();
        txt.text                   = label;
        txt.fontSize               = 18;
        txt.color                  = Color.white;
        txt.alignment              = TextAnchor.MiddleCenter;
        txt.resizeTextForBestFit   = true;
        txt.resizeTextMinSize      = 10;
        txt.resizeTextMaxSize      = 20;
        txt.horizontalOverflow     = HorizontalWrapMode.Wrap;
        txt.verticalOverflow       = VerticalWrapMode.Overflow;
        if (uiFont != null) txt.font = uiFont;

        return btn;
    }

    private Text MakeText(GameObject parent, string content,
        Vector2 anchoredPos, Vector2 size, Vector2 anchor,
        TextAnchor alignment = TextAnchor.MiddleLeft, int fontSize = 13, bool bold = false)
    {
        var obj = new GameObject("InfoText");
        obj.transform.SetParent(parent.transform, false);

        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var txt = obj.AddComponent<Text>();
        txt.text      = content;
        txt.fontSize  = fontSize;
        txt.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        txt.color     = new Color(0.9f, 0.9f, 0.9f, 0.92f);
        txt.alignment = alignment;
        if (uiFont != null) txt.font = uiFont;

        return txt;
    }

    private static Font GetFont()
    {
        Font f = Resources.Load<Font>("Fonts/NotoSansJP");
        if (f != null) return f;
        f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }
}
