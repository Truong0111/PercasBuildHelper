#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using PercasHelper.Runtime;

namespace PercasHelper.Runtime
{
    [InitializeOnLoad]
    public static class PercasHelperPackageInitializer
    {
        private const string AssetFolderPath = "Assets/Resources";
        private const string AssetPath = "Assets/Resources/PercasHelperRuntimeSettings.asset";

        static PercasHelperPackageInitializer()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(AssetPath))
                {
                    CreateRuntimeSettingsAsset();
                }
            };
        }

        private static void CreateRuntimeSettingsAsset()
        {
            if (!Directory.Exists(AssetFolderPath))
            {
                Directory.CreateDirectory(AssetFolderPath);
            }

            var settings = ScriptableObject.CreateInstance<PercasRuntimeSettingsSO>();
            settings.ShowLogs = true;
            settings.FrameRate = 60;

            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();

            Debug.Log("[PercasHelperPackage] Created default PercasHelperRuntimeSettings.asset in Resources/");
        }
    }
}

#endif