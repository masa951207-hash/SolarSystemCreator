using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ⑦ タイトル画面。
/// 「ヤマグ　チ。　惑星の運動について」を全画面表示し、
/// スタートボタンが押されたら GameManager.Instance にシミュレーション開始を依頼する。
/// </summary>
public class TitleScreen : MonoBehaviour
{
    private Canvas canvas;
    private Font   uiFont;

    private static readonly Color BgColor        = new Color(0.005f, 0.008f, 0.020f, 1f);
    private static readonly Color TitleColor     = new Color(0.98f,  0.95f,  0.70f,  1f);
    private static readonly Color SubtitleColor  = new Color(0.70f,  0.85f,  1.00f,  1f);
    private static readonly Color DescColor      = new Color(0.65f,  0.65f,  0.70f,  1f);
    private static readonly Color StartBtnBg     = new Color(0.12f,  0.48f,  0.20f,  0.95f);
    private static readonly Color StartBtnHover  = new Color(0.18f,  0.62f,  0.28f,  1f);
    private static readonly Color PlutoPanelBg   = new Color(0.04f,  0.06f,  0.14f,  0.88f);
    private static readonly Color PlutoHeaderCol = new Color(0.80f,  0.90f,  1.00f,  1f);
    private static readonly Color PlutoTextCol   = new Color(0.82f,  0.82f,  0.88f,  1f);
    private static readonly Color PlutoAccentCol = new Color(0.98f,  0.92f,  0.60f,  1f);

    void Start()
    {
        uiFont = GetFont();
        BuildTitleCanvas();
    }

    // ────────────────────────────────────────────
    //  UI 構築
    // ────────────────────────────────────────────
    private void BuildTitleCanvas()
    {
        var go = new GameObject("TitleCanvas");
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0f;
        go.AddComponent<GraphicRaycaster>();

        // 全画面背景
        var bg = MakeFullRect(go, "BG");
        bg.AddComponent<Image>().color = BgColor;

        // 星屑風デコレーション
        AddStarfield(go);

        // ── タイトルテキスト（上部）──────────────────
        var titleObj = MakeFullRect(go, "TitleText");
        var titleRt  = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.1f, 0.77f);
        titleRt.anchorMax = new Vector2(0.9f, 0.98f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        var titleTxt       = titleObj.AddComponent<Text>();
        titleTxt.text      = "ヤマグ　\nチ。\n惑星の運動について";
        titleTxt.fontSize  = 72;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.color     = TitleColor;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.resizeTextForBestFit = true;
        titleTxt.resizeTextMinSize    = 20;
        titleTxt.resizeTextMaxSize    = 72;
        if (uiFont != null) titleTxt.font = uiFont;

        // ── サブタイトル ──────────────────────────
        var subObj = MakeFullRect(go, "Subtitle");
        var subRt  = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.15f, 0.71f);
        subRt.anchorMax = new Vector2(0.85f, 0.77f);
        subRt.offsetMin = Vector2.zero;
        subRt.offsetMax = Vector2.zero;

        var subTxt       = subObj.AddComponent<Text>();
        subTxt.text      = "太陽系シミュレーション  —  N体重力計算";
        subTxt.fontSize  = 26;
        subTxt.color     = SubtitleColor;
        subTxt.alignment = TextAnchor.MiddleCenter;
        subTxt.resizeTextForBestFit = true;
        subTxt.resizeTextMinSize    = 10;
        subTxt.resizeTextMaxSize    = 28;
        if (uiFont != null) subTxt.font = uiFont;

        // ── 冥王星発見パネル（中央大エリア）──────────
        var plutoPanel = MakeFullRect(go, "PlutoPanel");
        var plutoPanelRt = plutoPanel.GetComponent<RectTransform>();
        plutoPanelRt.anchorMin = new Vector2(0.05f, 0.20f);
        plutoPanelRt.anchorMax = new Vector2(0.95f, 0.70f);
        plutoPanelRt.offsetMin = Vector2.zero;
        plutoPanelRt.offsetMax = Vector2.zero;
        plutoPanel.AddComponent<Image>().color = PlutoPanelBg;

        // パネルヘッダー
        var plutoHeaderObj = MakeFullRect(plutoPanel, "PlutoHeader");
        var plutoHeaderRt  = plutoHeaderObj.GetComponent<RectTransform>();
        plutoHeaderRt.anchorMin = new Vector2(0.02f, 0.83f);
        plutoHeaderRt.anchorMax = new Vector2(0.98f, 1.00f);
        plutoHeaderRt.offsetMin = Vector2.zero;
        plutoHeaderRt.offsetMax = Vector2.zero;

