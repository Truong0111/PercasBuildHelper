using System;
using System.IO;
using System.Text.RegularExpressions;
using DG.DemiEditor;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Percas.Editor
{
    public class PercasConfigSO : ScriptableObject
    {
        [field: SerializeField] public string ProductName { get; set; }
        [field: SerializeField] public string PackageName { get; set; }
        [field: SerializeField] public string AliasName { get; set; }
        [field: SerializeField] public bool UsePackageNameForPass { get; set; } = true;
        [field: SerializeField] public string KeyStorePass { get; set; }
        [field: SerializeField, Min(0)] public int MajorVersion { get; set; } = 0;
        [field: SerializeField, Min(0)] public int Version { get; set; } = 1;

        [field: Tooltip(
            "Version Name is formatted as {Major.Minor.Patch}.\n" +
            "For more information, visit https://docs.unity3d.com/Manual/upm-semver.html")]
        [field: SerializeField, ReadOnly]
        public string VersionName { get; set; } = "0.0.1";

        [field: Tooltip(
            "Version Code is formatted based on version name.\n" +
            "For examples:\n" +
            "\"2.3.11\" becomes version code 20311\n" +
            "\"3.0.0\" becomes version code 30000")]
        [field: SerializeField, ReadOnly]
        public int VersionCode { get; set; } = 1;

        [field: SerializeField] public Texture2D IconTexture { get; set; }
        
        private const string PercasConfigFileName = "PercasConfig";
        private const string PercasConfigResDir = "Assets/Percas";
        private const string PercasConfigFileExtension = ".asset";

        private const string PercasConfigFilePath =
            PercasConfigResDir + "/" + PercasConfigFileName + PercasConfigFileExtension;

        public static PercasConfigSO LoadInstance()
        {
            var instance = AssetDatabase.LoadAssetAtPath<PercasConfigSO>(PercasConfigFilePath);

            if (instance == null)
            {
                Debug.LogWarning("PercasConfigSO not found. Creating a new one.");
                Directory.CreateDirectory(PercasConfigResDir);
                instance = CreateInstance<PercasConfigSO>();
                AssetDatabase.CreateAsset(instance, PercasConfigFilePath);
                AssetDatabase.SaveAssets();
            }

            return instance;
        }

        private void Reset()
        {
            IconTexture = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown)[0];
        }

        private void OnEnable()
        {
            ProductName = PlayerSettings.productName;
            if (ProductName.IsNullOrEmpty()) ProductName = Constants.DefaultProductName;
#if UNITY_IOS || UNITY_ANDROID
            PackageName = PlayerSettings.applicationIdentifier;
            if (PackageName.IsNullOrEmpty()) PackageName = Constants.DefaultPackageName;
#endif
#if UNITY_ANDROID
            PlayerSettings.Android.useCustomKeystore = true;

            AliasName = PlayerSettings.Android.keyaliasName;
            if (AliasName.IsNullOrEmpty()) AliasName = Constants.DefaultAlias;
#endif
        }

        public void Apply()
        {
            AssetDatabase.SaveAssets();
            SetValueToPlayerSetting();
        }

        public string GetKeyStore()
        {
            return UsePackageNameForPass ? PackageName : KeyStorePass;
        }

        private void SetValueToPlayerSetting()
        {
            if (IconTexture != null)
            {
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[]
                {
                    IconTexture
                });
            }

            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageName);
        }

        public void IncreaseVersion()
        {
            if (!IsValidVersionCode() || !IsValidVersionName())
            {
                throw new InvalidOperationException($"Invalid version info: {VersionName} ({VersionCode})");
            }

            Version++;
            UpdateVersionCodeAndNameBasedOnVersion();
        }

        public void DecreaseVersion()
        {
            if (!IsValidVersionCode() || !IsValidVersionName())
            {
                throw new InvalidOperationException(
                    $"Version code or version name is invalid. \nversionName: {VersionName} \nversionCode: {VersionCode} ");
            }

            Version--;
            UpdateVersionCodeAndNameBasedOnVersion();
        }

        public void ResetVersion()
        {
            MajorVersion = 0;
            Version = 1;
            UpdateVersionCodeAndNameBasedOnVersion();
        }

        private void UpdateVersionCodeAndNameBasedOnVersion()
        {
            Version = Mathf.Max(0, Version);
            if (MajorVersion == 0 && Version == 0)
            {
                Version = 1;
            }

            VersionName = $"{MajorVersion}.0.{Version}";
            VersionCode = MajorVersion + Version;

            ConfigBuild.FixSettingBuild();
        }

        private bool IsValidVersionName()
        {
            return Regex.IsMatch(VersionName, @"\d+\.\d+\.\d+");
        }

        private bool IsValidVersionCode() => true;

        public override string ToString()
        {
            return
                $"{base.ToString()}, {nameof(ProductName)}: {ProductName}, {nameof(PackageName)}: {PackageName}, {nameof(MajorVersion)}: {MajorVersion}, {nameof(Version)}: {Version}, {nameof(VersionName)}: {VersionName}, {nameof(VersionCode)}: {VersionCode}, {nameof(IconTexture)}: {IconTexture}";
        }
    }
}