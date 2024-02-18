using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldenVisuals : MonoBehaviour
{
    [SerializeField] private LineRenderer _borderPath;

    [SerializeField] private List<ParticleSystem> _particleSystems;
    [SerializeField] private float _spinSpeed;


    float totalLength = 0f;
    float t = 0; 

    private void Awake()
    {
        // Calculate the total length of the line
        for (int i = 1; i < _borderPath.positionCount; i++)
        {
            totalLength += Vector3.Distance(_borderPath.GetPosition(i - 1), _borderPath.GetPosition(i));
        }
    }

    private void Update()
    {
        for(int i = 0; i < _particleSystems.Count; i++)
        {
            var ps = _particleSystems[i];
            float percentage = (t + (1 / (float)_particleSystems.Count * i)) % 1;
            Vector3 nextPos = GetPosOnLineByPercentage(percentage);
            Vector3 prevPos = ps.transform.position;
            ps.transform.localPosition = nextPos; 
            ps.transform.LookAt(prevPos);

        }

        t = (t + _spinSpeed) % 1;
    }

    private Vector3 GetPosOnLineByPercentage(float t)
    {
        if (t < 0)
            t = 0;
        else if (t > 1)
            t = 1;

        float targetLength = t * totalLength;
        float currentLength = 0f;

        // Find the segment of the line where the target length lies
        for (int i = 1; i < _borderPath.positionCount; i++)
        {
            Vector3 startPos = _borderPath.GetPosition(i - 1);
            Vector3 endPos = _borderPath.GetPosition(i);
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
        return _borderPath.GetPosition(_borderPath.positionCount - 1);
    }

}
