#if UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
using UnityEngine;

namespace yugop.fullscreen.Windows
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public static implicit operator Rect(NativeRect other)
        {
            return Rect.MinMaxRect(other.left, other.top, other.right, other.bottom);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct MonitorInfoEx
    {
        private const int CCHDEVICENAME = 0x20;

        public int size;
        public NativeRect monitor;
        public NativeRect work;
        public uint flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string DeviceName;

        public void Init()
        {
            this.size = 40 + 1 * CCHDEVICENAME;
            this.DeviceName = string.Empty;
        }
    }
}
#endif