        var headerTxt       = plutoHeaderObj.AddComponent<Text>();
        headerTxt.text      = "【  冥王星発見の経緯  】";
        headerTxt.fontSize  = 28;
        headerTxt.fontStyle = FontStyle.Bold;
        headerTxt.color     = PlutoHeaderCol;
        headerTxt.alignment = TextAnchor.MiddleCenter;
        headerTxt.resizeTextForBestFit = true;
        headerTxt.resizeTextMinSize    = 12;
        headerTxt.resizeTextMaxSize    = 30;
        if (uiFont != null) headerTxt.font = uiFont;

        // パネル本文（段落1・2）
        var plutoBody1Obj = MakeFullRect(plutoPanel, "PlutoBody1");
        var plutoBody1Rt  = plutoBody1Obj.GetComponent<RectTransform>();
        plutoBody1Rt.anchorMin = new Vector2(0.03f, 0.50f);
        plutoBody1Rt.anchorMax = new Vector2(0.97f, 0.83f);
        plutoBody1Rt.offsetMin = Vector2.zero;
        plutoBody1Rt.offsetMax = Vector2.zero;

        var body1Txt      = plutoBody1Obj.AddComponent<Text>();
        body1Txt.text     =
            "19世紀、天文学者たちは天王星の軌道に不思議な「ずれ」を発見した。\n" +
            "ニュートン力学で計算した軌道と、実際の位置が一致しないのだ。\n" +
            "このずれは偶然ではなく、未知の天体の重力によるものではないか——\n" +
            "そう考えた科学者たちは、望遠鏡ではなく紙と計算式で宇宙に挑んだ。";
        body1Txt.fontSize = 20;
        body1Txt.color    = PlutoTextCol;
        body1Txt.alignment           = TextAnchor.MiddleCenter;
        body1Txt.resizeTextForBestFit = true;
        body1Txt.resizeTextMinSize    = 9;
        body1Txt.resizeTextMaxSize    = 22;
        if (uiFont != null) body1Txt.font = uiFont;

        // アクセント行（強調）
        var plutoAccentObj = MakeFullRect(plutoPanel, "PlutoAccent");
        var plutoAccentRt  = plutoAccentObj.GetComponent<RectTransform>();
        plutoAccentRt.anchorMin = new Vector2(0.05f, 0.34f);
        plutoAccentRt.anchorMax = new Vector2(0.95f, 0.50f);
        plutoAccentRt.offsetMin = Vector2.zero;
        plutoAccentRt.offsetMax = Vector2.zero;

        var accentTxt      = plutoAccentObj.AddComponent<Text>();
        accentTxt.text     =
            "1846年 — ル・ヴェリエとアダムスが軌道方程式を解き\n" +
            "「その場所に天体がある」と予測。狙った位置で海王星が発見された。";
        accentTxt.fontSize = 21;
        accentTxt.fontStyle = FontStyle.Bold;
        accentTxt.color    = PlutoAccentCol;
        accentTxt.alignment           = TextAnchor.MiddleCenter;
        accentTxt.resizeTextForBestFit = true;
        accentTxt.resizeTextMinSize    = 9;
        accentTxt.resizeTextMaxSize    = 23;
        if (uiFont != null) accentTxt.font = uiFont;

        // パネル本文（段落3）
        var plutoBody2Obj = MakeFullRect(plutoPanel, "PlutoBody2");
        var plutoBody2Rt  = plutoBody2Obj.GetComponent<RectTransform>();
        plutoBody2Rt.anchorMin = new Vector2(0.03f, 0.14f);
        plutoBody2Rt.anchorMax = new Vector2(0.97f, 0.34f);
        plutoBody2Rt.offsetMin = Vector2.zero;
        plutoBody2Rt.offsetMax = Vector2.zero;

        var body2Txt      = plutoBody2Obj.AddComponent<Text>();
        body2Txt.text     =
            "しかし海王星発見後もわずかなずれが残った。未知の「惑星X」を求め、\n" +
            "1930年、クライド・トンボーがついに冥王星を発見する。\n" +
            "計算が先に海王星と冥王星を見つけていた——観測より先に、数式が宇宙の扉を開いた。";
        body2Txt.fontSize = 20;
        body2Txt.color    = PlutoTextCol;
        body2Txt.alignment           = TextAnchor.MiddleCenter;
        body2Txt.resizeTextForBestFit = true;
        body2Txt.resizeTextMinSize    = 9;
        body2Txt.resizeTextMaxSize    = 22;
        if (uiFont != null) body2Txt.font = uiFont;

        // フッター強調
        var plutoFooterObj = MakeFullRect(plutoPanel, "PlutoFooter");
        var plutoFooterRt  = plutoFooterObj.GetComponent<RectTransform>();
        plutoFooterRt.anchorMin = new Vector2(0.05f, 0.00f);
        plutoFooterRt.anchorMax = new Vector2(0.95f, 0.14f);
        plutoFooterRt.offsetMin = Vector2.zero;
        plutoFooterRt.offsetMax = Vector2.zero;

