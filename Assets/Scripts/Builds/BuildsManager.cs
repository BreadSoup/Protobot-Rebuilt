﻿using System;
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
        public static BuildsManager Instance { get; private set; }
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

        public string attemptPath = "";
        public BuildData attemptData;
        
        public BuildDataUnityEvent OnLoadBuild;
        public BuildDataUnityEvent OnSaveBuild;

        private bool avoidingQuit = false;
        private bool forceQuit = false;

        private bool IsNotSaved => buildPath == "";

        private void Awake() {
            saveInput.performed += Save;
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(gameObject);
            }
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
        [DllImport("__Internal")]
        private static extern void SaveFile(string data, string fileName);
        public void Save()
        {
            BuildData sceneBuildData = new BuildData();
            BinaryFormatter bf = new BinaryFormatter();
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                sceneBuildData = SceneBuild.ToBuildData();
                bf = new BinaryFormatter();
                using (MemoryStream ms = new MemoryStream())
                {
                    bf.Serialize(ms, sceneBuildData);
                    string data = Convert.ToBase64String(ms.ToArray());
                    Debug.Log(data);
                    string dataUri = Uri.EscapeDataString(data);
                    SaveFile(dataUri, "example.pbb");
                }
            }
            else
            {

                if (buildPath == "")
                {
                    SaveAs();
                    return;
                }

                sceneBuildData = SceneBuild.ToBuildData();

                bf = new BinaryFormatter();

                FileStream file = File.Create(buildPath);
                bf.Serialize(file, sceneBuildData);
                file.Close();

                savedBuildData = sceneBuildData;
                OnSaveBuild?.Invoke(sceneBuildData);
            }
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
        private static extern void OpenFile();
        public void OpenBuild()
        {
            string[] paths;
            //string paths;
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                paths = StandaloneFileBrowser.OpenFilePanel("Open Build File", "", "pbb", false);
            }
            else
            {
                // paths = Application.ExternalEval("openFile();");
                OpenFile();
                return;
            }

            if (paths.Length == 0 || paths[0] == "") return;

            var path = paths[0];

            var build = ParsePath(path);
            AttemptLoad(build, path);
        }
        
        /// <summary>
        /// Converts a given file path to BuildData
        /// </summary>
        public static BuildData ParsePath(string filePath) {
            if (!File.Exists(filePath) || !filePath.Contains(".pbb")) return null;
            
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(filePath, FileMode.Open);

            BuildData build = (BuildData)bf.Deserialize(file);
            file.Close();

            return build;
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
        }

        /// <summary>
        /// Loads an empty build with an untitled.pbb
        /// </summary>
        public void CreateNewBuild() {
            AttemptLoad(SceneBuild.DefaultBuild, "");
        }
    }
}