using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 実解像度で描いた映像を、4K 出力用の上下 2 段レイアウトへ変換する。
[ExecuteAlways]
[DisallowMultipleComponent]
public class ChikaProjector : MonoBehaviour {
    // --- インスペクタ設定 ---
    [Header("マッピング出力を有効にする場合はチェック")]
    public bool mappingEnabled = true; // 出力マッピングを有効にするか。
    [Header("表示領域外の背景色")]
    public Color backgroundColor = Color.black; // 4K 出力の余白色。
    [Space(20)]
    [Header("シーンを描画するカメラ（未設定時は Camera.main）")]
    public Camera targetCamera; // 実解像度の映像を描画するカメラ。未設定時は Camera.main、無ければ同じ GameObject の Camera。
    [Space(20)]
    [Header("解像度の設定（触らなくて良い）")]
    [Header("プロジェクターが実際に投影する映像の解像度（7100×950）")]
    public int designWidth = 7100; // デザイン用 RenderTexture の横ピクセル数。
    public int designHeight = 950; // デザイン用 RenderTexture の縦ピクセル数。
    [Header("PCからプロジェクターに出力する解像度（3840×2160）")]
    public int outputWidth = 3840; // 出力先ディスプレイの横ピクセル数。
    public int outputHeight = 2160; // 出力先ディスプレイの縦ピクセル数。

    [Space(20)]
    [Header("ビルドしたスタンドアロンアプリ用の設定（Editor再生時には効かない／挙動は未確認です）")]
    [Header("開始時に強制的に出力解像度を指定値へ変更するか")]
    public bool forceOutputResolution = false; // PlayMode 中に出力解像度を指定値へ変更するか。
    [Header("上記がONの時のウインドウをどのように描画するか（挙動は未確認です）")]
    public FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow; // 解像度変更時のフルスクリーン種別。

    [Space(20)]
    [Header("詳細設定（触らなくて良い）")]
    public int displayLayer = 31; // 表示用 Quad だけを描画するレイヤー。
    public int displayDepthOffset = 100; // 表示用 Camera を元 Camera より後に描画する深度差。


    // --- 内部状態 ---

    private const string RenderTextureName = "ChikaProjector Design Texture";
    private const string DisplayRootName = "ChikaProjector Display";
    private const string TopQuadName = "ChikaProjector Top Half";
    private const string BottomQuadName = "ChikaProjector Bottom Half";
    private const string UnlitTextureShaderName = "Unlit/Texture";
    private const string SpritesShaderName = "Sprites/Default";

    private Camera activeCamera; // 現在 RenderTexture を割り当てているカメラ。
    private RenderTexture designTexture; // 7100x950 の実解像度映像。
    private RenderTexture originalTargetTexture; // 有効化前に Camera が持っていた描画先。
    private int originalCullingMask; // 有効化前に Camera が持っていた描画レイヤー。
    private bool hasOriginalCameraState; // 元 Camera 状態を保存済みか。

    private GameObject displayRoot; // 表示用オブジェクトの親。
    private Camera displayCamera; // 最終出力用の Orthographic Camera。
    private GameObject topQuad; // 左半分を上段へ表示する Quad。
    private GameObject bottomQuad; // 右半分を下段へ表示する Quad。
    private Material topMaterial; // 左半分表示用 Material。
    private Material bottomMaterial; // 右半分表示用 Material。
#if UNITY_EDITOR
    private bool isValidateApplyQueued; // OnValidate 後の状態反映を予約済みか。
#endif

    // --- ライフサイクル ---

    // 初期値を補完する。
    private void Reset() {
        targetCamera = ResolveDefaultTargetCamera();
    }

    // 有効化時にマッピング状態を反映する。
    private void OnEnable() {
        ApplyMappingState();
    }

    // 毎フレーム、設定変更と Game View サイズ変更を反映する。
    private void LateUpdate() {
        ApplyMappingState();
    }

    // 無効化時に元 Camera 状態へ戻す。
    private void OnDisable() {
        DisableMapping();
    }

