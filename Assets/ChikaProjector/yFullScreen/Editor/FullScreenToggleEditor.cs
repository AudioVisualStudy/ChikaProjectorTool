using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace yugop.fullscreen
{
    [CustomEditor(typeof(FullScreenToggle))]
    internal class FullScreenToggleEditor : Editor
    {
        private static int s_recordingInstanceId;
        private const string KeyFieldName = "yFullScreen_KeyField";

        private static readonly HashSet<KeyCode> ExcludedKeys = new HashSet<KeyCode>
        {
            KeyCode.None, KeyCode.Escape,
            KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftShift, KeyCode.RightShift,
            KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.LeftCommand, KeyCode.RightCommand,
            KeyCode.Tab, KeyCode.CapsLock, KeyCode.Backspace, KeyCode.Return, KeyCode.Space,
            KeyCode.Delete, KeyCode.Insert, KeyCode.Home, KeyCode.End,
            KeyCode.PageUp, KeyCode.PageDown, KeyCode.Pause, KeyCode.Print, KeyCode.Numlock, KeyCode.ScrollLock,
            KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("再生時に自動でフルスクリーン化する", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fullScreenOnPlay"), new GUIContent("FullScreen on Play"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("起動キーの指定：制御・修飾系のキーはNG（元のF12に設定されます）", EditorStyles.boldLabel);
            var keyProp = serializedObject.FindProperty("fullScreenKey");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("FullScreen Key");

            GUI.SetNextControlName(KeyFieldName);
            var currentKey = (KeyCode)keyProp.intValue;
            var label = currentKey == KeyCode.None ? "F12" : currentKey.ToString();
            var rect = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));

            var isRecording = s_recordingInstanceId == target.GetInstanceID();
            if (GUI.Button(rect, isRecording ? "Press any key..." : label, EditorStyles.miniButton))
            {
                s_recordingInstanceId = target.GetInstanceID();
                GUI.FocusControl(KeyFieldName);
            }

            if (isRecording && Event.current.type == EventType.KeyDown)
            {
                var key = Event.current.keyCode;
                if (key == KeyCode.Escape)
                {
                    keyProp.intValue = (int)KeyCode.None;
                }
                else if (key == KeyCode.None && Event.current.character >= '0' && Event.current.character <= '9')
                {
                    var c = Event.current.character;
                    key = c == '0' ? KeyCode.Alpha0 : KeyCode.Alpha1 + (c - '1');
                    keyProp.intValue = (int)key;
                }
                else if (ExcludedKeys.Contains(key))
                    keyProp.intValue = (int)KeyCode.None;
                else
                {
                    keyProp.intValue = (int)key;
                }
                s_recordingInstanceId = 0;
                GUI.FocusControl(null);
                Event.current.Use();
                serializedObject.ApplyModifiedProperties();
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
