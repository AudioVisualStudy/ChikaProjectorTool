using System;
using UnityEditor;
using UnityEngine;
using View = UnityEngine.ScriptableObject;
using ContainerWindow = UnityEngine.ScriptableObject;

namespace yugop.fullscreen
{
    /// <summary>macOS + Unity 6.3 以降で ShowMode.NoShadow を使うと半透明になるバグの回避用。</summary>
    internal static class FullscreenShowMode
    {
        public static int GetInitialShowMode()
        {
#if UNITY_EDITOR_OSX
            if (IsUnity63OrNewer())
                return (int)ShowMode.PopupMenu;
#endif
            return (int)ShowMode.NoShadow;
        }

        private static bool IsUnity63OrNewer()
        {
            string v = Application.unityVersion;
            if (string.IsNullOrEmpty(v)) return false;
            if (!v.StartsWith("6000.")) return false;
            if (v.Length < 6) return true;
            return string.CompareOrdinal(v, "6000.3") >= 0;
        }
    }
    /// <summary>Manages the fullscreen ContainerWindow and view pyramid.</summary>
    public abstract class FullscreenContainer : ScriptableObject
    {
        [SerializeField] private int m_ourIndex = -1;
        [SerializeField] private bool m_old;

        private static int CurrentIndex
        {
            get => EditorPrefs.GetInt("yFullScreen.FullscreenIdx", 0);
            set => EditorPrefs.SetInt("yFullScreen.FullscreenIdx", value);
        }

        [SerializeField] public ViewPyramid m_src;
        [SerializeField] public ViewPyramid m_dst;

        public ViewPyramid ActualViewPyramid => new ViewPyramid(m_dst.Container);
        public View FullscreenedView => ActualViewPyramid.View;

        public Rect Rect
        {
            get => m_dst.Container != null ? m_dst.Container.GetPropertyValue<Rect>("position") : new Rect();
            set
            {
                if (m_dst.Container == null) return;
                m_dst.Container.InvokeMethod("SetMinMaxSizes", value.size, value.size);
                m_dst.Container.SetPropertyValue("position", value);
            }
        }

        private void Update()
        {
            if (m_dst.Container == null)
                Close();
        }

        protected virtual void OnEnable()
        {
            if (m_ourIndex == -1)
            {
                m_ourIndex = CurrentIndex++;
                name = $"Fullscreen #{m_ourIndex}";
                hideFlags = HideFlags.HideAndDontSave;
            }

#if UNITY_2018_1_OR_NEWER
            EditorApplication.wantsToQuit += WantsToQuit;
#endif

            if (m_old && m_dst.Container == null)
            {
                After.Frames(1, () => DestroyImmediate(this, true));
            }

            m_old = true;
            EditorApplication.update += Update;
        }

        protected virtual void OnDisable()
        {
            EditorApplication.update -= Update;
#if UNITY_2018_1_OR_NEWER
            EditorApplication.wantsToQuit -= WantsToQuit;
#endif
        }

        protected virtual void OnDestroy()
        {
            if (m_dst.Container != null)
                m_dst.Container.InvokeMethod("Close");
        }

        public virtual void Close()
        {
            if (m_dst.Container != null)
                m_dst.Container.InvokeMethod("Close");
            DestroyImmediate(this, true);
        }

        public virtual void Focus()
        {
            if (FullscreenedView != null && FullscreenedView.IsOfType(Types.GUIView))
                FullscreenedView.InvokeMethod("Focus");
        }

        public virtual bool IsFocused()
        {
            return EditorWindow.focusedWindow != null && EditorWindow.focusedWindow == ActualViewPyramid.Window;
        }

#if UNITY_2018_1_OR_NEWER
        private bool WantsToQuit()
        {
            Close();
            return true;
        }
#endif

        protected ViewPyramid CreateFullscreenViewPyramid(Rect rect, EditorWindow childWindow)
        {
            var hv = (ScriptableObject)CreateInstance(Types.HostView);
            var cw = (ScriptableObject)CreateInstance(Types.ContainerWindow);

            hv.name = name;
            cw.name = name;
            childWindow.name = name;

            hv.SetPropertyValue("actualView", childWindow);
            cw.SetPropertyValue("position", rect);
            cw.SetPropertyValue("rootView", hv);

            childWindow.InvokeMethod("MakeParentsSettingsMatchMe");

            var loadPosition = false;
            var displayImmediately = true;
            var setFocus = true;

            int showMode = FullscreenShowMode.GetInitialShowMode();
            if (cw.HasMethod("Show", new[] { typeof(int), typeof(bool), typeof(bool), typeof(bool), typeof(int) }))
                cw.InvokeMethod("Show", showMode, loadPosition, displayImmediately, setFocus, 0);
            else if (cw.HasMethod("Show", new[] { typeof(int), typeof(bool), typeof(bool) }))
                cw.InvokeMethod("Show", showMode, loadPosition, displayImmediately);
            else
                cw.InvokeMethod("Show", showMode, loadPosition, displayImmediately, setFocus);

            cw.InvokeMethod("SetMinMaxSizes", rect.size, rect.size);
            cw.SetFieldValue("m_ShowMode", (int)ShowMode.PopupMenu);
            cw.SetFieldValue("m_DontSaveToLayout", true);

            // macOS + PopupMenu: Show() がウィンドウ位置を調整する場合があるため再設定
            cw.SetPropertyValue("position", rect);

            return new ViewPyramid { Window = childWindow, View = hv, Container = cw };
        }

        protected void SetFreezeContainer(ContainerWindow containerWindow, bool freeze)
        {
            if (containerWindow != null && containerWindow.HasMethod("SetFreezeDisplay", new[] { typeof(bool) }))
                containerWindow.InvokeMethod("SetFreezeDisplay", freeze);
        }

        protected virtual void BeforeOpening()
        {
            if (m_dst.Container != null)
                throw new InvalidOperationException("Container already has a fullscreened view");
        }

        protected virtual void AfterOpening()
        {
            var targetRect = Rect;
            After.Frames(2, () =>
            {
                // macOS: ウィンドウサーバが非同期で位置を変えてしまう場合への対策
                if (m_dst.Container != null)
                    Rect = targetRect;
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            });
        }
    }
}
