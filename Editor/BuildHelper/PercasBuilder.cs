using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PercasHelper.Editor
{
    public class PercasBuilder : EditorWindow
    {
        private const string BYPASS_PASSWORD = "percasPass";
        private const string BUILDS_FOLDER = "/../Builds";
        private const string APK_EXTENSION = "*.apk";
        private const string IPA_EXTENSION = "*.ipa";
        private const int BUTTON_HEIGHT = 25;
        private const int SPACING = 5;

        private enum BuildType
        {
            Mono2X,
            Mono2XCleanBuild,
            Final
        }

        private bool isCustomBuildFileName;
        private string buildFileName = string.Empty;

        private bool packageName;
        private bool splash;
        private bool icon;
        private string bypassChecklistPassword = string.Empty;

        private bool testMono2X = true;
        private bool testMono2XCleanBuild;
        private bool final;

        private BuildTarget selectedBuildTarget = BuildTarget.Android;

        private string selectedBuildPath = string.Empty;
        private int selectedBuildIndex;
        private string[] availableBuildFiles = Array.Empty<string>();

        [MenuItem("Percas/Build Window")]
        public static void ShowWindow()
        {
            GetWindow<PercasBuilder>("Percas Builder");
        }

        private void OnEnable()
        {
            RefreshBuildFileList();
        }

        public void OnGUI()
        {
            EditorGUILayout.Space(SPACING);
            DrawBuildFileNameSection();
            DrawChecklistSection();
            DrawBuildActionsSection();
            DrawUtilitiesSection();
            DrawBuildInstallationSection();
        }

        private void DrawBuildFileNameSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                isCustomBuildFileName = EditorGUILayout.Toggle("Use custom name", isCustomBuildFileName);
                if (isCustomBuildFileName)
                    buildFileName = EditorGUILayout.TextField("Build file name", buildFileName);
                else
                    buildFileName = string.Empty;
            }
        }

        private void DrawChecklistSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Final Build Checklist", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);
                using (new EditorGUI.DisabledScope(true))
                {
                    packageName = EditorGUILayout.Toggle("Package Name", packageName);
                    splash = EditorGUILayout.Toggle("Splash", splash);
                    icon = EditorGUILayout.Toggle("Icon", icon);
                }

                EditorGUILayout.Space(SPACING);
                bypassChecklistPassword = EditorGUILayout.TextField("Bypass Password", bypassChecklistPassword);
                if (GUILayout.Button("Validate Final Build Requirements", GUILayout.Height(BUTTON_HEIGHT)))
                    ValidateChecklist();
            }
        }

        private void DrawBuildActionsSection()
        {
            EditorGUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Build Actions", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);

                EditorGUI.BeginDisabledGroup(true);
                var targetBuild =
#if UNITY_ANDROID
                    BuildTarget.Android;
#elif UNITY_IOS
                    BuildTarget.iOS;
#endif
                selectedBuildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Target Platform", targetBuild);
                EditorGUI.EndDisabledGroup();
#if UNITY_ANDROID
                HandleBuildTypeToggle("Test Mono2x", BuildType.Mono2X, ref testMono2X);
                HandleBuildTypeToggle("Clean Build", BuildType.Mono2XCleanBuild, ref testMono2XCleanBuild);
#endif

                EditorGUILayout.Space(SPACING);
                bool canBuildFinal = IsChecklistValid() || IsBypassPasswordValid();
                using (new EditorGUI.DisabledScope(!canBuildFinal))
                {
                    HandleBuildTypeToggle("Final Build", BuildType.Final, ref final);
                }

                EditorGUILayout.Space(SPACING);
                if (GUILayout.Button("Build", GUILayout.Height(BUTTON_HEIGHT)))
                    ExecuteBuild();
            }
        }

        private void DrawUtilitiesSection()
        {
            EditorGUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Build Utilities", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Open Build Folder", GUILayout.Height(BUTTON_HEIGHT))) 
                        OpenBuildFolder();
                }

                if (GUILayout.Button("Clean Build Folder", GUILayout.Height(BUTTON_HEIGHT)))
                    CleanBuildFolder();
            }
        }

        private void DrawBuildInstallationSection()
        {
            EditorGUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Build Installation", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);

                if (availableBuildFiles.Length == 0)
                {
                    EditorGUILayout.LabelField("No build files found in Builds folder", EditorStyles.helpBox);
                    if (GUILayout.Button("Refresh Build List", GUILayout.Height(BUTTON_HEIGHT)))
                        RefreshBuildFileList();
                }
                else
                {
                    selectedBuildIndex = Mathf.Clamp(selectedBuildIndex, 0, availableBuildFiles.Length - 1);
                    selectedBuildIndex = EditorGUILayout.Popup("Build File", selectedBuildIndex, availableBuildFiles);
                    selectedBuildPath = availableBuildFiles[selectedBuildIndex];

#if UNITY_ANDROID
                    if (GUILayout.Button("Install APK", GUILayout.Height(BUTTON_HEIGHT)))
                    {
                        InstallSelectedApk();
                    }

                    if (GUILayout.Button("Open Debugging Port To Device", GUILayout.Height(BUTTON_HEIGHT)))
                    {
                        OpenDebuggingPortToDevice();
                    }
#endif
                }
            }
        }

        private void HandleBuildTypeToggle(string label, BuildType buildType, ref bool toggleValue)
        {
            bool previousValue = toggleValue;
            toggleValue = EditorGUILayout.Toggle(label, toggleValue);
            if (toggleValue && toggleValue != previousValue) SetBuildType(buildType);
        }

        private void SetBuildType(BuildType type)
        {
            testMono2X = type == BuildType.Mono2X;
            testMono2XCleanBuild = type == BuildType.Mono2XCleanBuild;
            final = type == BuildType.Final;
        }

        private void ExecuteBuild()
        {
            try
            {
                var buildOptions = testMono2XCleanBuild ? BuildOptions.CleanBuildCache : BuildOptions.None;
                ConfigBuild.BuildGame(final, buildOptions, isCustomBuildFileName, buildFileName, selectedBuildTarget);
                EditorApplication.delayCall += RefreshBuildFileList;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Build failed: {ex.Message}");
            }
        }

        private void ValidateChecklist()
        {
            var config = PercasConfigSO.LoadInstance();
            if (config == null)
            {
                Debug.LogError("PercasConfigSO instance not found!");
                return;
            }

            var validator = new Validator(config);
            packageName = validator.CheckPackageName();
            icon = validator.CheckIcon();
            splash = validator.CheckSplash();

            if (!IsChecklistValid() && !IsBypassPasswordValid())
            {
                SetBuildType(BuildType.Mono2X);
                Debug.LogWarning("Checklist validation failed. Defaulting to Mono2X build.");
            }
            else
            {
                Debug.Log("Checklist validation passed!");
            }
        }

        private bool IsChecklistValid() => packageName && splash && icon;
        private bool IsBypassPasswordValid() => bypassChecklistPassword == BYPASS_PASSWORD;
        private void OpenBuildFolder() => ConfigBuild.OpenFileBuild();

        private void CleanBuildFolder()
        {
            if (EditorUtility.DisplayDialog("Clean Build Folder", "Delete all builds?", "Delete", "Cancel"))
            {
                ConfigBuild.CleanFolderBuild();
                RefreshBuildFileList();
            }
        }

        private void RefreshBuildFileList()
        {
            string extension = selectedBuildTarget == BuildTarget.Android ? APK_EXTENSION : IPA_EXTENSION;
            string path = Application.dataPath + BUILDS_FOLDER;

            if (!Directory.Exists(path))
            {
                availableBuildFiles = Array.Empty<string>();
                return;
            }

            try
            {
                availableBuildFiles = Directory.GetFiles(path, extension, SearchOption.AllDirectories)
                    .Select(Path.GetFileNameWithoutExtension)
                    .OrderByDescending(f => f)
                    .ToArray();

                selectedBuildIndex = 0;
                selectedBuildPath = availableBuildFiles.Length > 0 ? availableBuildFiles[0] : string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to list build files: {ex.Message}");
                availableBuildFiles = Array.Empty<string>();
            }
        }

        #region APK Management
        
        private void InstallSelectedApk()
        {
            if (string.IsNullOrEmpty(selectedBuildPath))
            {
                UnityEngine.Debug.LogError("No APK selected for installation.");
                return;
            }

            try
            {
                string sdkRoot = AndroidExternalToolsSettings.sdkRootPath;
                if (string.IsNullOrEmpty(sdkRoot))
                {
                    UnityEngine.Debug.LogError(
                        "Android SDK path not found. Please configure Android SDK in Unity preferences.");
                    return;
                }

                string adbPath = Path.Combine(sdkRoot, "platform-tools", "adb");

                string buildsPath = Application.dataPath + BUILDS_FOLDER;
                string[] apkFiles =
                    Directory.GetFiles(buildsPath, selectedBuildPath + ".apk", SearchOption.AllDirectories);

                if (apkFiles.Length == 0)
                {
                    UnityEngine.Debug.LogError($"APK file not found: {selectedBuildPath}.apk");
                    return;
                }

                string apkPath = $"\"{apkFiles[0]}\"";

                EditorUtility.DisplayProgressBar($"Installing {selectedBuildPath}...", "Please wait", 0.5f);

                using var process = CreateAdbProcess(adbPath, $"install -r {apkPath}");
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                LogProcessOutput(output, error);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"APK installation failed: {ex.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void OpenDebuggingPortToDevice()
        {
            try
            {
                string bundleToolPath = Path.Combine(
                    EditorApplication.applicationPath,
                    "../Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools"
                );

                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = bundleToolPath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();
                process.StandardInput.WriteLine($"adb forward tcp:34999 localabstract:Unity-{Application.identifier}");
                process.StandardInput.WriteLine("adb reverse tcp:34998 tcp:34999");
                process.StandardInput.Flush();
                process.StandardInput.Close();

                process.WaitForExit(5000);

                UnityEngine.Debug.Log("Debugging port opened successfully.");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to open debugging port: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private static Process CreateAdbProcess(string adbPath, string arguments)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
        }

        private static void LogProcessOutput(string output, string error)
        {
            if (!string.IsNullOrWhiteSpace(output))
                UnityEngine.Debug.Log($"[ADB OUTPUT] {output.Trim()}");

            if (!string.IsNullOrWhiteSpace(error))
                UnityEngine.Debug.LogError($"[ADB ERROR] {error.Trim()}");
        }

        #endregion
    }
}