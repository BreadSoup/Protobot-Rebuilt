using System.Collections;
using System.Collections.Generic;
using Protobot.Builds;
using UnityEngine;
using TMPro;
using UnityEngine.PlayerLoop;


namespace Protobot.UI {
    [RequireComponent(typeof(TMP_Text), typeof(Tooltip))]
    public class BuildNameText : MonoBehaviour {
        private TMP_Text text;
        private Tooltip tooltip;
        [SerializeField] private BuildsManager buildsManager;
        
        void Start() {
            text = GetComponent<TMP_Text>();
            tooltip = GetComponent<Tooltip>();
            
            SceneBuild.OnGenerateBuild += _ => {
                UpdateDisplay();
            };

            buildsManager.OnSaveBuild.AddListener(_ => UpdateDisplay());
            UpdateDisplay();
        }

        void UpdateDisplay() {
            var path = buildsManager.buildPath;
            tooltip.text = path;
            string fileName = (path == "") ? "untitled.pbb" : System.IO.Path.GetFileName(path);
            text.text = fileName;
        }
    }
}
