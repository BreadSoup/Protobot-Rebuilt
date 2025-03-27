using UnityEngine;
using UnityEngine.Events;
using System;

namespace Protobot.SelectionSystem {
    [Serializable]
    public class SelectionManager : MonoBehaviour {
        public ISelection current;

        [Space(10)]

        public bool allowRepeatedSelection;
        public bool clearOnFailConditions;

        [Space(10)]

        [SerializeField] private Selector[] selectors;
        [SerializeField] private SelectionResponse[] responses;
        [SerializeField] private SelectionCondition[] conditions;
        private IResponseSelector[] responseSelectors = new IResponseSelector[0];

        [Header("Events")]
        [SerializeField] public GameObjectUnityEvent OnUpdateSelection;
        [SerializeField] public UnityEvent OnClearSelection;


        private void Awake() {
            FindSelectors();
            responses = GetComponents<SelectionResponse>();
            conditions = GetComponents<SelectionCondition>();
            responseSelectors = GetComponents<IResponseSelector>();
        }

        #region Setup
        private void FindSelectors() {
            selectors = GetComponents<Selector>();

            foreach (Selector selector in selectors) {
                selector.setEvent += selection => {
                    SetCurrent(selection);
                };

                selector.clearEvent += () => {
                    ClearCurrent();
                };
            }
        }

        private void RunSetResponses(ISelection newSel) {
            foreach (SelectionResponse r in responses) {
                if ((r.RespondOnlyToSelectors && newSel.selector != null) || !r.RespondOnlyToSelectors)
                    r.OnSet(newSel);
            }
        }

        private void RunClearResponses(ClearInfo clearInfo) {
            foreach (SelectionResponse r in responses)
                if ((r.RespondOnlyToSelectors && clearInfo.selection.selector != null) || !r.RespondOnlyToSelectors)
                    r.OnClear(clearInfo);
        }

        #endregion

        private bool PassedAllConditions(ISelection sel, bool clearing = false) {
            if (conditions != null) {
                foreach (SelectionCondition condition in conditions) {
                    if (!condition.GetValue(sel) && ((clearing && !condition.allowClearing) || !clearing)) {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool CheckRepeatedSelection(ISelection newSel) {
            return (!allowRepeatedSelection && newSel != current) || allowRepeatedSelection;
        }

        private ISelection GetLatestResponseSelection(ISelection newSel) {
            foreach (IResponseSelector cs in responseSelectors) {
                ISelection responseSelection = cs.GetResponseSelection(newSel);
                if (responseSelection != null)
                    return responseSelection;
            }
            return null;
        }

        public void SetCurrent(ISelection newSelection) {
            if (!PassedAllConditions(newSelection)) {
                if (clearOnFailConditions)
                    ClearCurrent();
                return;
            }

            if (!CheckRepeatedSelection(newSelection))
                return;

            ClearCurrent(true);

            ISelection responseSelection = GetLatestResponseSelection(newSelection);

            if (responseSelection != null)
                SetCurrent(responseSelection);
            else {
                current = newSelection;
                current.Select();
                RunSetResponses(newSelection);
                OnUpdateSelection?.Invoke(newSelection.gameObject);
            }

        }

        public void ClearCurrent(bool setting = false, bool noSelector = false) {
            if (current?.gameObject != null) {
                if (!PassedAllConditions(current, true)) {
                    return;
                }

                ClearInfo info = new ClearInfo(current, setting);
                if (noSelector)
                    current.selector = null;

                RunClearResponses(info);
                current.Deselect();
                OnClearSelection?.Invoke();
                current = null;
            }
        }
    }
}