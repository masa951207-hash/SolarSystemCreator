using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 時間制御UI + シミュレーション統計表示。
/// 画面右上に速度ボタン、ポーズボタン、情報テキストを自動生成する。
/// </summary>
public class TimeUI : MonoBehaviour
{
    private Canvas   canvas;
    private Text     infoText;
    private Button[] speedButtons = new Button[4];
    private Button   pauseButton;
    private Text     pauseLabel;
    private Button   camModeButton;
    private Text     camModeLabel;
    private Button   geoModeButton;
    private Text     geoModeLabel;
    private Button   resetButton;
    private Button   alignButton;

    private Font uiFont;

    private static readonly Color NormalColor    = new Color(0.15f, 0.15f, 0.2f,  0.85f);
    private static readonly Color SelectedColor  = new Color(0.15f, 0.55f, 0.85f, 0.95f);
    private static readonly Color PauseColor     = new Color(0.7f,  0.3f,  0.1f,  0.9f);
    private static readonly Color EarthViewColor  = new Color(0.1f,  0.5f,  0.25f, 0.9f);
    private static readonly Color GeocentricColor = new Color(0.55f, 0.35f, 0.10f, 0.9f);
    private static readonly Color ResetColor     = new Color(0.7f,  0.15f, 0.15f, 0.95f);
    private static readonly Color AlignColor     = new Color(0.2f,  0.55f, 0.30f, 0.95f);

    void Start()
    {
        uiFont = GetFont();
        BuildCanvas();
    }

    void Update()
    {
        RefreshHighlight();
        UpdateInfoText();

        // キーボードショートカット
        if (Input.GetKeyDown(KeyCode.Space))  SimulationManager.Instance?.TogglePause();
        if (Input.GetKeyDown(KeyCode.Alpha1)) TimeController.Instance?.SetSpeed(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TimeController.Instance?.SetSpeed(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TimeController.Instance?.SetSpeed(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TimeController.Instance?.SetSpeed(3);

        // ⑤ [E] で地球視点 / 俯瞰モードトグル
        if (Input.GetKeyDown(KeyCode.E))
            Camera.main?.GetComponent<CameraMode>()?.ToggleMode();

        // [G] で天動説モード / 俯瞰モードトグル
        if (Input.GetKeyDown(KeyCode.G))
            Camera.main?.GetComponent<CameraMode>()?.ToggleGeocentricMode();

        // [R] でリセット
        if (Input.GetKeyDown(KeyCode.R))
            GameManager.ResetSimulation();

        // [A] で惑星直列
        if (Input.GetKeyDown(KeyCode.A))
            GameManager.AlignPlanets();
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
        go.AddComponent<GraphicRaycaster>();

        // ── 速度ボタン群（右上）──────────────────
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            speedButtons[i] = MakeButton(go,
                TimeController.SpeedLabels[i],
                new Vector2(-230f + i * 55f, -10f),
                new Vector2(50f, 28f),
                new Vector2(1f, 1f));
            speedButtons[i].onClick.AddListener(() => TimeController.Instance?.SetSpeed(idx));
        }

        // ── ポーズボタン ─────────────────────────
        pauseButton = MakeButton(go, "▶ PLAY",
            new Vector2(-10f, -10f),
            new Vector2(70f, 28f),
            new Vector2(1f, 1f));
        pauseButton.onClick.AddListener(() => SimulationManager.Instance?.TogglePause());
        pauseLabel = pauseButton.GetComponentInChildren<Text>();

        // ── ⑤ 地球視点ボタン（速度ボタン左） ──────
        camModeButton = MakeButton(go, "[E] 地球視点",
            new Vector2(-295f, -10f),
            new Vector2(60f, 28f),
            new Vector2(1f, 1f));
        camModeButton.onClick.AddListener(() =>
            Camera.main?.GetComponent<CameraMode>()?.ToggleMode());
        camModeLabel = camModeButton.GetComponentInChildren<Text>();

        // ── 天動説ボタン（地球視点ボタン左）──────
        geoModeButton = MakeButton(go, "[G] 天動説",
            new Vector2(-360f, -10f),
            new Vector2(60f, 28f),
            new Vector2(1f, 1f));
        geoModeButton.onClick.AddListener(() =>
            Camera.main?.GetComponent<CameraMode>()?.ToggleGeocentricMode());
        geoModeLabel = geoModeButton.GetComponentInChildren<Text>();

        // ── リセットボタン（右上2段目の右端）─────────
        resetButton = MakeButton(go, "[R] リセット",
            new Vector2(-10f, -46f),
            new Vector2(90f, 28f),
            new Vector2(1f, 1f));
        resetButton.onClick.AddListener(GameManager.ResetSimulation);
        var resetImg = resetButton.GetComponent<Image>();
        if (resetImg != null) resetImg.color = ResetColor;

        // ── 惑星直列ボタン（リセット左隣）────────────
        alignButton = MakeButton(go, "[A] 直列",
            new Vector2(-105f, -46f),
            new Vector2(90f, 28f),
            new Vector2(1f, 1f));
        alignButton.onClick.AddListener(GameManager.AlignPlanets);
        var alignImg = alignButton.GetComponent<Image>();
        if (alignImg != null) alignImg.color = AlignColor;

        // ── 統計テキスト ─────────────────────────
        infoText = MakeText(go, "",
            new Vector2(-10f, -82f),
            new Vector2(230f, 110f),
            new Vector2(1f, 1f),
            TextAnchor.UpperRight, 11);

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

        // カメラモードボタンの色更新
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

    private void UpdateInfoText()
    {
        if (infoText == null) return;
        var sim  = SimulationManager.Instance;
        var grav = GravitySystem.Instance;
        if (sim == null || grav == null) return;

        var camMode = Camera.main?.GetComponent<CameraMode>();
        string modeStr = camMode?.CurrentMode == CameraMode.Mode.EarthView
            ? "地球視点" : "俯瞰";

        infoText.text =
            $"天体数 : {grav.GetBodyCount()}/30\n" +
            $"経過時間: {sim.GetFormattedTime()}\n" +
            $"速度   : {TimeController.Instance?.SimulationSpeed:F0}x\n" +
            $"視点   : {modeStr}\n" +
            $"\n[Space] ポーズ  [1-4] 速度\n[T] 惑星パネル  [E] 地球視点\n[G] 天動説  [A] 直列  [R] リセット";
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

        var txt = txtObj.AddComponent<Text>();
        txt.text      = label;
        txt.fontSize  = 11;
        txt.color     = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        if (uiFont != null) txt.font = uiFont;

        return btn;
    }

    private Text MakeText(GameObject parent, string content,
        Vector2 anchoredPos, Vector2 size, Vector2 anchor,
        TextAnchor alignment = TextAnchor.MiddleLeft, int fontSize = 12)
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
        txt.color     = new Color(0.9f, 0.9f, 0.9f, 0.85f);
        txt.alignment = alignment;
        if (uiFont != null) txt.font = uiFont;

        return txt;
    }

    private static Font GetFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }
}
