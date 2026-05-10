using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 惑星生成パネル。画面左側に自動生成される。
/// スライダーで各パラメータを設定し "Create Planet" ボタンで生成。
/// </summary>
public class CreatePlanetPanel : MonoBehaviour
{
    // ── スライダー ────────────────────────────
    private Slider sizeSlider;
    private Slider massSlider;
    private Slider distSlider;
    private Slider speedSlider;
    private Slider rotSlider;

    // ── ラベル ────────────────────────────────
    private Text sizeLabel;
    private Text massLabel;
    private Text distLabel;
    private Text speedLabel;
    private Text rotLabel;
    private Text warningLabel;

    // ── カラー ────────────────────────────────
    private Color   selectedColor = new Color(0.4f, 0.8f, 1f);
    private Image   colorPreview;

    // ── パネル参照 ────────────────────────────
    private GameObject  panel;
    private bool        isOpen = true;
    private Font        uiFont;

    // ── 警告表示タイマー ──────────────────────
    private float warningTimer = 0f;

    // ── カラープリセット ───────────────────────
    private static readonly Color[] ColorPresets =
    {
        new Color(0.4f,  0.8f,  1.0f),   // 水色
        new Color(0.9f,  0.3f,  0.15f),  // 火星赤
        new Color(0.95f, 0.75f, 0.3f),   // 砂漠黄
        new Color(0.55f, 0.35f, 0.9f),   // 紫
        new Color(0.3f,  0.9f,  0.45f),  // 緑
        new Color(0.9f,  0.9f,  0.9f),   // 白
    };

    // ── パネル色定数 ──────────────────────────
    private static readonly Color PanelBg     = new Color(0.05f, 0.05f, 0.1f,  0.88f);
    private static readonly Color CreateBtnBg = new Color(0.15f, 0.55f, 0.2f,  0.95f);
    private static readonly Color ToggleBg    = new Color(0.1f,  0.1f,  0.2f,  0.9f);

    // ────────────────────────────────────────────
    void Start()
    {
        uiFont = GetFont();
        BuildCanvas();
    }

    void Update()
    {
        // [T] でパネルトグル
        if (Input.GetKeyDown(KeyCode.T))
            TogglePanel();

        // 警告ラベルのタイマー
        if (warningTimer > 0f)
        {
            warningTimer -= Time.deltaTime;
            if (warningTimer <= 0f && warningLabel != null)
                warningLabel.text = "";
        }
    }

    // ────────────────────────────────────────────
    //  Canvas & パネル構築
    // ────────────────────────────────────────────
    private void BuildCanvas()
    {
        var root = new GameObject("PlanetCreatorCanvas");
        var cv = root.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        root.AddComponent<GraphicRaycaster>();

        // ── トグルボタン（常時表示）──────────────
        var toggleBtn = MakeButton(root, "[ T ] 惑星を追加",
            new Vector2(10, -10), new Vector2(180, 28),
            new Vector2(0, 1), ToggleBg);
        toggleBtn.onClick.AddListener(TogglePanel);

        // ── メインパネル ─────────────────────────
        panel = MakePanel(root, new Vector2(10, -46), new Vector2(240, 450));

        float y = -14f;

        // スライダー5本
        (sizeSlider,  sizeLabel)  = MakeSliderRow(panel, "サイズ",    0.3f, 3.0f,                        1.0f, ref y);
        // ③ 質量上限は太陽質量 - 1（太陽以上は作成不可）
        (massSlider,  massLabel)  = MakeSliderRow(panel, "質量",      0.1f, PlanetFactory.SunMass - 1f,  1.0f, ref y);
        (distSlider,  distLabel)  = MakeSliderRow(panel, "公転距離",  5f,   250f,                        20f,  ref y);
        (speedSlider, speedLabel) = MakeSliderRow(panel, "公転速度",  0f,   10f,                         3f,   ref y);
        (rotSlider,   rotLabel)   = MakeSliderRow(panel, "自転速度",  0f,   50f,                         10f,  ref y);

        y -= 6f;

        // カラーピッカー
        MakeColorRow(panel, ref y);
        y -= 10f;

        // 軌道速度自動計算
        distSlider.onValueChanged.AddListener(_ => AutoCalcSpeed());

        // 全スライダーの値変化 → ラベル更新
        foreach (var sl in new[] { sizeSlider, massSlider, distSlider, speedSlider, rotSlider })
            sl.onValueChanged.AddListener(_ => RefreshLabels());

        // 生成ボタン
        var createBtn = MakeButton(panel, "✦  Create Planet",
            new Vector2(0, y), new Vector2(210, 38),
            new Vector2(0.5f, 1f), CreateBtnBg);
        createBtn.onClick.AddListener(DoCreate);
        y -= 44f;

        // ③ 警告ラベル
        warningLabel = MakeLabel(panel, "", new Vector2(10, y), new Vector2(220, 36));
        warningLabel.color     = new Color(1f, 0.4f, 0.4f);
        warningLabel.fontSize  = 11;
        warningLabel.alignment = TextAnchor.UpperLeft;

        RefreshLabels();
    }

