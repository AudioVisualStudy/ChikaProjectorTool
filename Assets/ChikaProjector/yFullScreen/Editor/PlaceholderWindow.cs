using UnityEditor;
using UnityEngine;

namespace yugop.fullscreen
{
    /// <summary>Placeholder shown in the dock while Game View is fullscreen.</summary>
    public class PlaceholderWindow : EditorWindow
    {
        [SerializeField] private int m_containerInstanceID;

        private FullscreenContainer FullscreenContainer
        {
            get
            {
                if (m_containerInstanceID == 0) return null;
#pragma warning disable 618
                var obj = EditorUtility.InstanceIDToObject(m_containerInstanceID);
#pragma warning restore 618
                return obj as FullscreenContainer;
            }
            set => m_containerInstanceID = value != null ? value.GetInstanceID() : 0;
        }

        internal void SetContainer(FullscreenContainer container)
        {
            FullscreenContainer = container;
        }

        private void OnGUI()
        {
            GUILayout.Space(20);
            GUILayout.Label("Game View is in fullscreen.");
            GUILayout.Label("Exit: Window > Fullscreen-GameView");
            GUILayout.Space(10);
            if (GUILayout.Button("Restore View", GUILayout.Height(24)) && FullscreenContainer != null)
                FullscreenContainer.Close();
        }
    }
}
