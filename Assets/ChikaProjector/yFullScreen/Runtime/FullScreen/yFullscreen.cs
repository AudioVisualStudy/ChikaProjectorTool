using System;
using System.Reflection;
using UnityEngine;

namespace yugop.fullscreen
{
    /// <summary>再生中に指定キー（または F12）で Game View フルスクリーンをトグルするコンポーネント。エディタ再生時のみ有効で、ビルド版（プロジェクタ等）では動作しません。レガシー／新 Input System どちらにも対応（追加パッケージ不要）。</summary>
    public class FullScreenToggle : MonoBehaviour
    {
        [SerializeField] private bool fullScreenOnPlay;
        [SerializeField] private KeyCode fullScreenKey = KeyCode.F12;

        /// <summary>FullScreen on Play が有効か。Editor から参照する。</summary>
        public bool FullScreenOnPlay => fullScreenOnPlay;

        /// <summary>再生中にフルスクリーン切り替えを要求したときに発火する。Editor 側のブリッジが購読する。</summary>
        public static event Action RequestToggleFullScreen;

        private static bool? _useNewInputSystem;
        private static bool _wasPlaying;
        private static int _lastToggleFrame = -1;
        private static float _lastToggleTime;

        private const float CooldownSeconds = 0.2f;

        /// <summary>プレイモード遷移などで静的な入力判定・クールダウン状態を破棄する。エディタのブリッジから呼ぶ。</summary>
        public static void ResetStaticInputState()
        {
            _useNewInputSystem = null;
            _wasPlaying = false;
            _lastToggleFrame = -1;
            _lastToggleTime = 0f;
        }

        private void OnEnable()
        {
            if (!Application.isEditor)
                return;
            if (Application.isPlaying)
                _useNewInputSystem = null;
        }

        private void Update()
        {
            // ビルド版（プロジェクタ再生含む）では何もしない。エディタの Play モードでのみ有効。
            if (!Application.isEditor)
                return;

            if (!Application.isPlaying)
            {
                if (_wasPlaying)
                    ResetStaticInputState();
                return;
            }
            _wasPlaying = true;

            bool triggered = IsToggleKeyPressed();
            if (!triggered) return;

            int frame = Time.frameCount;
            float elapsed = Time.realtimeSinceStartup - _lastToggleTime;

            if (_lastToggleFrame == frame)
                return;
            if (_lastToggleFrame >= 0 && elapsed < CooldownSeconds)
                return;

            _lastToggleFrame = frame;
            _lastToggleTime = Time.realtimeSinceStartup;
            RequestToggleFullScreen?.Invoke();
        }

        private bool IsToggleKeyPressed()
        {
            var key = fullScreenKey == KeyCode.None ? KeyCode.F12 : fullScreenKey;

            if (_useNewInputSystem == null)
            {
                try
                {
                    bool down = Input.GetKeyDown(key);
                    _useNewInputSystem = false;
                    return down;
                }
                catch (InvalidOperationException)
                {
                    _useNewInputSystem = true;
                }
            }

            if (_useNewInputSystem == true)
                return WasKeyPressedThisFrameViaReflection(key);

            try
            {
                return Input.GetKeyDown(key);
            }
            catch (InvalidOperationException)
            {
                _useNewInputSystem = true;
                return WasKeyPressedThisFrameViaReflection(key);
            }
        }

        /// <summary>新 Input System をリフレクションで参照し、キー押下を取得。パッケージ参照なしで新 Input のみのプロジェクトに対応。</summary>
        private static bool WasKeyPressedThisFrameViaReflection(KeyCode keyCode)
        {
            try
            {
                var keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
                if (keyboardType == null) return false;

                var currentProp = keyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                if (currentProp == null) return false;

                var keyboard = currentProp.GetValue(null);
                if (keyboard == null) return false;

                var keyEnumType = Type.GetType("UnityEngine.InputSystem.Key, Unity.InputSystem");
                if (keyEnumType == null) return false;

                var keyName = KeyCodeToInputSystemKeyName(keyCode);
                var keyValue = Enum.Parse(keyEnumType, keyName, true);
                if (keyValue == null) return false;

                var indexer = keyboardType.GetMethod("get_Item", new[] { keyEnumType });
                if (indexer == null) return false;

                var keyControl = indexer.Invoke(keyboard, new[] { keyValue });
                if (keyControl == null) return false;

                var wasPressedProp = keyControl.GetType().GetProperty("wasPressedThisFrame", BindingFlags.Public | BindingFlags.Instance);
                if (wasPressedProp == null) return false;

                return (bool)wasPressedProp.GetValue(keyControl);
            }
            catch
            {
                return false;
            }
        }

        private static string KeyCodeToInputSystemKeyName(KeyCode k)
        {
            var name = k.ToString();
            if (name.StartsWith("Alpha"))
                name = "Digit" + name.Substring(5);
            else if (name.StartsWith("Keypad"))
                name = "Numpad" + name.Substring(6);
            return name;
        }
    }
}
