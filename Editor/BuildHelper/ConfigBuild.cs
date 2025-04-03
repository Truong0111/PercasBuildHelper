using System;
using System.IO;
using System.Linq;
using UnityEditor;
using System.Diagnostics;
using DG.DemiEditor;
using UnityEditor.Android;
using UnityEngine;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;

namespace Percas.Editor
{
    public class ConfigBuild : EditorWindow
    {
#pragma warning disable CS0414
        static bool flagchange;
#pragma warning restore CS0414

        private static PercasConfigSO percasConfigSO;

        public static PercasConfigSO PercasConfigSO
        {
            get
            {
                percasConfigSO ??= PercasConfigSO.LoadInstance();
                return percasConfigSO;
            }
        }

        public static void OpenFileBuild()
        {
            string path = Path.Combine(Application.dataPath, "../Builds");
            Process proc = new Process();
            proc.StartInfo.FileName = path;
            proc.Start();
        }

        public static void CleanFolderBuild()
        {
            string path = Path.Combine(Application.dataPath, "../Builds");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
        }

        public static void CreateTemplateCmdConvertFile()
        {
            var directoryName = Path.GetDirectoryName(GetFilePath());
            if (directoryName != null)
            {
                var fileName = Directory.GetFiles(directoryName).First(s => s.EndsWith(".aab"));
                ConvertAabToApk(fileName);
            }
        }

        public static void ConvertAabToApk(string aabPath)
        {
#if !UNITY_ANDROID
            throw new PlatformNotSupportedException("Please switch to Android Platform!");
#endif

            aabPath = Path.ChangeExtension(aabPath, "aab");
            if (!File.Exists(aabPath)) return;
            string filename = Path.GetFileNameWithoutExtension(aabPath);
            string keystore = Application.dataPath + "/../" + PlayerSettings.Android.keystoreName;
            string keystorePass = PercasConfigSO.GetKeyStore();
            string keystoreAlias = PercasConfigSO.AliasName;
            string keystoreAliasPass = PercasConfigSO.GetKeyStore();

            #region commands

            // Get java from UnityEditor
            string javaPath = Path.Combine(
#if UNITY_ANDROID
                AndroidExternalToolsSettings.jdkRootPath,
#endif
                @"bin\java");
 
            Debug.Assert(Directory.Exists(Directory.GetDirectoryRoot(javaPath)),
                "Project does not provide a valid java path. " +
                "Check if you are using Unity recommended JDK in External Tools. " +
                $"Looked at: {javaPath}");

            // Get bundletool.jar from UnityEditor
            string bundleToolPath = Path.Combine(EditorApplication.applicationPath,
                "../Data/PlaybackEngines/AndroidPlayer/Tools");

            bundleToolPath = Directory.GetFiles(bundleToolPath, "bundletool*.jar", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            Debug.Assert(!bundleToolPath.IsNullOrEmpty(), "Project does not support Android");

            // Running java command to execute bundle tool and build apks file
            string buildApksCmd =
                $" \"{javaPath}\" -jar \"{bundleToolPath}\" build-apks "
                + $"--bundle=\"{filename}.aab\" --output=\"{filename}.apks\" --mode=universal "
                + $"--ks=\"{keystore}\" --ks-pass=pass:{keystorePass} "
                + $"--ks-key-alias=\"{keystoreAlias}\" --key-pass=pass:{keystoreAliasPass} ";

            // Apks to Zip
            string renameToZipCmd = $" rename \"{filename}.apks\"  \"{filename}\".zip";

            // Zip to apk
            string createFolderCmd = $"mkdir \"{filename}\"";
            string extractCmd = $"tar -xf \"{filename}.zip\" -C \"{filename}\" ";
            string renameApkFileCmd =
                $"RENAME \"{Path.Combine(filename, "universal.apk")}\" \"{filename}.apk\"";

            Debug.Log("aab to zip command:\n \n \n" + buildApksCmd + "\n" + renameToZipCmd + "\n" + createFolderCmd +
                      "\n" + extractCmd + "\n" + renameApkFileCmd + "\n");

            #endregion

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe", WorkingDirectory = Path.GetDirectoryName(aabPath) ?? string.Empty,
                    RedirectStandardInput = true, UseShellExecute = false
                }
            };

