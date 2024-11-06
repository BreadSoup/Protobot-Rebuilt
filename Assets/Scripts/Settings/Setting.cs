using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

namespace Protobot {
    [Serializable]
    public class Setting : MonoBehaviour {
        public string currentValue;

        public object RealValue {
            get {
                if (int.TryParse(currentValue, out int intResult))
                    return intResult;
                else if (float.TryParse(currentValue, out float floatResult))
                    return floatResult;
                else if (bool.TryParse(currentValue, out bool boolResult))
                    return boolResult;

                return currentValue;
            }
        }

        private string SavedValue => PlayerPrefs.GetString(name);

        public IntEvent OnUpdateIntValue;
        public BoolEvent OnUpdateBoolValue;
        public FloatEvent OnUpdateFloatValue;

        public IntEvent OnInitIntValue;
        public BoolEvent OnInitBoolValue;
        public FloatEvent OnInitFloatValue;

        public void Start() {
            InitValue();
        }

        public void InitValue() {
            if (SavedValue.Length != 0) {
                SetValue(SavedValue);
            }

            Type type = RealValue.GetType();

            if (type == typeof(int)) {
                OnInitIntValue.Invoke((int)RealValue);
            }
            else if (type == typeof(float)) {
                OnInitFloatValue.Invoke((float)RealValue);
            }
            else if (type == typeof(bool)) {
                OnInitBoolValue.Invoke((bool)RealValue);
            }
        }

        public void SaveValue() {
            PlayerPrefs.SetString(name, currentValue);
            Debug.Log(currentValue);
        }

        public void SetValue(string newValue) {
            currentValue = newValue;

            Type type = RealValue.GetType();

            if (type == typeof(int)) {
                OnUpdateIntValue.Invoke((int)RealValue);
            }
            else if (type == typeof(float)) {
                OnUpdateFloatValue.Invoke((float)RealValue);
            }
            else if (type == typeof(bool)) {
                OnUpdateBoolValue.Invoke((bool)RealValue);
            }

            SaveValue();
        }

        public void SetValue(int newValue) => SetValue(newValue.ToString());
        public void SetValue(float newValue) => SetValue(newValue.ToString());
        public void SetValue(bool newValue) => SetValue(newValue.ToString());
    }
}
