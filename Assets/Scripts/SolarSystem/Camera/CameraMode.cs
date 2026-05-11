using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// カメラモード切替。
/// TopDown    : 太陽を中心に右クリックドラッグで回転・スクロールでズーム。
/// EarthView  : 地球地上から全天体を観測。右クリックドラッグで視点回転。
/// Geocentric : 天動説モード。地球を中心に俯瞰し、他惑星の見かけの動きを観察。
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraMode : MonoBehaviour
{
    public enum Mode { TopDown, EarthView, Geocentric }

    public Mode CurrentMode { get; private set; } = Mode.TopDown;

    // ── TopDown パラメータ ─────────────────────────
    private float orbitDist   = 110f;  // 木星(78)まで初期表示できる距離
    private float orbitPitch  = 72f;
    private float orbitYaw    = 0f;
    private Vector3 orbitTarget = Vector3.zero;

    // ── EarthView パラメータ ───────────────────────
    private float earthViewYaw   = 0f;
    private float earthViewPitch = 0f;
    private CelestialBody earthBody;

    // ── Geocentric（天動説）パラメータ ────────────
    private float geoOrbitDist  = 50f;   // 地球中心の初期ズーム
    private float geoOrbitPitch = 72f;
    private float geoOrbitYaw   = 0f;

    // ── 天動説軌道トレイル ──────────────────────
    // 各天体の「地球からの相対位置」の履歴を保持し
    // 地球中心座標での見かけの軌道として描画する
    private readonly Dictionary<string, Queue<Vector3>> geoRelPos  = new Dictionary<string, Queue<Vector3>>();
    private readonly Dictionary<string, LineRenderer>   geoLines   = new Dictionary<string, LineRenderer>();
    private readonly Dictionary<string, Vector3>        geoLastRel = new Dictionary<string, Vector3>();
    private const int   GeoTrailMax     = 600;
    private const float GeoTrailMinStep = 0.35f;

    // ── 共通ドラッグ制御 ───────────────────────────
    private Vector3 lastMousePos;
    private bool    isDragging;
    private bool    isTouchOnUI;   // タッチ開始位置がUI上かどうか

    // ── OnGUI スタイル ─────────────────────────────
    private GUIStyle labelStyle;
    private GUIStyle boldLabelStyle;
    private static Texture2D dotTex;

    void Start()
    {
        labelStyle = new GUIStyle
        {
            fontSize = 11,
            normal   = { textColor = Color.white }
        };
        boldLabelStyle = new GUIStyle
        {
            fontSize    = 12,
            fontStyle   = FontStyle.Bold,
            normal      = { textColor = Color.white }
        };
        dotTex = MakeWhiteTexture();
    }

    // ────────────────────────────────────────────
    //  毎フレーム更新
    // ────────────────────────────────────────────
    void Update()
    {
        if      (CurrentMode == Mode.TopDown)   UpdateTopDown();
        else if (CurrentMode == Mode.EarthView) UpdateEarthView();
        else
        {
            UpdateGeocentric();
            RecordGeoTrails();
            RefreshGeoLines();
        }
    }

    private void UpdateTopDown()
    {
        // マウス右クリックドラッグ
        if (Input.GetMouseButtonDown(1)) { isDragging = true;  lastMousePos = Input.mousePosition; }
        if (Input.GetMouseButtonUp(1))   { isDragging = false; }

        Vector2 dragDelta = Vector2.zero;
        if (isDragging)
        {
            Vector3 d = Input.mousePosition - lastMousePos;
            dragDelta    = new Vector2(d.x, d.y);
            lastMousePos = Input.mousePosition;
        }
        else
        {
            dragDelta = GetTouchDragDelta();
        }

        orbitYaw   += dragDelta.x * 0.35f;
        orbitPitch -= dragDelta.y * 0.35f;
        orbitPitch  = Mathf.Clamp(orbitPitch, 10f, 89f);

        // スクロール or ピンチでズーム
        float zoom = Input.GetAxis("Mouse ScrollWheel") * 15f + GetPinchZoomDelta();
        orbitDist -= zoom;
        orbitDist  = Mathf.Clamp(orbitDist, 8f, 600f);

        Quaternion rot = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
        transform.position = orbitTarget + rot * (Vector3.back * orbitDist);
        transform.LookAt(orbitTarget);
    }

    // ── ⑤ 地球地上観測モード ──────────────────────
    private void UpdateEarthView()
    {
        if (earthBody == null) FindEarth();
        if (earthBody == null) return;

        // 地球の半径の外側（表面 + 余白0.3）にカメラを置く
        // 0.5f だと球の内側に入り青一色になるため修正
        float above = earthBody.data.radius + 0.3f;
        transform.position = earthBody.transform.position + Vector3.up * above;

        // マウス右クリック or 1本指スワイプで視点回転
        if (Input.GetMouseButtonDown(1)) { isDragging = true;  lastMousePos = Input.mousePosition; }
        if (Input.GetMouseButtonUp(1))   { isDragging = false; }

        Vector2 dragDelta = Vector2.zero;
        if (isDragging)
        {
            Vector3 d = Input.mousePosition - lastMousePos;
            dragDelta    = new Vector2(d.x, d.y);
            lastMousePos = Input.mousePosition;
        }
        else
        {
            dragDelta = GetTouchDragDelta();
        }

        earthViewYaw   += dragDelta.x * 0.35f;
        earthViewPitch -= dragDelta.y * 0.35f;
        earthViewPitch  = Mathf.Clamp(earthViewPitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(earthViewPitch, earthViewYaw, 0f);
    }

    // ────────────────────────────────────────────
    //  天動説モード：地球を中心とした俯瞰ビュー
    //  地球が画面の中心に固定され、他の惑星の
    //  見かけの動き（逆行運動など）が観察できる
    // ────────────────────────────────────────────
    private void UpdateGeocentric()
    {
        if (earthBody == null) FindEarth();
        if (earthBody == null) return;

        Vector3 earthPos = earthBody.transform.position;

        if (Input.GetMouseButtonDown(1)) { isDragging = true;  lastMousePos = Input.mousePosition; }
        if (Input.GetMouseButtonUp(1))   { isDragging = false; }

        Vector2 geoDragDelta = Vector2.zero;
        if (isDragging)
        {
            Vector3 d = Input.mousePosition - lastMousePos;
            geoDragDelta = new Vector2(d.x, d.y);
            lastMousePos = Input.mousePosition;
        }
        else
        {
            geoDragDelta = GetTouchDragDelta();
        }

        geoOrbitYaw   += geoDragDelta.x * 0.35f;
        geoOrbitPitch -= geoDragDelta.y * 0.35f;
        geoOrbitPitch  = Mathf.Clamp(geoOrbitPitch, 10f, 89f);

        float zoom = Input.GetAxis("Mouse ScrollWheel") * 10f + GetPinchZoomDelta();
        geoOrbitDist -= zoom;
        geoOrbitDist  = Mathf.Clamp(geoOrbitDist, 5f, 400f);

        Quaternion rot = Quaternion.Euler(geoOrbitPitch, geoOrbitYaw, 0f);
        transform.position = earthPos + rot * (Vector3.back * geoOrbitDist);
        transform.LookAt(earthPos);
    }

    // ────────────────────────────────────────────
    //  天動説トレイル：地球中心相対座標で記録・描画
    // ────────────────────────────────────────────

    /// <summary>
    /// 各天体の現在位置を地球からの相対ベクトルとして記録する。
    /// 一定距離以上移動したフレームのみ追加（軌跡の密度を均一に保つ）。
    /// </summary>
    private void RecordGeoTrails()
    {
        if (earthBody == null || GravitySystem.Instance == null) return;
        Vector3 earthPos = earthBody.transform.position;

        foreach (var body in GravitySystem.Instance.GetBodies())
        {
            if (body == null || body == earthBody) continue;

            string  key    = body.data.planetName;
            Vector3 relPos = body.transform.position - earthPos;

            if (!geoRelPos.ContainsKey(key))
            {
                geoRelPos[key]  = new Queue<Vector3>();
                geoLastRel[key] = relPos + Vector3.one * 9999f; // 初回は必ず追加
            }

            if (Vector3.Distance(relPos, geoLastRel[key]) < GeoTrailMinStep) continue;
            geoLastRel[key] = relPos;

            geoRelPos[key].Enqueue(relPos);
            if (geoRelPos[key].Count > GeoTrailMax)
                geoRelPos[key].Dequeue();
        }
    }

    /// <summary>
    /// 保存済みの相対位置に「現在の地球位置」を加算し
    /// LineRenderer のワールド座標を毎フレーム更新する。
    /// →地球が動いても軌跡が地球中心に張り付いて見える。
    /// </summary>
    private void RefreshGeoLines()
    {
        if (earthBody == null || GravitySystem.Instance == null) return;
        Vector3 earthPos = earthBody.transform.position;

        foreach (var body in GravitySystem.Instance.GetBodies())
        {
            if (body == null || body == earthBody) continue;

            string key = body.data.planetName;
            if (!geoRelPos.ContainsKey(key) || geoRelPos[key].Count < 2) continue;

            if (!geoLines.ContainsKey(key) || geoLines[key] == null)
                geoLines[key] = CreateGeoLine(key, body.data.color);

            var  pts = new Vector3[geoRelPos[key].Count];
            int  i   = 0;
            foreach (var rel in geoRelPos[key]) pts[i++] = earthPos + rel;

            var lr = geoLines[key];
            lr.positionCount = pts.Length;
            lr.SetPositions(pts);
        }
    }

    private LineRenderer CreateGeoLine(string bodyName, Color color)
    {
        var go = new GameObject($"GeoTrail_{bodyName}");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth    = 0.14f;
        lr.endWidth      = 0.03f;
        lr.startColor    = new Color(color.r, color.g, color.b, 0f);
        lr.endColor      = new Color(color.r, color.g, color.b, 0.85f);

        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 1f) }
        );
        lr.colorGradient = grad;

        string[] shaders = {
            "Universal Render Pipeline/Particles/Unlit",
            "Sprites/Default",
            "Unlit/Color"
        };
        foreach (var s in shaders)
        {
            var shader = Shader.Find(s);
            if (shader == null) continue;
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color",     color);
            lr.material = mat;
            break;
        }
        return lr;
    }

    private void ClearGeoTrails()
    {
        geoRelPos.Clear();
        geoLastRel.Clear();
        foreach (var lr in geoLines.Values)
            if (lr != null) Destroy(lr.gameObject);
        geoLines.Clear();
    }

    private static void ShowHeliocentricTrails(bool show)
    {
        if (GravitySystem.Instance == null) return;
        foreach (var body in GravitySystem.Instance.GetBodies())
        {
            if (body == null) continue;
            var lr = body.GetComponent<LineRenderer>();
            if (lr != null) lr.enabled = show;
        }
    }

    private void ShowGeocentricTrails(bool show)
    {
        foreach (var lr in geoLines.Values)
            if (lr != null) lr.enabled = show;
    }

    // ────────────────────────────────────────────
    //  ⑤ 地球視点：全天体を画面上の点で表示
    // ────────────────────────────────────────────
    void OnGUI()
    {
        // 天動説モードのオーバーレイ（上部モードテキストの下 y=68 以降に配置）
        if (CurrentMode == Mode.Geocentric)
        {
            GUI.Label(new Rect(10, 68, 600, 22),
                "[ 天動説モード - 地球中心ビュー ]", boldLabelStyle);
            GUI.Label(new Rect(10, 90, 600, 18),
                "地球が画面中心に固定。惑星の見かけの動き（逆行運動）が観察できます。", labelStyle);
            GUI.Label(new Rect(10, 108, 600, 18),
                "スワイプ: 視点回転　ピンチ/スクロール: ズーム　[G]: 俯瞰に戻る", labelStyle);
            return;
        }

        if (CurrentMode != Mode.EarthView) return;
        if (GravitySystem.Instance == null || earthBody == null) return;

        Camera cam = GetComponent<Camera>();
        if (cam == null) return;

        foreach (var body in GravitySystem.Instance.GetBodies())
        {
            if (body == null)        continue;
            if (body == earthBody)   continue;  // 地球自体はスキップ

            Vector3 screenPos = cam.WorldToScreenPoint(body.transform.position);
            if (screenPos.z < 0f) continue;

            float dist = Vector3.Distance(earthBody.transform.position, body.transform.position);

            // 太陽は常に明るく・大きく表示
            float dotSize, alpha;
            if (body.isSun)
            {
                dotSize = 32f;
                alpha   = 1f;
            }
            // 月は近いので大きく
            else if (body.data.planetName == "Moon")
            {
                dotSize = Mathf.Clamp(body.data.radius * 400f / Mathf.Max(dist, 0.1f), 6f, 28f);
                alpha   = 1f;
            }
            else
            {
                dotSize = Mathf.Clamp(body.data.radius * 200f / Mathf.Max(dist, 0.1f), 3f, 18f);
                alpha   = Mathf.Clamp01(30f / dist);
            }

            Color c = body.data.color;
            c.a = alpha;
            GUI.color = c;

            float sx = screenPos.x - dotSize * 0.5f;
            float sy = Screen.height - screenPos.y - dotSize * 0.5f;
            GUI.DrawTexture(new Rect(sx, sy, dotSize, dotSize), dotTex);

            GUI.color = Color.white;
            GUI.Label(new Rect(sx + dotSize + 3f, sy - 2f, 90f, 16f),
                body.data.planetName, labelStyle);
        }

        // 月（視覚専用、GravitySystemには未登録）を個別に描画
        var moonGo = GameObject.Find("Moon");
        if (moonGo != null && earthBody != null)
        {
            Vector3 moonScreen = cam.WorldToScreenPoint(moonGo.transform.position);
            if (moonScreen.z >= 0f)
            {
                float moonDist = Vector3.Distance(earthBody.transform.position, moonGo.transform.position);
                float moonSize = Mathf.Clamp(0.27f * 400f / Mathf.Max(moonDist, 0.1f), 6f, 30f);
                Color mc = PlanetFactory.MoonColor;
                mc.a = 1f;
                GUI.color = mc;
                float msx = moonScreen.x - moonSize * 0.5f;
                float msy = Screen.height - moonScreen.y - moonSize * 0.5f;
                GUI.DrawTexture(new Rect(msx, msy, moonSize, moonSize), dotTex);
                GUI.color = Color.white;
                GUI.Label(new Rect(msx + moonSize + 3f, msy - 2f, 90f, 16f), "Moon", labelStyle);
            }
        }

        GUI.color = Color.white;

        // ヘッダ（上部モードテキストの下 y=68 以降に配置）
        GUI.Label(new Rect(10, 68, 500, 22),
            "[ 地球視点 - 地上観測モード ]", boldLabelStyle);
        GUI.Label(new Rect(10, 90, 500, 18),
            "スワイプ: 視点回転　[E]: 俯瞰モードに戻る", labelStyle);
    }

    // ────────────────────────────────────────────
    //  公開 API
    // ────────────────────────────────────────────

    /// <summary>軌道クリアボタンから呼ぶ。天動説トレイルだけを消去する。</summary>
    public void ClearGeoTrailsPublic() => ClearGeoTrails();

    /// <summary>リセット時に呼ぶ。earthBody 参照をクリアしTopDownに戻す。</summary>
    public void OnReset()
    {
        // earthBody を null にする前に表示を戻す
        if (CurrentMode == Mode.EarthView)
            SetEarthRendererVisible(true);
        if (CurrentMode == Mode.Geocentric)
            ShowHeliocentricTrails(true);
        ClearGeoTrails();
        earthBody = null;
        if (CurrentMode != Mode.TopDown)
            CurrentMode = Mode.TopDown;
    }

    public void SetMode(Mode mode)
    {
        // 地球視点から離脱：地球の Renderer を元に戻す
        if (CurrentMode == Mode.EarthView && mode != Mode.EarthView)
            SetEarthRendererVisible(true);

        // 天動説モードから離脱：地動説トレイルを復元し天動説トレイルを隠す
        if (CurrentMode == Mode.Geocentric && mode != Mode.Geocentric)
        {
            ShowHeliocentricTrails(true);
            ShowGeocentricTrails(false);
        }

        CurrentMode = mode;

        if (mode == Mode.EarthView)
        {
            FindEarth();
            InitEarthViewDirection();
            // 地球球体の内側から描画しないよう Renderer を非表示にする
            SetEarthRendererVisible(false);
        }
        else if (mode == Mode.Geocentric)
        {
            FindEarth();
            geoOrbitPitch = 72f;
            geoOrbitYaw   = 0f;
            geoOrbitDist  = 50f;
            ClearGeoTrails();
            ShowHeliocentricTrails(false);
        }
    }

    // EarthView 中だけ地球の Renderer を隠す
    private void SetEarthRendererVisible(bool visible)
    {
        if (earthBody == null) return;
        var r = earthBody.GetComponent<Renderer>();
        if (r != null) r.enabled = visible;
    }

    /// <summary>[E] キー: TopDown ↔ EarthView トグル</summary>
    public void ToggleMode() =>
        SetMode(CurrentMode == Mode.EarthView ? Mode.TopDown : Mode.EarthView);

    /// <summary>[G] キー: TopDown ↔ Geocentric（天動説）トグル</summary>
    public void ToggleGeocentricMode() =>
        SetMode(CurrentMode == Mode.Geocentric ? Mode.TopDown : Mode.Geocentric);

    // ────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────
    private void FindEarth()
    {
        if (GravitySystem.Instance == null) return;
        earthBody = GravitySystem.Instance.FindByName("Earth");

        if (earthBody == null)
            foreach (var b in GravitySystem.Instance.GetBodies())
                if (b != null && !b.isSun) { earthBody = b; break; }
    }

    // 初期視点：太陽と反対側（夜空側）を向く
    private void InitEarthViewDirection()
    {
        if (earthBody == null) return;
        Vector3 toSun = (Vector3.zero - earthBody.transform.position).normalized;
        Vector3 away  = -toSun;
        earthViewYaw   = Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg;
        earthViewPitch = 0f;
    }

    // ────────────────────────────────────────────
    //  タッチ入力ヘルパー
    // ────────────────────────────────────────────

    /// <summary>
    /// 1本指スワイプのデルタを返す。
    /// UI上でのタッチは無視（ボタン・スライダーとの競合防止）。
    /// </summary>
    private Vector2 GetTouchDragDelta()
    {
        if (Input.touchCount != 1) return Vector2.zero;

        Touch t = Input.GetTouch(0);

        if (t.phase == TouchPhase.Began)
            isTouchOnUI = EventSystem.current != null &&
                          EventSystem.current.IsPointerOverGameObject(t.fingerId);

        if (isTouchOnUI) return Vector2.zero;
        if (t.phase == TouchPhase.Moved) return t.deltaPosition;
        return Vector2.zero;
    }

    /// <summary>
    /// 2本指ピンチのズーム量を返す（正 = ズームイン）。
    /// </summary>
    private static float GetPinchZoomDelta()
    {
        if (Input.touchCount < 2) return 0f;

        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        Vector2 prev0 = t0.position - t0.deltaPosition;
        Vector2 prev1 = t1.position - t1.deltaPosition;

        float prevDist = (prev0 - prev1).magnitude;
        float curDist  = (t0.position - t1.position).magnitude;

        // 指が広がる（ピンチアウト）→ズームイン → 正の値
        return (curDist - prevDist) * 0.08f;
    }

    private static Texture2D MakeWhiteTexture()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return tex;
    }
}
