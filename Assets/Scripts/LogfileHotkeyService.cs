﻿using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using UnityEngine;

using Newtonsoft.Json;

public class LogfileHotkeyService : MonoBehaviour {

    public KMModSettings ModSettings;
    public KMGameInfo GameInfo;

    class Settings {
        public string keyViewLogfile;
        public string keyClearLogfile;
        public string keyViewLastBombLogfile;
        public bool? copyLogfileLinkWhenViewed;
    }
    Settings keySettings;

    KeyCode viewCode;
    bool[] viewModifiers; // CTRL ALT WINDOWS SHIFT
    KeyCode clearCode;
    bool[] clearModifiers;
    KeyCode viewLastBombCode;
    bool[] viewLastBombModifiers;

    public void Start() {
        string settings = string.Join("\n", ModSettings.Settings.Split('\n').Where(x => !x.Trim().StartsWith("//")).ToArray());
        keySettings = JsonConvert.DeserializeObject<Settings>(settings);
        if (keySettings.keyViewLogfile == null || keySettings.keyClearLogfile == null || keySettings.keyViewLastBombLogfile == null || keySettings.copyLogfileLinkWhenViewed == null) {
            WriteDefaultSettings();
            Settings newSettings = new Settings();
            newSettings.keyViewLogfile = keySettings.keyViewLogfile != null ? keySettings.keyViewLogfile : "_F8";
            newSettings.keyClearLogfile = keySettings.keyClearLogfile != null ? keySettings.keyClearLogfile : "_F9";
            newSettings.keyViewLastBombLogfile = keySettings.keyViewLastBombLogfile != null ? keySettings.keyViewLastBombLogfile : "_F7";
            newSettings.copyLogfileLinkWhenViewed = keySettings.copyLogfileLinkWhenViewed != null ? keySettings.copyLogfileLinkWhenViewed : true;
            keySettings = newSettings;
        }
        SetupKey(out viewCode, out viewModifiers, keySettings.keyViewLogfile, KeyCode.F8, new bool[4] { false, false, false, true });
        SetupKey(out clearCode, out clearModifiers, keySettings.keyClearLogfile, KeyCode.F9, new bool[4] { false, false, false, true });
        SetupKey(out viewLastBombCode, out viewLastBombModifiers, keySettings.keyViewLastBombLogfile, KeyCode.F7, new bool[4] { false, false, false, true });
        Debug.LogFormat("[LogfileHotkey] \"View Log\" key combination set to: \"{0}{1}{2}{3}{4}\".", viewModifiers[0] ? "CTRL+" : "", viewModifiers[1] ? "ALT+" : "", viewModifiers[2] ? "WIN/CMD+" : "", viewModifiers[3] ? "SHIFT+" : "", viewCode);
        Debug.LogFormat("[LogfileHotkey] \"View Last Bomb Log\" key combination set to: \"{0}{1}{2}{3}{4}\".", viewLastBombModifiers[0] ? "CTRL+" : "", viewLastBombModifiers[1] ? "ALT+" : "", viewLastBombModifiers[2] ? "WIN/CMD+" : "", viewLastBombModifiers[3] ? "SHIFT+" : "", viewLastBombCode);
        Debug.LogFormat("[LogfileHotkey] \"Clear Log\" key combination set to: \"{0}{1}{2}{3}{4}\".", clearModifiers[0] ? "CTRL+" : "", clearModifiers[1] ? "ALT+" : "", clearModifiers[2] ? "WIN/CMD+" : "", clearModifiers[3] ? "SHIFT+" : "", clearCode);
        Debug.LogFormat("[LogfileHotkey] Auto-link copying set to: \"{0}\".", keySettings.copyLogfileLinkWhenViewed);
        GameInfo.OnStateChange += delegate (KMGameInfo.State state) {
            if (state.Equals(KMGameInfo.State.Gameplay)) {
                LogfileUploader.Instance.loggingEnabled = true;
                LogfileUploader.Instance.ClearLastBombLog();
                Debug.LogFormat("[LogfileHotkey] Log capturing enabled.");
            } else {
                LogfileUploader.Instance.loggingEnabled = false;
                Debug.LogFormat("[LogfileHotkey] Log capturing disabled.");
            }
        };
    }

