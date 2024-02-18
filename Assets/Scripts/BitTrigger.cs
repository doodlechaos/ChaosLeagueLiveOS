using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class BitTrigger : MonoBehaviour
{
    [SerializeField] private AutoPredictions _autoPredictions; 
    [SerializeField] private FluidType _fluidType;

    [SerializeField] private MeshRenderer _loadingBarBody;
    [SerializeField] private Color _bodyBackgroundColor;
    [SerializeField] private Color _headerBackgroundColor; 

    [SerializeField] private TextMeshPro _loadingBarText;

    [SerializeField] private string _announcmentMessage; //Starts with player username

    [SerializeField] private string appConfigCostName;
    private List<(int bits, string username)> _additionQ = new List<(int bits, string username)>();
    private int _currBits = 0;
    private int _bitCost = 0;
    [Serializable] public class MyEventType : UnityEvent { }
    public MyEventType TriggerEvent;

    private MaterialPropertyBlock _matPropBlock;

    [SerializeField] private bool testAddBitsButton;
    [SerializeField] private int testAddBitsAmount;
    [SerializeField] private string testUsername; 

    [SerializeField] private int _framesPerAudioTrigger;
    private int _framesSinceLastAudio = 0;

    private string _latestUsername = ""; 

    private void OnValidate()
    {
        if (testAddBitsButton)
        {
            testAddBitsButton = false;
            AddBits(testUsername, testAddBitsAmount);
        }
    }

    private void Start()
    {
        _matPropBlock = new MaterialPropertyBlock();

        _matPropBlock.SetColor("_BodyBackgroundColor", _bodyBackgroundColor);
        _matPropBlock.SetColor("_HeaderBackgroundColor", _headerBackgroundColor);

        _bitCost = AppConfig.inst.GetI(appConfigCostName);
        UpdateLoadingBar();
    }

    public void AddBits(string username, int count)
    {
        _bitCost = AppConfig.inst.GetI(appConfigCostName);
        _additionQ.Add((count, username));
    }

    private void Update()
    {
        _framesSinceLastAudio = Math.Min(_framesSinceLastAudio + 1, _framesPerAudioTrigger + 1);

        if (_additionQ.Count > 0)
        {
            
            int rate = Math.Min(_additionQ[0].bits, 5);
            _additionQ[0] = (_additionQ[0].bits - rate, _additionQ[0].username);
            _latestUsername = _additionQ[0].username;

            if (_additionQ[0].bits <= 0)
                _additionQ.RemoveAt(0); 

            _currBits += rate;
            UpdateLoadingBar();

            if (_framesSinceLastAudio > _framesPerAudioTrigger)
            {
                float t = _currBits / (float)_bitCost;
                float pitch = Mathf.Lerp(0.5f, 2, t);
                AudioController.inst.PlaySound(AudioController.inst.SingleBitDing, pitch, pitch);
                _framesSinceLastAudio = 0; 
            }

        }
        if(_currBits >= _bitCost)
        {
            _currBits -= _bitCost;
            TriggerEvent.Invoke();
            MyTTS.inst.Announce($"{_latestUsername} {_announcmentMessage}");

            if(_autoPredictions != null)
            {
                if (_fluidType == FluidType.water)
                    _autoPredictions.WaterSignal();
                else if(_fluidType == FluidType.lava)
                    _autoPredictions.LavaSignal();
            }

            UpdateLoadingBar();
        }

    }

    private void UpdateLoadingBar()
    {
        //Set the text
        string text = $"{MyUtil.AbbreviateNum4Char(_currBits)}/{MyUtil.AbbreviateNum4Char(_bitCost)}";
        _loadingBarText.SetText(text);

        //Set the shader fill amount
        float t = _currBits / (float)_bitCost;
        _matPropBlock.SetFloat("_SliderPercentage", 1 - t);
        _loadingBarBody.SetPropertyBlock(_matPropBlock);
    }

}
