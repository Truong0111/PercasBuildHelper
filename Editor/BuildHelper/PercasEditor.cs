using UnityEngine;
using UnityEditor;
using System;
using Unity.VisualScripting;
using System.Reflection;

namespace Percas.Editor
{
    public class PercasEditor : EditorWindow
    {
        private enum EditorTab
        {
            Settings,
            Build,
            Utility
        }

        private EditorTab _currentTab = EditorTab.Settings;
        private PercasConfigSO _buildSettings;
        private PercasUtilitySO _utilitySettings;
        private PercasBuilder _percasBuilder;
        private Vector2 _scrollPosition;
        private bool _isRepainting = false;

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
            _buildSettings = PercasConfigSO.LoadInstance();
            _utilitySettings = PercasUtilitySO.LoadInstance();
            _percasBuilder = CreateInstance<PercasBuilder>();

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
            if (!_isRepainting)
            {
                _isRepainting = true;
                EditorApplication.update += OnEditorUpdate;
            }
        }

        private void StopContinuousRepaint()
        {
            if (_isRepainting)
            {
                _isRepainting = false;
                EditorApplication.update -= OnEditorUpdate;
            }
        }

        private void OnEditorUpdate()
        {
            if (_isRepainting && _utilitySettings.ShowPerformanceStats)
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
            _currentTab = (EditorTab)GUILayout.Toolbar(
                (int)_currentTab,
                new[] { "Settings", "Build", "Utility" },
                EditorStyles.toolbarButton
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_currentTab)
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

            Undo.RecordObject(_buildSettings, "Change Product Name");
            _buildSettings.ProductName = EditorGUILayout.TextField("Product Name", _buildSettings.ProductName);
            EditorUtility.SetDirty(_buildSettings);

            Undo.RecordObject(_buildSettings, "Change Package Name");
            _buildSettings.PackageName = EditorGUILayout.TextField("Package Name", _buildSettings.PackageName);
            EditorUtility.SetDirty(_buildSettings);

            Undo.RecordObject(_buildSettings, "Change Alias Name");
            _buildSettings.AliasName = EditorGUILayout.TextField("Alias Name", _buildSettings.AliasName);
            EditorUtility.SetDirty(_buildSettings);

            Undo.RecordObject(_buildSettings, "Change Package Name Pass");
            _buildSettings.UsePackageNameForPass =
                EditorGUILayout.Toggle("PackageName As Pass", _buildSettings.UsePackageNameForPass);
            EditorUtility.SetDirty(_buildSettings);

            EditorGUI.BeginDisabledGroup(_buildSettings.UsePackageNameForPass);
            Undo.RecordObject(_buildSettings, "Change Key Store Pass");
            _buildSettings.KeyStorePass = EditorGUILayout.PasswordField("Key Store Pass", _buildSettings.KeyStorePass);
            EditorUtility.SetDirty(_buildSettings);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);
            Undo.RecordObject(_buildSettings, "Change Custom Keystore");
            _buildSettings.UseCustomKeystore =
                EditorGUILayout.Toggle("Use Custom Keystore", _buildSettings.UseCustomKeystore);
            EditorUtility.SetDirty(_buildSettings);

            if (_buildSettings.UseCustomKeystore)
            {
                EditorGUI.BeginChangeCheck();
                string newPath = _buildSettings.CustomKeystorePath;
                using (new EditorGUILayout.HorizontalScope())
                {
                    newPath = EditorGUILayout.TextField("Keystore Path", newPath);
                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        string path = EditorUtility.OpenFilePanel("Select Keystore File", "Assets", "keystore");
                        if (!string.IsNullOrEmpty(path))
                        {
                            string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                            newPath = relativePath;
                        }
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    if (!newPath.StartsWith("Assets/"))
                    {
                        EditorUtility.DisplayDialog("Invalid Path", "Keystore path must start with 'Assets/'", "OK");
                        return;
                    }

                    Undo.RecordObject(_buildSettings, "Change Keystore Path");
                    _buildSettings.CustomKeystorePath = newPath;
                    EditorUtility.SetDirty(_buildSettings);
                }
            }

            EditorGUILayout.Space(5);
            Undo.RecordObject(_buildSettings, "Change Split Application Binary");
            _buildSettings.SplitApplicationBinary =
                EditorGUILayout.Toggle("Split Application Binary", _buildSettings.SplitApplicationBinary);
            EditorUtility.SetDirty(_buildSettings);

            if (_buildSettings.SplitApplicationBinary)
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
            Undo.RecordObject(_buildSettings, "Change Major Version");
            _buildSettings.MajorVersion = EditorGUILayout.IntField("Major Version", _buildSettings.MajorVersion);
            EditorUtility.SetDirty(_buildSettings);

            Undo.RecordObject(_buildSettings, "Change Version");
            _buildSettings.Version = EditorGUILayout.IntField("Version", _buildSettings.Version);
            EditorUtility.SetDirty(_buildSettings);

