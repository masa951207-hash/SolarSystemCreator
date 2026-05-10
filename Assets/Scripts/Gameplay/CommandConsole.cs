using UnityEngine;
using UnityEngine.InputSystem;
using Platformer.Mechanics;

namespace Platformer.Gameplay
{
    public class CommandConsole : MonoBehaviour
    {
        bool isOpen = false;
        string inputText = "";
        string feedbackText = "";
        float feedbackTimer = 0;
        bool platformPlaceEnabled = false;

        PlayerController player;
        Camera mainCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn()
        {
            new GameObject("CommandConsole").AddComponent<CommandConsole>();
        }

        void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            mainCamera = Camera.main;
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                isOpen = !isOpen;
                if (isOpen)
                {
                    inputText = "";
                    if (player != null) player.controlEnabled = false;
                }
                else
                {
                    if (player != null) player.controlEnabled = true;
                }
            }

            if (platformPlaceEnabled && !isOpen && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                PlacePlatform();
        }

        void OnGUI()
        {
            GUIStyle infoStyle = new GUIStyle(GUI.skin.label) { fontSize = 22 };

            if (feedbackTimer > 0)
            {
                feedbackTimer -= Time.deltaTime;
                GUI.color = Color.yellow;
                GUI.Label(new Rect(10, 10, 800, 35), feedbackText, infoStyle);
                GUI.color = Color.white;
            }

            if (platformPlaceEnabled)
            {
                GUI.color = Color.cyan;
                GUI.Label(new Rect(10, 50, 700, 35), "足場設置モード中 - クリックで足場を置く / Tabキーで入力", infoStyle);
                GUI.color = Color.white;
            }

            if (!isOpen) return;

            int h = 225;
            GUI.Box(new Rect(0, Screen.height - h, Screen.width, h), "");

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            GUIStyle fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 28 };
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 24 };

            GUI.Label(new Rect(10, Screen.height - h + 10, 700, 35), "コマンド入力 (Tabキーで閉じる / Enterで実行)", labelStyle);

            GUI.SetNextControlName("ConsoleInput");
            inputText = GUI.TextField(new Rect(10, Screen.height - h + 55, Screen.width - 210, 100), inputText, fieldStyle);
            GUI.FocusControl("ConsoleInput");

            bool submitted = GUI.Button(new Rect(Screen.width - 195, Screen.height - h + 55, 180, 100), "実行", buttonStyle);
            if (submitted || (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame))
            {
                ExecuteCommand(inputText);
                inputText = "";
                isOpen = false;
                if (player != null) player.controlEnabled = true;
            }
        }

        void ExecuteCommand(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return;

            if (player == null) player = FindFirstObjectByType<PlayerController>();

            if (Has(cmd, "足場", "platform", "ブロック"))
            {
                platformPlaceEnabled = !platformPlaceEnabled;
                ShowFeedback(platformPlaceEnabled
                    ? "足場設置モード ON → クリックで足場を置けます"
                    : "足場設置モード OFF");
            }
            else if (Has(cmd, "無敵", "invincible", "god", "ダメージ"))
            {
                if (player != null)
                {
                    player.isInvincible = !player.isInvincible;
                    ShowFeedback(player.isInvincible ? "無敵モード ON" : "無敵モード OFF");
                }
            }
            else if (Has(cmd, "速", "スピード", "fast", "speed"))
            {
                if (player != null)
                {
                    player.maxSpeed = player.maxSpeed > 14f ? 7f : player.maxSpeed * 2f;
                    ShowFeedback($"スピード: {player.maxSpeed} (元は 7)");
                }
            }
            else if (Has(cmd, "ジャンプ", "jump", "高く", "飛"))
            {
                if (player != null)
                {
                    player.jumpTakeOffSpeed = player.jumpTakeOffSpeed > 14f ? 7f : player.jumpTakeOffSpeed * 2f;
                    ShowFeedback($"ジャンプ力: {player.jumpTakeOffSpeed} (元は 7)");
                }
            }
            else if (Has(cmd, "リセット", "reset", "元に戻", "もとに戻"))
            {
                if (player != null)
                {
                    player.maxSpeed = 7f;
                    player.jumpTakeOffSpeed = 7f;
                    player.isInvincible = false;
                }
                platformPlaceEnabled = false;
                ShowFeedback("すべてリセットしました");
            }
            else
            {
                ShowFeedback($"「{cmd}」は認識できませんでした");
            }
        }

        bool Has(string input, params string[] keywords)
        {
            foreach (var kw in keywords)
                if (input.Contains(kw)) return true;
            return false;
        }

        void ShowFeedback(string msg)
        {
            feedbackText = msg;
            feedbackTimer = 3f;
        }

        void PlacePlatform()
        {
            var worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0;
            worldPos.x = Mathf.Round(worldPos.x * 2f) / 2f;
            worldPos.y = Mathf.Round(worldPos.y * 2f) / 2f;

            var go = new GameObject("Platform");
            go.transform.position = worldPos;
            go.transform.localScale = new Vector3(3f, 0.5f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateFlatSprite();
            sr.color = new Color(0.5f, 0.35f, 0.2f);
            sr.sortingOrder = 1;

            go.AddComponent<BoxCollider2D>();
        }

        Sprite CreateFlatSprite()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }
    }
}
