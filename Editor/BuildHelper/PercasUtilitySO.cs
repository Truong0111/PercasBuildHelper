using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Linq;

namespace PercasHelper.Editor
{
    public class PercasUtilitySO : ScriptableObject
    {
        [field: SerializeField] public float TimeScale { get; set; } = 1f;
        [field: SerializeField] public float FixedDeltaTime { get; set; } = 1f / 60f;
        [field: SerializeField] public int TargetFrameRate { get; set; } = 60;
        [field: SerializeField] public bool ShowLogs { get; set; } = true;
        [field: SerializeField] public bool ShowPerformanceStats { get; set; } = true;

        private const string PercasUtilityFileName = "PercasUtility";
        private const string PercasUtilityResDir = "Assets/Percas";
        private const string PercasUtilityFileExtension = ".asset";

        private const string PercasUtilityFilePath =
            PercasUtilityResDir + "/" + PercasUtilityFileName + PercasUtilityFileExtension;

        private static bool _isInitialized = false;
        private float _deltaTime = 0.0f;
        private float _fps = 0.0f;
        private int _batches = 0;
        private int _drawCalls = 0;
        private int _triangles = 0;
        private int _vertices = 0;
        private long _totalMemory = 0;
        private long _usedMemory = 0;
        private int _textureMemory = 0;
        private int _meshMemory = 0;
        private int _materialCount = 0;

        public static PercasUtilitySO LoadInstance()
        {
            var instance = AssetDatabase.LoadAssetAtPath<PercasUtilitySO>(PercasUtilityFilePath);

            if (instance == null)
            {
                Debug.LogWarning("PercasUtilitySO not found. Creating a new one.");
                Directory.CreateDirectory(PercasUtilityResDir);
                instance = CreateInstance<PercasUtilitySO>();
                AssetDatabase.CreateAsset(instance, PercasUtilityFilePath);
                AssetDatabase.SaveAssets();
            }

            return instance;
        }

        private void OnEnable()
        {
            TimeScale = Time.timeScale;
            FixedDeltaTime = Time.fixedDeltaTime;
            TargetFrameRate = Application.targetFrameRate;

            if (!_isInitialized)
            {
                Application.logMessageReceived += HandleLog;
                EditorApplication.update += UpdatePerformanceStats;
                _isInitialized = true;
            }

            UpdateLogSettings();
        }

        private void OnDisable()
        {
            if (_isInitialized)
            {
                Application.logMessageReceived -= HandleLog;
                EditorApplication.update -= UpdatePerformanceStats;
                _isInitialized = false;
            }
        }

        public void Apply()
        {
            Time.timeScale = TimeScale;
            Time.fixedDeltaTime = FixedDeltaTime;
            Application.targetFrameRate = TargetFrameRate;
            UpdateLogSettings();
            AssetDatabase.SaveAssets();
        }

        public void ResetTimeSettings()
        {
            TimeScale = 1f;
            FixedDeltaTime = 0.02f;
            TargetFrameRate = 60;
            Apply();
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (!ShowLogs) return;
        }

        public void UpdateLogSettings()
        {
            Debug.unityLogger.logEnabled = ShowLogs;
        }

        private void UpdatePerformanceStats()
        {
            if (!ShowPerformanceStats) return;

            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            _fps = 1.0f / _deltaTime;

            _batches = UnityStats.batches;
            _drawCalls = UnityStats.drawCalls;
            _triangles = UnityStats.triangles;
            _vertices = UnityStats.vertices;

            _totalMemory = SystemInfo.systemMemorySize;
            _usedMemory = GC.GetTotalMemory(false);
            _textureMemory = UnityStats.usedTextureMemorySize;

            if (Application.isPlaying)
            {
                _meshMemory = CalculateMeshMemory();
                _materialCount = CalculateMaterialCount();
            }
        }

        private int CalculateMeshMemory()
        {
            try
            {
                var meshes = Resources.FindObjectsOfTypeAll<Mesh>();
                return meshes.Sum(mesh => mesh.vertexCount * 32 + mesh.triangles.Length * 4);
            }
            catch
            {
                return 0;
            }
        }

        private int CalculateMaterialCount()
        {
            try
            {
                var materials = Resources.FindObjectsOfTypeAll<Material>();
                return materials.Length;
            }
            catch
            {
                return 0;
            }
        }

        public void GetPerformanceStats(
            out float fps, 
            out int batches, 
            out int drawCalls, 
            out int tris, 
            out int verts,
            out long totalMemory,
            out long usedMemory,
            out int textureMemory,
            out int meshMemory,
            out int materialCount)
        {
            fps = _fps;
            batches = _batches;
            drawCalls = _drawCalls;
            tris = _triangles;
            verts = _vertices;
            totalMemory = _totalMemory;
            usedMemory = _usedMemory;
            textureMemory = _textureMemory;
            meshMemory = _meshMemory;
            materialCount = _materialCount;
        }
    }
}