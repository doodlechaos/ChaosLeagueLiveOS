using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class QuipBattleGameQuestions {

    public List<string> list = new List<string>();
    System.Random rng = new System.Random();


    private void getFileData()
    {
        string promptsPath = Path.Combine(Application.streamingAssetsPath, "quipBattlePrompts.json");
        string promptsJSON = File.ReadAllText(promptsPath);

        quipBattleQuestions qBQ = JsonConvert.DeserializeObject<quipBattleQuestions>(promptsJSON);
        list = qBQ.prompts;
    }

    public string GetPrompt() {
        if (list.Count == 0) getFileData();
        if (list.Count == 0) return "No quip prompts found";

        int index = rng.Next(0, list.Count);
        string str = list[index];
        list.RemoveAt(index);
        return str;
    }
}

[Serializable]
public class quipBattleQuestions {
    public List<string> prompts;
}
