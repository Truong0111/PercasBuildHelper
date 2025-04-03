using UnityEngine;
using UnityEditor;
using System;

namespace Percas.Editor
{
    public class PercasEditor : EditorWindow
    {
        private enum EditorTab
        {
            Settings,
            Build
        }

        private EditorTab _currentTab = EditorTab.Settings;

        private PercasConfigSO _buildSettings;
        private PercasBuilder _percasBuilder;

        private Vector2 _scrollPosition;

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
            _percasBuilder = CreateInstance<PercasBuilder>();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _currentTab = (EditorTab)GUILayout.Toolbar(
                (int)_currentTab,
                new[] { "Settings", "Build" },
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
            }

            EditorGUILayout.EndScrollView();
        }

        private void RenderSettingsTab()
        {
            EditorGUILayout.Space(5);

            // ====== Product Information ======
            EditorGUILayout.LabelField("Product Information", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            _buildSettings.ProductName = EditorGUILayout.TextField("Product Name", _buildSettings.ProductName);
            _buildSettings.PackageName = EditorGUILayout.TextField("Package Name", _buildSettings.PackageName);
            _buildSettings.AliasName = EditorGUILayout.TextField("Alias Name", _buildSettings.AliasName);
            _buildSettings.UsePackageNameForPass =
                EditorGUILayout.Toggle("PackageName As Pass", _buildSettings.UsePackageNameForPass);
            EditorGUI.BeginDisabledGroup(_buildSettings.UsePackageNameForPass);
            _buildSettings.KeyStorePass = EditorGUILayout.PasswordField("Key Store Pass", _buildSettings.KeyStorePass);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // ====== Version Management ======
            EditorGUILayout.LabelField("Version Management", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            _buildSettings.MajorVersion = EditorGUILayout.IntField("Major Version", _buildSettings.MajorVersion);
            _buildSettings.Version = EditorGUILayout.IntField("Version", _buildSettings.Version);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Version Name", _buildSettings.VersionName);
            EditorGUILayout.TextField("Version Code", _buildSettings.VersionCode.ToString());
            EditorGUI.EndDisabledGroup();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Increment Version", GUILayout.Height(20)))
                {
                    _buildSettings.IncreaseVersion();
                }

                if (GUILayout.Button("Decrement Version", GUILayout.Height(20)))
                {
                    _buildSettings.DecreaseVersion();
                }

                if (GUILayout.Button("Reset Version", GUILayout.Height(20)))
                {
                    _buildSettings.ResetVersion();
                }
            }

            EditorGUILayout.EndVertical();

            // ====== Icon ======
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Application Icon", EditorStyles.boldLabel);
            _buildSettings.IconTexture =
                (Texture2D)EditorGUILayout.ObjectField("App Icon", _buildSettings.IconTexture, typeof(Texture2D),
                    false);
            
            EditorGUILayout.Space(8);
            if (GUILayout.Button("Apply", GUILayout.Height(20)))
            {
                _buildSettings.Apply();
            }

            if (GUILayout.Button("Open Player Settings", GUILayout.Height(20)))
            {
                OpenPlayerSettings();
            }
        }

        private void RenderBuildTab()
        {
            if (_percasBuilder != null)
            {
                _percasBuilder.OnGUI();
            }
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