using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshRenderer))]
public class DefenseBrick : MonoBehaviour, TravelingIndicatorIO
{
    private DefaultDefenseV2 _defaultDefense;
    private FluidSpawner _dbs;
    private long _startHP;
    private long _hp;

    [SerializeField] public Rigidbody2D Rb2D;
    [SerializeField] public BoxCollider2D BoxCollider2D;

    [SerializeField] public MeshRenderer MeshRenderer;
    [SerializeField] private TextMeshPro _displayText;

    [SerializeField] private Material _goldOreMaterial; 

    private DefenseBrickType _type;

    private float[] _goldenChance = { 0.17f, 0.02f, 0.01f, 0.001f };

    private bool _isGold = false; 

    public void InitBrick(FluidSpawner dbs, DefaultDefenseV2 ddV2, DefenseBrickType type, int _HP, Material _mat, Color labelColor)
    {
        gameObject.SetActive(true);
        _type = type;

        _isGold = false;
        float t = Random.Range(0f, 1f);
        if (type == DefenseBrickType.Dirt && t <= _goldenChance[0])
            _isGold = true;
/*        else if(type == DefenseBrickType.Coal && t <= _goldenChance[1])
            _isGold = true;
        else if(type == DefenseBrickType.Iron && t <= _goldenChance[2])
            _isGold = true;
        else if(type == DefenseBrickType.Diamond && t <= _goldenChance[3])
            _isGold = true;*/

        this._dbs = dbs;
        this._defaultDefense = ddV2;
        _hp = (_isGold) ? Mathf.Min(_HP, 256) : _HP; //If gold ore, limit the value to 256 per dirt
        _startHP = _hp;

        if (_mat != null)
        {
            if (_isGold)
                MeshRenderer.material = _goldOreMaterial;
            else
                MeshRenderer.material = _mat;

        }

        _displayText.color = labelColor;

        UpdateHPText();
    }

    public void ResetBrick(int _startHP)
    {
        this._startHP = _startHP;
        gameObject.SetActive(true);
        _hp = this._startHP;
        MeshRenderer.material.SetFloat("_BreakPercent", 0);

        UpdateHPText();
    }

    public void BreakBrick(PlayerBall pb)
    {
        if(_isGold )
            _defaultDefense.GoldBreakSignal(pb, transform.position, (int)_startHP);

        //If it's default, make it inactive
        if (_defaultDefense != null)
        {
            gameObject.SetActive(false);
            return;
        }

    }

    private void TakeDamage(PlayerBall pb)
    {
        long damageAmount = Math.Min(GetDamageAmount(pb.GetPoints()), GetDamageAmount(_hp));


        _hp -= damageAmount;
        pb.Ph.SubtractPoints(damageAmount, canKill: true, createTextPopup: false, contributeToROI: false);

        AudioController.inst.PlaySound(AudioController.inst.DefenseBrickTakeDamage, 0.7f, 1.3f);

        if (_hp <= 0)
        {
            BreakBrick(pb);
            return;
        }

        // UpdateColor based on damage taken
        //Color color = GetComponent<SpriteRenderer>().color;
        //color.a = (float)HP / StartHP;
        MeshRenderer.material.SetFloat("_BreakPercent", 1 - (float)_hp / _startHP);

        UpdateHPText();
    }

    public long GetDamageAmount(long number)
    {
        if (number < 1000)
            return 1;

        int mag = (int)(Math.Floor(Math.Log10(number)) / 3); // Truncates to 6, divides to 2

        switch (mag)
        {
            case 0:
                return 1;
            case 1:
                return 1000; //K
            case 2:
                return 1_000_000; //M
            case 3:
                return 1_000_000_000; //B
            case 4:
                return 1_000_000_000_000; //t
            case 5:
                return 1_000_000_000_000_000; //q
            case 6:
                return 1_000_000_000_000_000_000; //Q
            default:
                return 1;
        }
    }

    private void UpdateHPText()
    {
        _displayText.SetText(MyUtil.AbbreviateNum3Char(_hp));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            PlayerBall pb = collision.transform.GetComponentInParent<PlayerBall>();
            if (pb != null)
                TakeDamage(pb);

        }
    }


    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            PlayerBall pb = collision.transform.GetComponentInParent<PlayerBall>();
            if(pb != null)
                TakeDamage(pb);

        }
    }

    public void ReceiveTravelingIndicator(TravelingIndicator TI)
    {
        _hp += TI.value;

        TextPopupMaster.Inst.CreateTextPopup(transform.position, Vector2.up, "+" + MyUtil.AbbreviateNum4Char(TI.value), Color.magenta);

        if (_hp > _startHP)
            _startHP = _hp;
        UpdateHPText();

    }

    public Vector3 Get_TI_IO_Position()
    {
        return _displayText.transform.position;
    }

/*    public GameObject GetGameObject()
    {
        return gameObject;
    }*/

    public DefenseBrickType GetBrickType()
    {
        return _type;
    }

}