    // ────────────────────────────────────────────
    //  ラベル更新
    // ────────────────────────────────────────────
    // 地球の参考値（PlanetFactory の定数と同じ値）
    private const float EarthRadius    = 1.00f;
    private const float EarthMass      = 0.0003f;
    private const float EarthOrbitDist = 15.0f;
    private const float EarthRotSpeed  = 15.0f;

    private void RefreshLabels()
    {
        float earthOrbitSpeed = PlanetFactory.CalcOrbitalSpeed(EarthOrbitDist);
        if (sizeLabel  != null) sizeLabel.text  = $"サイズ: {sizeSlider.value:F2}（地球: {EarthRadius:F2}）";
        if (massLabel  != null) massLabel.text  = $"質量: {massSlider.value:F4}（地球: {EarthMass:F4}）";
        if (distLabel  != null) distLabel.text  = $"公転距離: {distSlider.value:F1}（地球: {EarthOrbitDist}）";
        if (speedLabel != null) speedLabel.text = $"公転速度: {speedSlider.value:F2}（地球: {earthOrbitSpeed:F2}）";
        if (rotLabel   != null) rotLabel.text   = $"自転速度: {rotSlider.value:F1}（地球: {EarthRotSpeed}）";
    }

    private void AutoCalcSpeed()
    {
        float calc = PlanetFactory.CalcOrbitalSpeed(distSlider.value);
        speedSlider.value = Mathf.Clamp(calc, speedSlider.minValue, speedSlider.maxValue);
    }

    // ────────────────────────────────────────────
    //  惑星生成
    // ────────────────────────────────────────────
    private void DoCreate()
    {
        if (PlanetFactory.Instance == null) return;

        // ③ 太陽以上の質量は作れない
        float sunMass = PlanetFactory.GetSunMass();
        if (massSlider.value >= sunMass)
        {
            ShowWarning($"質量が太陽 ({sunMass:F0}) 以上のため\n作成できません");
            return;
        }

        bool isGas = sizeSlider.value > 2.0f;
        var data = new PlanetData
        {
            planetName    = "Planet",
            radius        = sizeSlider.value,
            mass          = massSlider.value,
            color         = selectedColor,
            planetType    = isGas ? PlanetType.Gas : PlanetType.Rocky,
            orbitDistance = distSlider.value,
            orbitSpeed    = speedSlider.value,
            rotationSpeed = rotSlider.value
        };

        var result = PlanetFactory.Instance.CreatePlanet(data);
        if (result == null && massSlider.value < sunMass)
            ShowWarning("天体数が上限 (30) に達しています");
    }

    private void ShowWarning(string msg)
    {
        if (warningLabel == null) return;
        warningLabel.text = msg;
        warningTimer = 3.5f;
    }

    private void TogglePanel()
    {
        isOpen = !isOpen;
        if (panel != null) panel.SetActive(isOpen);
    }

    // ────────────────────────────────────────────
    //  カラー選択行
    // ────────────────────────────────────────────
    private void MakeColorRow(GameObject parent, ref float y)
    {
        MakeLabel(parent, "カラー :", new Vector2(10, y), new Vector2(60, 20));

        float px = 75f;
        for (int i = 0; i < ColorPresets.Length; i++)
        {
            Color c   = ColorPresets[i];
            int   idx = i;

            var dot = MakeColorDot(parent, c, new Vector2(px, y));
            dot.onClick.AddListener(() => {
                selectedColor = ColorPresets[idx];
                if (colorPreview != null) colorPreview.color = selectedColor;
            });
            px += 25f;
        }

        var previewObj = new GameObject("ColorPreview");
        previewObj.transform.SetParent(parent.transform, false);
        var rt = previewObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(px + 5f, y);
        rt.sizeDelta = new Vector2(22, 22);
        colorPreview = previewObj.AddComponent<Image>();
        colorPreview.color = selectedColor;

        y -= 32f;
    }

