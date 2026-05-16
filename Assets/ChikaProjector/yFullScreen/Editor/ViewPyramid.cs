using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using View = UnityEngine.ScriptableObject;
using ContainerWindow = UnityEngine.ScriptableObject;

namespace yugop.fullscreen
{
    /// <summary>Represents the pyramid: EditorWindow → HostView → ContainerWindow.</summary>
    [Serializable]
    public struct ViewPyramid
    {
        public EditorWindow Window
        {
            get
            {
#pragma warning disable 618
                if (!m_window && m_windowInstanceID != 0)
                    m_window = (EditorWindow)EditorUtility.InstanceIDToObject(m_windowInstanceID);
#pragma warning restore 618
                return m_window;
            }
            set
            {
                m_window = value;
                m_windowInstanceID = m_window ? m_window.GetInstanceID() : 0;
            }
        }

        public View View
        {
            get
            {
#pragma warning disable 618
                if (!m_view && m_viewInstanceID != 0)
                    m_view = (View)EditorUtility.InstanceIDToObject(m_viewInstanceID);
#pragma warning restore 618
                return m_view;
            }
            set
            {
                if (value != null)
                    value.EnsureOfType(Types.View);
                m_view = value;
                m_viewInstanceID = m_view ? m_view.GetInstanceID() : 0;
            }
        }

        public ContainerWindow Container
        {
            get
            {
#pragma warning disable 618
                if (!m_container && m_containerInstanceID != 0)
                    m_container = (ContainerWindow)EditorUtility.InstanceIDToObject(m_containerInstanceID);
#pragma warning restore 618
                return m_container;
            }
            set
            {
                if (value != null)
                    value.EnsureOfType(Types.ContainerWindow);
                m_container = value;
                m_containerInstanceID = m_container ? m_container.GetInstanceID() : 0;
            }
        }

        [SerializeField] private EditorWindow m_window;
        [SerializeField] private View m_view;
        [SerializeField] private ContainerWindow m_container;
        [SerializeField] private int m_windowInstanceID;
        [SerializeField] private int m_viewInstanceID;
        [SerializeField] private int m_containerInstanceID;

        public ViewPyramid(ScriptableObject viewOrWindow)
        {
            m_window = null;
            m_view = null;
            m_container = null;
            m_windowInstanceID = 0;
            m_viewInstanceID = 0;
            m_containerInstanceID = 0;

            if (!viewOrWindow)
                return;

            if (viewOrWindow.IsOfType(typeof(EditorWindow)))
            {
                m_window = viewOrWindow as EditorWindow;
                m_view = m_window.GetFieldValue<View>("m_Parent");
                m_container = m_view != null ? m_view.GetPropertyValue<ContainerWindow>("window") : null;
            }
            else if (viewOrWindow.IsOfType(Types.View))
            {
                m_view = viewOrWindow;
                m_container = m_view.GetPropertyValue<ContainerWindow>("window");
            }
            else if (viewOrWindow.IsOfType(Types.ContainerWindow))
            {
                m_container = viewOrWindow;
                m_view = m_container.GetPropertyValue<View>("rootView");
            }
            else
                throw new ArgumentException("Param must be of type EditorWindow, View or ContainerWindow", nameof(viewOrWindow));

            if (!m_window && m_view != null && m_view.IsOfType(Types.HostView))
                m_window = m_view.GetPropertyValue<EditorWindow>("actualView");

            m_windowInstanceID = m_window ? m_window.GetInstanceID() : 0;
            m_viewInstanceID = m_view ? m_view.GetInstanceID() : 0;
            m_containerInstanceID = m_container ? m_container.GetInstanceID() : 0;
        }
    }
}
