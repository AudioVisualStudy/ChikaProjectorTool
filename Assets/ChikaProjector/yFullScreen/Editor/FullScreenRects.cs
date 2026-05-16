using UnityEditor;
using UnityEngine;

namespace yugop.fullscreen
{
    /// <summary>Returns main display rect for fullscreen.</summary>
    [InitializeOnLoad]
    public static class FullScreenRects
    {
        // PopupMenu ContainerWindow 閉直後に pixelsPerPoint が一時的に 1 になるため、正しい値をキャッシュしておく
        private static float _reliablePpp;

        static FullScreenRects()
        {
            _reliablePpp = EditorGUIUtility.pixelsPerPoint;
        }

        /// <summary>Main display rect in editor points (DPI-corrected so ContainerWindow scale matches).</summary>
        public static Rect GetMainDisplayRect()
        {
#if UNITY_EDITOR_WIN
            if (Application.platform == RuntimePlatform.WindowsEditor)
                return Windows.MainDisplayRect.GetUnityCorrectedRect();
#endif
            int pixelW = Screen.currentResolution.width;
            int pixelH = Screen.currentResolution.height;

            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                float ppp = EditorGUIUtility.pixelsPerPoint;
                if (ppp > 1f)
                    _reliablePpp = ppp;
                float scale = _reliablePpp > 0f ? _reliablePpp : Mathf.Max(ppp, 1f);
                return new Rect(0f, 0f, pixelW / scale, pixelH / scale);
            }

            return new Rect(0f, 0f, pixelW, pixelH);
        }
    }
}
