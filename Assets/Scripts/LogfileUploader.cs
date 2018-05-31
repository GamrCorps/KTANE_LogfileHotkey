using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

public class LogfileUploader : MonoBehaviour {

    public static LogfileUploader Instance;

    [HideInInspector]
    public bool loggingEnabled = false;

    [HideInInspector]
    public bool logPostBomb = false;

    public string Log { get; private set; }
    public string LastBombLog { get; private set; }

    [HideInInspector]
    public string analysisUrl = null;

    private string[] _blacklistedLogLines = {
       "[ServicesSteam]",
       "[BombGenerator] Instantiated EmptyComponent",
       "[BombGenerator] Filling remaining spaces with empty components.",
       "[BombGenerator] BombTypeEnum: Default",
       "[FileUtilityHelper]",
       "[MenuPage]",
       "[PlayerSettingsManager]",
       "[LeaderboardBulkSubmissionWorker]",
       "[MissionManager]",
       "[AlarmClock]",
       "[AlarmClockExtender]",
       "[Alarm Clock Extender]",
       "[StatsManager]",
       "[BombGenerator] Instantiated TimerComponent",
       "[BombGenerator] Instantiating RequiresTimerVisibility components on",
       "[BombGenerator] Instantiating remaining components on any valid face.",
       "[PrefabOverride]",
       "Tick delay:",
       "Calculated FPS: ",
       "[ModuleCameras]",
       "[TwitchPlays]",
       "(Filename:  Line: 21)",
       "[Factory]"
    };

    private string[] _gameplayBlacklistedLogLines = {
        "[PaceMaker]"
    };

    private LogService[] services = {
        new HastebinLogService()
    };

    private void Awake() {
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
        DoPost(Log, openLink, copyLink);
    }

    public void OpenLastBomb(bool copyLink, bool openLink = true) {
        analysisUrl = null;
        DoPost(LastBombLog, openLink, copyLink);
    }

    private void DoPost(string data, bool openLink, bool copyLink) {
        data = "Initialize engine version: Log Viewer Hotkey\n" + data;

        byte[] encodedData = System.Text.Encoding.UTF8.GetBytes(data);
        int dataLength = encodedData.Length;

        bool tooLong = false;

        foreach (LogService service in services) {
            string baseURL = service.BaseURL;
            int uploadLimit = service.UploadLimit;

            tooLong = false;
            if (dataLength >= uploadLimit) {
                Debug.LogFormat("[LogfileHotkey] Data ({0}B) is too long for {1} ({2}B)", dataLength, baseURL, uploadLimit);
                tooLong = true;
                continue;
            }

            Debug.LogFormat("[LogfileHotkey] Attempting to post log to {0}", baseURL);
            StartCoroutine(service.Upload(encodedData, openLink, copyLink));
        }

        if (tooLong) {
            Debug.Log("[LogfileHotkey] Error uploading log file: too long.");
        }
    }

    private void HandleLog(string message, string stackTrace, LogType type) {
        if (!logPostBomb && _gameplayBlacklistedLogLines.Any(message.StartsWith)) return;
        if (_blacklistedLogLines.Any(message.StartsWith)) return;
        if (message.StartsWith("Function ") && message.Contains(" may only be called from main thread!")) return;
        if (loggingEnabled) {
            Log += message + "\n";
            LastBombLog += message + "\n";
        }
    }
}

abstract class LogService {
    public virtual string Name { get; protected set; }
    public virtual string BaseURL { get; protected set; }
    public virtual int UploadLimit { get; protected set; }

    public abstract IEnumerator Upload(byte[] data, bool openLink, bool copyLink);
}

static class LogUploadUtils {
    public static bool OpenLink(string link) {
        if (string.IsNullOrEmpty(link)) {
            Debug.Log("[LogfileHotkey] No analysis URL available, can't open");
            return false;
        }
        Debug.Log("[LogfileHotkey] Opening link in default browser");
        Application.OpenURL(link);
        return true;
    }

    public static bool CopyLink(string link) {
        if (string.IsNullOrEmpty(link)) {
            Debug.Log("[LogfileHotkey] No analysis URL available, can't copy");
            GUIUtility.systemCopyBuffer = "LotfileHotkey Error: Check the log file for more information.";
            return false;
        }
        Debug.Log("[LogfileHotkey] Copying link into clipboard");
        GUIUtility.systemCopyBuffer = link;
        return true;
    }

    private const string LogAnalyzerLink = "https://ktane.timwi.de/More/Logfile%20Analyzer.html";

    public static string LogAnalyzerFor(string url) {
        return string.Format("{0}#url={1}", LogAnalyzerLink, url);
    }
}

class HastebinLogService : LogService {
    public HastebinLogService() {
        this.Name = "Hastebin";
        this.BaseURL = "hastebin.com";
        this.UploadLimit = 400000;
    }

    public override IEnumerator Upload(byte[] data, bool openLink, bool copyLink) {
        string url = "https://hastebin.com/documents";

        WWW www = new WWW(url, data);

        yield return www;

        if (www.error == null) {
            // example result
            // {"key":"oxekofidik"}

            string key = www.text;
            key = key.Substring(0, key.Length - 2);
            key = key.Substring(key.LastIndexOf("\"") + 1);
            string rawUrl = "https://hastebin.com/raw/" + key;

            Debug.Log("[LogfileHotkey] Hastebin: paste now available at " + rawUrl);

            string analysisUrl = LogUploadUtils.LogAnalyzerFor(rawUrl);

            if (openLink) {
                LogUploadUtils.OpenLink(analysisUrl);
            }

            if (copyLink) {
                LogUploadUtils.CopyLink(analysisUrl);
            }
        } else {
            Debug.Log("[LogfileHotkey] Error uploading to Hastebin: " + www.error);
        }

        yield break;
    }
}