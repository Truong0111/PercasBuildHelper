using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace PercasHelper.Editor
{
    public class PercasBuilder : EditorWindow
    {
        #region Constants

        private const string BYPASS_PASSWORD = "percasPass";
        private const string BUILDS_FOLDER = "/../Builds";
        private const string APK_EXTENSION = "*.apk";
        private const int BUTTON_HEIGHT = 25;
        private const int SPACING = 5;

        #endregion

        #region Enums

        private enum BuildType
        {
            Mono2X,
            Mono2XCleanBuild,
            Final
        }

        #endregion

        #region Fields

        private bool isVersionTypeFileName;
        private bool isCustomBuildFileName;
        private string buildFileName = string.Empty;

        private bool packageName;
        private bool splash;
        private bool icon;
        private string bypassChecklistPassword = string.Empty;

        private bool testMono2X = true;
        private bool testMono2XCleanBuild;
        private bool final;

        private string selectedApkPath = string.Empty;
        private int selectedAPKIndex;

        private string[] availableApkFiles = Array.Empty<string>();

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            RefreshApkList();
        }

        public void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.Space(SPACING);
                DrawBuildFileNameSection();
                DrawChecklistSection();
                DrawBuildActionsSection();
                DrawUtilitiesSection();
                DrawApkInstallationSection();
            }
        }

        #endregion

        #region GUI Sections

        private void DrawBuildFileNameSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // isVersionTypeFileName = EditorGUILayout.Toggle("Version build file name", isVersionTypeFileName);
                isCustomBuildFileName = EditorGUILayout.Toggle("Use custom name", isCustomBuildFileName);

                if (isCustomBuildFileName)
                {
                    buildFileName = EditorGUILayout.TextField("Build file name", buildFileName);
                }
                else
                {
                    buildFileName = string.Empty;
                }
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
                {
                    ValidateChecklist();
                }
            }
        }

        private void DrawBuildActionsSection()
        {
            EditorGUILayout.Space(10);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Build Actions", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);

                HandleBuildTypeToggle("Test Mono2x", BuildType.Mono2X, ref testMono2X);
                HandleBuildTypeToggle("Clean Build", BuildType.Mono2XCleanBuild, ref testMono2XCleanBuild);

                EditorGUILayout.Space(SPACING);

                bool canBuildFinal = IsChecklistValid() || IsBypassPasswordValid();
                using (new EditorGUI.DisabledScope(!canBuildFinal))
                {
                    HandleBuildTypeToggle("Final Build", BuildType.Final, ref final);
                }

                EditorGUILayout.Space(SPACING);
                if (GUILayout.Button("Build", GUILayout.Height(BUTTON_HEIGHT)))
                {
                    ExecuteBuild();
                }
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
                    if (GUILayout.Button("Clean Build Folder", GUILayout.Height(BUTTON_HEIGHT)))
                        CleanBuildFolder();
                }
            }
        }

        private void DrawApkInstallationSection()
        {
            EditorGUILayout.Space(10);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("APK Installation", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);

                if (availableApkFiles.Length == 0)
                {
                    EditorGUILayout.LabelField("No APK files found in Builds folder", EditorStyles.helpBox);
                    if (GUILayout.Button("Refresh APK List", GUILayout.Height(BUTTON_HEIGHT)))
                    {
                        RefreshApkList();
                    }
                }
                else
                {
                    selectedAPKIndex = Mathf.Clamp(selectedAPKIndex, 0, availableApkFiles.Length - 1);

                    EditorGUI.BeginChangeCheck();
                    selectedAPKIndex = EditorGUILayout.Popup("APK File", selectedAPKIndex, availableApkFiles);
                    if (EditorGUI.EndChangeCheck())
                    {
                        selectedApkPath = availableApkFiles[selectedAPKIndex];
                    }

                    if (GUILayout.Button("Install APK", GUILayout.Height(BUTTON_HEIGHT)))
                    {
                        InstallSelectedApk();
                    }
                }

                if (GUILayout.Button("Open Debugging Port To Device", GUILayout.Height(BUTTON_HEIGHT)))
                {
                    OpenDebuggingPortToDevice();
                }
            }
        }

        #endregion

        #region Build Management

        private void HandleBuildTypeToggle(string label, BuildType buildType, ref bool toggleValue)
        {
            bool previousValue = toggleValue;
            toggleValue = EditorGUILayout.Toggle(label, toggleValue);

            if (toggleValue && toggleValue != previousValue)
            {
                SetBuildType(buildType);
            }
        }

        private void SetBuildType(BuildType type)
        {
            testMono2X = type == BuildType.Mono2X;
            testMono2XCleanBuild = type == BuildType.Mono2XCleanBuild;
            final = type == BuildType.Final;
        }

        private void ValidateChecklist()
        {
            var config = PercasConfigSO.LoadInstance();
            if (config == null)
            {
                UnityEngine.Debug.LogError("PercasConfigSO instance not found!");
                return;
            }

            var validator = new Validator(config);
            packageName = validator.CheckPackageName();
            icon = validator.CheckIcon();
            splash = validator.CheckSplash();

            if (!IsChecklistValid() && !IsBypassPasswordValid())
            {
                SetBuildType(BuildType.Mono2X);
                UnityEngine.Debug.LogWarning("Checklist validation failed. Defaulting to Mono2X build.");
            }
            else
            {
                UnityEngine.Debug.Log("Checklist validation passed!");
            }
        }

        private bool IsChecklistValid() => packageName && splash && icon;
        private bool IsBypassPasswordValid() => bypassChecklistPassword == BYPASS_PASSWORD;

        private void ExecuteBuild()
        {
            try
            {
                var buildOptions = testMono2XCleanBuild ? BuildOptions.CleanBuildCache : BuildOptions.None;
                ConfigBuild.BuildGame(final, buildOptions, isCustomBuildFileName, buildFileName);

                // Refresh APK list after build
                EditorApplication.delayCall += RefreshApkList;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Build failed: {ex.Message}");
            }
        }

        #endregion

        #region Utility Methods

        private void OpenBuildFolder() => ConfigBuild.OpenFileBuild();

        private void CleanBuildFolder()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Clean Build Folder",
                "Are you sure you want to delete all contents in the Build Folder?",
                "Delete All",
                "Cancel"
            );

            if (confirmed)
            {
                ConfigBuild.CleanFolderBuild();
                RefreshApkList();
            }
        }

        #endregion

        #region APK Management

        private void RefreshApkList()
        {
            availableApkFiles = GetAvailableApkFiles().ToArray();

            if (availableApkFiles.Length > 0)
            {
                selectedAPKIndex = 0;
                selectedApkPath = availableApkFiles[0];
            }
            else
            {
                selectedAPKIndex = 0;
                selectedApkPath = string.Empty;
            }
        }

        private IEnumerable<string> GetAvailableApkFiles()
        {
            string buildsPath = Application.dataPath + BUILDS_FOLDER;

            if (!Directory.Exists(buildsPath))
                return Enumerable.Empty<string>();

            try
            {
                return Directory.GetFiles(buildsPath, APK_EXTENSION, SearchOption.AllDirectories)
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .OrderByDescending(name => name);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to get APK files: {ex.Message}");
                return Enumerable.Empty<string>();
            }
        }

        private void InstallSelectedApk()
        {
            if (string.IsNullOrEmpty(selectedApkPath))
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
                    Directory.GetFiles(buildsPath, selectedApkPath + ".apk", SearchOption.AllDirectories);

                if (apkFiles.Length == 0)
                {
                    UnityEngine.Debug.LogError($"APK file not found: {selectedApkPath}.apk");
                    return;
                }

                string apkPath = $"\"{apkFiles[0]}\"";

                EditorUtility.DisplayProgressBar($"Installing {selectedApkPath}...", "Please wait", 0.5f);

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