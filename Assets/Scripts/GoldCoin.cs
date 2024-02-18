using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GoldCoin : TravelingIndicator
{
    [SerializeField] private Rigidbody2D _rb2D;

    [SerializeField] private AnimationCurve _dragOvertime;

    [SerializeField] private AnimationCurve _magnetStrengthOverTime;
    [SerializeField] private MeshRenderer _mr;
    [SerializeField] private float zPlane = -1; 

    private GoldDistributor _goldDistributor;

    private float _timeAlive = 0; 
    public void InitializeCoin(GoldDistributor goldDistributor, Vector3 origin, Vector2 vel, TravelingIndicatorIO _target, long _value, Color color)
    {
        value = _value;
        Target = _target;

        TI_Type = TI_Type.GiveGoldDoBonus; 

        _goldDistributor = goldDistributor;

        origin.z = zPlane; 
        _rb2D.transform.position = origin;

        _rb2D.velocity = vel;
        _mr.material.color = color;

        _timeAlive = 0; 
    }

    // Update is called once per frame
    void Update()
    {
        if (Target == null || Target.Get_TI_IO_Position() == null)
        {
            _goldDistributor.ReturnGoldToPool(this);
            return;
        }

        Vector2 direction = (Vector2)(Target.Get_TI_IO_Position() - _rb2D.transform.position).normalized;
        _rb2D.drag = _dragOvertime.Evaluate(_timeAlive / _dragOvertime.keys.Last().time); 
        _rb2D.AddForce(direction * _magnetStrengthOverTime.Evaluate(_timeAlive / _magnetStrengthOverTime.keys.Last().time));

        if (Vector2.Distance(_rb2D.transform.position, Target.Get_TI_IO_Position()) < 0.5f && _timeAlive >= 1)
        {
            Target.ReceiveTravelingIndicator(this);
            _goldDistributor.ReturnGoldToPool(this);
            return;
        }

        _timeAlive += Time.deltaTime;
    }
}
