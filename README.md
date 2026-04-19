# VMCBlendShapeControl
Beat Saber から VirtualMotionCapture へ、曲時間およびゲームイベントに連動した BlendShape 制御を OSC 送信する Mod です。

本 README は、現在の実装コードに合わせて記載しています。

# インストール方法
1. 事前に BSIPA 環境を導入してください。
2. 依存 Mod として BSML / SiraUtil を導入してください。
3. `VMCBlendShapeControl.dll` を Beat Saber の `Plugins` フォルダへ配置してください。

# 機能と設定について
ゲーム内の MOD 設定画面（BSML）から「VMC BlendShape Control」の各種機能を設定できます。

本 MOD には、大まかに以下の 2 つの制御機能があります。

1. **Time Based Expression（曲時間連動）**
	 - JSON スクリプト（`DefaultVMCBlendShape.json` / `SongVMCBlendShape.json` / `NalulunaAvatarsEvents.json`）の曲時間イベントに到達したら Action を実行します。
2. **Event Based Expression（イベント連動）**
	 - 開始/終了/ポーズ/コンボ/ミス/クリアなどのゲームイベント発生時に Action を実行します。

## MOD 設定画面（UI）の設定項目について

### Core
- **Enable Time Based Expression**: 曲時間連動機能の有効/無効
- **Enable Event Based Expression**: イベント連動機能の有効/無効
- **Use Song Specific Script**: カスタム譜面フォルダ内の `SongVMCBlendShape.json` / `NalulunaAvatarsEvents.json` を優先するかの有効/無効

### OSC
- **Enable OSC Receiver (BlendShape detect)**: 受信 OSC から BlendShape 名を自動検出する機能の有効/無効
- **VMC Host**: 送信先ホスト（既定: `127.0.0.1`）
- **VMC Send Port**: 送信ポート（既定: `39540`）
- **VMC Listen Port**: 受信ポート（既定: `39539`）
- **Refresh**: OSC 受信再起動 + 検出候補の再読込
- **Detected from VMC / Preview**: 現在検出済み BlendShape 名の表示

### Default Action
- **Default Transition**: Action の `Transition` が 0 以下の場合に使う既定遷移秒数（既定: `0.1`）
- **Default Neutral BlendShape**: 設定値として保持（現状は主に候補表示用途）
- **Default Neutral Value**: 設定値として保持（現状は主に候補表示用途）

### Event Enable（Event Based Expression）
各イベントを個別に ON/OFF できます。

- **Game Start Event**: 譜面開始
- **Game End Event**: 譜面終了（Clear/Fail 共通で発火）
- **Pause Event**: ポーズ時
- **Resume Event**: ポーズ解除時
- **Combo Event**: コンボが `Count` の倍数に達した時
- **Combo Drop Event**: コンボが途切れた時
- **Miss Event**: ミス（見逃し/不正確カット）時
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
	- 空文字はドロップダウン上で `-` として表示
- **Value / Duration / Transition**
	- 対象イベントの Action を編集
- **下部 Summary**
	- `Target=...` を先頭に、現在編集中 Action の内容を表示

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
- **SongTime**: 発火時刻（秒）。文字列で指定
	- `5.5` のような小数秒を指定
	- カルチャ差異により `.` / `,` の両方を許容する実装ですが、`5.5` 形式を推奨
- **Action.BlendShape**: 送信する BlendShape 名
- **Action.Value**: 送信値（実行時に 0.0～1.0 へクランプ）
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
- 新しい Action が来たら、実行中 Action を中断して最新 Action を優先します（先勝ちキューではなく最新優先）。
- これにより、イベント連打時の遅延蓄積を抑える設計になっています。

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