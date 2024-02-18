using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api.Helix.Models.Predictions;
using UnityEngine;


public enum PredictionType { kingsChoice, rebellion, lava, water, legendary, ticketsOverUnder}
public class AutoPredictions : MonoBehaviour
{
    [SerializeField] private List<PredictionObj> _predictions;
    private int _currPredictionIndex = 0;
    private Prediction _runningPrediction = null;

    //[SerializeField] private bool _initPredictionsButton;

    private bool _kingWordFlag;
    private bool _rebellionFlag;
    private bool _lavaFlag; 
    private bool _waterFlag;
    private bool _legendaryFlag;

    private WaitForSeconds _wait1Sec = new WaitForSeconds(1);

    private void Start()
    {
        InitAutoPredictions(); 
    }


    public void InitAutoPredictions()
    {
        StartCoroutine(PredictionsLoop()); 
    }

    private IEnumerator PredictionsLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(60 * 2);

            //Choose the next prediction type
            _currPredictionIndex = (_currPredictionIndex + 1) % _predictions.Count;
            PredictionObj nextPred = _predictions[_currPredictionIndex];

            _ = StartPrediction(nextPred);

            if (nextPred.PredictionType == PredictionType.kingsChoice)
                yield return RunKingsChoicePrediction(nextPred);
            else if (nextPred.PredictionType == PredictionType.rebellion)
                yield return RunRebellionPrediction(nextPred);
            else if (nextPred.PredictionType == PredictionType.lava)
                yield return RunLavaPrediction(nextPred);
            else if (nextPred.PredictionType == PredictionType.water)
                yield return RunWaterPrediction(nextPred);
            else if (nextPred.PredictionType == PredictionType.legendary)
                yield return RunLegendaryPrediction(nextPred);
        }
    }
    private IEnumerator RunKingsChoicePrediction(PredictionObj predObj)
    {
        DateTime startTime = DateTime.Now;
        _kingWordFlag = false;
        while (DateTime.Now < startTime.AddMinutes(predObj.MinutesDuration))
        {
            yield return _wait1Sec;
            if (_kingWordFlag)
                break;
        }
        string winningOutcomeId;
        if (_kingWordFlag)
            winningOutcomeId = _runningPrediction.Outcomes[0].Id;
        else
            winningOutcomeId = _runningPrediction.Outcomes[1].Id;

        FinishPrediction(winningOutcomeId);
        yield return null;
    }

    private IEnumerator RunRebellionPrediction(PredictionObj predObj)
    {
        DateTime startTime = DateTime.Now;
        _rebellionFlag = false;
        while (DateTime.Now < startTime.AddMinutes(predObj.MinutesDuration))
        {
            yield return _wait1Sec;
            if (_rebellionFlag)
                break;
        }
        string winningOutcomeId;
        if (_rebellionFlag)
            winningOutcomeId = _runningPrediction.Outcomes[0].Id;
        else
            winningOutcomeId = _runningPrediction.Outcomes[1].Id;
        
        FinishPrediction(winningOutcomeId); 
        yield return null; 
    }
    private IEnumerator RunLavaPrediction(PredictionObj predObj)
    {
        DateTime startTime = DateTime.Now;
        _lavaFlag = false;
        while (DateTime.Now < startTime.AddMinutes(predObj.MinutesDuration))
        {
            yield return _wait1Sec;
            if (_lavaFlag)
                break;
        }
        string winningOutcomeId;
        if (_lavaFlag)
            winningOutcomeId = _runningPrediction.Outcomes[0].Id;
        else
            winningOutcomeId = _runningPrediction.Outcomes[1].Id;
        

        FinishPrediction(winningOutcomeId);
        yield return null;
    }
    private IEnumerator RunWaterPrediction(PredictionObj predObj)
    {
        DateTime startTime = DateTime.Now;
        _waterFlag = false;
        while (DateTime.Now < startTime.AddMinutes(predObj.MinutesDuration))
        {
            yield return _wait1Sec;
            if (_waterFlag)
                break;
        }
        string winningOutcomeId;
        if (_waterFlag)
            winningOutcomeId = _runningPrediction.Outcomes[0].Id;
        else
            winningOutcomeId = _runningPrediction.Outcomes[1].Id;

        FinishPrediction(winningOutcomeId);
        yield return null;
    }

    private IEnumerator RunLegendaryPrediction(PredictionObj predObj)
    {
        DateTime startTime = DateTime.Now;
        _legendaryFlag = false;
        while (DateTime.Now < startTime.AddMinutes(predObj.MinutesDuration))
        {
            yield return _wait1Sec;
            if (_legendaryFlag)
                break;
        }
        string winningOutcomeId;
        if (_legendaryFlag)
            winningOutcomeId = _runningPrediction.Outcomes[0].Id;
        else
            winningOutcomeId = _runningPrediction.Outcomes[1].Id;

        FinishPrediction(winningOutcomeId);
        yield return null;
    }

    public void FinishPrediction(string winningOutcomeID)
    {
        _ = TwitchApi.FinishPrediction(_runningPrediction.Id, winningOutcomeID); 
        _runningPrediction = null; 
    }

    public async Task StartPrediction(PredictionObj predObj)
    {
        Debug.Log("Inside start prediction task"); 
        try
        {
            _runningPrediction = await TwitchApi.StartPrediction(predObj);
        }
        catch (Exception ex)
        {
            Debug.Log("Failed to start prediction. Likely because one is already running. Cancelling all predictions. \n" + ex.Message);
            try
            {
                await TwitchApi.CancelAllPredictions();
            }catch(Exception ex2)
            {
                Debug.Log("Failed to cancel all predictions. \n" + ex2.Message);
            }
        }
        
    }

    public void KingWordSignal()
    {
        _kingWordFlag = true; 
    }

    public void RebellionSignal()
    {
        _rebellionFlag = true; 
    }

    public void LavaSignal()
    {
        _lavaFlag = true;
    }

    public void WaterSignal()
    {
        _waterFlag = true; 
    }

    public void LegendarySignal()
    {
        _legendaryFlag = true; 
    }

    public void NewKingSignal(string newKingName, int prevKingDuration)
    {
/*        PredictionObj predObj = _predictions[_currPredictionIndex];

        predObj.Title = $"How long will {newKingName.TruncateString(14)} hold the throne?"; //Must stay below 45 characters to avoid error in api

        if (predObj.PredictionType != PredictionType.KingDuration)
            return;

        if (_runningPrediction == null)
        {
            _ = StartPrediction();
            return;
        }

        string winningOutcomeId = (prevKingDuration < predObj.Minutes * 60) ? _runningPrediction.Outcomes[0].Id : _runningPrediction.Outcomes[1].Id;
        FinishPrediction(winningOutcomeId); */
    }


    private string GetOutcomeIdByTitle(Outcome[] outcomes, string title)
    {
        foreach(Outcome outcome in outcomes)
        {
            if (string.Equals(outcome.Title.Trim(), title.Trim(), StringComparison.OrdinalIgnoreCase))
                return outcome.Id;
        }
        return null; 
    }

    private void OnApplicationQuit()
    {
        _ = TwitchApi.CancelAllPredictions(); 
    }

}

