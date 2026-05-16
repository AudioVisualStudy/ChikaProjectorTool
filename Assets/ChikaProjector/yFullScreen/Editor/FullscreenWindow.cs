using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using View = UnityEngine.ScriptableObject;
using ContainerWindow = UnityEngine.ScriptableObject;

namespace yugop.fullscreen
{
    /// <summary>Fullscreen container for an EditorWindow (Game View).</summary>
    public class FullscreenWindow : FullscreenContainer
    {
        [SerializeField] private bool m_createdByFullscreenOnPlay;

        internal bool CreatedByFullscreenOnPlay
        {
            get => m_createdByFullscreenOnPlay;
            set => m_createdByFullscreenOnPlay = value;
        }

        private void SwapWindows(EditorWindow a, EditorWindow b)
        {
            var parentA = a.GetFieldValue<View>("m_Parent");
            var parentB = b.GetFieldValue<View>("m_Parent");
            var containerA = parentA.GetPropertyValue<ContainerWindow>("window");
            var containerB = parentB.GetPropertyValue<ContainerWindow>("window");
            var selectedPaneA = parentA.GetPropertyValue<EditorWindow>("actualView");
            var selectedPaneB = parentB.GetPropertyValue<EditorWindow>("actualView");

            SetFreezeContainer(containerA, true);
            SetFreezeContainer(containerB, true);

            parentA.SetPropertyValue("actualView", b);
            parentB.SetPropertyValue("actualView", a);
            ReplaceDockAreaPane(parentA, a, b);
            ReplaceDockAreaPane(parentB, b, a);

            a.InvokeMethod("MakeParentsSettingsMatchMe");
            b.InvokeMethod("MakeParentsSettingsMatchMe");

            if (selectedPaneA != a)
                parentA.SetPropertyValue("actualView", selectedPaneA);
            if (selectedPaneB != b)
                parentB.SetPropertyValue("actualView", selectedPaneB);

            SetFreezeContainer(containerA, false);
            SetFreezeContainer(containerB, false);
        }

        private static void ReplaceDockAreaPane(View dockArea, EditorWindow originalPane, EditorWindow newPane)
        {
            if (dockArea != null && dockArea.HasField("m_Panes"))
            {
                var dockedPanes = dockArea.GetFieldValue<List<EditorWindow>>("m_Panes");
                var dockIndex = dockedPanes.IndexOf(originalPane);
                if (dockIndex >= 0)
                    dockedPanes[dockIndex] = newPane;
            }
        }

        public override void Focus()
        {
            var window = ActualViewPyramid.Window;
            if (window != null)
                window.Focus();
            else
                base.Focus();
        }

        public override bool IsFocused() => EditorWindow.focusedWindow != null && EditorWindow.focusedWindow == ActualViewPyramid.Window;

        protected override void AfterOpening()
        {
            base.AfterOpening();
            HideToolbar();
        }

        /// <summary>Hide the Game View toolbar by setting HostView position offset (top = toolbar height).</summary>
        private void HideToolbar()
        {
            if (m_dst.View == null) return;
            var h = (int)FullScreenUtility.GetToolbarHeight();
            var offset = new RectOffset(0, 0, h, 0);
            var rect = offset.Add(new Rect(Vector2.zero, Rect.size));
            m_dst.View.InvokeMethod("SetPosition", rect);
        }

        internal void OpenWindow(Rect rect, Type type, EditorWindow window = null, bool disposableWindow = false)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (!type.IsOfType(typeof(EditorWindow)))
                throw new ArgumentException("Type must inherit from EditorWindow", nameof(type));
            if (window is PlaceholderWindow)
                return;
            if (window != null && FullScreen.GetFullscreenFromView(window) != null)
                return;
            if (window != null && window.HasProperty("isFullscreen") && window.GetPropertyValue<bool>("isFullscreen"))
            {
                window.ShowNotification(new GUIContent("Use built-in fullscreen exit (e.g. Alt+F4) first."));
                return;
            }

            BeforeOpening();

            if (window != null)
                m_src = new ViewPyramid(window);

            var childWindow = window != null
                ? (EditorWindow)CreateInstance<PlaceholderWindow>()
                : (EditorWindow)CreateInstance(type);

            if (childWindow is PlaceholderWindow ph)
                ph.SetContainer(this);

            m_dst = CreateFullscreenViewPyramid(rect, childWindow);

            if (window != null)
                SwapWindows(m_src.Window, m_dst.Window);

            Rect = rect;

            if (disposableWindow && childWindow is PlaceholderWindow)
            {
                childWindow.Close();
                m_dst.Window = m_src.Window;
            }

            AfterOpening();
        }

        private bool IsPlaceholderVisible()
        {
            if (!(m_dst.Window is PlaceholderWindow))
                return false;
            var pyramid = new ViewPyramid(m_dst.Window);
            if (pyramid.View == null || !pyramid.View.IsOfType(Types.HostView))
                return false;
            var actualView = pyramid.View.GetPropertyValue<View>("actualView");
            return actualView == m_dst.Window;
        }

        public override void Close()
        {
            var shouldRefocus = IsFocused() && IsPlaceholderVisible();
            if (m_src.Window != null && m_dst.Window != null)
                SwapWindows(m_src.Window, m_dst.Window);
            base.Close();
            if (shouldRefocus && m_src.Window != null)
                m_src.Window.Focus();
        }
    }
}
