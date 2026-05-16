# ChikaProjectorTool

多摩美上野毛キャンパス本館地下の横長プロジェクターに Unity から出力するためのマッピング変換ツールです。

---

## スクリーンショット

### 編集画面

マッピング OFF、Game View を実サイズ（7100 × 950）で制作している状態の例です。

<img width="640" height="360" alt="編集画面" src="https://github.com/user-attachments/assets/fa11b50a-c8da-4199-9e01-bae713eb0059" />

### マッピング変換後の再生画面

マッピング ON、4K 出力に変換して再生している状態の例です。

<img width="640" height="360" alt="マッピング変換後の再生画面" src="https://github.com/user-attachments/assets/9ed2ad16-94dc-4579-835b-5d46528495dd" />

---

## 必要環境

### 推奨環境

- **Unity Editor**: `6000.3.6` 以上で制作しています。別バージョンは未検証ですが、比較的新しいバージョンであれば動く可能性は高いです。

| 項目 | 内容 |
|------|------|
| Unity Editor | 推奨: `6000.3.6` 以上。 |
| レンダーパイプライン | Universal Render Pipeline / Built-in Render Pipeline |
| OS | Windows 11 で検証済み。macOS は未検証（たぶん動く） |

---

## 使い方

### 下準備

1. **PC のディスプレイ解像度を 4K（3840 × 2160）に設定**します。
2. **制作する Unity プロジェクトの Game View** に、次の 2 つの解像度プリセットを追加します（Game View 左上の解像度メニュー → **+**）。

   | プリセット名（例） | Type | Width × Height |
   |------------------|------|----------------|
   | プロジェクター実サイズ | Aspect Ratio | 7100 × 950 |
   | 4K | Fixed Resolution | 3840 × 2160 |

Game View の解像度プリセット追加画面の例です。

<img width="500" height="260" alt="Game View 解像度プリセット" src="https://github.com/user-attachments/assets/717f88ac-f1ce-4f74-bef0-9c1bc8c48a07" />

---

### インストール

1. **Release ページ**から UnityPackage（`.unitypackage`）をダウンロードします。
2. 自分の Unity プロジェクトで **Assets → Import Package → Custom Package...** からインポートします。

---

### 設定

1. **`ChikaProjectorPrefab`** をヒエラルキービューにドラッグ＆ドロップし、シーンに追加します。
2. インスペクタの **ChikaProjector** コンポーネントで **`Mapping Enabled`** にチェックを入れると、マッピングが有効になります。
3. 同じ Prefab に付いている **FullScreenToggle**（yFullScreen）について:
   - **再生後**、インスペクタで指定したショートカットキー（既定: **F12**）を押すとフルスクリーン再生になります。もう一度 **F12** で解除できます。
   - **`Full Screen On Play`** にチェックすると、再生開始時に自動でフルスクリーンになります。

インスペクタの設定例です。

<img width="426" height="882" alt="ChikaProjector インスペクタ" src="https://github.com/user-attachments/assets/7c645ba8-d5e5-4f92-88bb-26cf4499e551" />

---

### 想定するワークフロー

#### 1. コンテンツ制作時

Game View をプロジェクター実サイズ（**7100 × 950**）のプリセットに設定して制作します。

<img width="640" height="360" alt="制作時の Game View（7100×950）" src="https://github.com/user-attachments/assets/10918137-a70e-41ea-9744-d519eab1301b" />

#### 2. 再生時

Game View を **4K（3840 × 2160）** のプリセットに切り替え、**`Mapping Enabled`** を ON、**`Full Screen On Play`** を ON にして再生します。

<img width="640" height="360" alt="再生時の Game View（4K・マッピング ON）" src="https://github.com/user-attachments/assets/e4ff652c-92b7-47d6-97ad-90c0e631845c" />

---

### 参考: ChikaProjector の主な項目

| 項目 | 説明 |
|------|------|
| `mappingEnabled` | マッピング出力の有効 / 無効 |
| `targetCamera` | 実解像度でシーンを描画するカメラ（未設定時は `Camera.main`、無い場合は同じ GameObject の `Camera`） |
| `designWidth` / `designHeight` | デザイン用 `RenderTexture` の解像度（既定: 7100 × 950） |
| `outputWidth` / `outputHeight` | マッピング後の論理出力解像度（既定: 3840 × 2160） |
| `forceOutputResolution` | PlayMode / ビルド時に `Screen.SetResolution` で出力解像度を合わせる（エディタでは効きにくい） |
| `fullScreenMode` | `forceOutputResolution` 利用時のフルスクリーン種別 |
