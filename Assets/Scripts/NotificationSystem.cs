using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NotificationSystem : MonoBehaviour {
    public float FadeOutTime = 1f;

    public GUIStyle titleStyle = new GUIStyle {
        
    };

    public GUIStyle descriptionStyle = new GUIStyle {

    };

    private List<Message> Messages = new List<Message>();

    private void Update() {
        foreach (Message msg in Messages) {
            msg.ElapsedTime += Time.deltaTime;
        }
        Messages.RemoveAll(msg => msg.ElapsedTime > msg.Duration + FadeOutTime);
    }

    private void OnGUI() {
        GUILayout.BeginVertical();
        foreach (Message msg in Messages) {
            GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, Mathf.Lerp(1, 0, msg.ElapsedTime < msg.Duration - FadeOutTime ? 0 : 1 - (msg.Duration + FadeOutTime - msg.ElapsedTime)));
            GUILayout.BeginVertical("box");
            if (msg.Title != null) {
                GUI.contentColor = msg.TitleColor;
                GUILayout.Label(msg.Title, titleStyle);
            }
            if (msg.Description != null) {
                GUI.contentColor = msg.DescriptionColor;
                GUILayout.Label(msg.Description, descriptionStyle);
            }
            GUILayout.EndVertical();
        }
        GUI.contentColor = Color.white;
        GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b);
        GUILayout.EndVertical();
    }

    public void Notify(string title, string description) {
        AddNotification(title, description, Color.white);
    }

    public void Warning(string title, string description) {
        AddNotification(title, description, Color.yellow);
    }

    public void Error(string title, string description) {
        AddNotification(title, description, new Color(1f, 0.35f, 0.35f));
    }

    public void AddNotification(string title, string description, Color? textColor = null, float duration = 0) {
        if (textColor == null) {
            Messages.Add(new Message(title, description, duration));
        } else {
            Messages.Add(new Message(title, description, textColor, textColor, duration));
        }
    }
}

class Message {
    public string Title;
    public string Description;
    public Color TitleColor;
    public Color DescriptionColor;
    public float Duration;
    public float ElapsedTime = 0;

    public Message(string title, string description, float duration = 0) : this(title, description, null, null, duration) { }

    public Message(string title, string description, Color? titleColor, Color? descriptionColor, float duration = 0) {
        Title = title;
        Description = description;
        TitleColor = titleColor ?? Color.white;
        DescriptionColor = descriptionColor ?? Color.white;
        Duration = duration == 0 ? 3f : duration;
    }
}