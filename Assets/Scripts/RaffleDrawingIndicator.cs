using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RaffleDrawingIndicator : MonoBehaviour
{
    private Material _mat;
    [SerializeField] private Color LitEmissionColor;
    [SerializeField] private Color FullEmissionColor;


    private void Awake()
    {
        _mat = GetComponent<Renderer>().material;
        //Mat.EnableKeyword("_EMISSION");
    }

    public void SetLit()
    {
        _mat.SetColor("_EmissionColor", LitEmissionColor);
        Debug.Log($"Setting emission color of {name} to {LitEmissionColor.ToHexString()}");
    }

    public void SetUnlit()
    {
        _mat.SetColor("_EmissionColor", Color.black);
    }

    public void SetFull()
    {
        _mat.SetColor("_EmissionColor", FullEmissionColor);
    }
}
