using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class QuipBattleGameQuestions {
    List<string> list = new List<string>();
    Random rng = new Random();

    private void getFileData()
    {
        quipBattleQuestions qBQ = JsonConvert.DeserializeObject<quipBattleQuestions>(File.ReadAllText("quipBattleQuestions.json"));
        list = qBQ.prompts;
    }

    public string GetPrompt() {
        if (list.Count == 0) getFileData();
        if (list.Count == 0) return "No quip questions found";

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
