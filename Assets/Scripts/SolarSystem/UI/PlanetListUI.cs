using UnityEngine;
using UnityEngine.UI;
using System.Text;

/// <summary>
/// 現在の天体一覧を画面左下に表示する。
/// 天体名・質量・自転回転数を表示。
/// </summary>
public class PlanetListUI : MonoBehaviour
{
    private RectTransform panelRt;
    private Text          listText;
    private Font          uiFont;

    private const float PanelWidth   = 330f;
    private const float LineHeight   = 30f;
    private const float HeaderHeight = 36f;
    private const float PadV         = 10f;
    private const float PadH         = 10f;

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
        scaler.matchWidthOrHeight  = 0f;
        go.AddComponent<GraphicRaycaster>();

        var panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(go.transform, false);
        panelRt                  = panelObj.AddComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(1f, 0f);
        panelRt.anchorMax        = new Vector2(1f, 0f);
        panelRt.pivot            = new Vector2(1f, 0f);
        panelRt.anchoredPosition = new Vector2(-10f, 10f);
        panelRt.sizeDelta        = new Vector2(PanelWidth, 200f);

        panelObj.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.10f, 0.82f);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(panelObj.transform, false);
        var textRt       = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(PadH, PadV);
        textRt.offsetMax = new Vector2(-PadH, -PadV);

        listText                    = textObj.AddComponent<Text>();
        listText.fontSize        = 22;
        listText.color           = new Color(0.9f, 0.9f, 0.9f);
        listText.alignment       = TextAnchor.UpperLeft;
        listText.supportRichText = true;
        listText.lineSpacing     = 1.2f;
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

            // ④ 質量表示
            string massStr = body.isSun
                ? $"{body.data.mass:F0}"
                : FormatMass(body.data.mass);

            if (body.isSun)
            {
                sb.Append($"<color=#{hex}>{marker}</color> {body.data.planetName}  M={massStr}\n");
            }
            else
            {
                // ⑥ 自転回転数
                string rotStr = $"{body.RotationCount:F1}";
                sb.Append($"<color=#{hex}>{marker}</color> {body.data.planetName}  M={massStr}  ↺{rotStr}\n");
            }
        }

        // 月（GravitySystem 未登録・視覚専用）
        if (hasMoon)
        {
            string moonHex = ColorUtility.ToHtmlStringRGB(PlanetFactory.MoonColor);
            sb.Append($"<color=#{moonHex}>[ ]</color> Moon  視覚専用\n");
        }

        listText.text = sb.ToString();

        float h = HeaderHeight + total * LineHeight + PadV * 2f;
        if (panelRt != null)
            panelRt.sizeDelta = new Vector2(PanelWidth, Mathf.Max(h, 40f));
    }

    // G2フォーマット（有効数字2桁）
    private static string FormatMass(float mass)
    {
        if (mass >= 0.01f)   return $"{mass:F4}";
        if (mass >= 0.0001f) return $"{mass:F6}";
        return $"{mass:E1}";
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
