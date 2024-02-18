using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public enum TI_Type { _Default, GivePoints, GiveGold, GiveGoldDoBonus, Multiply, Tomato};

public class TI_Text : TravelingIndicator
{
    private TextPopupMaster _textPopupMaster;

    [SerializeField] private TextMeshPro _text;
    [SerializeField] private MeshRenderer _pfpMeshRenderer;

    private float moveSpeed;

    //private Vector3 _startPosition;
    private bool _isLob;
    private float _fullDistance;

    private float _lobDuration = 1.5f;
    private float _lobTimer = 0;

    private Vector3 _lobApexTarget = new Vector3(0, 0, -45);

    [SerializeField] private AnimationCurve _lobHeight;

    public void InitializeNewTI(TextPopupMaster master,
        Vector3 origin,
        TravelingIndicatorIO _target, 
        long _value, 
        string _displayText, 
        Color _textColor, 
        float _moveSpeed,
        Vector3 _scale,
        Texture avatarTex,
        bool isLob,
        TI_Type ti_type = TI_Type._Default)
    {
        //CLDebug.Inst.Log("initializing new TI with target position: " + _target.GetPosition());
        _textPopupMaster = master;
        Target = _target;
        value = _value;
        _isLob = isLob;
        TI_Type = ti_type;

        _lobTimer = 0;
        _text.SetText(_displayText);

        transform.localScale = _scale;

        if (avatarTex == null)
            _pfpMeshRenderer.enabled = false;
        else
        {
            _pfpMeshRenderer.enabled = true;
            _pfpMeshRenderer.material.mainTexture = avatarTex;
        }

        Origin = origin;
        _fullDistance = Vector2.Distance(Origin, _target.Get_TI_IO_Position());

        _text.color = _textColor;
        moveSpeed = _moveSpeed;
    }

    public void Update()
    {

        if (Target == null || Target.Get_TI_IO_Position() == null)
        {
            ReturnTIToPool();
            return;
        }

        if (_isLob)
        {
            UpdateLob();
            return;
        }

        //Move it towards the target
        Vector3 nextPos = Vector3.MoveTowards(transform.position, Target.Get_TI_IO_Position(), moveSpeed);

        if (Vector2.Distance(nextPos, Target.Get_TI_IO_Position()) < 0.1f)
        {
            Target.ReceiveTravelingIndicator(this);
            ReturnTIToPool();
            return;
        }

        transform.position = nextPos; 
    }

    private void ReturnTIToPool()
    {
        _pfpMeshRenderer.material.mainTexture = null; 
        _textPopupMaster.ReturnTITextToPool(this);

    }
    public void UpdateLob()
    {

        _lobTimer += Time.deltaTime;

        float t = _lobTimer / _lobDuration;

        if (t >= 1)
        {
            Target.ReceiveTravelingIndicator(this);
            ReturnTIToPool();
            AudioController.inst.PlaySound(AudioController.inst.TomatoSplat, 0.9f, 1.1f);
        }

        Vector3 l1 = Vector3.Lerp(Origin, _lobApexTarget, t);
        Vector3 l2 = Vector3.Lerp(l1, Target.Get_TI_IO_Position(), t);

        transform.position = l2;

    }

/*    public Vector3 GetOriginPos()
    {
        return _startPosition; 
    }*/
}



public interface TravelingIndicatorIO
{
    public void ReceiveTravelingIndicator(TravelingIndicator TI);
    public Vector3 Get_TI_IO_Position();
}

