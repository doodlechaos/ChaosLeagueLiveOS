using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[Flags]
public enum PBEffect
{
    None = 0,
    Add = 1,
    Multiply = 2,
    Explode = 4,
    Divide = 8,
    Subtract = 16,
    Zero = 32,
    Implode = 64
}

public class PBEffector : MonoBehaviour, TravelingIndicatorIO
{
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private TextMeshPro _textLabel;
    //[SerializeField] private PBDetector _pbDetector;
    [SerializeField] private PBEffect _effect;
    [SerializeField] private float _value;
    private float _currValue;
    [SerializeField] private bool _infiniteDurability;
    [SerializeField] private int _maxHP = 5;
    [SerializeField] private bool _zeroPlayerBallVelOnDetection = false; 

    public int CurrentHP;

    [SerializeField] private bool _colorByValue;
    [SerializeField] private PointColorMap _colorMap;
    private MaterialPropertyBlock _materialPropertyBlock;

    [SerializeField] private bool _overridePopupDirection;
    [SerializeField] private Vector3 _textPopupDirection;
    [SerializeField] private bool _manualReset;
    [SerializeField] public AudioSource OverrideExplodeAudio;

    [SerializeField] private List<MeshRenderer> _logicColor;
    [SerializeField] private EffectorLogic _triggerLogic;
    [SerializeField] private float _logicValue = 1;

    [SerializeField] private MaterialPropertyBlock _logicPropBlock;
    private float RGB_t;
    [SerializeField] private float RGB_speed = 0.01f;
    [SerializeField] private Gradient _RGB_Gradient;

    public List<MultiplierZone> OverlappingZones = new List<MultiplierZone>();

    [SerializeField] private bool _simulateTriggerLogic;

    [Serializable] public class DetectedTIEvent : UnityEvent<TravelingIndicator> { }
    [SerializeField] DetectedTIEvent OnTIDetected;

    public void Awake()
    {
        _currValue = _value;

        if (_materialPropertyBlock == null)
            _materialPropertyBlock = new MaterialPropertyBlock();

        if (_logicPropBlock == null)
            _logicPropBlock = new MaterialPropertyBlock();
    }

    public void OnValidate()
    {
        if (_manualReset)
        {
            _manualReset = false;
            ResetEffector();
        }

        if (_simulateTriggerLogic)
        {
            _simulateTriggerLogic = false;
            TriggerLogic(); 
        }
    }

    public void Init(PBEffect effect, int value, int maxHp = -1)
    {
        gameObject.SetActive(true); 

        SetEffect(effect, false); 
        SetCurrValue(value, false);
        _maxHP = maxHp;
        if (maxHp <= 0)
            _infiniteDurability = true;
        else
            _infiniteDurability = false;
        ResetHealth(false);
        UpdateVisuals(); 
    }

    public void ResetEffector()
    {
        if(_materialPropertyBlock == null)
            _materialPropertyBlock = new MaterialPropertyBlock();

        if(_logicPropBlock == null)
            _logicPropBlock = new MaterialPropertyBlock();

        ResetValue(false);
        OverlappingZones.Clear();

        ResetHealth();

        gameObject.SetActive(true); 
        
        UpdateVisuals();
    }

    public void ResetHealth(bool updateVisuals = true)
    {
        CurrentHP = _maxHP;
        if (updateVisuals)
            UpdateVisuals();
    }

    public void ResetValue(bool updateVisuals = true)
    {
        _currValue = _value;
        if(updateVisuals)
            UpdateVisuals();
    }
    public void SetCurrValue(int val, bool updateVisuals = true)
    {
        _currValue = val;
        if(updateVisuals)
            UpdateVisuals();
    }
    public void IncrementCurrValue(float amount)
    {
        _currValue += amount;
        UpdateVisuals();
    }
    public void DecrementCurrValue(float amount)
    {
        _currValue -= amount;

/*        if(_currValue < 0)
            _effect |= PBEffect.Explode; */

        UpdateVisuals();
    }
    public void MultiplyCurrValue(float amount)
    {
        _currValue *= amount;
        UpdateVisuals();
    }
    public void NthTriangleCurrValue()
    {
        _logicValue++;
        IncrementCurrValue(_logicValue); 
    }

    public int GetZoneMultiplier()
    {
        int multiplier = 0;
        foreach (MultiplierZone zone in OverlappingZones)
            multiplier += zone.Multiplier;

        return multiplier;
    }

