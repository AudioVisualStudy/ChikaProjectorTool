using System;
using UnityEditor;

namespace yugop.fullscreen
{
    /// <summary>Run a callback after a number of editor frames.</summary>
    public static class After
    {
        public static void Condition(Func<bool> condition, Action callback)
        {
            EditorApplication.CallbackFunction update = null;
            update = () =>
            {
                if (condition())
                {
                    EditorApplication.update -= update;
                    callback();
                }
            };
            EditorApplication.update += update;
        }

        public static void Frames(int frames, Action callback)
        {
            var f = 0;
            Condition(() => f++ >= frames, callback);
        }
    }
}
