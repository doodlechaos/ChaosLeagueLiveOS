using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class CLDebug : MonoBehaviour
{
    public static CLDebug Inst;

    private bool overlayEnabled = false;

    [SerializeField] private bool loggingEnabled = false;

    public void Initialize()
    {
        Inst = this;
        Application.logMessageReceived += HandleUnityOriginatingLog;
    }
    // Update is called once per frame
    void Update()
    {
        if (!overlayEnabled)
            return;
    }

    private void HandleUnityOriginatingLog(string logString, string stackTrace, LogType type)
    {
        // Process the log message here
        // 'logString' contains the log message
        // 'stackTrace' contains the stack trace (if applicable)
        // 'type' contains the type of log message (error, warning, or log)

        if (type == LogType.Error)
        {
            // Here, you can handle the error messages as you wish.
            // For example, you can log them, display them in-game, or take any other action.
            ReportError("ERROR", "LOG STRING: \n" + logString + "\nSTACK TRACE: \n" + stackTrace);
        }

        else if (type == LogType.Exception)
        {
            ReportError("EXCEPTION", "LOG STRING: \n" + logString + "\nSTACK TRACE: \n" + stackTrace);
        }
    }

    public void Log(string message, Type context = null)
    {
        if (!loggingEnabled)
            return;

        Debug.Log(message);

    }

    public void LogError(string message, Type context = null)
    {
        if (!loggingEnabled)
            return;

        Debug.LogError(message);

        ReportError("LogError", message);

    }

    public void ReportError(string label, string message)
    {
        if (AppConfig.inst == null)
            return;

        if (!AppConfig.inst.GetB("Send_Error_Webhooks"))
            return;

        _ = ReportToDiscord(label, message, AppConfig.inst.GetS("ERROR_WEBHOOK_URL"));
        Debug.Log("REPORTED: " + label + " " + message);
    }

    public void ReportDonation(string label, string message)
    {
        if (AppConfig.inst == null)
            return;

        if (!AppConfig.inst.GetB("Send_Donation_Webhooks"))
            return;

        _ = ReportToDiscord(label, message, AppConfig.inst.GetS("DONATION_WEBHOOK_URL"));
        Debug.Log("REPORTED: " + label + " " + message);
    }

    public async Task ReportToDiscord(string label, string message, string webhookURL)
    {

        try
        {
            if (webhookURL == null || webhookURL.Length <= 0)
                return;

            using (HttpClient httpClient = new HttpClient())
            {
                // Prepare the payload for the webhook
                var payload = new
                {
                    username = label,
                    content = message,
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(webhookURL, content);

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    Debug.Log($"Message sent to {webhookURL} successfully!");
                }
                else
                {
                    //Can't send this as an error message to avoid infinite loop
                    Debug.Log($"Error sending message to {webhookURL}. Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"A error occurred while sending message to webhook: {ex.Message}");
        }
    }

}