        var footerTxt      = plutoFooterObj.AddComponent<Text>();
        footerTxt.text     = "— 観測より先に、計算が宇宙の扉を開いた —";
        footerTxt.fontSize = 20;
        footerTxt.fontStyle = FontStyle.BoldAndItalic;
        footerTxt.color    = PlutoAccentCol;
        footerTxt.alignment           = TextAnchor.MiddleCenter;
        footerTxt.resizeTextForBestFit = true;
        footerTxt.resizeTextMinSize    = 9;
        footerTxt.resizeTextMaxSize    = 22;
        if (uiFont != null) footerTxt.font = uiFont;

        // ── スタートボタン ──────────────────────────
        var btnObj = MakeFullRect(go, "StartButton");
        var btnRt  = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.35f, 0.08f);
        btnRt.anchorMax = new Vector2(0.65f, 0.18f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;

        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = StartBtnBg;

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        var colors = btn.colors;
        colors.normalColor      = StartBtnBg;
        colors.highlightedColor = StartBtnHover;
        colors.pressedColor     = new Color(0.08f, 0.35f, 0.14f);
        btn.colors = colors;

        btn.onClick.AddListener(OnStartClicked);

        var btnLblObj = new GameObject("BtnLabel");
        btnLblObj.transform.SetParent(btnObj.transform, false);
        var btnLblRt = btnLblObj.AddComponent<RectTransform>();
        btnLblRt.anchorMin = Vector2.zero;
        btnLblRt.anchorMax = Vector2.one;
        btnLblRt.sizeDelta = Vector2.zero;

        var btnTxt       = btnLblObj.AddComponent<Text>();
        btnTxt.text      = "▶  スタート";
        btnTxt.fontSize  = 36;
        btnTxt.fontStyle = FontStyle.Bold;
        btnTxt.color     = Color.white;
        btnTxt.alignment = TextAnchor.MiddleCenter;
        btnTxt.resizeTextForBestFit = true;
        btnTxt.resizeTextMinSize    = 14;
        btnTxt.resizeTextMaxSize    = 40;
        if (uiFont != null) btnTxt.font = uiFont;

        // ── 操作説明（最下部）──────────────────────
        var hintObj = MakeFullRect(go, "Hint");
        var hintRt  = hintObj.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0.1f, 0.01f);
        hintRt.anchorMax = new Vector2(0.9f, 0.07f);
        hintRt.offsetMin = Vector2.zero;
        hintRt.offsetMax = Vector2.zero;

        var hintTxt       = hintObj.AddComponent<Text>();
        hintTxt.text      =
            "スワイプ: 視点回転  ピンチ: ズーム  [E] 地球視点  [G] 天動説\n" +
            "[Space] ポーズ  [1〜4] 速度変更  右クリック+ドラッグでも操作可";
        hintTxt.fontSize  = 16;
        hintTxt.color     = new Color(0.5f, 0.5f, 0.55f);
        hintTxt.alignment = TextAnchor.MiddleCenter;
        hintTxt.resizeTextForBestFit = true;
        hintTxt.resizeTextMinSize    = 7;
        hintTxt.resizeTextMaxSize    = 18;
        if (uiFont != null) hintTxt.font = uiFont;
    }

    // ────────────────────────────────────────────
    //  スタートボタン押下
    // ────────────────────────────────────────────
    private void OnStartClicked()
    {
        GameManager.Instance?.StartSimulationFromTitle();
        Destroy(canvas.gameObject);
        Destroy(gameObject);
    }

    // ────────────────────────────────────────────
    //  星屑デコレーション（OnGUI で描くと重いため TextMesh 風に小さい Image を散布）
    // ────────────────────────────────────────────
    private static void AddStarfield(GameObject parent)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        var rng = new System.Random(42);
        for (int i = 0; i < 120; i++)
        {
            float ax = (float)rng.NextDouble();
            float ay = (float)rng.NextDouble();
            float sz = (float)rng.NextDouble() * 3f + 1f;
            float br = (float)rng.NextDouble() * 0.6f + 0.15f;

            var starObj = MakeFullRect(parent, $"Star{i}");
            var starRt  = starObj.GetComponent<RectTransform>();
            starRt.anchorMin = new Vector2(ax, ay);
            starRt.anchorMax = new Vector2(ax, ay);
            starRt.sizeDelta = new Vector2(sz, sz);
            starRt.anchoredPosition = Vector2.zero;

            var img = starObj.AddComponent<Image>();
            img.color = new Color(br, br, br + 0.1f, 0.8f);
        }
    }

    // ────────────────────────────────────────────
    //  ヘルパー
    // ────────────────────────────────────────────
    private static GameObject MakeFullRect(GameObject parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        var rt       = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        return obj;
    }

    private static Font GetFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }
}
