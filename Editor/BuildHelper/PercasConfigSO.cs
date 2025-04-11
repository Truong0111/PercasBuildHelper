using System;
using System.IO;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace PercasHelper.Editor
{
    public enum VersionCodeType
    {
        Increase,
        DateTime,
        Semantic
    }

    public class PercasConfigSO : ScriptableObject
    {
        [field: SerializeField] public string ProductName { get; set; }
        [field: SerializeField] public string PackageName { get; set; }
        [field: SerializeField] public string AliasName { get; set; }
        [field: SerializeField] public bool UsePackageNameForPass { get; set; } = true;
        [field: SerializeField] public string KeyStorePass { get; set; }
        [field: SerializeField] public bool UseCustomKeystore { get; set; } = true;
        [field: SerializeField] public string CustomKeystorePath { get; set; } = Constants.Path.KeyStore;
        [field: SerializeField] public bool SplitApplicationBinary { get; set; } = false;
        [field: SerializeField] public VersionCodeType VersionCodeType { get; set; } = VersionCodeType.Increase;
        [field: SerializeField, Min(0)] public int MajorVersion { get; set; } = 0;
        [field: SerializeField, Min(0)] public int Version { get; set; } = 1;
        [field: SerializeField, ReadOnly] public string VersionName { get; set; } = "0.0.1";
        [field: SerializeField] public bool IsUseCustomVersionCode { get; set; } = false;
        [field: SerializeField] public int VersionCode { get; set; } = 1;
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
            if (string.IsNullOrEmpty(ProductName)) ProductName = Constants.DefaultProductName;
#if UNITY_IOS || UNITY_ANDROID
            PackageName = PlayerSettings.applicationIdentifier;
            if (string.IsNullOrEmpty(PackageName)) PackageName = Constants.DefaultPackageName;
#endif
#if UNITY_ANDROID
            PlayerSettings.Android.useCustomKeystore = true;

            AliasName = PlayerSettings.Android.keyaliasName;
            if (string.IsNullOrEmpty(AliasName)) AliasName = Constants.DefaultAlias;
#endif
        }

        public void Apply()
        {
            UpdateVersionCodeAndNameBasedOnVersion();
            SetValueToPlayerSetting();
            AssetDatabase.SaveAssets();
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
#if UNITY_ANDROID
            PlayerSettings.Android.bundleVersionCode = VersionCode;
            PlayerSettings.bundleVersion = VersionName;
            PlayerSettings.Android.useCustomKeystore = UseCustomKeystore;
            if (UseCustomKeystore)
            {
                PlayerSettings.Android.keystoreName = CustomKeystorePath;
            }

            PlayerSettings.Android.keyaliasName = AliasName;
            PlayerSettings.Android.keyaliasPass = GetKeyStore();
            PlayerSettings.Android.keystorePass = GetKeyStore();
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.useAPKExpansionFiles = SplitApplicationBinary;
#endif
        }

        public void OnVersionChanged()
        {
            if (!IsValidVersionCode() || !IsValidVersionName())
            {
                throw new InvalidOperationException($"Invalid version info: {VersionName} ({VersionCode})");
            }

            UpdateVersionCodeAndNameBasedOnVersion();
        }

        public void IncreaseVersion()
        {
            Version++;
            OnVersionChanged();
        }

        public void DecreaseVersion()
        {
            Version--;
            OnVersionChanged();
        }

        public void ResetVersion()
        {
            MajorVersion = 0;
            Version = 1;
            OnVersionChanged();
        }

        private void UpdateVersionCodeAndNameBasedOnVersion()
        {
            Version = Mathf.Max(0, Version);
            if (MajorVersion == 0 && Version == 0)
            {
                Version = 1;
            }

            VersionName = $"{MajorVersion}.0.{Version}";

            if (IsUseCustomVersionCode)
            {
            }
            else
            {
                switch (VersionCodeType)
                {
                    case VersionCodeType.Increase:
                        VersionCode = MajorVersion + Version;
                        break;
                    case VersionCodeType.DateTime:
                        VersionCode = int.Parse(DateTime.Now.ToString("ddMMyyyy"));
                        break;
                    case VersionCodeType.Semantic:
                        VersionCode = MajorVersion * 10000 + Version;
                        break;
                }
            }
            
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