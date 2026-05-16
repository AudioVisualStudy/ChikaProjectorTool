using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace yugop.fullscreen
{
    /// <summary>再生中にランタイムの FullScreenToggle が発火するイベントを購読し、Game View フルスクリーンをトグルする。</summary>
    [InitializeOnLoad]
    internal static class FullScreenPlayModeBridge
    {
        private const string MenuRoot = "Tools/yFullScreen";

        static FullScreenPlayModeBridge()
        {
            FullScreenToggle.RequestToggleFullScreen += OnRequestToggle;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    CloseFullscreenOnPlayInstances();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    OpenFullscreenIfRequested();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    FullScreenToggle.ResetStaticInputState();
                    break;
            }
        }

        private static void CloseFullscreenOnPlayInstances()
        {
            foreach (var fs in FullScreen.GetAllFullscreen())
            {
                if (fs is FullscreenWindow fw && fw.CreatedByFullscreenOnPlay)
                    fw.Close();
            }
        }

        private static Type GetGameViewType() => Types.PlayModeView ?? Types.GameView;

        private static (Type type, EditorWindow window) GetGameViewContext()
        {
            var t = GetGameViewType();
            if (t == null) return (null, null);
            var w = FullScreenUtility.GetMainGameView();
            if (w == null) return (null, null);
            return (t, w);
        }

        private static void OpenFullscreenIfRequested()
        {
            var toggles = UnityEngine.Object.FindObjectsByType<FullScreenToggle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (toggles == null || toggles.Length == 0) return;
            if (!toggles.Any(t => t != null && t.FullScreenOnPlay)) return;

            var (gameViewType, gameView) = GetGameViewContext();
            if (gameViewType == null || gameView == null) return;
            if (FullScreen.GetFullscreenFromView(gameView) != null) return;

            var fullscreen = FullScreen.MakeFullscreen(gameViewType, gameView);
            if (fullscreen != null)
                fullscreen.CreatedByFullscreenOnPlay = true;
        }

        private static void OnRequestToggle()
        {
            if (!EditorApplication.isPlaying) return;
            var (gameViewType, gameView) = GetGameViewContext();
            if (gameViewType == null || gameView == null) return;
            FullScreen.ToggleFullscreen(gameViewType, gameView);
        }

        [MenuItem(MenuRoot + "/Add FullScreenToggle to selected GameObject", false, 0)]
        private static void AddToggleToSelection()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning("[yFullScreen] No GameObject selected. Select a GameObject in the hierarchy.");
                return;
            }
            if (go.GetComponent<FullScreenToggle>() != null)
            {
                Debug.Log("[yFullScreen] FullScreenToggle is already on " + go.name);
                return;
            }
            go.AddComponent<FullScreenToggle>();
            Debug.Log("[yFullScreen] Added FullScreenToggle to " + go.name);
        }

        [MenuItem(MenuRoot + "/Add FullScreenToggle to selected GameObject", true)]
        private static bool AddToggleToSelectionValidate()
        {
            return Selection.activeGameObject != null;
        }
    }
}
