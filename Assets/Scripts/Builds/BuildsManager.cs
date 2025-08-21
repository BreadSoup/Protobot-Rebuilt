using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
using SFB;

using Protobot.UI;
using Protobot.InputEvents;
using UnityEngine.SceneManagement;

namespace Protobot.Builds {
    public class BuildsManager : MonoBehaviour {
        [SerializeField] private InputEvent saveInput;

        [SerializeField] private UnsavedChangesUI unsavedChangesMenu;
        
        /// <summary>
        /// Stores the path where the current build is located
        /// </summary>
        public string buildPath = "";
        
        /// <summary>
        /// Stores the currently saved data for the loaded build
        /// </summary>
        private BuildData savedBuildData;

        private string attemptPath = "";
        private BuildData attemptData;
        
        public BuildDataUnityEvent OnLoadBuild;
        public BuildDataUnityEvent OnSaveBuild;

        private bool avoidingQuit = false;
        private bool forceQuit = false;

        private bool IsNotSaved => buildPath == "";

        private void Awake() {
            saveInput.performed += Save;
        }

        public void Start() {
            buildPath = "";
            
            SceneBuild.OnGenerateBuild += (data) => {
                OnLoadBuild.Invoke(data);
            };
            

            Application.wantsToQuit += () => {
                avoidingQuit = HasUnsavedChanges() && !forceQuit;
                if (avoidingQuit) {
                    unsavedChangesMenu.Enable(IsNotSaved);
                }

                return !avoidingQuit;
            };

            unsavedChangesMenu.OnPressDiscard += () => {
                if (avoidingQuit) 
                    Quit();
                else
                    LoadAttempt();
            };

            unsavedChangesMenu.OnPressSave += () => {
                if (avoidingQuit)
                    SaveAndQuit();
                else
                    SaveAndLoadAttempt();
            };
            
            string[] arguments = Environment.GetCommandLineArgs();
            string initPath = arguments[0];
            var initData = ParsePath(initPath);

            if (initData != null)
                AttemptLoad(initData, initPath);
        }

        public string GetFileName() => PathToFileName(buildPath);
        
        public static string PathToFileName(string path) => (path.Length > 0) ? path.Split('\\')[^1] : "";
        
        public void Save() {
            if (buildPath == "") {
                SaveAs();
                return;
            }

            var sceneBuildData = SceneBuild.ToBuildData();
            
            BinaryFormatter bf = new BinaryFormatter();

            FileStream file = File.Create(buildPath);
            bf.Serialize(file, sceneBuildData);
            file.Close();

            savedBuildData = sceneBuildData;
            OnSaveBuild?.Invoke(sceneBuildData);
        }

        public void SaveAs() {
            var path = StandaloneFileBrowser.SaveFilePanel("Save Build File", "", "", "pbb");

            if (path == "") return;

            buildPath = path;
            Save();
        }

        public void SaveAndQuit() {
            Save();
            Quit();
        }

        public void Quit() {
            forceQuit = true;
            Application.Quit();
        }

        public void AttemptQuit() {
            Application.Quit();
        }

        private bool HasUnsavedChanges() {
            var sceneBuild = SceneBuild.ToBuildData();
            
            if (IsNotSaved) {
                return sceneBuild.parts.Length > 0;
            }

            return !sceneBuild.CompareData(savedBuildData);
        }
        [DllImport("__Internal")]
        private static extern void WebGl_Open(string gameObjectName, string methodName);
        public void OpenBuild()
        {
            //gameObject.SetActive(true);
            Debug.Log("GameObject active? " + gameObject.activeInHierarchy);
            Debug.Log("GameObject name: " + gameObject.name);
#if UNITY_WEBGL && !UNITY_EDITOR
            gameObject.SetActive(true); //for some reason WebGL deactivates the GameObject when built???
            WebGl_Open("BuildsManager", "WebGL_OpenBuild");
            //ShowFileUploadDialog();
#else
            var paths = StandaloneFileBrowser.OpenFilePanel("Open Build File", "", "pbb", false);
            if (paths.Length == 0 || paths[0] == "") return;

            var path = paths[0];

            var build = ParsePath(path);
            AttemptLoad(build, path);
#endif
        }

        public void WebGL_OpenBuild(string base64Data)
        {
            byte[] bytes = Convert.FromBase64String(base64Data);
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                BinaryFormatter bf = new BinaryFormatter();
                BuildData build = (BuildData)bf.Deserialize(ms);
                SceneBuild.GenerateBuild(build);
            }
        }
        
        /// <summary>
        /// Converts a given file path to BuildData
        /// </summary>
        public static BuildData ParsePath(string filePath) {
            if (!File.Exists(filePath) || !filePath.Contains(".pbb")) return null;
            
            BinaryFormatter bf = new BinaryFormatter();
            
#if UNITY_WEBGL && !UNITY_EDITOR
            byte[] bytes = Convert.FromBase64String(filePath); 
            using (MemoryStream ms = new MemoryStream(bytes)) {
                return (BuildData)bf.Deserialize(ms);
            }
#else
            FileStream file = File.Open(filePath, FileMode.Open);
            
            BuildData build = (BuildData)bf.Deserialize(file);
            file.Close();

            return build;
#endif
        }
        
        /// <summary>
        /// Sets the attempt variables to be ready once a load call is given
        /// </summary>
        /// <param name="newData"></param>
        /// <param name="newPath"></param>
        public void AttemptLoad(BuildData newData, string newPath) {
            attemptData = newData;
            attemptPath = newPath;

            if (HasUnsavedChanges()) {
                unsavedChangesMenu.Enable(IsNotSaved);
            }
            else {
                LoadAttempt();
            }
        }

        public void SaveAndLoadAttempt() {
            Save();
            LoadAttempt();
        }

        public void LoadAttempt() {
            savedBuildData = attemptData;
            buildPath = attemptPath;
            SceneBuild.GenerateBuild(attemptData);

            // Call OutputPartsList to update the weight display after placing a part
                PartListOutput partListOutput = FindObjectOfType<PartListOutput>();
                if (partListOutput != null) {
                    partListOutput.CalculatePartsList(); // Recalculate and notify weight update
                }
        }

        /// <summary>
        /// Loads an empty build with an untitled.pbb
        /// </summary>
        public void CreateNewBuild() {
            AttemptLoad(SceneBuild.DefaultBuild, "");
        }
    }
}