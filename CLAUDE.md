# MyGame 開発ルール（初心者向け）

## プロジェクト概要

Unity公式テンプレート「2D Platformer Microgame」をベースにした2Dサイドスクロールアクションゲーム。
プレイヤーが左右に走り、ジャンプし、敵を踏んで倒したりコインを集めたりするゲーム。

## フォルダ構成

```
Assets/
├── Audio/          効果音・BGM
├── Character/      プレイヤー・敵のスプライト・アニメーション
├── Environment/    背景・地面などのステージ素材
├── Scenes/         ゲーム画面（SampleScene）
├── Scripts/        C#スクリプト
│   ├── Core/       シミュレーション基盤（編集禁止）
│   ├── Mechanics/  ゲームの部品（PlayerController等）
│   ├── Gameplay/   ゲームイベント（死亡・衝突・ゴール等）
│   ├── UI/         画面表示
│   ├── View/       視覚エフェクト
│   └── Model/      ゲームパラメータ
├── Tiles/          タイルマップ用タイル素材
└── Tutorials/      チュートリアル素材（編集不要）
```

## 1. ファイル・フォルダ管理

- 新しいスクリプトは `Assets/Scripts/` の適切なサブフォルダに置く
  - 動く仕組み → `Mechanics/`
  - 当たった時の処理 → `Gameplay/`
  - 画面表示 → `UI/`
- ファイル名は英語・スペースなし・大文字始まり（例：`PlayerJump.cs`）
- シーンを増やす前に `SampleScene` を完成させる

## 2. スクリプト編集のルール

- 既存スクリプトをいきなり書き換えない — まず読んで理解してからコピーして試す
- 変更は1箇所ずつ — 複数箇所を同時に変えるとバグの原因が分からなくなる
- 数値はInspectorで変えられる `public` 変数にする

```csharp
// 悪い例
velocity.y = 7;

// 良い例
public float jumpTakeOffSpeed = 7;
velocity.y = jumpTakeOffSpeed;
```

## 3. バージョン管理（Git）

- 作業前に必ずコミットしておく（壊しても戻せる）
- コミットメッセージは日本語でOK（例：「足場を3つ追加」「ジャンプ力を調整」）
- `Library/` フォルダはGitに含めない（`.gitignore` に追加）

## 4. テスト・確認のルール

- 変更したらすぐPlayボタンで動作確認する
- Consoleウィンドウを常に表示し、赤いエラーはその場で解決する
- 動いたら保存（Ctrl+S）

## 5. 触ってはいけない場所

| 場所 | 理由 |
|---|---|
| `Assets/Scripts/Core/` | シミュレーション基盤。壊すと全体が動かなくなる |
| `Assets/Mod Assets/` | チュートリアル用素材。本体に関係ない |
| `Packages/manifest.json` | パッケージ管理。手動編集するとUnityが起動しなくなることがある |
| `ProjectSettings/` | プロジェクト全体の設定。基本触らない |

## 6. 推奨する学習順序

1. `PlayerController.cs` の `maxSpeed` / `jumpTakeOffSpeed` を変えて動作を試す
2. Tilemapでステージの形を変える
3. 敵の配置・パトロールルートを変える（PatrolPath）
4. 新しいアイテム（Token）を追加する
5. UIを改造してスコア表示を変える

## 主要スクリプト早見表

| ファイル | 役割 | 触るタイミング |
|---|---|---|
| `Mechanics/PlayerController.cs` | プレイヤーの移動・ジャンプ | スピード・ジャンプ調整 |
| `Mechanics/EnemyController.cs` | 敵のAI | 敵の動きを変えたい時 |
| `Mechanics/Health.cs` | HP管理 | HP数を変えたい時 |
| `Mechanics/TokenController.cs` | コイン収集 | アイテムを追加したい時 |
| `Mechanics/GameController.cs` | ゲーム全体管理 | ゲームの流れを変えたい時 |
| `UI/MainUIController.cs` | スコア・UI表示 | 画面表示を変えたい時 |
