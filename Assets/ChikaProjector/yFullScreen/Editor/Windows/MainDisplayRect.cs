#if UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace yugop.fullscreen.Windows
{
    /// <summary>Returns main display rect in Unity editor coordinate system (DPI-corrected for Windows).</summary>
    internal static class MainDisplayRect
    {
        private const int MonitorInfoSize = 72;

        /// <summary>Get the main display rect in Unity's editor coordinate system. Use this for fullscreen window position/size.</summary>
        public static Rect GetUnityCorrectedRect()
        {
            var list = new List<(Rect MonitorArea, float scaleFactor2, bool Primary)>();

            bool OnMonitor(IntPtr hMonitor, IntPtr hdcMonitor, ref NativeRect lprcMonitor, IntPtr dwData)
            {
                var mi = new MonitorInfoEx();
                mi.Init();
                mi.size = MonitorInfoSize;

                if (!User32.GetMonitorInfo(hMonitor, ref mi))
                    return true;

                uint dpiX = 96, dpiY = 96;
                try
                {
                    ShCore.GetDpiForMonitor(hMonitor, MonitorDpiType.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                }
                catch { /* use 96 if shcore fails */ }

                float scaleFactor2 = dpiX / 96f;
                bool primary = (mi.flags & 1) != 0;
                list.Add((mi.monitor, scaleFactor2, primary));
                return true;
            }

            User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, OnMonitor, IntPtr.Zero);

            if (list.Count == 0)
                return FallbackRect();

            float firstScale = list[0].scaleFactor2;
            var primaryItem = list.Find(t => t.Primary);
            if (primaryItem.MonitorArea.size.x == 0)
                primaryItem = list[0];

            var origin = primaryItem.MonitorArea.min;
            var size = primaryItem.MonitorArea.size;

            Rect dpiCorrected = new Rect(
                Mathf.Round(origin.x / firstScale),
                Mathf.Round(origin.y / firstScale),
                Mathf.Round(size.x / primaryItem.scaleFactor2),
                Mathf.Round(size.y / primaryItem.scaleFactor2)
            );

            return InternalEditorUtility.GetBoundsOfDesktopAtPoint(dpiCorrected.center);
        }

        private static Rect FallbackRect()
        {
            var r = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height);
            return EditorGUIUtility.PixelsToPoints(r);
        }
    }
}
#endif
