using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MechanicalPivot : MonoBehaviour, IResetable
{
    [SerializeField] private Transform _pivotRoot; 

    [SerializeField] private float angle1;
    [SerializeField] private float angle2;
    private bool _currSide = true; 
    private float _currentAngle; 
    public enum PivotMode { FlipFlop, HoldAndReturn};

    [SerializeField] private PivotMode pivotMode = PivotMode.FlipFlop;
    [SerializeField] private float _holdDuration = 0;

    [SerializeField] private float _rotateSpeed = 1;

    [SerializeField] private bool _testSignal;

    private Coroutine _flipCoroutine;

    private void OnValidate()
    {
        if (_testSignal)
        {
            _testSignal = false;
            ReceiveSignal(); 
        }
    }

    public void MyReset()
    {
        _currSide = true;
        _currentAngle = angle1;
        _pivotRoot.localEulerAngles = new Vector3(0, 0, _currentAngle); 
    }

    public void ReceiveSignal()
    {
        if (pivotMode == PivotMode.FlipFlop)
        {
            if (_flipCoroutine != null)
                StopCoroutine(_flipCoroutine);

            _flipCoroutine = StartCoroutine(Flip());
        }
    }

    public IEnumerator Flip()
    {
        _currSide = !_currSide; 
        //Move towards the farther away angle
        float targetAngle = (_currSide) ? angle1 : angle2;

        float pitchMin = (_currSide) ? 0.9f : 1f;
        float pitchMax = (_currSide) ? 1f : 1.1f;
        AudioController.inst.PlaySound(AudioController.inst.MechanicalPivotMove, pitchMin, pitchMax); 

        while(Mathf.Abs(_currentAngle - targetAngle) > _rotateSpeed)
        {
            if (targetAngle > _currentAngle)
                _currentAngle += _rotateSpeed;
            else
                _currentAngle -= _rotateSpeed;

            SetAngle(_currentAngle); 
            yield return null; 
        }
        SetAngle(targetAngle);
        
    }

    private void SetAngle(float angle)
    {
        Vector3 euler = _pivotRoot.localEulerAngles;
        euler.z = angle;
        _pivotRoot.localEulerAngles = euler;
    }


}
