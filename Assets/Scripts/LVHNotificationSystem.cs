using System;
using System.Collections.Generic;
using UnityEngine;

public class LVHNotificationSystem : MonoBehaviour {

    public static LVHNotificationSystem Instance;

    private List<Notification> displayedNotifications;

    private void Awake() {
        this.displayedNotifications = new List<Notification>();

        Instance = this;
    }

    public bool SendNotification(Notification notification) {
        try {
            this.displayedNotifications.Add(notification);
            return true;
        } catch {
            return false;
        }
    }

    private void OnGUI() { 
        for(int i = 0; i < this.displayedNotifications.Count; i++) {
            Notification not = this.displayedNotifications[i];
            GUI.Box(new Rect(0, 0, Screen.width / 16 * 3, Screen.width / 24), not.Text, not.ColorStyle);
            if (not.DisplayTimeElapsed > not.DisplayTime) {
                displayedNotifications.RemoveAt(i);
            }
        } 
    }

    private void Update() {
        foreach (Notification not in displayedNotifications) {
            not.DisplayTimeElapsed += Time.deltaTime;
        }
    }
}

public class Notification {
    public string Text;
    public Color Color;
    public GUIStyle ColorStyle;
    public float DisplayTime;
    public float DisplayTimeElapsed;

    public Notification(string text, Color color, float time) {
        this.Text = text;
        this.Color = color;
        setupColorStyle(color);
        this.DisplayTime = time;
        this.DisplayTimeElapsed = 0;
    }

    private void setupColorStyle(Color color) {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.Apply();
        this.ColorStyle = new GUIStyle();
        this.ColorStyle.normal.background = texture;
        this.ColorStyle.margin = new RectOffset(10, 10, 10, 10);
        this.ColorStyle.fontSize = 20;
        this.ColorStyle.alignment = TextAnchor.MiddleCenter;
        this.ColorStyle.padding = new RectOffset(5, 5, 5, 5);
    }
}

public class NotificationContent {
    public static Color SuccessColor = Color.green;
    public static Color FailureColor = Color.red;

    public static NotificationContent LogUploadComplete = new NotificationContent("Log upload complete. Opening Analyzer.", SuccessColor);
    public static NotificationContent LogLinkCopied = new NotificationContent("Logfile Analyzer link copied to clipboard.", SuccessColor);
    public static NotificationContent LogTooLong = new NotificationContent("Error: log is too long.", FailureColor);

    public string Text;
    public Color Color;
    
    public NotificationContent(string text, Color color) {
        this.Text = text;
        this.Color = color;
    }
}