            process.Start();
            using (StreamWriter sw = process.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine(buildApksCmd);
                    sw.WriteLine(renameToZipCmd);
                    sw.WriteLine(createFolderCmd);
                    sw.WriteLine(extractCmd);
                    sw.WriteLine(renameApkFileCmd);
                }
            }

            string batFileName = Path.Combine(Path.GetDirectoryName(aabPath), "ExtractAabToApk.bat");
            using (StreamWriter sw = new StreamWriter(batFileName))
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine(buildApksCmd);
                    sw.WriteLine(renameToZipCmd);
                    sw.WriteLine(createFolderCmd);
                    sw.WriteLine(extractCmd);
                    sw.WriteLine(renameApkFileCmd);
                }
            }
        }

        public static void BuildGame(bool final, BuildOptions buildOptions = BuildOptions.None)
        {
            SetUpFinalBuild(final);
            string filePath = GetFilePath();
            AddBuildFolder(filePath);
            FixSettingBuild();
            string[] levels = EditorBuildSettings.scenes.Where(x => x.enabled).Select(scene => scene.path).ToArray();
            var report = BuildPipeline.BuildPlayer(levels, filePath, BuildTarget.Android, buildOptions);
            OpenFileBuild();
            SetUpFinalBuild(false);
            ConvertAabToApk(report.summary.outputPath);
        }

        private static void SetUpFinalBuild(bool final)
        {
#if UNITY_ANDROID
            EditorUserBuildSettings.buildAppBundle = final;

            PlayerSettings.SetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup,
                final ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);

            if (final)
            {
                var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android).Split(';')
                    .ToList();
                if (!symbols.Contains("FINAL_BUILD"))
                {
                    symbols.Add("FINAL_BUILD");
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, symbols.ToArray());
                }
            }
            else
            {
                var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android).Split(';')
                    .ToList();
                if (symbols.Contains("FINAL_BUILD"))
                {
                    symbols.Remove("FINAL_BUILD");
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, symbols.ToArray());
                }
            }
#endif
        }

        private static void AddBuildFolder(string filePath)
        {
            string directoryName = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }

        public static void FixSettingBuild()
        {
            flagchange = false;
#if UNITY_IOS || UNITY_ANDROID
            if (!PercasConfigSO.PackageName.Equals(PlayerSettings.applicationIdentifier))
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PercasConfigSO.PackageName);
                flagchange = true;
            }

            if (!PercasConfigSO.VersionName.Equals(PlayerSettings.bundleVersion))
            {
                PlayerSettings.bundleVersion = PercasConfigSO.VersionName;
                flagchange = true;
            }
#endif
#if UNITY_ANDROID
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.bundleVersionCode = PercasConfigSO.VersionCode;
            PlayerSettings.Android.keystoreName = Constants.Path.KeyStore;
            PlayerSettings.Android.keyaliasName = PercasConfigSO.AliasName;
            PlayerSettings.Android.keyaliasPass = PercasConfigSO.GetKeyStore();
            PlayerSettings.Android.keystorePass = PercasConfigSO.GetKeyStore();
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
#endif
            PlayerSettings.productName = PercasConfigSO.ProductName;
        }

        static string GetFilePath()
        {
            string gameName = GetValidFileName(PlayerSettings.productName);
            return Path.Combine(Application.dataPath,
                $"../Builds/{gameName.Replace(" ", "")}_{DateTime.Now:HH-mm-ssTdd-MM-yyyy}." +
                $"{(EditorUserBuildSettings.buildAppBundle ? "aab" : "apk")}");
        }

        static string GetValidFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName;
        }
    }
}