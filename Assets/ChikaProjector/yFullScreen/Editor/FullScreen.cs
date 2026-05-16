using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace yugop.fullscreen {
    /// <summary>Entry point for Game View fullscreen toggle.</summary>
    public static class FullScreen {
        /// <summary>All active fullscreen containers.</summary>
        public static FullscreenContainer[] GetAllFullscreen() {
            var all = Resources.FindObjectsOfTypeAll<FullscreenContainer>();
            return all.Where(fs => fs.m_dst.Container != null).ToArray();
        }

        /// <summary>Get the fullscreen container that contains the given view/window, or null.</summary>
        public static FullscreenContainer GetFullscreenFromView(ScriptableObject viewOrWindow) {
            if (viewOrWindow == null) return null;
            var pyramid = new ViewPyramid(viewOrWindow);
            return GetAllFullscreen()
                .FirstOrDefault(fs => fs.ActualViewPyramid.View == pyramid.View);
        }

        /// <summary>Create a fullscreen window for the given type and optional window.</summary>
        public static FullscreenWindow MakeFullscreen(Type type, EditorWindow window = null, bool disposableWindow = false) {
            var rect = FullScreenRects.GetMainDisplayRect();
            var fullscreen = ScriptableObject.CreateInstance<FullscreenWindow>();
            fullscreen.OpenWindow(rect, type, window, disposableWindow);
            return fullscreen;
        }

        /// <summary>Toggle fullscreen: open if not fullscreen, close if already fullscreen.</summary>
        public static void ToggleFullscreen(Type type, EditorWindow window = null) {
            var existing = window != null ? GetFullscreenFromView(window) : null;
            if (existing != null) {
                existing.Close();
                return;
            }
            MakeFullscreen(type, window);
        }
    }
}
