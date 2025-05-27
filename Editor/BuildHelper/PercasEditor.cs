using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;

namespace PercasHelper.Editor
{
    public class PercasEditor : EditorWindow
    {
        #region Constants

        private const int BUTTON_HEIGHT = 25;
        private const int BROWSE_BUTTON_WIDTH = 60;
        private const int MIN_WINDOW_WIDTH = 400;
        private const int MIN_WINDOW_HEIGHT = 500;
        private const int SPACING = 5;
        private const string KEYSTORE_EXTENSION = "keystore";
        private const string ASSETS_PREFIX = "Assets/";

        #endregion

        #region Enums

        private enum EditorTab
        {
            Settings,
            Build
        }

        #endregion

        #region Fields

        [SerializeField] private EditorTab currentTab = EditorTab.Settings;
        [SerializeField] private Vector2 scrollPosition;

        private PercasConfigSO buildSettings;
        private PercasBuilder percasBuilder;

        private static Type projectSettingsWindowType;
        private static MethodInfo selectProviderMethod;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            buildSettings = PercasConfigSO.LoadInstance();
            if (buildSettings == null)
            {
                Debug.LogError("Failed to load PercasConfigSO instance!");
                return;
            }

            percasBuilder = CreateInstance<PercasBuilder>();
            CacheReflectionData();
        }

        private void OnDestroy()
        {
            if (percasBuilder != null)
                DestroyImmediate(percasBuilder);
        }