            Undo.RecordObject(_buildSettings, "Change Version Code Type");
            _buildSettings.VersionCodeType =
                (VersionCodeType)EditorGUILayout.EnumPopup("Version Code Type", _buildSettings.VersionCodeType);
            EditorUtility.SetDirty(_buildSettings);

            if (EditorGUI.EndChangeCheck())
            {
                _buildSettings.OnVersionChanged();
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Version Name", _buildSettings.VersionName);
            EditorGUILayout.TextField("Version Code", _buildSettings.VersionCode.ToString());
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Increment Version", GUILayout.Height(25)))
                {
                    Undo.RecordObject(_buildSettings, "Increment Version");
                    _buildSettings.IncreaseVersion();
                }

                if (GUILayout.Button("Decrement Version", GUILayout.Height(25)))
                {
                    Undo.RecordObject(_buildSettings, "Decrement Version");
                    _buildSettings.DecreaseVersion();
                }

                if (GUILayout.Button("Reset Version", GUILayout.Height(25)))
                {
                    Undo.RecordObject(_buildSettings, "Reset Version");
                    _buildSettings.ResetVersion();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Application Icon", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            Undo.RecordObject(_buildSettings, "Change App Icon");
            _buildSettings.IconTexture =
                (Texture2D)EditorGUILayout.ObjectField("App Icon", _buildSettings.IconTexture, typeof(Texture2D),
                    false);
            EditorUtility.SetDirty(_buildSettings);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Actions", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            if (GUILayout.Button("Apply", GUILayout.Height(25)))
            {
                _buildSettings.Apply();
            }

            if (GUILayout.Button("Open Player Settings", GUILayout.Height(25)))
            {
                OpenPlayerSettings();
            }

            EditorGUILayout.EndVertical();
        }

        private void RenderBuildTab()
        {
            if (_percasBuilder != null)
            {
                _percasBuilder.OnGUI();
            }
        }

        private void RenderUtilityTab()
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Time Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(_utilitySettings, "Change Time Scale");
            float timeScale = EditorGUILayout.FloatField("Time Scale", _utilitySettings.TimeScale);
            if (EditorGUI.EndChangeCheck())
            {
                _utilitySettings.TimeScale = timeScale;
                EditorUtility.SetDirty(_utilitySettings);
            }

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(_utilitySettings, "Change Fixed Delta Time");
            float fixedDeltaTime = EditorGUILayout.FloatField("Fixed Delta Time", _utilitySettings.FixedDeltaTime);
            if (EditorGUI.EndChangeCheck())
            {
                _utilitySettings.FixedDeltaTime = fixedDeltaTime;
                EditorUtility.SetDirty(_utilitySettings);
            }

            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(_utilitySettings, "Change Target Frame Rate");
            int targetFrameRate = EditorGUILayout.IntField("Target Frame Rate", _utilitySettings.TargetFrameRate);
            if (EditorGUI.EndChangeCheck())
            {
                _utilitySettings.TargetFrameRate = targetFrameRate;
                EditorUtility.SetDirty(_utilitySettings);
            }

            EditorGUILayout.HelpBox(
                "Note: This setting will be overridden if any script calls Application.targetFrameRate.",
                MessageType.Warning);

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Apply", GUILayout.Height(25)))
            {
                _utilitySettings.Apply();
            }
            
            if (GUILayout.Button("Reset Time Settings", GUILayout.Height(25)))
            {
                _utilitySettings.ResetTimeSettings();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Log Utility", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(_utilitySettings, "Change Show Logs");
            bool showLogs = EditorGUILayout.Toggle("Show Logs", _utilitySettings.ShowLogs);
            if (EditorGUI.EndChangeCheck())
            {
                _utilitySettings.ShowLogs = showLogs;
                EditorUtility.SetDirty(_utilitySettings);
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
            Undo.RecordObject(_utilitySettings, "Change Show Performance Stats");
            bool showStats = EditorGUILayout.Toggle("Show Performance Stats", _utilitySettings.ShowPerformanceStats);
            if (EditorGUI.EndChangeCheck())
            {
                _utilitySettings.ShowPerformanceStats = showStats;
                EditorUtility.SetDirty(_utilitySettings);
            }

            if (_utilitySettings.ShowPerformanceStats && Application.isPlaying)
            {
                _utilitySettings.GetPerformanceStats(
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
            else if (_utilitySettings.ShowPerformanceStats)
            {
                EditorGUILayout.HelpBox("Performance stats are only available during play mode.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        public static void OpenPlayerSettings()
        {
            EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
            var projectSettingsWindow = GetWindow(Type.GetType("UnityEditor.ProjectSettingsWindow,UnityEditor"));
            var projectSettingsMethod = projectSettingsWindow.GetType().GetMethod("SelectProviderByName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (projectSettingsMethod != null)
            {
                projectSettingsMethod.Invoke(projectSettingsWindow, new object[] { "Project/Player" });
            }
        }

        private void OnDestroy()
        {
            if (_percasBuilder)
            {
                DestroyImmediate(_percasBuilder);
            }
        }
    }
}