using UnityEngine;
using System.IO;

namespace Protobot.Builds.Windows {
    // Retain the existing type name so scene and code references remain compatible.
    // The implementation is shared by Windows and macOS players.
    public static class WindowsSavingConfig {
        public static string saveDirectoryPath {
            get {
                var directoryPath = Path.Combine(Application.persistentDataPath, "Builds");
                Directory.CreateDirectory(directoryPath);
                return directoryPath;
            }
        }

        public static string saveFileType => ".Build";
        
        [RuntimeInitializeOnLoadMethod]
        private static void Init() {
            _ = saveDirectoryPath;
        }
    }
}
