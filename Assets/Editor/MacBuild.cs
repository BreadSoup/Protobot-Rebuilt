using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Protobot.BuildTools {
    public static class MacBuild {
        private const string DefaultOutputPath = "Builds/macOS/Protobot.app";
        private const int UniversalArchitecture = 2;

        private static readonly string[] WindowsOnlyPlugins = {
            "Assets/Plugins/StandaloneFileBrowser/Plugins/System.Windows.Forms.dll",
            "Assets/Plugins/StandaloneFileBrowser/Plugins/Ookii.Dialogs.dll"
        };

        private const string MacFileBrowserPlugin =
            "Assets/Plugins/StandaloneFileBrowser/Plugins/StandaloneFileBrowser.bundle";

        private static string ProjectRootPath => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [MenuItem("Build/Build macOS Universal")]
        public static void BuildMacOS() {
            try {
                BuildMacOSInternal();
            }
            catch (Exception exception) {
                Debug.LogError($"macOS build failed: {exception}");

                if (Application.isBatchMode) {
                    EditorApplication.Exit(1);
                }

                throw;
            }
        }

        private static void BuildMacOSInternal() {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX)) {
                throw new InvalidOperationException(
                    "macOS Build Support is not installed for this Unity Editor. " +
                    "Install it from Unity Hub before building."
                );
            }

            ValidateNativePlugins();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX) {
                if (Application.isBatchMode) {
                    throw new InvalidOperationException(
                        "The active build target is not macOS. Start batchmode builds with " +
                        "-buildTarget StandaloneOSX."
                    );
                }

                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildTargetGroup.Standalone,
                        BuildTarget.StandaloneOSX)) {
                    throw new InvalidOperationException("Unity could not switch the active build target to macOS.");
                }
            }

            // Unity 2021.3 uses 2 for a macOS Universal (x86_64 + arm64) player.
            PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, UniversalArchitecture);

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0) {
                throw new InvalidOperationException("No enabled scenes were found in Editor Build Settings.");
            }

            var absoluteOutputPath = Path.Combine(ProjectRootPath, DefaultOutputPath);
            var outputDirectory = Path.GetDirectoryName(absoluteOutputPath);
            if (string.IsNullOrEmpty(outputDirectory)) {
                throw new InvalidOperationException($"Invalid macOS output path: {absoluteOutputPath}");
            }

            Directory.CreateDirectory(outputDirectory);
            Debug.Log($"Building macOS Universal player to: {absoluteOutputPath}");

            var options = new BuildPlayerOptions {
                scenes = scenes,
                locationPathName = absoluteOutputPath,
                targetGroup = BuildTargetGroup.Standalone,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result != BuildResult.Succeeded) {
                throw new InvalidOperationException(
                    $"macOS build finished with result {summary.result}. " +
                    $"Errors: {summary.totalErrors}, warnings: {summary.totalWarnings}."
                );
            }

            if (!Directory.Exists(absoluteOutputPath)) {
                throw new DirectoryNotFoundException(
                    $"Unity reported success, but the app bundle was not found at {absoluteOutputPath}."
                );
            }

            Debug.Log(
                $"macOS build succeeded: {absoluteOutputPath} " +
                $"({summary.totalSize} bytes, {summary.totalWarnings} warnings)."
            );
        }

        private static void ValidateNativePlugins() {
            ValidatePluginCompatibility(MacFileBrowserPlugin, shouldSupportMacOS: true);

            foreach (var pluginPath in WindowsOnlyPlugins) {
                ValidatePluginCompatibility(pluginPath, shouldSupportMacOS: false);
            }

            // The legacy NuGet gRPC libraries live under the repository-level Packages folder.
            // Unity does not import them because they are not under Assets and are not declared in
            // Packages/manifest.json. Fail early if a native gRPC library is later moved into Assets,
            // where its current Intel-only macOS binary could be included in a Universal player.
            var grpcNativeLibraries = Directory
                .GetFiles(Application.dataPath, "libgrpc_csharp_ext*.dylib", SearchOption.AllDirectories);

            if (grpcNativeLibraries.Length > 0) {
                throw new InvalidOperationException(
                    "Legacy native gRPC libraries were found under Assets and cannot be included " +
                    $"in the Universal macOS build: {string.Join(", ", grpcNativeLibraries)}"
                );
            }
        }

        private static void ValidatePluginCompatibility(string pluginPath, bool shouldSupportMacOS) {
            var physicalPluginPath = Path.Combine(ProjectRootPath, pluginPath);
            if (!File.Exists(physicalPluginPath) && !Directory.Exists(physicalPluginPath)) {
                throw new FileNotFoundException($"Required plugin path was not found: {pluginPath}");
            }

            var importer = AssetImporter.GetAtPath(pluginPath) as PluginImporter;
            if (importer == null) {
                throw new InvalidOperationException($"Could not inspect plugin importer settings: {pluginPath}");
            }

            var supportsMacOS = importer.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX);
            if (supportsMacOS != shouldSupportMacOS) {
                var expectation = shouldSupportMacOS ? "enabled" : "disabled";
                throw new InvalidOperationException(
                    $"Plugin must be {expectation} for macOS builds: {pluginPath}"
                );
            }
        }
    }
}