    public void SetEffect(PBEffect effect, bool updateVisuals = true)
    {
        _effect = effect;
        if(updateVisuals)
            UpdateVisuals(); 
    }

    public float GetZoneMultiplyAppliedValue()
    {
        int zoneMult = GetZoneMultiplier();

        if (_effect == PBEffect.Multiply || _effect == PBEffect.Divide)
            return _currValue + ((zoneMult <= 1) ? 0 : (zoneMult * 0.1f));

        return _currValue * ((zoneMult < 1) ? 1 : zoneMult);
    }

    public void UpdateVisuals()
    {
        if (_colorMap == null)
            return;

        if (_materialPropertyBlock == null)
            _materialPropertyBlock = new MaterialPropertyBlock();

        if (_logicPropBlock == null)
            _logicPropBlock = new MaterialPropertyBlock();

        (Color meshColor, Color _labelColor) = _colorMap.GetColors((long)GetZoneMultiplyAppliedValue(), _effect);
        if (_colorByValue)
        {
            if (_effect.HasFlag(PBEffect.Explode) && ! _effect.HasFlag(PBEffect.Add))
            {
                meshColor = Color.grey;
            }
            if (_effect.HasFlag(PBEffect.Zero))
            {
                if (GetZoneMultiplier() > 0)
                    meshColor = Color.green;
                else
                    meshColor = Color.red;
            }
            if (_effect.HasFlag(PBEffect.Multiply))
            {
                meshColor = Color.cyan;
                _labelColor = Color.black;
            }
            if (_effect.HasFlag(PBEffect.Divide))
            {
                meshColor = MyColors.Orange;
                _labelColor = Color.black;
            }
            if (_effect.HasFlag(PBEffect.Subtract))
            {
                meshColor = Color.red;
                _labelColor = Color.white;
            }
            if (_effect == PBEffect.None)
            {
                meshColor = Color.grey;
            }

            //Debug.Log($"Setting matPropBlock to {meshColor.ColorToHexString()} in {this.name}"); 
            _materialPropertyBlock.SetColor("_MyBaseColor", meshColor);
            _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
            _textLabel.color = _labelColor;
        }

        //Set the color for how fast or slow it will increase or decrease each time it's triggered
        Color logicRingColor = _colorMap.GetLogicColor(_triggerLogic, (int)_logicValue); 
        
        if(logicRingColor != Color.clear)
        {
            _logicPropBlock.SetColor("_BaseColor", logicRingColor);
            foreach(var mesh in _logicColor)
                mesh.SetPropertyBlock(_logicPropBlock);
        }

        _textLabel.SetText(MyUtil.GetLabel(_effect, GetZoneMultiplyAppliedValue(), GetZoneMultiplier()));

        if (_infiniteDurability || _maxHP <= 0)
            return;

        _materialPropertyBlock.SetColor("_MyBaseColor", meshColor.WithAlpha(CurrentHP / (float)_maxHP));
        _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
    }

    public Color GetMeshColor()
    {
        Color color = _materialPropertyBlock.GetColor("_MyBaseColor");
        return color;
    }

    public void DetectedPB(PlayerBall pb)
    {
        if(_zeroPlayerBallVelOnDetection)
            pb._rb2D.velocity = Vector3.zero;

        if (!_infiniteDurability && CurrentHP <= 0)
            return;

        Vector3 textPopupDirection = (transform.position - pb.GetPosition()).normalized;

        if (_overridePopupDirection)
            textPopupDirection = _textPopupDirection;

        HandlePBAction(pb, _effect, GetZoneMultiplyAppliedValue(), textPopupDirection);

        TriggerLogic();

        if (_infiniteDurability)
            return;

        CurrentHP--;

        UpdateVisuals();

        if (CurrentHP <= 0)
            DisableEffector();
    }

    private void TriggerLogic()
    {
        if (_triggerLogic == EffectorLogic.increment)
            IncrementCurrValue(_logicValue);
        else if (_triggerLogic == EffectorLogic.decrement)
            DecrementCurrValue(_logicValue);
        else if (_triggerLogic == EffectorLogic.nthtriangle)
            NthTriangleCurrValue();
        else if(_triggerLogic == EffectorLogic.multiply)
            MultiplyCurrValue(_logicValue);
        else if (_triggerLogic == EffectorLogic.reset)
            ResetValue();
    }