        private void OnGUI()
        {
            DrawTabHeader();
            EditorGUILayout.Space(SPACING);
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scrollScope.scrollPosition;
                if (currentTab == EditorTab.Settings) DrawSettingsTab();
                else DrawBuildTab();
            }
        }

        #endregion

        #region Menu Items

        [MenuItem("Percas/Build Helper", priority = 0)]
        private static void ShowWindow()
        {
            var window = GetWindow<PercasEditor>();
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.titleContent = new GUIContent("Percas Build Helper");
            window.Show();
        }

        [MenuItem("Percas/Open build folder", priority = 1)]
        public static void OpenFileBuild() => ConfigBuild.OpenFileBuild();

        [MenuItem("Percas/Build Game &_b", priority = 2)]
        public static void BuildGame() => ConfigBuild.BuildGame(false);

        #endregion

        #region GUI Drawing

        private void DrawTabHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                currentTab = (EditorTab)GUILayout.Toolbar((int)currentTab, new[] { "Settings", "Build" },
                    EditorStyles.toolbarButton);
            }
        }

        #endregion

        #region Reflection Cache

        private static void CacheReflectionData()
        {
            projectSettingsWindowType ??= Type.GetType("UnityEditor.ProjectSettingsWindow,UnityEditor");
            if (projectSettingsWindowType != null && selectProviderMethod == null)
            {
                selectProviderMethod = projectSettingsWindowType.GetMethod("SelectProviderByName",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }

        #endregion

        #region Settings Tab

        private void DrawSettingsTab()
        {
            if (buildSettings == null)
            {
                EditorGUILayout.HelpBox("PercasConfigSO not found. Please create one first.", MessageType.Error);
                return;
            }

            DrawSection("Product Information", () =>
            {
                DrawTextField("Product Name", ref buildSettings.ProductName);
                DrawTextField("Package Name", ref buildSettings.PackageName);
                DrawTextField("Alias Name", ref buildSettings.AliasName);

                DrawPasswordForKeyStore();
                DrawCustomKeystore();
                DrawSplitApplicationBinary();
            });

            DrawSection("Version Management", () =>
            {
                DrawIntField("Major Version", ref buildSettings.VersionMajor);
                DrawIntField("Minor Version", ref buildSettings.VersionMinor);
                DrawIntField("Patch Version", ref buildSettings.VersionPatch);
                DrawEnumField("Version Code Type", ref buildSettings.VersionCodeType);

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.TextField("Version Name", buildSettings.VersionName);
                
                DrawIntField("Version Code", ref buildSettings.VersionCode);

                DrawVersionActions();
            });

            DrawSection("Application Icon",
                () =>
                {
                    buildSettings.IconTexture = (Texture2D)EditorGUILayout.ObjectField("App Icon",
                        buildSettings.IconTexture, typeof(Texture2D), false);
                });

            DrawCustomActions();
        }

        private void DrawPasswordForKeyStore()
        {
            DrawToggleField("PackageName As Pass", ref buildSettings.UsePackageNameForPass);
            if (buildSettings.UsePackageNameForPass) return;
            
            DrawPasswordField("Key Store Pass", ref buildSettings.KeyStorePass);
        }
        
        private void DrawCustomKeystore()
        {
            DrawToggleField("Use Custom Keystore", ref buildSettings.UseCustomKeystore);
            if (!buildSettings.UseCustomKeystore) return;

            string newPath = buildSettings.CustomKeystorePath;
            using (new EditorGUILayout.HorizontalScope())
            {
                newPath = EditorGUILayout.TextField("Keystore Path", newPath);
                if (GUILayout.Button("Browse", GUILayout.Width(BROWSE_BUTTON_WIDTH)))
                {
                    string selectedPath = EditorUtility.OpenFilePanel("Select Keystore File",
                        GetInitialDirectory(newPath), KEYSTORE_EXTENSION);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        newPath = selectedPath.StartsWith(Application.dataPath)
                            ? ASSETS_PREFIX + selectedPath.Substring(Application.dataPath.Length)
                            : selectedPath;
                    }
                }
            }

            if (newPath != buildSettings.CustomKeystorePath && ValidatePath(newPath))
                buildSettings.CustomKeystorePath = newPath;

            if (!string.IsNullOrEmpty(buildSettings.CustomKeystorePath) &&
                !buildSettings.CustomKeystorePath.StartsWith(ASSETS_PREFIX))
            {
                EditorGUILayout.HelpBox("Using an external keystore file.", MessageType.Warning);
            }
        }

        private string GetInitialDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return "Assets";
            if (path.StartsWith(ASSETS_PREFIX))
            {
                string dir = Path.Combine(Application.dataPath, path.Substring(ASSETS_PREFIX.Length));
                return Directory.Exists(Path.GetDirectoryName(dir)) ? Path.GetDirectoryName(dir) : "Assets";
            }

            return File.Exists(path) ? Path.GetDirectoryName(path) : "Assets";
        }

        private bool ValidatePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Invalid Path", "Keystore path cannot be empty", "OK");
                return false;
            }

            if (!path.StartsWith(ASSETS_PREFIX) && !File.Exists(path))
            {
                EditorUtility.DisplayDialog("Invalid Path", "Keystore file not found", "OK");
                return false;
            }

            return true;
        }

        private void DrawSplitApplicationBinary()
        {
            DrawToggleField("Split Application Binary", ref buildSettings.SplitApplicationBinary);
            if (buildSettings.SplitApplicationBinary)
            {
                EditorGUILayout.HelpBox("Split APKs for architectures.", MessageType.Info);
            }
        }

        private void DrawVersionActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Increment", GUILayout.Height(BUTTON_HEIGHT)))
                    buildSettings.IncreaseVersion();
                if (GUILayout.Button("Decrement", GUILayout.Height(BUTTON_HEIGHT)))
                    buildSettings.DecreaseVersion();
                if (GUILayout.Button("Reset", GUILayout.Height(BUTTON_HEIGHT)))
                    buildSettings.ResetVersion();
                if (GUILayout.Button("Refresh", GUILayout.Height(BUTTON_HEIGHT)))
                    buildSettings.RefreshVersion();
            }
        }

        private void DrawCustomActions()
        {
            /* Placeholder for future settings */
        }

        #endregion

        #region Build Tab

        private void DrawBuildTab()
        {
            // EditorGUILayout.LabelField("Build settings and options go here.");
            percasBuilder.OnGUI();
        }

        #endregion

        #region Helpers

        private void DrawSection(string title, Action content)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(title, EditorStyles.boldLabel);
                EditorGUILayout.Space(2);
                content?.Invoke();
            }
        }

        private void DrawTextField(string label, ref string value)
        {
            string newValue = EditorGUILayout.TextField(label, value);
            if (!Equals(newValue, value)) value = newValue;
        }

        private void DrawPasswordField(string label, ref string value)
        {
            string newValue = EditorGUILayout.PasswordField(label, value);
            if (!Equals(newValue, value)) value = newValue;
        }

        private void DrawToggleField(string label, ref bool value)
        {
            bool newValue = EditorGUILayout.Toggle(label, value);
            if (!Equals(newValue, value)) value = newValue;
        }

        private void DrawIntField(string label, ref int value)
        {
            int newValue = EditorGUILayout.IntField(label, value);
            if (!Equals(newValue, value)) value = newValue;
        }

        private void DrawEnumField<T>(string label, ref T value) where T : Enum
        {
            T newValue = (T)EditorGUILayout.EnumPopup(label, value);
            if (!Equals(newValue, value)) value = newValue;
        }

        #endregion
    }
}