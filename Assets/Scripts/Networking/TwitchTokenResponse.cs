using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class TwitchTokenResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }
    [JsonProperty("token_type")]
    public string TokenType { get; set; }
    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }
    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; }
    [JsonProperty("scope")]
    public string[] Scope { get; set; }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("AccessToken: " + (AccessToken ?? ""));
        sb.AppendLine("TokenType: " + (TokenType ?? ""));
        sb.AppendLine("ExpiresIn: " + ExpiresIn);
        sb.AppendLine("RefreshToken: " + (RefreshToken ?? ""));
        if (Scope != null && Scope.Length > 0)
        {
            sb.Append("Scopes: ");
            foreach (var s in Scope)
                sb.Append($" {s}");
        }

        return sb.ToString();
    }
}