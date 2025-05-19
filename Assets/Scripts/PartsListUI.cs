using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Protobot {
    public class PartsListUI : MonoBehaviour {
        [SerializeField] private GameObject listObjectPrefab;
        [SerializeField] private Transform menuContainer;


        public void ClearCurrentList() {
            for (int i = 0; i < menuContainer.childCount; i++) {
                Destroy(menuContainer.GetChild(i).gameObject);
            }
        }

        public void GenerateList() {
            ClearCurrentList();

            List<GameObject> loadedObjs = PartsManager.FindLoadedObjects();

            for (int i = 0; i < loadedObjs.Count; i++) {
                GameObject clone = Instantiate(listObjectPrefab);
                clone.transform.SetParent(menuContainer);
                clone.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -i * 21.5f);
                clone.transform.localScale = Vector3.one;

                SavedObject savedObjComp = loadedObjs[i].GetComponent<SavedObject>();
                clone.GetComponent<ListObjectUI>().SetDisplay(savedObjComp);
            }
        }
    }
}