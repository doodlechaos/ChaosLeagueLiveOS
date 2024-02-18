using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerWire : MonoBehaviour, IResetable
{
    [SerializeField] private LineRenderer _line;
    [SerializeField] private ParticleSystem _signalParticleSystem; 

    [SerializeField] private float _signalTraverseDuration;

    [Serializable] public class WireSignalEvent : UnityEvent { }
    [SerializeField] WireSignalEvent OnSignalArriveDestination;

    [SerializeField] private bool _testSignal = false; 

    private Coroutine _sendSignalC;

    private void OnValidate()
    {
        if (_testSignal)
        {
            _testSignal = false;
            SendSignal();
        }
    }


    public void MyReset()
    {
        if (_sendSignalC != null)
            StopCoroutine(_sendSignalC);
        _signalParticleSystem.Stop();
    }

    public void SendSignal()
    {
        _sendSignalC = StartCoroutine(SendSignalC()); 
    }

    private IEnumerator SendSignalC()
    {
        float totalLength = 0; 
        // Calculate the total length of the line
        for (int i = 1; i < _line.positionCount; i++)
            totalLength += Vector3.Distance(_line.GetPosition(i - 1), _line.GetPosition(i));

        float timer = 0;
        _signalParticleSystem.Play(); 
        while (timer < _signalTraverseDuration)
        {
            float t = timer / _signalTraverseDuration;
            _signalParticleSystem.transform.localPosition = GetPosOnLineByPercentage(totalLength, t);
            timer += Time.deltaTime;
            yield return null;
        }
        _signalParticleSystem.transform.position = GetPosOnLineByPercentage(totalLength, 0);
        _signalParticleSystem.Stop();

        OnSignalArriveDestination.Invoke(); 

    }

    private Vector3 GetPosOnLineByPercentage(float totalLength, float t)
    {
        if (t < 0)
            t = 0;
        else if (t > 1)
            t = 1;

        float targetLength = t * totalLength;
        float currentLength = 0f;

        // Find the segment of the line where the target length lies
        for (int i = 1; i < _line.positionCount; i++)
        {
            Vector3 startPos = _line.GetPosition(i - 1);
            Vector3 endPos = _line.GetPosition(i);
            float segmentLength = Vector3.Distance(startPos, endPos);

            if (currentLength + segmentLength >= targetLength)
            {
                // Interpolate within this segment to find the position
                float remainingLength = targetLength - currentLength;
                float interpolationFactor = remainingLength / segmentLength;
                return Vector3.Lerp(startPos, endPos, interpolationFactor);
            }

            currentLength += segmentLength;
        }

        // If the target length is at the end of the line, return the last point
        return _line.GetPosition(_line.positionCount - 1);
    }


}