    // 破棄時に生成物を片付ける。
    private void OnDestroy() {
        DisableMapping();
        ReleaseGeneratedResources();
    }

    // インスペクタ値を安全な範囲に丸める。
    private void OnValidate() {
        ClampSettings();
        QueueApplyMappingStateInEditor();
    }

    // --- サブ（マッピング制御用） ---

    // 現在の設定に合わせてマッピングの有効・無効を切り替える。
    private void ApplyMappingState() {
        ClampSettings();
        ResolveTargetCamera();

        if (!mappingEnabled || targetCamera == null) {
            DisableMapping();
            return;
        }

        if (activeCamera != targetCamera) {
            DisableMapping();
            activeCamera = targetCamera;
        }

        SaveOriginalCameraState();
        ApplyOutputResolution();
        EnsureRenderTexture();
        EnsureDisplayObjects();
        ConfigureSourceCamera();
        ConfigureDisplayCamera();
        ConfigureDisplayQuads();
    }

    // 通常表示へ戻す。
    private void DisableMapping() {
        if (activeCamera == null) {
            activeCamera = targetCamera;
        }

        RestoreOriginalCameraState();
        RestoreFallbackCameraState();
        SetDisplayObjectsActive(false);
        activeCamera = null;
    }

    // 対象 Camera を解決する。
    private void ResolveTargetCamera() {
        if (targetCamera != null) {
            return;
        }

        targetCamera = ResolveDefaultTargetCamera();
    }

    // 未指定時の描画先 Camera を決める。
    private Camera ResolveDefaultTargetCamera() {
        Camera mainCamera = Camera.main;
        if (mainCamera != null) {
            return mainCamera;
        }

        return GetComponent<Camera>();
    }

    // 有効化前の Camera 状態を保存する。
    private void SaveOriginalCameraState() {
        if (hasOriginalCameraState || activeCamera == null) {
            return;
        }

        originalTargetTexture = activeCamera.targetTexture;
        originalCullingMask = activeCamera.cullingMask;
        hasOriginalCameraState = true;
    }

    // 保存していた Camera 状態へ戻す。
    private void RestoreOriginalCameraState() {
        if (!hasOriginalCameraState || activeCamera == null) {
            return;
        }

        activeCamera.targetTexture = originalTargetTexture;
        activeCamera.cullingMask = originalCullingMask;
        hasOriginalCameraState = false;
        originalTargetTexture = null;
    }

    // 状態保存前に参照が切れた場合でも、通常の Camera 出力へ戻す。
    private void RestoreFallbackCameraState() {
        if (activeCamera == null || activeCamera.targetTexture == null) {
            return;
        }

        if (activeCamera.targetTexture == designTexture || activeCamera.targetTexture.name == RenderTextureName) {
            activeCamera.targetTexture = null;
        }
    }

    // PlayMode 中だけ必要に応じて出力解像度を変更する。
    private void ApplyOutputResolution() {
        if (!Application.isPlaying || !forceOutputResolution) {
            return;
        }

        if (Screen.width == outputWidth && Screen.height == outputHeight) {
            return;
        }

        Screen.SetResolution(outputWidth, outputHeight, fullScreenMode);
    }

    // --- サブ（RenderTexture 用） ---

    // 実解像度用 RenderTexture を用意する。
    private void EnsureRenderTexture() {
        if (designTexture != null && designTexture.width == designWidth && designTexture.height == designHeight) {
            return;
        }

        ReleaseRenderTexture();

        designTexture = new RenderTexture(designWidth, designHeight, 24, RenderTextureFormat.ARGB32);
        designTexture.name = RenderTextureName;
        designTexture.filterMode = FilterMode.Bilinear;
        designTexture.wrapMode = TextureWrapMode.Clamp;
        designTexture.Create();
    }

    // 実解像度用 RenderTexture を解放する。
    private void ReleaseRenderTexture() {
        if (designTexture == null) {
            return;
        }

        designTexture.Release();
        DestroyGeneratedObject(designTexture);
        designTexture = null;
    }

