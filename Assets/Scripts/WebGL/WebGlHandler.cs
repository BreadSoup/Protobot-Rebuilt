using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using AOT;
using Protobot.Builds;
using UnityEngine;

namespace WebGL
{
    public class WebGlHandler : MonoBehaviour
    {
        [SerializeField] private BuildsManager buildsManager;
        // Reference to the BuildsManager
        //private BuildsManager buildsManager;

        private void Start()
        {
            // Find the BuildsManager object in the scene
            /*GameObject buildsManagerObject = GameObject.FindObjectOfType<BuildsManager>().gameObject;

            if (buildsManagerObject != null)
            {
                Debug.Log("GameObject with BuildsManager component found.");

                // Get the BuildsManager component
                buildsManager = buildsManagerObject.GetComponent<BuildsManager>();

                if (buildsManager != null)
                {
                    Debug.Log("BuildsManager component found.");
                }
                else
                {
                    Debug.Log("BuildsManager component not found.");
                }
            }
            else
            {
                Debug.Log("GameObject with BuildsManager component not found.");
            }*/
        }

        //private string name;
        // Changed from static to instance method
        [MonoPInvokeCallback(typeof(Action<string>))]
        public void ReceiveFileData(string base64String)
        {
            Debug.Log("received file data");
            if (string.IsNullOrEmpty(base64String)) return;
            Debug.Log("coverting path");
            byte[] data = Convert.FromBase64String(base64String);
            using (var stream = new MemoryStream(data))
            {
                // Create a binary formatter
                var formatter = new BinaryFormatter();

                // Deserialize the data from the stream
                BuildData build = (BuildData)formatter.Deserialize(stream);

                Debug.Log("about to generate");
                SceneBuild.GenerateBuild(build);
            }
            
            /*Debug.Log(name);
            var filepath =  Path.Combine( System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments ), name);

            buildsManager.AttemptLoad(build, filepath);*/
        }
        [MonoPInvokeCallback(typeof(Action<string>))]
        public void ReceiveFileName(string fileName)
        {
            if (buildsManager == null)
            {
                Debug.Log("BuildsManager is null.");
            }
            else
            {
                buildsManager.buildPath = fileName;
            }
        }
    }
}