    private void HandlePBAction(PlayerBall pb, PBEffect effect, float value, Vector3 textPopupDirection)
    {
        if (pb.IsExploding)
            return;

        if (effect.HasFlag(PBEffect.Add))
        {
            if ((long)value >= 0)
                pb.Ph.AddPoints((long)value, createTextPopup: true, textPopupDirection);
            else
            {
                pb.Ph.SubtractPoints((long)value * -1, true, createTextPopup: true);
                if (OverrideExplodeAudio != null)
                    AudioController.inst.PlaySound(OverrideExplodeAudio, 0.9f, 1.1f);
                else
                    AudioController.inst.PlaySound(AudioController.inst.DeathByContact, 0.9f, 1.1f);
                pb.ExplodeBall();
            }
        }
        if (effect.HasFlag(PBEffect.Divide))
        {
            SendToKing(pb, pb.Ph.pp.SessionScore / (long)value / 2);
            pb.Ph.DividePoints(value, textPopup: true, textPopupDirection);
        }
        if (effect.HasFlag(PBEffect.Multiply))
            pb.Ph.MultiplyPoints(value, createTextPopup:true, textPopupDirection);
        if (effect.HasFlag(PBEffect.Subtract))
        {
            SendToKing(pb, (long)value / 2);
            pb.Ph.SubtractPoints((long)value, canKill: false, createTextPopup: true, textPopupDirection);
        }
        if (effect.HasFlag(PBEffect.Zero) && GetZoneMultiplier() <= 0)
        {
            SendToKing(pb, pb.Ph.pp.SessionScore / 2);
            pb.Ph.ZeroPoints(kill: false, true, textPopupDirection);
        }
        if (effect.HasFlag(PBEffect.Explode))
        {
            if(OverrideExplodeAudio != null)
                AudioController.inst.PlaySound(OverrideExplodeAudio, 0.9f, 1.1f);
            else
                AudioController.inst.PlaySound(AudioController.inst.DeathByContact, 0.9f, 1.1f); 
            pb.ExplodeBall();
        }
        else if (effect.HasFlag(PBEffect.Implode))
            pb.ExplodeBall(true);
    }

    private void SendToKing(PlayerBall pb, long amount)
    {
        var target = pb.Ph.GetGameManager().GetKingController().currentKing;

        if (target == null)
            return;

        amount = Math.Min(pb.Ph.pp.SessionScore, amount);

        if (amount <= 0)
            return;

        string text = "+" + MyUtil.AbbreviateNum4Char(amount);
        TextPopupMaster.Inst.CreateTravelingIndicator(text, amount, pb.Ph, target.Ph, 0.2f, Color.cyan, pb.Ph.PfpTexture);
    }

    private void DisableEffector()
    {
        gameObject.SetActive(false);

        Debug.Log("disabling effector"); 

        //Bypass animation root and go two levels up
        ChaosObs chaosObs = transform.parent.GetComponentInParent<ChaosObs>();
        if (chaosObs == null)
            return;

        chaosObs.ReturnToPool();

    }



    public void ReceiveTravelingIndicator(TravelingIndicator TI)
    {
        OnTIDetected.Invoke(TI);

        if (TI.TI_Type == TI_Type.Multiply)
        {
            MultiplyCurrValue(TI.value);
        }
    }

    public Vector3 Get_TI_IO_Position()
    {
        return transform.position;
    }

    public PBEffect GetEffect()
    {
        return _effect; 
    }
/*    public GameObject GetGameObject()
    {
        return gameObject;
    }*/

    private void Update()
    {
        if(_triggerLogic == EffectorLogic.nthtriangle || _triggerLogic == EffectorLogic.multiply)
        {
            _logicPropBlock.SetColor("_BaseColor", _RGB_Gradient.Evaluate(RGB_t));
            foreach (var mesh in _logicColor)
                mesh.SetPropertyBlock(_logicPropBlock);
            RGB_t = (RGB_t + RGB_speed) % 1;
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        //Debug.Log($"entering collision between {collision.gameObject.name} and {gameObject.name}");
        if (collision.gameObject.layer != LayerMask.NameToLayer("MultiplierZone"))
            return;

        //Debug.Log($"Found superchatzone layer");

        MultiplierZone zone = collision.gameObject.GetComponentInParent<MultiplierZone>();
        if (zone == null)
            return;

        OverlappingZones.Add(zone);
        UpdateVisuals();
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        //Debug.Log("entering collision");
        if (collision.gameObject.layer != LayerMask.NameToLayer("MultiplierZone"))
            return;

        MultiplierZone zone = collision.gameObject.GetComponentInParent<MultiplierZone>();
        if (zone == null)
            return;

        OverlappingZones.Remove(zone);
        UpdateVisuals();
    }
}
