using UnityEngine;
using UnityEngine.UI;
using System.Text;

/// <summary>
/// 現在の天体一覧を画面左下に表示する。
/// GravitySystem に登録された天体（合体・消滅も反映）＋視覚専用の月を表示。
/// </summary>
public class PlanetListUI : MonoBehaviour
{
    private RectTransform panelRt;
    private Text          listText;
    private Font          uiFont;

    private const float PanelWidth   = 170f;
    private const float LineHeight   = 14f;
    private const float HeaderHeight = 18f;
    private const float PadV         = 8f;
    private const float PadH         = 8f;

    void Start()
    {
        uiFont = GetFont();
        BuildCanvas();
    }

    void Update()
    {
        UpdateList();
    }

    // ────────────────────────────────────────────
    //  UI 構築
    // ────────────────────────────────────────────
    private void BuildCanvas()
    {
        var go     = new GameObject("PlanetListCanvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();

        // パネル背景（左下アンカー）
        var panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(go.transform, false);
        panelRt                  = panelObj.AddComponent<RectTransform>();
        panelRt.anchorMin        = Vector2.zero;
        panelRt.anchorMax        = Vector2.zero;
        panelRt.pivot            = Vector2.zero;
        panelRt.anchoredPosition = new Vector2(10f, 10f);
        panelRt.sizeDelta        = new Vector2(PanelWidth, 200f);

        panelObj.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.10f, 0.82f);

        // テキスト
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(panelObj.transform, false);
        var textRt       = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(PadH, PadV);
        textRt.offsetMax = new Vector2(-PadH, -PadV);

        listText                    = textObj.AddComponent<Text>();
        listText.fontSize        = 11;
        listText.color           = new Color(0.9f, 0.9f, 0.9f);
        listText.alignment       = TextAnchor.UpperLeft;
        listText.supportRichText = true;
        if (uiFont != null) listText.font = uiFont;
    }

    // ────────────────────────────────────────────
    //  毎フレーム更新
    // ────────────────────────────────────────────
    private void UpdateList()
    {
        if (listText == null || GravitySystem.Instance == null) return;

        var  bodies  = GravitySystem.Instance.GetBodies();
        bool hasMoon = GameObject.Find("Moon") != null;
        int  total   = bodies.Count + (hasMoon ? 1 : 0);

        var sb = new StringBuilder();
        sb.Append($"<b>天体一覧  {total}/30</b>\n");

        foreach (var body in bodies)
        {
            if (body == null) continue;

            string hex    = ColorUtility.ToHtmlStringRGB(body.data.color);
            string marker = body.isSun                        ? "[☀]"
                          : body.data.planetName == "Halley"  ? "[★]"
                          :                                     "[ ]";

            sb.Append($"<color=#{hex}>{marker}</color> {body.data.planetName}\n");
        }

        // 月（GravitySystem 未登録・視覚専用）
        if (hasMoon)
        {
            string moonHex = ColorUtility.ToHtmlStringRGB(PlanetFactory.MoonColor);
            sb.Append($"<color=#{moonHex}>[ ]</color> Moon\n");
        }

        listText.text = sb.ToString();

        // パネル高さを天体数に合わせて自動調整
        float h = HeaderHeight + total * LineHeight + PadV * 2f;
        if (panelRt != null)
            panelRt.sizeDelta = new Vector2(PanelWidth, Mathf.Max(h, 40f));
    }

    private static Font GetFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }
}