    public void Update() {
        if (Input.GetKeyDown(viewCode) && CheckModifiers(ref viewModifiers)) {
            LogfileUploader.Instance.Open(keySettings.copyLogfileLinkWhenViewed != null && (bool)keySettings.copyLogfileLinkWhenViewed);
        }
        if (Input.GetKeyDown(clearCode) && CheckModifiers(ref clearModifiers)) {
            LogfileUploader.Instance.Clear();
            Debug.LogFormat("[LogfileHotkey] Cleared internal log file.");
        }
        if (Input.GetKeyDown(viewLastBombCode) && CheckModifiers(ref viewLastBombModifiers)) {
            LogfileUploader.Instance.OpenLastBomb(keySettings.copyLogfileLinkWhenViewed != null && (bool)keySettings.copyLogfileLinkWhenViewed);
        }
    }

    private void SetupKey(out KeyCode keyCode, out bool[] modifiers, string keyCombination, KeyCode defaultKeyCode, bool[] defaultKeyModifiers) {
        if (keyCombination != null && Regex.IsMatch(keyCombination, @"^[\^!@_]*([a-zA-Z0-9]+)$")) {
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

    private string GetModSettingsPath(bool directory) {
        string ModSettingsDirectory = Path.Combine(Application.persistentDataPath, "Modsettings");
        return directory ? ModSettingsDirectory : Path.Combine(ModSettingsDirectory, "LogfileHotkey-settings.txt");
    }

    public bool WriteDefaultSettings() {
        Debug.LogFormat("[LogfileHotkey] Writing Settings File: {0}", GetModSettingsPath(false));
        try {
            if (!Directory.Exists(GetModSettingsPath(true))) {
                Directory.CreateDirectory(GetModSettingsPath(true));
            }

            string settings = string.Format(
@"{{
  // These lines are ignored as comments.
  // Use these two settings to change the hotkeys for viewing or clearing the log file.
  // Note that ""clearing"" the logfile does not affect the file on your system, only the internal copy.
  // These hotkeys should be formated with optional modifier keys followed by a valid non-modifier KeyCode.
  // Examples:
  //  - ""^X"" = Ctrl+X
  //  - ""Escape"" = Esc
  //  - ""!@Tab"" = Alt+Windows/Command+Tab
  // See https://docs.unity3d.com/ScriptReference/KeyCode.html for a KeyCode Reference.
  // Modifier Keys are '^'=Control, '_'=Shift, '!'=Alt, '@'=Windows/Command
  // Note that any invalid hotkey will result in the default hotkey. Look at the log for more information. Also, don't use Mouse* or Joystick* KeyCodes unless you know exactly what you are doing.

  ""keyViewLogfile"": ""{0}"",
  ""keyClearLogfile"": ""{1}"",
  ""keyViewLastBombLogfile"": ""{2}"",
  ""copyLogfileLinkWhenViewed"":  {3}
}}", keySettings.keyViewLogfile != null ? keySettings.keyViewLogfile : "_F8",
keySettings.keyClearLogfile != null ? keySettings.keyClearLogfile : "_F9",
keySettings.keyViewLastBombLogfile != null ? keySettings.keyViewLastBombLogfile : "_F7",
keySettings.copyLogfileLinkWhenViewed != null ? keySettings.copyLogfileLinkWhenViewed.ToString().ToLower() : "true"
);
            File.WriteAllText(GetModSettingsPath(false), settings);
            return true;
        } catch (Exception ex) {
            Debug.LogFormat("[LogfileHotkey] Failed to Create settings file due to Exception:\n{0}\nStack Trace:\n{1}", ex.Message,
                ex.StackTrace);
            return false;
        }
    }
}