using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class QuipBattleGameQuestions {
    List<string> list;
    Random rng = new Random();

    public QuipBattleGameQuestions()
    {
        quipBattleQuestions qBQ = JsonConvert.DeserializeObject<quipBattleQuestions>(File.ReadAllText("quipBattleQuestions.json"));
        list = qBQ.prompts;
    }

    public string GetPrompt() {
        return list[rng.Next(0, list.Count)];
    }
}

[Serializable]
public class quipBattleQuestions {
  public List<string> prompts;
}
