using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class TimwiUploadTest: MonoBehaviour {

	void Start () {
        StartCoroutine(Upload2());
	}

    public IEnumerator Upload() {
        StreamReader reader = new StreamReader("Assets/Tests/output_log.txt");
        string stringData = reader.ReadToEnd();
        reader.Close();

        string url = "https://ktane.timwi.de/upload-log";
        //string url = "http://127.0.0.1:8080";

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("log", stringData, null, "output_log.txt"));
        //formData.Add(new MultipartFormDataSection("log", stringData, "text/plain"));

        Debug.Log(Encoding.UTF8.GetString(formData[0].sectionData));

        UnityWebRequest www = UnityWebRequest.Post(url, formData);
        
        yield return www.Send();

        if (www.isNetworkError || www.isHttpError) {
            Debug.Log("Error: " + www.error + " (" + www.responseCode + ")");
        } else {
            string rawUrl = www.downloadHandler.text;

            Debug.Log("Response Text: " + rawUrl);
        }

        Debug.Log("Response Text: " + www.downloadHandler.text);

        yield break;
    }

    public IEnumerator Upload2() {
        StreamReader reader = new StreamReader("Assets/Tests/output_log.txt");
        string stringData = reader.ReadToEnd();
        reader.Close();

        //string url = "https://ktane.timwi.de/upload-log";
        string url = "http://127.0.0.1:8080";

        List<IMultipartFormSection> form = new List<IMultipartFormSection>
        {
           new MultipartFormFileSection("log", stringData, null, "output_log.txt")
        };
        // generate a boundary then convert the form to byte[]
        byte[] boundary = UnityWebRequest.GenerateBoundary();
        byte[] formSections = UnityWebRequest.SerializeFormSections(form, boundary);
        // my termination string consisting of CRLF--{boundary}--
        byte[] terminate = Encoding.UTF8.GetBytes(String.Concat("\r\n--", Encoding.UTF8.GetString(boundary), "--"));
        // Make my complete body from the two byte arrays
        byte[] body = new byte[formSections.Length + terminate.Length];
        Buffer.BlockCopy(formSections, 0, body, 0, formSections.Length);
        Buffer.BlockCopy(terminate, 0, body, formSections.Length, terminate.Length);
        // Set the content type - NO QUOTES around the boundary
        string contentType = String.Concat("multipart/form-data; boundary=", Encoding.UTF8.GetString(boundary));
        // Make my request object and add the raw body. Set anything else you need here
        UnityWebRequest www = new UnityWebRequest();
        UploadHandler uploader = new UploadHandlerRaw(body);
        uploader.contentType = contentType;
        www.uploadHandler = uploader;

        www.downloadHandler = new DownloadHandlerBuffer();

        www.method = "POST";
        www.url = url;

        yield return www.Send();

        if (www.isNetworkError || www.isHttpError) {
            Debug.Log("Error: " + www.error + " (" + www.responseCode + ")");
        } else {
            string rawUrl = www.downloadHandler.text;

            Debug.Log("Response Text: " + rawUrl);
        }

        Debug.Log("Response Text: " + www.downloadHandler.text);

        yield break;
    }
}
