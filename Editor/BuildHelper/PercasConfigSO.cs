using System;
using System.IO;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

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
        public string ProductName = Constants.DefaultProductName;
        public string PackageName = Constants.DefaultPackageName;
        public string AliasName = Constants.DefaultAlias;
        public bool UsePackageNameForPass = true;
        public string KeyStorePass;
        public bool UseCustomKeystore = true;
        public string CustomKeystorePath = Constants.Path.KeyStore;
        public bool SplitApplicationBinary = false;
        public VersionCodeType VersionCodeType = VersionCodeType.Increase;
        public int VersionMajor = 0;
        public int VersionMinor = 0;
        public int VersionPatch = 1;
        public string VersionName = "0.0.1";
        public int VersionCode;
        public Texture2D IconTexture;

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
            PlayerSettings.Android.bundleVersionCode = VersionPatch;
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
                throw new InvalidOperationException($"Invalid version info: {VersionName} ({VersionPatch})");
            }

            UpdateVersionCodeAndNameBasedOnVersion();
        }

        public void IncreaseVersion()
        {
            VersionPatch++;
            OnVersionChanged();
        }

        public void DecreaseVersion()
        {
            VersionPatch--;
            OnVersionChanged();
        }

        public void ResetVersion()
        {
            VersionMajor = 0;
            VersionMinor = 0;
            VersionPatch = 1;
            OnVersionChanged();
        }

        public void RefreshVersion()
        {
            OnVersionChanged();
        }

        private void UpdateVersionCodeAndNameBasedOnVersion()
        {
            VersionPatch = Mathf.Max(0, VersionPatch);

            if (VersionMajor == 0 && VersionPatch == 0)
            {
                VersionPatch = 1;
            }
            
            VersionName = $"{VersionMajor}.{VersionMinor}.{VersionPatch}";
            
            switch (VersionCodeType)
            {
                case VersionCodeType.Increase:
                    VersionCode = VersionMajor + VersionMinor + VersionPatch;
                    break;
                case VersionCodeType.DateTime:
                    VersionCode = int.Parse(DateTime.Now.ToString("ddMMyyyy"));
                    break;
                case VersionCodeType.Semantic:
                    VersionCode = VersionMajor * 10000 + VersionMinor * 100 + VersionPatch;
                    break;
            }
            
            ConfigBuild.FixSettingBuild();
        }

        private bool IsValidVersionName()
        {
            return Regex.IsMatch(VersionName, @"\d+\.\d+\.\d+");
        }

        private bool IsValidVersionCode() => true;
    }
}