# ChikaProjectorTool

多摩美上野毛キャンパス本館地下の横長プロジェクターにUNITYから出力するためのマッピング変換ツールです。

## 必要環境

### 推奨環境

- **Unity Editor**: `6000.3.6` 以上で制作しています。別バージョンは未検証ですが、比較的新しいバージョンであれば動く可能性は高いです。

| 項目 | 内容 |
|------|------|
| Unity Editor | 推奨: `6000.3.6` 以上。 |
| レンダーパイプライン | Universal Render Pipeline / Built-in Render Pipeline |
| OS | Windows 11 で検証済み。macOS は未検証（たぶん動く） |

## 使い方

### 下準備

1. **PC のディスプレイ解像度を 4K（3840 × 2160）に設定**します。
2. **制作する Unity プロジェクトの Game View** に、次の 2 つの解像度プリセットを追加します（Game View 左上の解像度メニュー → **+**）。

   | プリセット名（例） | Type | Width × Height |
   |------------------|------|----------------|
   | プロジェクター実サイズ | Aspect Ratio | 7100 × 950 |
   | 4K| Fixed Resolution | 3840 × 2160 |


### インストール

1. **Release ページ**から UnityPackage（`.unitypackage`）をダウンロードします。
2. 自分の Unity プロジェクトで **Assets → Import Package → Custom Package...** からインポートします。


### 設定

1. **`ChikaProjectorPrefab`** をヒエラルキービューにドラッグ＆ドロップし、シーンに追加します。
2. インスペクタの **ChikaProjector** コンポーネントで **`Mapping Enabled`** にチェックを入れると、マッピングが有効になります。
3. 同じ Prefab に付いている **FullScreenToggle**（yFullScreen）について:
   - **再生後**、インスペクタで指定したショートカットキー（既定: **F12**）を押すとフルスクリーン再生になります。もう一度 **F12** で解除できます。
   - **`Full Screen On Play`** にチェックすると、再生開始時に自動でフルスクリーンになります。

### 想定するワークフロー

1. **コンテンツ制作時**  
   Game View をプロジェクター実サイズ（**7100 × 950**）のプリセットに設定して制作します。
2. **再生時**  
   Game View を **4K（3840 × 2160）** のプリセットに切り替え、**`Mapping Enabled`** を ON、**`Full Screen On Play`** を ON にして再生します。

### 参考: ChikaProjector の主な項目

| 項目 | 説明 |
|------|------|
| `mappingEnabled` | マッピング出力の有効 / 無効 |
| `targetCamera` | 実解像度でシーンを描画するカメラ（未設定時は `Camera.main`、無い場合は同じ GameObject の `Camera`） |
| `designWidth` / `designHeight` | デザイン用 `RenderTexture` の解像度（既定: 7100 × 950） |
| `outputWidth` / `outputHeight` | マッピング後の論理出力解像度（既定: 3840 × 2160） |
| `forceOutputResolution` | PlayMode / ビルド時に `Screen.SetResolution` で出力解像度を合わせる（エディタでは効きにくい） |
| `fullScreenMode` | `forceOutputResolution` 利用時のフルスクリーン種別 |
