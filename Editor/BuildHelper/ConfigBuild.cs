using System;
using System.IO;
using System.Linq;
using UnityEditor;
using System.Diagnostics;
using UnityEditor.Android;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;

namespace PercasHelper.Editor
{
    public class ConfigBuild : EditorWindow
    {
#pragma warning disable CS0414
        static bool _flagChange;
#pragma warning restore CS0414

        private static PercasConfigSO _percasConfigSo;

        public static PercasConfigSO PercasConfigSo => _percasConfigSo ??= PercasConfigSO.LoadInstance();

        public static void OpenFileBuild()
        {
            string path = Path.Combine(Application.dataPath, "../Builds");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        public static void CleanFolderBuild()
        {
            string path = Path.Combine(Application.dataPath, "../Builds");
            if (Directory.Exists(path)) Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        public static void CreateTemplateCmdConvertFile()
        {
#if UNITY_ANDROID
            var directoryName = Path.GetDirectoryName(GetFilePath());
            if (directoryName != null)
            {
                var fileName = Directory.GetFiles(directoryName).First(s => s.EndsWith(".aab"));
                ConvertAabToApk(fileName);
            }
#endif
        }

        public static void ConvertAabToApk(string aabPath)
        {
#if UNITY_ANDROID
            aabPath = Path.ChangeExtension(aabPath, "aab");
            if (!File.Exists(aabPath)) return;
            string filename = Path.GetFileNameWithoutExtension(aabPath);
            string keystore = Application.dataPath + "/../" + PlayerSettings.Android.keystoreName;
            string keystorePass = PercasConfigSo.GetKeyStore();
            string keystoreAlias = PercasConfigSo.AliasName;
            string keystoreAliasPass = PercasConfigSo.GetKeyStore();

            string javaPath = Path.Combine(AndroidExternalToolsSettings.jdkRootPath, "bin", "java");
            Debug.Assert(File.Exists(javaPath), $"Java path invalid: {javaPath}");

            string bundleToolPath = Directory.GetFiles(
                Path.Combine(EditorApplication.applicationPath, "../Data/PlaybackEngines/AndroidPlayer/Tools"),
                "bundletool*.jar", SearchOption.TopDirectoryOnly).FirstOrDefault();
            Debug.Assert(!string.IsNullOrEmpty(bundleToolPath), "BundleTool jar not found");

            string buildApksCmd =
                $"\"{javaPath}\" -jar \"{bundleToolPath}\" build-apks --bundle=\"{filename}.aab\" " +
                $"--output=\"{filename}.apks\" --mode=universal --ks=\"{keystore}\" --ks-pass=pass:{keystorePass} " +
                $"--ks-key-alias=\"{keystoreAlias}\" --key-pass=pass:{keystoreAliasPass}";

            string renameToZipCmd = $"rename \"{filename}.apks\" \"{filename}.zip\"";
            string createFolderCmd = $"mkdir \"{filename}\"";
            string extractCmd = $"tar -xf \"{filename}.zip\" -C \"{filename}\"";
            string renameApkCmd = $"rename \"{Path.Combine(filename, "universal.apk")}" + $"\" \"{filename}.apk\"";

            string batFile = Path.Combine(Path.GetDirectoryName(aabPath) ?? string.Empty, "ExtractAabToApk.bat");
            File.WriteAllLines(batFile, new[] { buildApksCmd, renameToZipCmd, createFolderCmd, extractCmd, renameApkCmd });

            var process = new Process
            {
                StartInfo = new ProcessStartInfo("cmd.exe")
                {
                    WorkingDirectory = Path.GetDirectoryName(aabPath) ?? string.Empty,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            using var sw = process.StandardInput;
            if (sw.BaseStream.CanWrite)
            {
                sw.WriteLine(buildApksCmd);
                sw.WriteLine(renameToZipCmd);
                sw.WriteLine(createFolderCmd);
                sw.WriteLine(extractCmd);
                sw.WriteLine(renameApkCmd);
            }
#endif
        }

        public static void BuildGame(bool final, BuildOptions buildOptions = BuildOptions.None,
            bool isCustomBuildFileName = false, string buildFileName = "", BuildTarget target = BuildTarget.Android)
        {
            SetUpFinalBuild(final, target);
            string filePath = GetFilePath(isCustomBuildFileName, buildFileName);
            AddBuildFolder(filePath);

            string[] levels = EditorBuildSettings.scenes.Where(x => x.enabled).Select(scene => scene.path).ToArray();
            var report = BuildPipeline.BuildPlayer(levels, filePath, target, buildOptions);

#if UNITY_ANDROID
            if (final)
            {
                ConvertAabToApk(report.summary.outputPath);
            }
#endif
            SetUpFinalBuild(false, target);
        }

        private static void SetUpFinalBuild(bool final, BuildTarget target)
        {
            var group = BuildPipeline.GetBuildTargetGroup(target);

            if (target == BuildTarget.Android)
            {
                EditorUserBuildSettings.buildAppBundle = final;
                PlayerSettings.SetScriptingBackend(group,
                    final ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);
            }
            else if (target == BuildTarget.iOS)
            {
            }

            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';').ToList();
            if (final && !symbols.Contains("FINAL_BUILD")) symbols.Add("FINAL_BUILD");
            if (!final && symbols.Contains("FINAL_BUILD")) symbols.Remove("FINAL_BUILD");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbols.ToArray());
        }

        private static void AddBuildFolder(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        static string GetFilePath(bool isCustomBuildFileName = false, string buildFileName = "")
        {
            string extension = "";
            string platformFolder = "";

#if UNITY_ANDROID
            extension = EditorUserBuildSettings.buildAppBundle ? "aab" : "apk";
            platformFolder = "Android";
#elif UNITY_IOS
            extension = "ipa";
            platformFolder = "iOS";
#else
            extension = "exe";
            platformFolder = EditorUserBuildSettings.activeBuildTarget.ToString();
#endif

            string fileName = isCustomBuildFileName && !string.IsNullOrEmpty(buildFileName)
                ? buildFileName
                : GetValidFileName(PlayerSettings.productName) + "_" + DateTime.Now.ToString("HH-mm-ssTdd-MM-yyyy");

            return Path.Combine(Application.dataPath, $"../Builds/{platformFolder}/{fileName}.{extension}");
        }

        static string GetValidFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c, '_');
            return fileName;
        }
    }
}