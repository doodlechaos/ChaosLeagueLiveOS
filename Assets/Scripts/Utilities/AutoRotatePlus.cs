using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoRotatePlus : MonoBehaviour
{

    [SerializeField] private float _intervalDuration = 5f;
    private float _intervalTimer = 0; 

    [SerializeField] private Vector3 _ossilateStartEuler;
    [SerializeField] private Vector3 _ossilateEndEuler;

    private bool toggle = true;

    [SerializeField] private bool _cyclic = false;

    // Update is called once per frame
    void Update()
    {
        float t = _intervalTimer / _intervalDuration;
        t = EasingFunction.EaseInOutQuad(0, 1, t);
        transform.eulerAngles = Vector3.Lerp(_ossilateStartEuler, _ossilateEndEuler, t);


        _intervalTimer += (toggle) ? Time.deltaTime : -Time.deltaTime;

        if (_cyclic && _intervalTimer < _intervalDuration)
            return;

        if (_cyclic)
        {
            _intervalTimer = 0;
            return;
        }

        if (_intervalTimer < 0)
            toggle = true;

        if (_intervalTimer > _intervalDuration)
            toggle = false;

    } 
}
