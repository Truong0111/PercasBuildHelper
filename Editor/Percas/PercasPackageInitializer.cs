#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Percas.Helper
{
    [InitializeOnLoad]
    public static class PercasPackageInitializer
    {
        private const string AssetFolderPath = "Assets/Resources";
        private const string AssetPath = "Assets/Resources/PercasRuntimeSettings.asset";

        static PercasPackageInitializer()
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

            Debug.Log("[PercasPackage] Created default PercasRuntimeSettings.asset in Resources/");
        }
    }
}

#endif