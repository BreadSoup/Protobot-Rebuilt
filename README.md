# Protobot Rebuilt

Protobot Rebuilt is a continuation of the Protobot project started by davegersh.

## Installation

Official binaries for Protobot Rebuilt can be found on the [Releases]() page of this repository. Unzip the "Protobot Rebuilt.zip" file and double-click the "Protobot Rebuilt.exe" file to start the program.

Video Guide: 

https://github.com/user-attachments/assets/7b37e938-61f8-463c-8c47-1981fb4ccd90

## Building for macOS

The macOS build is currently intended for local development and testing. It produces an unsigned `Protobot.app`; Apple Developer signing, notarization, DMG packaging, and a formal macOS release are not part of the current build process.

### Requirements

- macOS with Unity Hub.
- Unity Editor **2021.3.5f1**, matching `ProjectSettings/ProjectVersion.txt`.
- The **Mac Build Support (Mono)** module for that exact Editor version. If you choose to change the scripting backend to IL2CPP, install **Mac Build Support (IL2CPP)** as well.
- A complete checkout of this repository.

Install or add the build support module from Unity Hub under **Installs > 2021.3.5f1 > Add modules**.

### Build from the Unity Editor

The recommended deterministic build is:

1. Open the repository root as a Unity project.
2. Wait for package import and script compilation to finish with no errors.
3. Select **Build > Build macOS Universal**.
4. Find the result at `Builds/macOS/Protobot.app`.

To use Unity's standard build window instead:

1. Open **File > Build Settings**.
2. Select **PC, Mac & Linux Standalone** and set **Target Platform** to **macOS**.
3. Select **Switch Platform** if necessary.
4. In Player Settings, set the macOS architecture to **Universal**.
5. Build to `Builds/macOS/Protobot.app`.

### Build from the command line

Run this command from the repository root:

```sh
"/Applications/Unity/Hub/Editor/2021.3.5f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -quit \
  -projectPath "$PWD" \
  -buildTarget StandaloneOSX \
  -executeMethod Protobot.BuildTools.MacBuild.BuildMacOS \
  -logFile -
```

The build method exits with a non-zero status when build support is missing, plugin validation fails, scripts do not compile, no scenes are enabled, or Unity reports a failed build.

### Run the local macOS app

After a successful build, launch the app from the repository root:

```sh
open "Builds/macOS/Protobot.app"
```

You can also double-click `Protobot.app` in Finder. The app is intentionally unsigned and not notarized at this stage. If macOS blocks the first launch, Control-click the app in Finder, choose **Open**, and confirm that you want to run it for local testing.

### Architectures and native plugins

- The build script configures a **Universal** application containing Intel `x86_64` and Apple Silicon `arm64` code.
- `StandaloneFileBrowser.bundle`, used by the native open/save dialogs, contains both `x86_64` and `arm64` architectures and is enabled for macOS.
- `System.Windows.Forms.dll` and `Ookii.Dialogs.dll` are Windows-only and are excluded from macOS players by their Unity plugin import settings.
- The legacy gRPC NuGet files under the repository-level `Packages/` folders are not referenced by `Packages/manifest.json`, are not used by the current REST-based Firebase client, and are not imported into the Unity player. Their macOS native library is Intel-only; do not move it under `Assets/` without replacing it with an Apple Silicon-compatible build.
- DOTween is a managed cross-platform assembly. Its separate editor DLL is limited to the Unity Editor by its importer settings.
- The bundled macOS file dialog plugin declares macOS 12.0 as its minimum system version. Compatibility with older macOS releases is not currently supported or verified.

Local build and recent-file data is stored below `Application.persistentDataPath`; on macOS this is normally under the user's `~/Library/Application Support` folder. Existing `.pbb` files keep the same serialized format and can still be opened from any user-selected location.

### GitHub Actions

`.github/workflows/build-macos.yml` builds `StandaloneOSX`, calls the same `MacBuild.BuildMacOS` method, preserves the `.app` bundle in a ZIP, and uploads it as the `Protobot-macOS-Universal` artifact.

The workflow requires valid Unity licensing secrets. Configure `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD` in the repository's Actions secrets, following the same licensing model as the existing Windows workflow. No credentials are stored in this repository.

### Current limitations

- The app is not signed or notarized.
- A CI-downloaded build can be blocked by Gatekeeper because it is unsigned.
- Apple Silicon launch has been verified with the local Universal build. Intel execution, file dialogs, local `.pbb` round trips, authentication, and cloud operations still require testing.
- Compatibility has been kept on Unity 2021.3.5f1; upgrading Unity is a separate migration and is not required for this build.



## Contributing

Pull requests are welcome and very much appreciated. You can also contribute by submitting issues for problems you find in the software.

#### Notes
- This version of Protobot uses Unity **2021.3.5f1** please have all changes support this version and have [`ProjectVersion.txt`](https://github.com/BreadSoup/Protobot-Rebuilt/blob/main/ProjectSettings/ProjectVersion.txt) `m_EditorVersion:` be `2021.3.5f1`
- Documentation for the code itself is limited and there may be unused sections of code, this should be expected.
Used code sections are well commented and organized modularly to be easily understood and used.
 
## License

[GPLv3](https://choosealicense.com/licenses/gpl-3.0/)