    // --- サブ（Camera 用） ---

    // 元 Camera を実解像度 RenderTexture へ描画する。
    private void ConfigureSourceCamera() {
        if (activeCamera == null || designTexture == null) {
            return;
        }

        activeCamera.targetTexture = designTexture;
        activeCamera.cullingMask = originalCullingMask & ~(1 << displayLayer);
    }

    // 最終出力用 Camera を設定する。ワールド座標 1 単位が出力ピクセル 1 個に相当するよう orthographicSize と aspect を合わせる。
    private void ConfigureDisplayCamera() {
        if (displayCamera == null || activeCamera == null) {
            return;
        }

        displayCamera.enabled = true;
        displayCamera.clearFlags = CameraClearFlags.SolidColor;
        displayCamera.backgroundColor = backgroundColor;
        displayCamera.cullingMask = 1 << displayLayer;
        displayCamera.depth = activeCamera.depth + displayDepthOffset;
        displayCamera.orthographic = true;
        displayCamera.orthographicSize = outputHeight * 0.5f;
        displayCamera.aspect = (float)outputWidth / outputHeight;
        displayCamera.rect = new Rect(0f, 0f, 1f, 1f);
        displayCamera.transform.localPosition = new Vector3(0f, 0f, -1f);
        displayCamera.transform.localRotation = Quaternion.identity;
        displayCamera.nearClipPlane = 0.01f;
        displayCamera.farClipPlane = 10f;
        displayCamera.useOcclusionCulling = false;
        displayCamera.allowHDR = false;
        displayCamera.allowMSAA = false;
    }

    // --- サブ（表示オブジェクト用） ---

    // 表示用 Camera / Quad / Material を用意する。
    private void EnsureDisplayObjects() {
        EnsureDisplayRoot();
        EnsureDisplayCamera();
        EnsureDisplayQuad(ref topQuad, TopQuadName);
        EnsureDisplayQuad(ref bottomQuad, BottomQuadName);
        EnsureDisplayMaterial(ref topMaterial, "ChikaProjector Top Material", new Vector2(0.5f, 1f), Vector2.zero);
        EnsureDisplayMaterial(ref bottomMaterial, "ChikaProjector Bottom Material", new Vector2(0.5f, 1f), new Vector2(0.5f, 0f));
        SetDisplayObjectsActive(true);
    }

    // 表示用オブジェクトの親を用意する。
    private void EnsureDisplayRoot() {
        if (displayRoot != null) {
            return;
        }

        displayRoot = new GameObject(DisplayRootName);
        displayRoot.hideFlags = HideFlags.DontSave;
        displayRoot.transform.SetParent(transform, false);
    }

    // 表示用 Camera を用意する。
    private void EnsureDisplayCamera() {
        if (displayCamera != null) {
            return;
        }

        GameObject cameraObject = new GameObject("ChikaProjector Display Camera");
        cameraObject.hideFlags = HideFlags.DontSave;
        cameraObject.transform.SetParent(displayRoot.transform, false);
        displayCamera = cameraObject.AddComponent<Camera>();
    }

    // 表示用 Quad を用意する。
    private void EnsureDisplayQuad(ref GameObject quad, string quadName) {
        if (quad != null) {
            return;
        }

        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = quadName;
        quad.hideFlags = HideFlags.DontSave;
        quad.transform.SetParent(displayRoot.transform, false);

        Collider quadCollider = quad.GetComponent<Collider>();
        if (quadCollider != null) {
            DestroyGeneratedObject(quadCollider);
        }
    }

    // 標準 Unlit シェーダーで切り出し用 Material を用意する。
    private void EnsureDisplayMaterial(ref Material material, string materialName, Vector2 scale, Vector2 offset) {
        if (material == null) {
            Shader shader = Shader.Find(UnlitTextureShaderName);
            if (shader == null) {
                shader = Shader.Find(SpritesShaderName);
            }

            material = new Material(shader);
            material.name = materialName;
            material.hideFlags = HideFlags.DontSave;
        }

        material.mainTexture = designTexture;
        material.mainTextureScale = scale;
        material.mainTextureOffset = offset;
    }

