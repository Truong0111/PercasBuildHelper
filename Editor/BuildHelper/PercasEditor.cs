﻿using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using PercasHelper.Runtime;

namespace PercasHelper.Editor
{
    public class PercasEditor : EditorWindow
    {
        private enum EditorTab
        {
            Settings,
            Build,
            Utility
        }

        private EditorTab currentTab = EditorTab.Settings;
        private PercasConfigSO buildSettings;
        private PercasUtilitySO utilitySettings;
        private PercasBuilder percasBuilder;
        private Vector2 scrollPosition;
        private bool isRepainting = false;

        [MenuItem("Percas/Build Helper", priority = 0)]
        private static void ShowWindow()
        {
            var window = GetWindow<PercasEditor>();
            window.minSize = new Vector2(400, 500);
            window.titleContent = new GUIContent("Percas Build Helper");
            window.Show();
        }

        [MenuItem("Percas/Open build folder", priority = 1)]
        public static void OpenFileBuild()
        {
            ConfigBuild.OpenFileBuild();
        }

        [MenuItem("Percas/Build Game &_b", priority = 2)]
        public static void BuildGame()
        {
            ConfigBuild.BuildGame(false);
        }

        private void OnEnable()
        {
            buildSettings = PercasConfigSO.LoadInstance();
            utilitySettings = PercasUtilitySO.LoadInstance();
            percasBuilder = CreateInstance<PercasBuilder>();

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            StopContinuousRepaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                StartContinuousRepaint();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                StopContinuousRepaint();
            }
        }

        private void StartContinuousRepaint()
        {
            if (!isRepainting)
            {
                isRepainting = true;
                EditorApplication.update += OnEditorUpdate;
            }
        }

        private void StopContinuousRepaint()
        {
            if (isRepainting)
            {
                isRepainting = false;
                EditorApplication.update -= OnEditorUpdate;
            }
        }

        private void OnEditorUpdate()
        {
            if (isRepainting && utilitySettings.ShowPerformanceStats)
            {
                Repaint();
            }
        }