    // ────────────────────────────────────────────
    //  UI ヘルパー
    // ────────────────────────────────────────────

    private (Slider slider, Text label) MakeSliderRow(
        GameObject parent, string title,
        float min, float max, float defaultVal,
        ref float y)
    {
        var lbl = MakeLabel(parent, $"{title} : {defaultVal:F2}",
            new Vector2(10, y), new Vector2(220, 20));
        y -= 22f;

        var sl = MakeSlider(parent, min, max, defaultVal,
            new Vector2(10, y), new Vector2(220, 20));
        y -= 28f;

        return (sl, lbl);
    }

    private Slider MakeSlider(GameObject parent, float min, float max, float val,
        Vector2 anchoredPos, Vector2 size)
    {
        var obj = new GameObject("Slider");
        obj.transform.SetParent(parent.transform, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var slider = obj.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value    = val;

        var bg    = MakeChild(obj, "Background", new Vector2(0f, 0.25f), new Vector2(1f, 0.75f));
        bg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);

        var fa   = MakeChild(obj, "Fill Area", new Vector2(0f, 0.25f), new Vector2(1f, 0.75f));
        var faRt = fa.GetComponent<RectTransform>();
        faRt.offsetMin = new Vector2(5, 0);
        faRt.offsetMax = new Vector2(-15, 0);

        var fill    = MakeChild(fa, "Fill", Vector2.zero, Vector2.one);
        var fillRt  = fill.GetComponent<RectTransform>();
        fillRt.sizeDelta = new Vector2(10, 0);
        fill.AddComponent<Image>().color = new Color(0.3f, 0.65f, 1f);
        slider.fillRect = fillRt;

        var ha   = MakeChild(obj, "Handle Slide Area", Vector2.zero, Vector2.one);
        var haRt = ha.GetComponent<RectTransform>();
        haRt.offsetMin = new Vector2(10, 0);
        haRt.offsetMax = new Vector2(-10, 0);

        var handle    = MakeChild(ha, "Handle", Vector2.zero, Vector2.zero);
        var handleRt  = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(20, 0);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        slider.handleRect    = handleRt;
        slider.targetGraphic = handleImg;

        return slider;
    }

    private GameObject MakeChild(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = Vector2.zero;
        return obj;
    }

    private Text MakeLabel(GameObject parent, string text, Vector2 pos, Vector2 size)
    {
        var obj = new GameObject("Label_" + text);
        obj.transform.SetParent(parent.transform, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var t = obj.AddComponent<Text>();
        t.text      = text;
        t.fontSize  = 12;
        t.color     = new Color(0.9f, 0.9f, 0.9f);
        t.alignment = TextAnchor.MiddleLeft;
        if (uiFont != null) t.font = uiFont;
        return t;
    }

    private Button MakeButton(GameObject parent, string label,
        Vector2 anchoredPos, Vector2 size, Vector2 anchor, Color bg)
    {
        var obj = new GameObject("Btn_" + label);
        obj.transform.SetParent(parent.transform, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot     = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img = obj.AddComponent<Image>();
        img.color = bg;

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
        txt.fontSize  = 13;
        txt.color     = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        if (uiFont != null) txt.font = uiFont;

        return btn;
    }

    private Button MakeColorDot(GameObject parent, Color c, Vector2 pos)
    {
        var obj = new GameObject("ColorDot");
        obj.transform.SetParent(parent.transform, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(20, 20);

        var img = obj.AddComponent<Image>();
        img.color = c;

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        return btn;
    }

    private GameObject MakePanel(GameObject parent, Vector2 pos, Vector2 size)
    {
        var obj = new GameObject("Panel");
        obj.transform.SetParent(parent.transform, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        obj.AddComponent<Image>().color = PanelBg;
        return obj;
    }

    private static Font GetFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }
}
