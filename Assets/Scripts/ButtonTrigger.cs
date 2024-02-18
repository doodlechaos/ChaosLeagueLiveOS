using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ButtonTrigger : MonoBehaviour, IResetable
{

    [Serializable] public class ButtonPressEvent : UnityEvent { }
    [SerializeField] ButtonPressEvent OnButtonPress;

    [SerializeField] private Transform _button; 
    [SerializeField] private int _pressesAllowed;

    [SerializeField] private Transform _upPos;
    [SerializeField] private Transform _pressedPos;

    [SerializeField] private float _buttonTravelTime;
    [SerializeField] private float _buttonHoldTime;

    [SerializeField] private bool _testPress; 

    private int _pressCount = 0;

    private Coroutine _pressButtonCR;

    private void OnValidate()
    {
        if (_testPress)
        {
            _testPress = false;
            PressButton(); 
        }
    }

    public void MyReset()
    {
        _pressCount = 0;
        _button.position = _upPos.position;

        if (_pressButtonCR != null)
            StopCoroutine(_pressButtonCR);
    }

    public void PressButton()
    {
        if (_button.position != _upPos.position)
            return;

        if(_pressButtonCR != null)
            StopCoroutine( _pressButtonCR );

        _pressButtonCR = StartCoroutine(PressButtonC()); 
    }

    private IEnumerator PressButtonC()
    {
        _pressCount++;
        AudioController.inst.PlaySound(AudioController.inst.ButtonDown, 0.95f, 1.05f);
        OnButtonPress.Invoke();

        //Animate button down
        float timer = 0; 
        while(timer < _buttonTravelTime)
        {
            float t = timer / _buttonTravelTime;
            _button.position = Vector3.Lerp(_upPos.position, _pressedPos.position, t);
            timer += Time.deltaTime;
            yield return null; 
        }
        _button.position = _pressedPos.position;


        if (_pressCount >= _pressesAllowed)
            yield break;

        //Animate button up
        timer = 0;
        while (timer < _buttonTravelTime)
        {
            float t = timer / _buttonTravelTime;
            _button.position = Vector3.Lerp(_pressedPos.position, _upPos.position, t);
            timer += Time.deltaTime;
            yield return null;
        }
        _button.position = _pressedPos.position;
        AudioController.inst.PlaySound(AudioController.inst.ButtonUp, 0.95f, 1.05f);

    }

}
