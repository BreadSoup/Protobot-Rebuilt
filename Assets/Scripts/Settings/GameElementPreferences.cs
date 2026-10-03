using System;
using UnityEngine;
using UnityEngine.UI;

namespace Protobot {
    public class GameElementPreferences : MonoBehaviour {
        public const string PushBackKey = "GameElements.ShowPushBack";
        public const string HighStakesKey = "GameElements.ShowHighStakes";
        public const string SpinUpKey = "GameElements.ShowSpinUp";

        public Toggle pushBackToggle;
        public Toggle highStakesToggle;
        public Toggle spinUpToggle;

        public static event Action Changed;

        void Awake() {
            Bind(pushBackToggle, PushBackKey);
            Bind(highStakesToggle, HighStakesKey);
            Bind(spinUpToggle, SpinUpKey);
        }

        static void Bind(Toggle toggle, string key) {
            toggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(key, 0) != 0);
            toggle.onValueChanged.AddListener(value => {
                PlayerPrefs.SetInt(key, value ? 1 : 0);
                PlayerPrefs.Save();
                Changed?.Invoke();
            });
        }

        // Keep every generator registered so preferences never affect saved builds.
        public static bool IsVisible(PartType part) {
            string key;
            switch (part.id) {
                case "PUBA": case "BLOK": case "CGOL": case "LOAD": case "LGOL":
                    key = PushBackKey;
                    break;
                case "FELD": case "RING": case "SAKE":
                    key = HighStakesKey;
                    break;
                case "DISC":
                    key = SpinUpKey;
                    break;
                default:
                    return true;
            }
            return PlayerPrefs.GetInt(key, 0) != 0;
        }
    }
}
