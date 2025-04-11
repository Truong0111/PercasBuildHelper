using UnityEngine;

namespace PercasHelper.Runtime
{
    public class PercasUtility
    {
        public static bool ShowLogs
        {
            get => PercasSettings.Log.Get();
            set => PercasSettings.Log.Set(value);
        }

        public static int FrameRate
        {
            get => PercasSettings.FrameRate.Get();
            set => PercasSettings.FrameRate.Set(value);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InitBeforeScene()
        {
            if (!ShowLogs)
            {
                Application.logMessageReceived += SuppressLogs;
            }

            Application.targetFrameRate = FrameRate;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InitAfterScene()
        {
        }

        static void SuppressLogs(string condition, string stackTrace, LogType type)
        {
        }
    }

    public static class PercasSettings
    {
        private const string RuntimeResourcePath = "PercasHelperRuntimeSettings";

        private static PercasRuntimeSettingsSO _runtimeSettings;

        static PercasSettings()
        {
            _runtimeSettings = Resources.Load<PercasRuntimeSettingsSO>(RuntimeResourcePath);
        }

        public static class Log
        {
            private const string ShowLogsKey = "Percas_Show_Log_Key";

            public static bool Get()
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(ShowLogsKey, true);
#else
                return _runtimeSettings != null ? _runtimeSettings.ShowLogs : true;
#endif
            }

            public static void Set(bool value)
            {
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetBool(ShowLogsKey, value);
                if(_runtimeSettings != null) _runtimeSettings.ShowLogs = value;
#endif
            }
        }

        public static class FrameRate
        {
            private const string FrameRateKey = "Percas_Frame_Rate_Key";

            public static int Get()
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetInt(FrameRateKey, 60);
#else
                return _runtimeSettings != null ? _runtimeSettings.FrameRate : 60;
#endif
            }

            public static void Set(int value)
            {
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetInt(FrameRateKey, value);
                if(_runtimeSettings != null) _runtimeSettings.FrameRate = value;
#endif
            }
        }
    }
}