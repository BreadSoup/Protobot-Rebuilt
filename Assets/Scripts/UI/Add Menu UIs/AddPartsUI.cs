using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Events;

namespace Protobot.UI {
    public class AddPartsUI : MonoBehaviour {
        public GameObject lastAddedObj;

        [Header("UI")]
        public Text EmptyListText;
        public string EmptySearchMessage;
        public Text searchText; //the text typed in the searchbar
        private string prevSearch; //the text typed in the searchbar
        public Toggle searchToggle;
        public Dropdown groupDropdown;
        [SerializeField] private float spacing;

        [Space(10)]
        public GameObject partUI; //the UI for individual packets
        public RectTransform partUIsContainer; //used for parenting

        public ToggleGroup partDisplayToggleGroup;
        private int toggleCount => partDisplayToggleGroup.ActiveToggles().Count<Toggle>();
        private int prevToggleCount;
        private bool started;

        [Space(10)]

        public UnityEvent OnSelectPartDisplay;
        public UnityEvent OnDeselectPartDisplay;
        

        void OnEnable() {
            GameElementPreferences.Changed += RefreshDisplayedParts;
            if (started) RefreshDisplayedParts();
        }

        void OnDisable() {
            GameElementPreferences.Changed -= RefreshDisplayedParts;
        }

        void OnDestroy() {
            PartDisplayUI.OnChangeSelected -= SelectPartDisplay;
        }

        void SelectPartDisplay(PartDisplayUI display) {
            OnSelectPartDisplay?.Invoke();
        }

        void RefreshDisplayedParts() {
            DeslectSelected();
            string group = groupDropdown.options[groupDropdown.value].text;
            if (searchToggle.isOn || group == "None") DisplaySearchResults();
            else DisplayListGroup(group);
        }

        void Start() {
            PartDisplayUI.OnChangeSelected += SelectPartDisplay;

            groupDropdown.onValueChanged.AddListener(index => {
                string group = groupDropdown.options[index].text;

                if (group == "None") {
                    DisplaySearchResults();
                }
                else {
                    DisplayListGroup(group);
                }
            });
            
            DisplaySearchResults();
            started = true;
        }
        
        void Update() {
            if (searchToggle.isOn && searchText.text != prevSearch) {
                DisplaySearchResults();
            }

            prevSearch = searchText.text;
            
            if (toggleCount == 0 && prevToggleCount != 0)
                OnDeselectPartDisplay?.Invoke();

            prevToggleCount = toggleCount;
        }

        public void DeslectSelected() {
            if (toggleCount != 0 && PartDisplayUI.selected != null)
                PartDisplayUI.selected.GetComponent<Toggle>().isOn = false;
        }

        public void SetEmptyListText(string message) {
            EmptyListText.gameObject.SetActive(true);
            EmptyListText.text = message;
        }

        public void DisplayListGroup(string group) {
            List<PartType> groupList = PartsManager.partTypes.Where(p => p.group.ToString() == group).ToList();
            UpdateDisplayedParts(groupList);
        }

        public void DisplaySearchResults() {
            searchToggle.isOn = true;
            string search = searchText.text.ToLower();
            List<PartType> searchList = PartsManager.partTypes.Where(p => 
                CompareSearch(search, p.name)
                && p.group != PartType.PartGroup.None).ToList();
                
            UpdateDisplayedParts(searchList);

        }

        public bool CompareSearch(string search, string compare) {
            compare = compare.ToLower();
            return (search.Contains(compare) || compare.Contains(search));
        }

        public void DestroyDisplayedParts() {
            int prevListLength = partUIsContainer.childCount;

            for (int c = 1; c < prevListLength; c++) {
                GameObject item = partUIsContainer.GetChild(c).gameObject;
                item.SetActive(false);
                Destroy(item);
            }
        }

        //updates list of objects shown given a list of PartPackets
        public void UpdateDisplayedParts(List<PartType> partsToDisplay) {
            partsToDisplay = partsToDisplay.Where(GameElementPreferences.IsVisible).ToList();
            EmptyListText.gameObject.SetActive(false);

            DestroyDisplayedParts();

            for (int i = 0; i < partsToDisplay.Count; i++) {
                GameObject newItem = Instantiate(partUI);
                newItem.transform.SetParent(partUIsContainer);

                RectTransform newRectTransform = newItem.GetComponent<RectTransform>();
                newRectTransform.localScale = Vector3.one;
                newRectTransform.anchoredPosition = new Vector2(0 ,i * (partUI.GetComponent<RectTransform>().sizeDelta.y + spacing));

                PartDisplayUI newPartDisplayUI = newItem.GetComponent<PartDisplayUI>();
                newPartDisplayUI.SetDisplay(partsToDisplay[i]);

                Toggle newToggle = newItem.GetComponent<Toggle>();
                newToggle.group = partDisplayToggleGroup;
            }
            partUIsContainer.sizeDelta = new Vector2(partUIsContainer.sizeDelta.x, Mathf.Max(0, partsToDisplay.Count * (partUI.GetComponent<RectTransform>().sizeDelta.y + spacing) - spacing));
            if (partsToDisplay.Count == 0)
                SetEmptyListText(searchToggle.isOn ? EmptySearchMessage : "No parts to display.");
        }
    }
}
