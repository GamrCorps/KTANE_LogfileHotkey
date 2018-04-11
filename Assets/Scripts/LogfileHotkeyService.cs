using System;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;

using Newtonsoft.Json;

public class LogfileHotkeyService : MonoBehaviour {

    public KMModSettings ModSettings;

    class Settings {
        public string keyViewLogfile;
        public string keyClearLogfile;
    }
    Settings keySettings;

    KeyCode viewCode;
    bool[] viewModifiers; // CTRL ALT WINDOWS SHIFT
    KeyCode clearCode;
    bool[] clearModifiers;

    public void Start() {
        string settings = string.Join("\n", ModSettings.Settings.Split('\n').Where(x => !x.Trim().StartsWith("//")).ToArray());
        keySettings = JsonConvert.DeserializeObject<Settings>(settings);
        SetupKey(out viewCode, out viewModifiers, keySettings.keyViewLogfile, KeyCode.F8, new bool[4] { false, false, false, true });
        SetupKey(out clearCode, out clearModifiers, keySettings.keyClearLogfile, KeyCode.F9, new bool[4] { false, false, false, true });
        Debug.LogFormat("[LogfileHotkey] \"View Log\" key combination set to: \"{0}{1}{2}{3}{4}\".", viewModifiers[0] ? "CTRL+" : "", viewModifiers[1] ? "ALT+" : "", viewModifiers[2] ? "WIN/CMD+" : "", viewModifiers[3] ? "SHIFT+" : "", viewCode);
        Debug.LogFormat("[LogfileHotkey] \"Clear Log\" key combination set to: \"{0}{1}{2}{3}{4}\".", clearModifiers[0] ? "CTRL+" : "", clearModifiers[1] ? "ALT+" : "", clearModifiers[2] ? "WIN/CMD+" : "", clearModifiers[3] ? "SHIFT+" : "", clearCode);
    }

    public void Update() {
        if (Input.GetKeyDown(viewCode) && CheckModifiers(ref viewModifiers)) {
            LogfileUploader.Instance.Open();
        }
        if (Input.GetKeyDown(clearCode) && CheckModifiers(ref clearModifiers)) {
            LogfileUploader.Instance.Clear();
            Debug.LogFormat("[LogfileHotkey] Cleared internal log file.");
        }
    }

    private void SetupKey(out KeyCode keyCode, out bool[] modifiers, string keyCombination, KeyCode defaultKeyCode, bool[] defaultKeyModifiers) {
        if (Regex.IsMatch(keyCombination, @"^[\^!@_]*([a-zA-Z0-9]+)$")) {
            modifiers = new bool[4] {
                keyCombination.Contains("^"),
                keyCombination.Contains("!"),
                keyCombination.Contains("@"),
                keyCombination.Contains("_"),
            };
            try {
                keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), Regex.Match(keyCombination, @"^[\^!@_]*([a-zA-Z0-9]+)$").Groups[1].ToString());
            } catch (ArgumentException) {
                Debug.LogFormat("[LogfileHotkey] Invalid KeyCode in key combination: \"{0}\".", keyCombination);
                modifiers = defaultKeyModifiers;
                keyCode = defaultKeyCode;
            }
        } else {
            Debug.LogFormat("[LogfileHotkey] Invalid key combination in settings: \"{1}\".", keyCombination);
            modifiers = defaultKeyModifiers;
            keyCode = defaultKeyCode;
        }
    }

    private bool CheckModifiers(ref bool[] modifiers) {
        return (modifiers[0] ? Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) : true) &&
               (modifiers[1] ? Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr) : true) &&
               (modifiers[2] ? Input.GetKey(KeyCode.LeftWindows) || Input.GetKey(KeyCode.RightWindows) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand) || Input.GetKey(KeyCode.LeftApple) || Input.GetKey(KeyCode.RightApple) : true) &&
               (modifiers[3] ? Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) : true);
    }
}