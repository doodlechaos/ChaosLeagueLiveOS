using System.Collections;
using System.Collections.Generic;
using TwitchLib.Api.Helix.Models.Predictions.CreatePrediction;
using UnityEngine;

//public enum PredictionType { KingDuration}

[CreateAssetMenu]
public class PredictionObj : ScriptableObject
{
    [SerializeField] public PredictionType PredictionType;

    [SerializeField] public string Title;

    [SerializeField] private List<string> _outcomeTitles;

    [SerializeField] public int PredictionWindowSec;

    [SerializeField] public int MinutesDuration;
    [SerializeField] public int Value;

    public Outcome[] GetOutcomes()
    {
        List<Outcome> outcomes = new List<Outcome>();
        foreach(var outcomeTitle in _outcomeTitles)
            outcomes.Add(new Outcome() { Title = outcomeTitle.TruncateString(25) });
        return outcomes.ToArray();
    }


}
