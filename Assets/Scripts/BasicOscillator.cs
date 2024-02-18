using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicOscillator : MonoBehaviour
{
    [SerializeField] private Transform _objToMove;
    [SerializeField] private Transform _pos1;
    [SerializeField] private Transform _pos2;

    [SerializeField] private float _speed = 1;

    // Update is called once per frame
    void Update()
    {
        float t = Mathf.PingPong(Time.time  * _speed, 1);
        t = EasingFunction.EaseInOutQuad(0, 1, t);
        _objToMove.position = Vector3.Lerp(_pos1.position, _pos2.position, t);
    }
}
