﻿using System;
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
        private enum BuildType
        {
            Mono2X,
            Mono2XCleanBuild,
            Final
        }

        private bool isCustomBuildFileName = false;
        private string buildFileName = "";
        private bool packageName;
        private bool splash;
        private bool icon;
        private string bypassChecklistPassword;
        private bool testMono2X = true;
        private bool testMono2XCleanBuild;
        private bool testIllcpp;
        private bool final;
        private string apk;
        private int selectedAPKIndex = 0;
        private BuildType currentBuildType = BuildType.Mono2X;

        public void OnGUI()
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            isCustomBuildFileName = EditorGUILayout.Toggle("Use custom name", isCustomBuildFileName);
            if (isCustomBuildFileName)
            {
                buildFileName = EditorGUILayout.TextField("Build file name", buildFileName);
            }
            else
            {
                buildFileName = "";
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Final Build Checklist", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUI.BeginDisabledGroup(true);
            packageName = EditorGUILayout.Toggle("Package Name", packageName);
            splash = EditorGUILayout.Toggle("Splash", splash);
            icon = EditorGUILayout.Toggle("Icon", icon);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);
            bypassChecklistPassword = EditorGUILayout.TextField("Bypass Password", bypassChecklistPassword);
            if (GUILayout.Button("I want to build final", GUILayout.Height(25)))
            {
                TestChecklist();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Build Actions", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            bool previousTestMono2X = testMono2X;
            testMono2X = EditorGUILayout.Toggle("Test Mono2x", testMono2X);
            if (testMono2X && testMono2X != previousTestMono2X) SetBuildType(BuildType.Mono2X);

            bool previousCleanBuild = testMono2XCleanBuild;
            testMono2XCleanBuild = EditorGUILayout.Toggle("Clean Build", testMono2XCleanBuild);
            if (testMono2XCleanBuild && testMono2XCleanBuild != previousCleanBuild)
                SetBuildType(BuildType.Mono2XCleanBuild);

            EditorGUILayout.Space(5);
            EditorGUI.BeginDisabledGroup((!packageName || !splash || !icon) && bypassChecklistPassword != "percasPass");
            final = EditorGUILayout.Toggle("Final Build", final);
            if (final) SetBuildType(BuildType.Final);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Build", GUILayout.Height(25)))
            {
                Build();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Build Utilities", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fix Settings", GUILayout.Height(25)))
                    FixSettings();
                if (GUILayout.Button("Create Aab To Apk Command", GUILayout.Height(25)))
                    CreateAabToApkCommand();
            }

            EditorGUILayout.Space(5);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Build Folder", GUILayout.Height(25)))
                    OpenBuildFolder();
                if (GUILayout.Button("Clean Build Folder", GUILayout.Height(25)))
                    CleanBuildFolder();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("APK Installation", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            var files = GetFiles().ToArray();

            if (files.Length == 0)
            {
                EditorGUILayout.LabelField("No APK files found in Builds folder", EditorStyles.helpBox);
                selectedAPKIndex = 0;
            }
            else
            {
                selectedAPKIndex = Mathf.Clamp(selectedAPKIndex, 0, files.Length - 1);

                EditorGUI.BeginChangeCheck();
                selectedAPKIndex = EditorGUILayout.Popup("APK File", selectedAPKIndex, files);
                if (EditorGUI.EndChangeCheck())
                {
                    apk = files[selectedAPKIndex];
                }

                if (GUILayout.Button("Install APK", GUILayout.Height(25)))
                {
                    apk = files[selectedAPKIndex];
                    InstallAPK();
                }
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Open Debugging Port To Device", GUILayout.Height(25)))
            {
                OpenDebuggingPortToDevice();
            }

            EditorGUILayout.EndVertical();
        }

        private void TestChecklist()
        {
            var validator = new Validator(PercasConfigSO.LoadInstance());
            packageName = validator.CheckPackageName();
            icon = validator.CheckIcon();
            splash = validator.CheckSplash();

            if ((!packageName || !splash || !icon) && bypassChecklistPassword != "percasPass")
            {
                SetBuildType(BuildType.Mono2X);
            }
        }

        private void SetBuildType(BuildType type)
        {
            currentBuildType = type;
            testMono2X = type == BuildType.Mono2X;
            testMono2XCleanBuild = type == BuildType.Mono2XCleanBuild;
            final = type == BuildType.Final;
        }

        private void FixSettings()
        {
            ConfigBuild.FixSettingBuild();
        }

        private void CreateAabToApkCommand()
        {
            ConfigBuild.CreateTemplateCmdConvertFile();
        }

        private void OpenBuildFolder()
        {
            ConfigBuild.OpenFileBuild();
        }

        private void CleanBuildFolder()
        {
            var sure = EditorUtility.DisplayDialog("Clean Build Folder",
                "Are you sure you want to delete all contents in the Build Folder?", "Delete All", "Cancel");
            if (!sure) return;
            ConfigBuild.CleanFolderBuild();
        }

        private void Build()
        {
            ConfigBuild.BuildGame(final, testMono2XCleanBuild ? BuildOptions.CleanBuildCache : BuildOptions.None,
                isCustomBuildFileName, buildFileName);
        }

        protected void OnEnable()
        {
            var files = GetFiles().ToList();
            if (files.Count > 0)
            {
                selectedAPKIndex = 0;
                apk = files[0];
            }
            else
            {
                selectedAPKIndex = 0;
                apk = string.Empty;
            }
        }

        private IEnumerable<string> GetFiles()
        {
            if (!Directory.Exists(Application.dataPath + "/../Builds"))
                return Array.Empty<string>();

            return Directory.GetFiles(Application.dataPath + "/../Builds", "*.apk", SearchOption.AllDirectories)
                .Select(Path.GetFileNameWithoutExtension)
                .OrderByDescending(s => s);
        }

        private void InstallAPK()
        {
            var sdkRoot = AndroidExternalToolsSettings.sdkRootPath;
            string bundleToolPath = Path.Combine(sdkRoot, "platform-tools");

            var apkPath = "\"" + Directory.GetFiles(Application.dataPath + "/../Builds", apk + ".apk",
                SearchOption.AllDirectories)[0] + "\"";

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe", WorkingDirectory = bundleToolPath, Arguments = apkPath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.EnableRaisingEvents = true;
            process.StandardInput.WriteLine($"adb install -r {apkPath}");
            process.StandardInput.WriteLine("exit");


            EditorUtility.DisplayProgressBar($"Installing {apk}...", "Please wait", 0);
            try
            {
                string output = process.StandardOutput.ReadToEnd();
                output = output.Substring(output.IndexOf(".apk\"", StringComparison.CurrentCultureIgnoreCase) +
                                          ".apk\"".Length);
                output = output.Trim();

                Debug.Log(output);
                var error = process.StandardError.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Debug.LogError(error);
                }

                process.WaitForExit(5000);
                process.Close();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void OpenDebuggingPortToDevice()
        {
            string bundleToolPath = Path.Combine(EditorApplication.applicationPath,
                "../Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools");

            using var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe", WorkingDirectory = bundleToolPath ?? string.Empty,
                    RedirectStandardInput = true, RedirectStandardOutput = true,
                    RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
                }
            };

            process.Start();
            process.StandardInput.WriteLine($"adb forward tcp:34999 localabstract:Unity-{Application.identifier}");
            process.StandardInput.WriteLine("adb reverse tcp:34998 tcp:34999");
            process.StandardInput.Flush();
        }
    }
}