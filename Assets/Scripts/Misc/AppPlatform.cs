using UnityEngine;

namespace Protobot {
    public static class AppPlatform {
        public static bool OnWindows => Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;
        public static bool OnMac => Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor;
        public static bool OnLinux => Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor;
        public static bool OnWeb => Application.platform == RuntimePlatform.WebGLPlayer;
    }
}
