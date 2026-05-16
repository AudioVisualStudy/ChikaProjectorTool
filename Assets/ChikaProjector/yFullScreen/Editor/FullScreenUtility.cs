using System.Linq;
using UnityEditor;
using UnityEngine;

namespace yugop.fullscreen
{
    /// <summary>Game View lookup for fullscreen.</summary>
    public static class FullScreenUtility
    {
        private const float DefaultToolbarHeight = 17f;

        /// <summary>Get the main Game View (PlayModeView) window.</summary>
        public static EditorWindow GetMainGameView()
        {
            if (Types.GameView != null && Types.GameView.HasMethod("GetMainGameView"))
                return Types.GameView.InvokeMethod<EditorWindow>("GetMainGameView");
            if (Types.PreviewEditorWindow != null && Types.PreviewEditorWindow.HasMethod("GetMainPreviewWindow"))
                return Types.PreviewEditorWindow.InvokeMethod<EditorWindow>("GetMainPreviewWindow");
            if (Types.PlayModeView != null && Types.PlayModeView.HasMethod("GetMainPlayModeView"))
                return Types.PlayModeView.InvokeMethod<EditorWindow>("GetMainPlayModeView");
            // Fallback: first GameView found
            var all = Resources.FindObjectsOfTypeAll(Types.GameView);
            return all.Length > 0 ? (EditorWindow)all[0] : null;
        }

        /// <summary>Default height of the Game View toolbar (for hiding it in fullscreen).</summary>
        public static float GetToolbarHeight()
        {
            try
            {
                if (typeof(EditorGUI).HasField("kWindowToolbarHeight"))
                {
                    var result = typeof(EditorGUI).GetFieldValue<object>("kWindowToolbarHeight");
                    if (result is int i)
                        return i;
                    return result.GetPropertyValue<float>("value");
                }
                return DefaultToolbarHeight;
            }
            catch
            {
                return DefaultToolbarHeight;
            }
        }
    }
}