    // 表示 Quad を、出力画面の上下半分それぞれの左上に 3550×950 でフィットさせる。
    private void ConfigureDisplayQuads() {
        if (topQuad == null || bottomQuad == null || topMaterial == null || bottomMaterial == null) {
            return;
        }

        float halfOutputW = outputWidth * 0.5f; // 画面左端から中央までの幅。
        float halfOutputH = outputHeight * 0.5f; // 画面中央線から上端までの高さ。
        float stripW = designWidth * 0.5f; // 左半分または右半分の幅。
        float stripH = designHeight; // 各ストリップの高さ。

        float centerX = -halfOutputW + stripW * 0.5f; // 左右どちらの半分でも同じ水平位置（左寄せ）。
        float topCenterY = halfOutputH - stripH * 0.5f; // 上半分の矩形の左上基準。
        float bottomCenterY = -stripH * 0.5f; // 下半分の矩形の左上は y=0 なので、その中心。

        ConfigureDisplayQuad(topQuad, topMaterial, new Vector3(centerX, topCenterY, 0f), new Vector3(stripW, stripH, 1f));
        ConfigureDisplayQuad(bottomQuad, bottomMaterial, new Vector3(centerX, bottomCenterY, 0f), new Vector3(stripW, stripH, 1f));
    }

    // 1 枚の表示 Quad に Material・位置・サイズを反映する。
    private void ConfigureDisplayQuad(GameObject quad, Material material, Vector3 position, Vector3 scale) {
        quad.layer = displayLayer;
        quad.transform.localPosition = position;
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = scale;

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        if (renderer != null) {
            renderer.sharedMaterial = material;
        }
    }

    // 表示用オブジェクト全体の表示状態を変える。
    private void SetDisplayObjectsActive(bool isActive) {
        if (displayRoot == null) {
            return;
        }

        displayRoot.SetActive(isActive);
    }

    // 自動生成したリソースを解放する。
    private void ReleaseGeneratedResources() {
        ReleaseRenderTexture();

        DestroyGeneratedObject(topMaterial);
        DestroyGeneratedObject(bottomMaterial);
        DestroyGeneratedObject(displayRoot);

        topMaterial = null;
        bottomMaterial = null;
        displayRoot = null;
        displayCamera = null;
        topQuad = null;
        bottomQuad = null;
    }

    // --- サブ（共通用） ---

    // 設定値を Unity API が扱える範囲に丸める。
    private void ClampSettings() {
        designWidth = Mathf.Max(2, designWidth);
        designHeight = Mathf.Max(2, designHeight);
        outputWidth = Mathf.Max(2, outputWidth);
        outputHeight = Mathf.Max(2, outputHeight);
        displayLayer = Mathf.Clamp(displayLayer, 0, 31);
        displayDepthOffset = Mathf.Max(1, displayDepthOffset);
    }

    // PlayMode / EditMode に応じた破棄 API を使う。
    private void DestroyGeneratedObject(Object target) {
        if (target == null) {
            return;
        }

        if (Application.isPlaying) {
            Destroy(target);
        } else {
            DestroyImmediate(target);
        }
    }

#if UNITY_EDITOR
    // インスペクタ変更後に安全なタイミングで状態を反映する。
    private void QueueApplyMappingStateInEditor() {
        if (Application.isPlaying || isValidateApplyQueued) {
            return;
        }

        isValidateApplyQueued = true;
        EditorApplication.delayCall += ApplyQueuedMappingStateInEditor;
    }

    // 予約していた状態反映を実行する。
    private void ApplyQueuedMappingStateInEditor() {
        isValidateApplyQueued = false;

        if (this == null || !isActiveAndEnabled) {
            return;
        }

        ApplyMappingState();
    }
#else
    // Player では Editor 用の予約処理を行わない。
    private void QueueApplyMappingStateInEditor() {
    }
#endif
}
