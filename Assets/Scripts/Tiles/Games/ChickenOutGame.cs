using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChickenOutGame : Game
{
    [SerializeField] private TextMeshPro _riskLevelText;
    [SerializeField] private Transform RiskFunnelLRoot;
    [SerializeField] private Transform RiskFunnelRRoot;

    [SerializeField] private int _maxRiskLevel = 20; 
    [SerializeField] private int _currRiskLevel = 0;

    [SerializeField] private float _maxRiskScale = 3.47f; 

    private void OnValidate()
    {
        UpdateRiskLevel();
    }

    public override void OnTilePreInit()
    {
        _currRiskLevel = 0;
        UpdateRiskLevel();
    }

    public override void StartGame()
    {
        //throw new NotImplementedException();
    }

    public override void CleanUpGame()
    {
        //throw new NotImplementedException();
        IsGameStarted = false;
    }

    public void IncreaseRisk()
    {
        _currRiskLevel++;
        UpdateRiskLevel(); 
    }

    public void UpdateRiskLevel()
    {
        if (_currRiskLevel > _maxRiskLevel)
            _currRiskLevel = _maxRiskLevel;

        _riskLevelText.SetText($"Risk Level: {_currRiskLevel + 1}/{_maxRiskLevel + 1}");

        float t = (_currRiskLevel) / (float)_maxRiskLevel;

        RiskFunnelLRoot.transform.localScale = new Vector3(Mathf.Lerp(0.01f, _maxRiskScale, t), 1, 1);
        RiskFunnelRRoot.transform.localScale = new Vector3(Mathf.Lerp(0.01f, _maxRiskScale, t), 1, 1);

    }
}
