using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 惑星生成パネル。
/// ③ スライダー値を地球比で表示し、追加前に3Dプレビュー（軌道リング＋仮想惑星）を表示。
/// ② ボタンを大きめにしスマホでも読みやすく調整。
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

    // ── ③ プレビュー用3Dオブジェクト ─────────
    private GameObject   previewSphere;
    private LineRenderer previewOrbitLine;

    // ── カラープリセット ───────────────────────
    private static readonly Color[] ColorPresets =
    {
        new Color(0.4f,  0.8f,  1.0f),
        new Color(0.9f,  0.3f,  0.15f),
        new Color(0.95f, 0.75f, 0.3f),
        new Color(0.55f, 0.35f, 0.9f),
        new Color(0.3f,  0.9f,  0.45f),
        new Color(0.9f,  0.9f,  0.9f),
    };

    private static readonly Color PanelBg     = new Color(0.05f, 0.05f, 0.10f, 0.90f);
    private static readonly Color CreateBtnBg = new Color(0.15f, 0.55f, 0.20f, 0.95f);
    private static readonly Color ToggleBg    = new Color(0.10f, 0.10f, 0.20f, 0.92f);

    // ── 地球基準値（PlanetFactory と同じ）────
    private const float EarthRadius    = 1.00f;
    private const float EarthMass      = 0.0003f;
    private const float EarthOrbitDist = 15.0f;
    private const float EarthRotSpeed  = 15.0f;

    // ────────────────────────────────────────────
    void Start()
    {
        uiFont = GetFont();
        BuildCanvas();
        CreatePreviewObjects();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            TogglePanel();

        if (warningTimer > 0f)
        {
            warningTimer -= Time.deltaTime;
            if (warningTimer <= 0f && warningLabel != null)
                warningLabel.text = "";
        }

        // ③ プレビューを毎フレーム更新
        UpdatePreview();
    }

    void OnDestroy()
    {
        DestroyPreview();
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
        scaler.matchWidthOrHeight  = 0f;
        root.AddComponent<GraphicRaycaster>();

        // ── トグルボタン（モード表示テキストの下に配置）──
        // 上部中央モードテキストが y=-10〜-60 を占有するため
        // トグルボタンは y=-68 以降に配置して重なりを回避
        var toggleBtn = MakeButton(root, "[ T ] 惑星を追加",
            new Vector2(10, -68), new Vector2(220, 44),
            new Vector2(0, 1), ToggleBg);
        toggleBtn.onClick.AddListener(TogglePanel);

        // ── メインパネル ─────────────────────────
        panel = MakePanel(root, new Vector2(10, -118), new Vector2(300, 600));

        float y = -14f;

        (sizeSlider,  sizeLabel)  = MakeSliderRow(panel, "サイズ",    0.3f, 3.0f,                        1.0f, ref y);
        (massSlider,  massLabel)  = MakeSliderRow(panel, "質量",      0.1f, PlanetFactory.SunMass - 1f,  1.0f, ref y);
        (distSlider,  distLabel)  = MakeSliderRow(panel, "公転距離",  5f,   250f,                        20f,  ref y);
        (speedSlider, speedLabel) = MakeSliderRow(panel, "公転速度",  0f,   10f,                         3f,   ref y);
        (rotSlider,   rotLabel)   = MakeSliderRow(panel, "自転速度",  0f,   50f,                         10f,  ref y);

        y -= 6f;
        MakeColorRow(panel, ref y);
        y -= 10f;

        distSlider.onValueChanged.AddListener(_ => AutoCalcSpeed());
        foreach (var sl in new[] { sizeSlider, massSlider, distSlider, speedSlider, rotSlider })
            sl.onValueChanged.AddListener(_ => RefreshLabels());

        // ③ プレビュー情報ラベル
        var previewInfoLabel = MakeLabel(panel, "",
            new Vector2(10, y), new Vector2(278, 36));
        previewInfoLabel.color    = new Color(0.7f, 1f, 0.7f);
        previewInfoLabel.fontSize = 11;
        previewInfoLabel.alignment = TextAnchor.UpperLeft;
        // ラベルは UpdatePreviewLabel で毎フレーム更新するため参照保持
        _previewInfoLabel = previewInfoLabel;
        y -= 42f;

        var createBtn = MakeButton(panel, "✦  惑星を追加",
            new Vector2(0, y), new Vector2(270, 48),
            new Vector2(0.5f, 1f), CreateBtnBg);
        createBtn.onClick.AddListener(DoCreate);
        y -= 50f;

        warningLabel = MakeLabel(panel, "", new Vector2(10, y), new Vector2(278, 36));
        warningLabel.color     = new Color(1f, 0.4f, 0.4f);
        warningLabel.fontSize  = 11;
        warningLabel.alignment = TextAnchor.UpperLeft;

        RefreshLabels();
    }

    private Text _previewInfoLabel;

    // ────────────────────────────────────────────
    //  ③ プレビューオブジェクト生成
    // ────────────────────────────────────────────
    private void CreatePreviewObjects()
    {
        // 軌道リング（LineRenderer）
        var orbitGo = new GameObject("PreviewOrbit");
        previewOrbitLine = orbitGo.AddComponent<LineRenderer>();
        previewOrbitLine.useWorldSpace = true;
        previewOrbitLine.startWidth    = 0.25f;
        previewOrbitLine.endWidth      = 0.25f;
        previewOrbitLine.loop          = true;
        previewOrbitLine.positionCount = 64;

        var orbitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                       ?? Shader.Find("Sprites/Default");
        if (orbitShader != null)
        {
            var mat = new Material(orbitShader);
            previewOrbitLine.material = mat;
        }

        // 仮想惑星スフィア
        previewSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        previewSphere.name = "PreviewPlanet";
        Object.Destroy(previewSphere.GetComponent<SphereCollider>());

        var sphereShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                        ?? Shader.Find("Sprites/Default");
        if (sphereShader != null)
        {
            var mat = new Material(sphereShader);
            previewSphere.GetComponent<Renderer>().material = mat;
        }
    }

    // ────────────────────────────────────────────
    //  ③ プレビュー更新
    // ────────────────────────────────────────────
    private void UpdatePreview()
    {
        if (previewSphere == null || previewOrbitLine == null) return;

        bool show = isOpen;
        previewSphere.SetActive(show);
        previewOrbitLine.enabled = show;

        if (!show) return;

        float dist   = distSlider != null ? distSlider.value : 20f;
        float size   = sizeSlider != null ? sizeSlider.value : 1f;
        Color col    = selectedColor;
        Color ringCol = new Color(col.r, col.g, col.b, 0.5f);
        Color sphereCol = new Color(col.r * 0.7f, col.g * 0.7f, col.b * 0.7f, 0.6f);

        // 軌道リング
        const int seg = 64;
        previewOrbitLine.positionCount = seg;
        for (int i = 0; i < seg; i++)
        {
            float angle = (float)i / seg * Mathf.PI * 2f;
            previewOrbitLine.SetPosition(i,
                new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist));
        }
        previewOrbitLine.startColor = ringCol;
        previewOrbitLine.endColor   = ringCol;
        if (previewOrbitLine.material != null)
        {
            previewOrbitLine.material.SetColor("_BaseColor", ringCol);
            previewOrbitLine.material.SetColor("_Color",     ringCol);
        }

        // 仮想惑星（+X 軸上に配置）
        previewSphere.transform.position   = new Vector3(dist, 0f, 0f);
        previewSphere.transform.localScale = Vector3.one * size * 2f;
        var rend = previewSphere.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.SetColor("_BaseColor", sphereCol);
            rend.material.SetColor("_Color",     sphereCol);
        }

        // プレビュー情報テキスト更新
        UpdatePreviewLabel(dist, size);
    }

    private void UpdatePreviewLabel(float dist, float size)
    {
        if (_previewInfoLabel == null) return;
        float distAU = dist / EarthOrbitDist;
        float sizeX  = size / EarthRadius;
        _previewInfoLabel.text =
            $"プレビュー: 距離 {distAU:F2} AU  サイズ 地球×{sizeX:F2}";
    }

    private void DestroyPreview()
    {
        if (previewSphere    != null) Object.Destroy(previewSphere);
        if (previewOrbitLine != null) Object.Destroy(previewOrbitLine.gameObject);
    }

    // ────────────────────────────────────────────
    //  ③ ラベル更新（地球比表示）
    // ────────────────────────────────────────────
    private void RefreshLabels()
    {
        float earthOrbitalSpeed = PlanetFactory.CalcOrbitalSpeed(EarthOrbitDist);

        float sizeRatio  = sizeSlider.value  / EarthRadius;
        float massRatio  = massSlider.value  / EarthMass;
        float distRatio  = distSlider.value  / EarthOrbitDist;
        float speedRatio = (earthOrbitalSpeed > 0f)
            ? speedSlider.value / earthOrbitalSpeed : 0f;
        float rotRatio   = (EarthRotSpeed > 0f)
            ? rotSlider.value / EarthRotSpeed : 0f;

        if (sizeLabel  != null)
            sizeLabel.text  = $"サイズ: {sizeSlider.value:F2}  ( 地球の {sizeRatio:F2}倍 )";
        if (massLabel  != null)
            massLabel.text  = $"質量: {massSlider.value:G3}  ( 地球の {massRatio:F1}倍 )";
        if (distLabel  != null)
            distLabel.text  = $"公転距離: {distSlider.value:F1}  ( {distRatio:F2} AU )";
        if (speedLabel != null)
            speedLabel.text = $"公転速度: {speedSlider.value:F2}  ( 地球の {speedRatio:F2}倍 )";
        if (rotLabel   != null)
            rotLabel.text   = $"自転速度: {rotSlider.value:F1}  ( 地球の {rotRatio:F2}倍 )";
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
            px += 26f;
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
        // 1.5倍フォント用に高さ・間隔を拡大
        var lbl = MakeLabel(parent, $"{title} : {defaultVal:F2}",
            new Vector2(10, y), new Vector2(278, 26));
        y -= 30f;

        var sl = MakeSlider(parent, min, max, defaultVal,
            new Vector2(10, y), new Vector2(278, 22));
        y -= 32f;

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
        var obj = new GameObject("Label_" + text.Substring(0, Mathf.Min(text.Length, 20)));
        obj.transform.SetParent(parent.transform, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var t = obj.AddComponent<Text>();
        t.text                   = text;
        t.fontSize               = 18;
        t.color                  = new Color(0.9f, 0.9f, 0.9f);
        t.alignment              = TextAnchor.MiddleLeft;
        t.resizeTextForBestFit   = true;
        t.resizeTextMinSize      = 11;
        t.resizeTextMaxSize      = 18;
        t.horizontalOverflow     = HorizontalWrapMode.Wrap;
        t.verticalOverflow       = VerticalWrapMode.Overflow;
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
        txtRt.offsetMin = new Vector2(4, 2);
        txtRt.offsetMax = new Vector2(-4, -2);

        var txt = txtObj.AddComponent<Text>();
        txt.text                 = label;
        txt.fontSize             = 16;
        txt.color                = Color.white;
        txt.alignment            = TextAnchor.MiddleCenter;
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize    = 9;
        txt.resizeTextMaxSize    = 18;
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
        rt.sizeDelta = new Vector2(22, 22);

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
        Font f = Resources.Load<Font>("Fonts/NotoSansJP");
        if (f != null) return f;
        f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }
}
