# VMCBlendShapeControl
Beat Saber から [VirtualMotionCapture](https://vmc.info/) へ、曲時間およびゲームイベントに連動した BlendShape 制御を [VMC Protocol](https://protocol.vmc.info/Reference)を使って OSC 送信する Mod です。

# インストール方法
1. [リリース](https://github.com/rynan4818/VMCBlendShapeControl/releases)から最新のzipファイルをダウンロードして下さい。
2. `VMCBlendShapeControl.dll` を Beat Saber の `Plugins` フォルダへ配置してください。

# 機能と設定について
ゲーム内の MOD 設定画面（BSML）から「VMC BlendShape Control」の各種機能を設定できます。

本 MOD には、大まかに以下の 2 つの制御機能があります。

1. **Time Based Expression（曲時間連動）**
	 - JSON スクリプト（`DefaultVMCBlendShape.json` / `SongVMCBlendShape.json` / `NalulunaAvatarsEvents.json`）の曲時間イベントに到達したら Action を実行します。
2. **Event Based Expression（イベント連動）**
	 - 開始/終了/ポーズ/コンボ/ミス/クリアなどのゲームイベント発生時に Action を実行します。

## MOD 設定画面（UI）の設定項目について
<img width="686" height="428" alt="image" src="https://github.com/user-attachments/assets/0706b06d-d557-412e-9b6d-6b605a0011df" />

<img width="597" height="326" alt="image" src="https://github.com/user-attachments/assets/0b21ed9e-a01b-4cc4-9f9c-ba017ac17d95" />

<img width="573" height="263" alt="image" src="https://github.com/user-attachments/assets/210e4db2-b7cc-4baf-bc7d-a0fa0773d055" />

<img width="587" height="345" alt="image" src="https://github.com/user-attachments/assets/a7566f9c-9779-45b2-a938-eb7f7ab3fef5" />

<img width="617" height="326" alt="image" src="https://github.com/user-attachments/assets/4f949ae0-5770-4537-8c07-5419cfcc75ab" />

<img width="640" height="336" alt="image" src="https://github.com/user-attachments/assets/09f1d13b-33d7-4a04-abea-0c0db167a5da" />

<img width="570" height="314" alt="image" src="https://github.com/user-attachments/assets/75a929ec-0dd4-462a-bbd9-118e0ebea499" />

### Core
- **Enable Time Based Expression**: 曲時間連動機能の有効/無効
- **Use Song Specific Script**: カスタム譜面フォルダ内の `SongVMCBlendShape.json` / `NalulunaAvatarsEvents.json` を優先するかの有効/無効
- **Enable Event Based Expression**: イベント連動機能の有効/無効

### OSC
- **Enable OSC Receiver (BlendShape detect)**: VMCの `OSCでモーション送信を有効にする` から BlendShape 名を自動検出する機能の有効/無効
- **VMC Host**: 送受信ホスト（既定: `127.0.0.1`）
- **VMC Send Port**: VMCへの送信ポート（既定: `39540`）
- **VMC Listen Port**: VMCからの受信ポート（既定: `39539`）
- **Refresh**: OSC 受信再起動 + 検出候補の再読込
- **Detected from VMC**: 現在検出済み BlendShape 名の表示

### Default Action
- **Default Transition**: Action の `Transition` が 0 以下の場合に使う既定遷移秒数（既定: `0.1`）

### Event Enable（Event Based Expression）
各イベントを個別に ON/OFF できます。

- **Game Start Event**: 譜面開始
- **Game End Event**: 譜面終了（Clear/Fail 共通でトリガー）
- **Pause Event**: ポーズ時
- **Resume Event**: ポーズ解除時
- **Combo Event**: コンボが `Combo Count` の倍数に達した時
- **Combo Count**: Combo Eventをトリガーする値
- **Combo Drop Event**: コンボが途切れた時
- **Miss Event**: ミス（見逃し/バッドカット）時
- **Bomb Event**: ボムを切った時
- **Clear Event**: クリア時
- **Fail Event**: 失敗時
- **Full Combo Event**: フルコンボクリア時

### Event Action Editor
- **Preset / Preset Name / Save Preset / Reload Presets**
	- Event Action の一括保存/再読込
	- 保存先: `UserData/VMCBlendShapeControl/EventActionSettings/*.JSON`
- **Edit Target Event**
	- 編集対象イベントを切り替え
- **BlendShape (dropdown/manual)**
	- 対象のBlendShape　`-----`は無効
- **Value**
	- BlendShapeの適用値
- **Duration(sec)**
	- 対象のBlendShapeに変更する１サイクル全体の時間(秒)
- **Transition(sec)**
	- 対象のBlendShapeに変化する時間(秒)
- **下部 サマリー**
	- 現在編集中 Action の内容を表示

---

## 各種設定の手動編集（UserData/VMCBlendShapeControl.json）

Beat Saber の `UserData/VMCBlendShapeControl.json` を手動編集すると、UI にない項目も設定できます。

主な項目:
- `vmcExpressionScriptPath`: 既定の時間連動スクリプトパス
- `enableTimeBasedExpression` / `enableEventBasedExpression`
- `vmcHost` / `vmcSendPort` / `vmcListenPort`
- `defaultTransition`
- 各 `*EventAction`（イベント実行 Action）

イベント Action の例:

```json
"gameStartEventAction": {
	"BlendShape": "Joy",
	"Value": 1.0,
	"Duration": 0.8,
	"Transition": 0.1
}
```

# 曲時間連動スクリプトの使用方法（Time Based Expression）
`Enable Time Based Expression` を有効にします。

`Use Song Specific Script` が有効な場合、カスタム譜面フォルダ内のスクリプトを次の優先順で探索します。

1. `SongVMCBlendShape.json`
2. `NalulunaAvatarsEvents.json`

上記が見つかった場合は、その曲専用スクリプトを使用します。
存在しない場合は `UserData/VMCBlendShapeControl/DefaultVMCBlendShape.json` を使用します。

## スクリプト JSON 例

```json
{
	"Version": "1.0.0",
	"Settings": {
		"defaultFacialExpressionTransition": 0.1
	},
	"TimeScript": [
		{
			"SongTime": "5.5",
			"Action": {
				"BlendShape": "Joy",
				"Value": 1.0,
				"Duration": 0.8,
				"Transition": 0.1
			}
		},
		{
			"SongTime": "7.0",
			"Action": {
				"BlendShape": "Sorrow",
				"Value": 1.0,
				"Duration": 0.8,
				"Transition": 0.1
			}
		}
	]
}
```

## TimeScript の各項目
- **SongTime**: トリガー時刻（秒）。文字列で指定
	- `5.5` のような小数秒を指定
	- 国毎の表記差異により `.` / `,` の両方を許容する実装ですが、`5.5` 形式を推奨
- **Action.BlendShape**: 送信する BlendShape 名
- **Action.Value**: 送信値（実行時に 0.0～1.0 に制限）
- **Action.Duration**: 1 サイクル全体の長さ（秒）
- **Action.Transition**: 変化にかける秒数（0 以下の場合は `defaultTransition` 使用）

### Duration / Transition の実行仕様
- `Duration > 0` の場合
	- 目標値へ遷移 → ホールド → 0 へ遷移 の 1 サイクルを実行
	- `effectiveTransition = min(Transition, Duration / 2)`
	- `hold = Duration - 2 * effectiveTransition`
- `Duration == 0` の場合
	- 目標値への片道遷移のみ実行（自動で 0 へ戻さない）

## `NalulunaAvatarsEvents.json` 互換フォーマット
`ChroMapper-CameraMovement` の README に記載されている `NalulunaAvatarsEvents.json` を読み込めます。

- `_events` のうち、次の `_key` に対応しています。
	- `BlendShape`: `_value`（`"プリセット"` または `"プリセット, 値"`）を Action に変換します。`_duration` は Action の `Duration` として扱います。
- `_events` のそれ以外の `_key`（例: `SetBlendShapeNeutral`, `StartAutoBlink`, `StopAutoBlink`, `StartSing`, `StopSing` など）は無視します。
- `_settings.defaultFacialExpressionTransitionSpeed` は、`defaultFacialExpressionTransition = 1.66 / speed`（秒）に変換して適用します。
- `_settings.blendShapesNoBlinkUser` / `_settings.noDefaultBlendShapeChangeKeys` は互換のため許容しますが、現行実装では参照しません。

互換フォーマット例:

```json
{
	"_version": "0.0.1",
	"_events": [
		{ "_time": 1.0, "_duration": 1.2, "_key": "BlendShape", "_value": "Joy, 1.0" }
	],
	"_settings": {
		"defaultFacialExpressionTransitionSpeed": 20
	}
}
```

# イベント連動の実行仕様（Event Based Expression）

## 発火イベント
- Game Start / End
- Pause / Resume
- Combo / Combo Drop
- Miss / Bomb
- Clear / Fail / Full Combo

## 実行キューの挙動
- 新しい Action が来たら、実行中 Action を中断して最新 Action を優先します。

# OSC 通信仕様

## 送信
- `/VMC/Ext/Blend/Val` に `(BlendShape名, 値)` を送信
- `/VMC/Ext/Blend/Apply` を送信

## 受信（BlendShape 自動検出）
- `/VMC/Ext/Blend/Val` の第 1 引数（文字列）を候補名として収集
- `/VMC/Ext/VRM` 受信時に候補をクリア

# Event Action Preset
- 保存先: `UserData/VMCBlendShapeControl/EventActionSettings`
- 拡張子: `.JSON`
- 保存内容:
	- `comboTriggerCount`
	- 各 `*EventAction` 一式

# ライセンス
MIT License
