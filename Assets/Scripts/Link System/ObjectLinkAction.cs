using System;
using Protobot.InputEvents;
using UnityEngine;
using UnityEngine.Events;

namespace Protobot
{
    public abstract class ObjectLinkAction : MonoBehaviour
    {
        [SerializeField]
        public UnityEvent OnExecute;

        [SerializeField]
        public ObjectLink refObj;

        [SerializeField]
        private InputEvent input;

        [SerializeField]
        private bool allowOverUI;

        public abstract void Execute();

        private void Awake()
        {
            if (!(input != null))
            {
                return;
            }
            InputEvent inputEvent = input;
            inputEvent.performed = (Action)Delegate.Combine(inputEvent.performed, (Action)delegate
            {
                //if ((!allowOverUI && !MouseInput.overUI) || allowOverUI)
                //{
                    Execute();
                //}
            });
        }
    }
}