        private void OnUndoRedoPerformed()
        {
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            currentTab = (EditorTab)GUILayout.Toolbar(
                (int)currentTab,
                new[] { "Settings", "Build", "Utility" },
                EditorStyles.toolbarButton
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case EditorTab.Settings:
                    RenderSettingsTab();
                    break;
                case EditorTab.Build:
                    RenderBuildTab();
                    break;
                case EditorTab.Utility:
                    RenderUtilityTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void RenderSettingsTab()
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Product Information", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            Undo.RecordObject(buildSettings, "Change Product Name");
            buildSettings.ProductName = EditorGUILayout.TextField("Product Name", buildSettings.ProductName);
            EditorUtility.SetDirty(buildSettings);

            Undo.RecordObject(buildSettings, "Change Package Name");
            buildSettings.PackageName = EditorGUILayout.TextField("Package Name", buildSettings.PackageName);
            EditorUtility.SetDirty(buildSettings);

            Undo.RecordObject(buildSettings, "Change Alias Name");
            buildSettings.AliasName = EditorGUILayout.TextField("Alias Name", buildSettings.AliasName);
            EditorUtility.SetDirty(buildSettings);

            Undo.RecordObject(buildSettings, "Change Package Name Pass");
            buildSettings.UsePackageNameForPass =
                EditorGUILayout.Toggle("PackageName As Pass", buildSettings.UsePackageNameForPass);
            EditorUtility.SetDirty(buildSettings);

            EditorGUI.BeginDisabledGroup(buildSettings.UsePackageNameForPass);
            Undo.RecordObject(buildSettings, "Change Key Store Pass");
            buildSettings.KeyStorePass = EditorGUILayout.PasswordField("Key Store Pass", buildSettings.KeyStorePass);
            EditorUtility.SetDirty(buildSettings);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);
            Undo.RecordObject(buildSettings, "Change Custom Keystore");
            buildSettings.UseCustomKeystore =
                EditorGUILayout.Toggle("Use Custom Keystore", buildSettings.UseCustomKeystore);
            EditorUtility.SetDirty(buildSettings);

            if (buildSettings.UseCustomKeystore)
            {
                EditorGUI.BeginChangeCheck();
                string newPath = buildSettings.CustomKeystorePath;
                using (new EditorGUILayout.HorizontalScope())
                {
                    newPath = EditorGUILayout.TextField("Keystore Path", newPath);
                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        string initialDir = "Assets";
                        if (!string.IsNullOrEmpty(newPath))
                        {
                            if (newPath.StartsWith("Assets/"))
                            {
                                string fullPath = Path.Combine(Application.dataPath, newPath.Substring(7));
                                if (Directory.Exists(Path.GetDirectoryName(fullPath)))
                                {
                                    initialDir = Path.GetDirectoryName(fullPath);
                                }
                            }
                            else if (File.Exists(newPath) || Directory.Exists(Path.GetDirectoryName(newPath)))
                            {
                                initialDir = Path.GetDirectoryName(newPath);
                            }
                        }

                        string path = EditorUtility.OpenFilePanel("Select Keystore File", initialDir, "keystore");
                        if (!string.IsNullOrEmpty(path))
                        {
                            if (path.StartsWith(Application.dataPath))
                            {
                                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                                newPath = relativePath;
                            }
                            else
                            {
                                newPath = path;
                            }
                        }
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    if (string.IsNullOrEmpty(newPath))
                    {
                        EditorUtility.DisplayDialog("Invalid Path", "Keystore path cannot be empty", "OK");
                        return;
                    }

                    if (!newPath.StartsWith("Assets/") && !File.Exists(newPath))
                    {
                        EditorUtility.DisplayDialog("Invalid Path", "Keystore file not found at specified path", "OK");
                        return;
                    }

                    Undo.RecordObject(buildSettings, "Change Keystore Path");
                    buildSettings.CustomKeystorePath = newPath;
                    EditorUtility.SetDirty(buildSettings);
                }

                if (!string.IsNullOrEmpty(buildSettings.CustomKeystorePath) &&
                    !buildSettings.CustomKeystorePath.StartsWith("Assets/"))
                {
                    EditorGUILayout.HelpBox(
                        "Using an external keystore file. This might cause issues when sharing the project or building on other machines.",
                        MessageType.Warning);
                }
            }

            EditorGUILayout.Space(5);
            Undo.RecordObject(buildSettings, "Change Split Application Binary");
            buildSettings.SplitApplicationBinary =
                EditorGUILayout.Toggle("Split Application Binary", buildSettings.SplitApplicationBinary);
            EditorUtility.SetDirty(buildSettings);

            if (buildSettings.SplitApplicationBinary)
            {
                EditorGUILayout.HelpBox(
                    "Split Application Binary will create separate APKs for different CPU architectures. This can reduce the final APK size but requires managing multiple APKs.",
                    MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Version Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(buildSettings, "Change Major Version");
            buildSettings.MajorVersion = EditorGUILayout.IntField("Major Version", buildSettings.MajorVersion);
            EditorUtility.SetDirty(buildSettings);

            Undo.RecordObject(buildSettings, "Change Version");
            buildSettings.Version = EditorGUILayout.IntField("Version", buildSettings.Version);
            EditorUtility.SetDirty(buildSettings);

            Undo.RecordObject(buildSettings, "Change Version Code Type");
            buildSettings.VersionCodeType =
                (VersionCodeType)EditorGUILayout.EnumPopup("Version Code Type", buildSettings.VersionCodeType);
            EditorUtility.SetDirty(buildSettings);

            if (EditorGUI.EndChangeCheck())
            {
                buildSettings.OnVersionChanged();
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Version Name", buildSettings.VersionName);
            EditorGUI.EndDisabledGroup();

            buildSettings.IsUseCustomVersionCode =
                EditorGUILayout.Toggle("Use Custom VersionCode", buildSettings.IsUseCustomVersionCode);

            if (buildSettings.IsUseCustomVersionCode)
            {
                buildSettings.VersionCode = EditorGUILayout.IntField("Version Code", buildSettings.VersionCode);
                EditorUtility.SetDirty(buildSettings);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Version Code", buildSettings.VersionCode.ToString());
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space(5);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Increment Version", GUILayout.Height(25)))
                {
                    Undo.RecordObject(buildSettings, "Increment Version");
                    buildSettings.IncreaseVersion();
                }

                if (GUILayout.Button("Decrement Version", GUILayout.Height(25)))
                {
                    Undo.RecordObject(buildSettings, "Decrement Version");
                    buildSettings.DecreaseVersion();
                }

                if (GUILayout.Button("Reset Version", GUILayout.Height(25)))
                {
                    Undo.RecordObject(buildSettings, "Reset Version");
                    buildSettings.ResetVersion();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Application Icon", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            Undo.RecordObject(buildSettings, "Change App Icon");
            buildSettings.IconTexture =
                (Texture2D)EditorGUILayout.ObjectField("App Icon", buildSettings.IconTexture, typeof(Texture2D),
                    false);
            EditorUtility.SetDirty(buildSettings);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Actions", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            if (GUILayout.Button("Apply", GUILayout.Height(25)))
            {
                buildSettings.Apply();
            }

            if (GUILayout.Button("Open Player Settings", GUILayout.Height(25)))
            {
                OpenPlayerSettings();
            }

            EditorGUILayout.EndVertical();
        }

        private void RenderBuildTab()
        {
            if (percasBuilder != null)
            {
                percasBuilder.OnGUI();
            }
        }

        private void RenderUtilityTab()
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Time Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(utilitySettings, "Change Time Scale");
            float timeScale = EditorGUILayout.FloatField("Time Scale", utilitySettings.TimeScale);
            if (EditorGUI.EndChangeCheck())
            {
                utilitySettings.TimeScale = timeScale;
                EditorUtility.SetDirty(utilitySettings);
            }

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(utilitySettings, "Change Fixed Delta Time");
            float fixedDeltaTime = EditorGUILayout.FloatField("Fixed Delta Time", utilitySettings.FixedDeltaTime);
            if (EditorGUI.EndChangeCheck())
            {
                utilitySettings.FixedDeltaTime = fixedDeltaTime;
                EditorUtility.SetDirty(utilitySettings);
            }

            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(utilitySettings, "Change Target Frame Rate");
            int targetFrameRate = EditorGUILayout.IntField("Target Frame Rate", utilitySettings.TargetFrameRate);
            if (EditorGUI.EndChangeCheck())
            {
                utilitySettings.TargetFrameRate = targetFrameRate;
                PercasSettings.FrameRate.Set(utilitySettings.TargetFrameRate);
                EditorUtility.SetDirty(utilitySettings);
            }

            EditorGUILayout.HelpBox(
                "Note: This setting will be overridden if any script calls Application.targetFrameRate.",
                MessageType.Warning);

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Apply", GUILayout.Height(25)))
            {
                utilitySettings.Apply();
            }

            if (GUILayout.Button("Reset Time Settings", GUILayout.Height(25)))
            {
                utilitySettings.ResetTimeSettings();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Log Utility", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(utilitySettings, "Change Show Logs");
            bool showLogs = EditorGUILayout.Toggle("Show Logs", utilitySettings.ShowLogs);
            if (EditorGUI.EndChangeCheck())
            {
                utilitySettings.ShowLogs = showLogs;
                PercasSettings.Log.Set(utilitySettings.ShowLogs);
                EditorUtility.SetDirty(utilitySettings);
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Clear Console", GUILayout.Height(25)))
            {
                var assembly = Assembly.GetAssembly(typeof(SceneView));
                var logEntries = assembly.GetType("UnityEditor.LogEntries");
                var clearMethod = logEntries.GetMethod("Clear");
                clearMethod.Invoke(new object(), null);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Performance Monitor", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(utilitySettings, "Change Show Performance Stats");
            bool showStats = EditorGUILayout.Toggle("Show Performance Stats", utilitySettings.ShowPerformanceStats);
            if (EditorGUI.EndChangeCheck())
            {
                utilitySettings.ShowPerformanceStats = showStats;
                EditorUtility.SetDirty(utilitySettings);
            }

            if (utilitySettings.ShowPerformanceStats && Application.isPlaying)
            {
                utilitySettings.GetPerformanceStats(
                    out var fps,
                    out var batches,
                    out var drawCalls,
                    out var tris,
                    out var verts,
                    out var totalMemory,
                    out var usedMemory,
                    out var textureMemory,
                    out var meshMemory,
                    out var materialCount);

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"FPS: {fps:F1}");
                EditorGUILayout.LabelField($"Draw Calls: {drawCalls}");
                EditorGUILayout.LabelField($"Batches: {batches}");
                EditorGUILayout.LabelField($"Triangles: {tris:N0}");
                EditorGUILayout.LabelField($"Vertices: {verts:N0}");
                EditorGUILayout.LabelField($"Materials: {materialCount}");
                EditorGUILayout.LabelField($"Texture Memory: {textureMemory / 1024 / 1024} MB");
                EditorGUILayout.LabelField($"Mesh Memory: {meshMemory / 1024 / 1024} MB");
                EditorGUILayout.LabelField($"Used Memory: {usedMemory / 1024 / 1024} MB");
                EditorGUILayout.LabelField($"Total Memory: {totalMemory} MB");
            }
            else if (utilitySettings.ShowPerformanceStats)
            {
                EditorGUILayout.HelpBox("Performance stats are only available during play mode.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private static void OpenPlayerSettings()
        {
            EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
            var projectSettingsWindow = GetWindow(Type.GetType("UnityEditor.ProjectSettingsWindow,UnityEditor"));
            var projectSettingsMethod = projectSettingsWindow.GetType().GetMethod("SelectProviderByName",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (projectSettingsMethod != null)
            {
                projectSettingsMethod.Invoke(projectSettingsWindow, new object[] { "Project/Player" });
            }
        }

        private void OnDestroy()
        {
            if (percasBuilder)
            {
                DestroyImmediate(percasBuilder);
            }
        }
    }
}