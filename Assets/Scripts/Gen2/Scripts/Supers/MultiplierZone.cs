using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class MultiplierZone : MonoBehaviour
{
    private RebellionController _rm;
    public PlayerHandler Ph;
    [SerializeField] private TextMeshPro _labelText;
    [SerializeField] private Transform _labelTextRotateOrigin;
    [SerializeField] private ParticleSystem _particleHype;
    [SerializeField] private Collider2D _collider;
    [SerializeField] private MeshRenderer _bodyMeshRenderer;

    private Vector2 _dollarColorMap;
    private Gradient _dollarToColor;
    private Gradient _dollarToTextColor;

    public int Multiplier;

    [SerializeField] private float _labelRotateSpeed = -70;

    public void Init(RebellionController scm, PlayerHandler ph, Vector2 dollarColorMap, Gradient dollarToColor, Gradient dollarToTextColor)
    {
        _rm = scm;
        Ph = ph;
        Multiplier = 0;

        _dollarColorMap = dollarColorMap;
        _dollarToColor = dollarToColor;
        _dollarToTextColor = dollarToTextColor;

        _collider.gameObject.SetActive(true);
        _labelText.gameObject.SetActive(true);
        _particleHype.gameObject.SetActive(true);
    }

    public void DisableZone()
    {
        _collider.gameObject.SetActive(false);
        _labelText.gameObject.SetActive(false);
        _particleHype.gameObject.SetActive(false);
    }

    public void IncrementMultiplier(int amount)
    {
        Multiplier += amount;
        UpdateVisuals();
    }
    public void DecrementMultiplier(int amount)
    {
        if (Multiplier - amount <= 0)
        {
            _rm.DestroySuperchatZone(this);
            return;
        }

        Multiplier -= amount;


        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        _labelText.SetText("x" + Multiplier);

        if (Multiplier <= 1)
        {
            _particleHype.Stop();
            _labelText.color = Color.white;
            return;
        }
        float t = Mathf.Clamp(Multiplier, 1, _dollarColorMap.y) / _dollarColorMap.y;

        var main = _particleHype.main;
        main.startColor = _dollarToColor.Evaluate(t);

        _bodyMeshRenderer.material.color = MyUtil.SetColorSaveAlpha(_dollarToColor.Evaluate(t), _bodyMeshRenderer.material.color);

        MinMaxCurve newCurve = new MinMaxCurve();
        newCurve.constantMin = 1;
        newCurve.constantMax = Mathf.Lerp(1, 10, t);
        newCurve.mode = ParticleSystemCurveMode.TwoConstants;
        main.startLifetime = newCurve;

        _labelText.color = _dollarToTextColor.Evaluate(t);
        _particleHype.Play();
    }


    private void Update()
    {
        if (Ph == null)
            return;

        transform.position = Ph.GetBallPos();

        Vector3 rotation = Vector3.forward * _labelRotateSpeed * Time.deltaTime; 
        _labelTextRotateOrigin.transform.Rotate(Vector3.forward * _labelRotateSpeed * Time.deltaTime);
        _labelText.transform.Rotate(-rotation); 
    }

    public float GetRadius()
    {
        return Mathf.Abs(_collider.bounds.max.x - _collider.bounds.min.x) / 2;
    }

}
