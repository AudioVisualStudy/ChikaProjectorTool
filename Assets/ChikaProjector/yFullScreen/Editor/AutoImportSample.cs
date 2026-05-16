using System.IO;
using UnityEditor;
using UnityEngine;

namespace yugop.fullscreen
{
    /// <summary>パッケージ初回読み込み時に、サンプル Prefab を Assets/Samples/yFullScreen/ へ自動コピーする。</summary>
    public static class AutoImportSample
    {
        private const string PackageName = "com.yugop.yfullscreen";
        private const string SamplePrefabName = "yFullScreen.prefab";
        private const string DestRelativePath = "Samples/yFullScreen";

        [InitializeOnLoadMethod]
        private static void RunOnce()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                string destDir = Path.Combine(Application.dataPath, DestRelativePath);
                string destPrefab = Path.Combine(destDir, SamplePrefabName);
                if (File.Exists(destPrefab))
                    return;

                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{PackageName}/Samples/{SamplePrefabName}");
                if (packageInfo == null)
                    return;

                string srcPrefab = Path.Combine(packageInfo.resolvedPath, "Samples", SamplePrefabName);
                string srcMeta = srcPrefab + ".meta";
                if (!File.Exists(srcPrefab))
                    return;

                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(srcPrefab, destPrefab);
                if (File.Exists(srcMeta))
                    File.Copy(srcMeta, destPrefab + ".meta");

                AssetDatabase.Refresh();
            };
        }
    }
}
