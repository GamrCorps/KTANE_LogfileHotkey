using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

// Shamelessly borrowed and modified from Twitch Plays KTANE code.
// Original file by CaitSith2, red031000, and samfun123

public class LogfileUploader : MonoBehaviour {
    public static LogfileUploader Instance;

    public bool loggingEnabled = false;

    public string Log { get; private set; }
    public string LastBombLog { get; private set; }

    [HideInInspector]
    public string analysisUrl = null;

    [HideInInspector]
    public string LOGPREFIX = "[LogfileHotkey] ";

    private string[] _blacklistedLogLines = {
       "[PaceMaker]",
       "[ServicesSteam]",
       "[BombGenerator] Instantiated EmptyComponent",
       "[BombGenerator] Filling remaining spaces with empty components.",
       "[BombGenerator] BombTypeEnum: Default",
       "[StatsManager]",
       "[FileUtilityHelper]",
       "[MenuPage]",
       "[PlayerSettingsManager]",
       "[LeaderboardBulkSubmissionWorker]",
       "[MissionManager]",
       "[AlarmClock]",
       "[AlarmClockExtender]",
       "[Alarm Clock Extender]",
       "[BombGenerator] Instantiated TimerComponent",
       "[BombGenerator] Instantiating RequiresTimerVisibility components on",
       "[BombGenerator] Instantiating remaining components on any valid face.",
       "[PrefabOverride]",
       "Tick delay:",
       "Calculated FPS: ",
       "[ModuleCameras]",
       "[TwitchPlays]",
       "(Filename:  Line: 21)"
    };

    private OrderedDictionary domainNames = new OrderedDictionary {
		// In order of preference (favourite first)
		// The integer value is the data size limit in bytes
		{ "hastebin.com", 400000 },
        { "ktane.w00ty.com", 2000000 }
    };

    public void Awake() {
        Instance = this;
    }

    public void OnEnable() {
        Application.logMessageReceived += HandleLog;
    }

    public void OnDisable() {
        Application.logMessageReceived -= HandleLog;
    }

    public void Clear() {
        Log = "";
        LastBombLog = "";
    }

    public void ClearLastBombLog() {
        LastBombLog = "";
    }

    public void Open(bool copyLink, bool openLink = true) {
        analysisUrl = null;
        StartCoroutine(DoPost(Log, openLink, copyLink));
    }

    public void OpenLastBomb(bool copyLink, bool openLink = true) {
        analysisUrl = null;
        StartCoroutine(DoPost(LastBombLog, openLink, copyLink));
    }

    private IEnumerator DoPost(string data, bool openLink, bool copyLink) {
        // This first line is necessary as the Log Analyser uses it as an identifier
        data = "Initialize engine version: Twitch Plays\n" + data;

        byte[] encodedData = System.Text.Encoding.UTF8.GetBytes(data);
        int dataLength = encodedData.Length;

        bool tooLong = false;

        foreach (DictionaryEntry domain in domainNames) {
            string domainName = (string)domain.Key;
            int maxLength = (int)domain.Value;

            tooLong = false;
            if (dataLength >= maxLength) {
                Debug.LogFormat(LOGPREFIX + "Data ({0}B) is too long for {1} ({2}B)", dataLength, domainName, maxLength);
                tooLong = true;
                continue;
            }

            Debug.Log(LOGPREFIX + "Posting new log to " + domainName);

            string url = "https://" + domainName + "/documents";

            WWW www = new WWW(url, encodedData);

            yield return www;

            if (www.error == null) {
                // example result
                // {"key":"oxekofidik"}

                string key = www.text;
                key = key.Substring(0, key.Length - 2);
                key = key.Substring(key.LastIndexOf("\"") + 1);
                string rawUrl = "https://" + domainName + "/raw/" + key;

                Debug.Log(LOGPREFIX + "Paste now available at " + rawUrl);

                analysisUrl = LogAnalyserFor(rawUrl);

                if (openLink) {
                    OpenLink();
                }

                if (copyLink) {
                    CopyLink();
                }

                break;
            } else {
                Debug.Log(LOGPREFIX + "Error: " + www.error);
            }
        }

        if (tooLong) {
            Debug.Log(LOGPREFIX + "Error uploading log file.");
        }

        yield break;
    }

    public bool OpenLink() {
        if (string.IsNullOrEmpty(analysisUrl)) {
            Debug.Log(LOGPREFIX + "No analysis URL available, can't open.");
            return false;
        }
        Debug.Log(LOGPREFIX + "Opening link in default browser.");
        Application.OpenURL(analysisUrl);
        return true;
    }

    public void CopyLink() {
        GUIUtility.systemCopyBuffer = analysisUrl;
    }

    private void HandleLog(string message, string stackTrace, LogType type) {
        if (_blacklistedLogLines.Any(message.StartsWith)) return;
        if (message.StartsWith("Function ") && message.Contains(" may only be called from main thread!")) return;
        if (loggingEnabled) {
            Log += message + "\n";
            LastBombLog += message + "\n";
        }
    }

    public string LogAnalyser = "https://ktane.timwi.de/More/Logfile%20Analyzer.html";

    public string LogAnalyserFor(string url) {
        return string.Format(LogAnalyser + "#url={0}", url);
    }
}