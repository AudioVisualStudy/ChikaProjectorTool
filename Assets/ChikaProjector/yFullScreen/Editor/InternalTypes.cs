using System;

namespace yugop.fullscreen
{
    /// <summary>UnityEditor internal types used for Game View fullscreen.</summary>
    public static class Types
    {
        public static readonly Type HostView = ReflectionUtility.FindClass("UnityEditor.HostView");
        public static readonly Type ContainerWindow = ReflectionUtility.FindClass("UnityEditor.ContainerWindow");
        public static readonly Type View = ReflectionUtility.FindClass("UnityEditor.View");
        public static readonly Type GUIView = ReflectionUtility.FindClass("UnityEditor.GUIView");
        public static readonly Type GameView = ReflectionUtility.FindClass("UnityEditor.GameView");
        public static readonly Type PreviewEditorWindow = ReflectionUtility.FindClass("UnityEditor.PreviewEditorWindow");
        public static readonly Type PlayModeView = ReflectionUtility.FindClass("UnityEditor.PlayModeView");
    }
}
