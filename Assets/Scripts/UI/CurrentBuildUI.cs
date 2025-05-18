using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Protobot.Builds;

namespace Protobot.UI {
    public class CurrentBuildUI : MonoBehaviour {
        [Header("UI")]
        [SerializeField] private Text fileTitleText; //the text that displays the title of the current file
        [SerializeField] private Text fileLastSavedText; //the text that displays when current save was last saved
        [SerializeField] private RectTransform buildNameContainer;

        private bool renaming = false;
        [SerializeField] private InputField renameField; //the input field for renaming the current build
        [SerializeField] private Text placeholder; //the input field for renaming the current build
        private string initPlaceholderName = "";

        [SerializeField] private StringUnityEvent OnRenameSuccess;

        public void UpdateTopBarUI(BuildData buildData) {
            fileTitleText.text = System.IO.Path.GetFileName(buildData.name);

            var width = Mathf.Clamp(fileTitleText.preferredWidth + 25, 150f, 300f);
            buildNameContainer.sizeDelta = new Vector2(width, buildNameContainer.sizeDelta.y);

            fileLastSavedText.text = "Last Saved\n" + buildData.lastWriteTime;
        }

        public void StartRenaming() {
            renameField.gameObject.SetActive(true);
            initPlaceholderName = placeholder.text;
            placeholder.text = "";
            renameField.Select();
        }

        public void Update() {
            if (renameField.isFocused) {
                renaming = true;
                if (Keyboard.current.escapeKey.isPressed) {
                    StopRenaming(false);
                }

                if (renameField.textComponent.text.Length > 0 && Keyboard.current.enterKey.isPressed) {
                    StopRenaming(true);
                }
            }
            else if (renaming) {
                StopRenaming(renameField.textComponent.text.Length > 0);
            }
        }

        public void StopRenaming(bool success) {
            if (success) {
                OnRenameSuccess?.Invoke(renameField.textComponent.text);
            }
            else {
                placeholder.text = initPlaceholderName;
            }

            placeholder.gameObject.SetActive(true);
            placeholder.enabled = true;

            renameField.gameObject.SetActive(false);
            renameField.textComponent.text = "";
            renaming = false;
        }
    }
}
