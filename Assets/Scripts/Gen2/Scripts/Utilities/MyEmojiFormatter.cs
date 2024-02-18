using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MyEmojiFormatter
{

    private static Dictionary<string,string> markdownMap = new Dictionary<string,string>();

    public static void LoadMapJSON(string mapJSON)
    {
        markdownMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(mapJSON);
    }

    public static string ReplaceShortcodeWithEmoji(string inputText)
    {
        // 1. Check for any emoji markdown and replace it with the real emoji
        foreach (string key in markdownMap.Keys)
        {
            string shortCode = ":" + key + ":";
            if (inputText.Contains(shortCode))
            {
                //CLDebug.Inst.Log("found match");
                inputText = inputText.Replace(shortCode, markdownMap[key]);
            }
        }

        return inputText; 
    }

}


