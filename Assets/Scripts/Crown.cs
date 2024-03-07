using Newtonsoft.Json;
using SpotifyAPI.Web;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Crown : MonoBehaviour
{
    [SerializeField] private MeshRenderer _crownMeshRenderer;
    [SerializeField] private MeshRenderer _gemsMeshRenderer; 
    [SerializeField] private MeshRenderer _fourthMeshRenderer; 
    [SerializeField] private MeshRenderer _fifthMeshRenderer; 
    [SerializeField] private MeshRenderer _sixthMeshRenderer; 

    [SerializeField] private Color _defaultCrownBase;

    [SerializeField] private bool _TestRandomColors;
    [SerializeField] private int _testColorCount;
    [SerializeField] private float _t3Intensity; 

    private MaterialPropertyBlock _basePropBlock;
    private MaterialPropertyBlock _trimPropBlock;
    private MaterialPropertyBlock _gemsPropBlock;

    private void Awake()
    {
        _basePropBlock = new MaterialPropertyBlock();
        _trimPropBlock = new MaterialPropertyBlock();
        _gemsPropBlock = new MaterialPropertyBlock();
    }

    private void OnValidate()
    {
        if (_TestRandomColors)
        {
            _TestRandomColors = false;
            TestRandomColors(); 
        }
    }

    public List<Color> GetRandomColorList(int length)
    {
        List<Color> colors = new List<Color>();
        for (int i = 0; i < length; i++)
        {
            if (length >= 3 && i == 1 || i == 2)
            {
                Color.RGBToHSV(Random.ColorHSV(), out float H, out float S, out float V);

                colors.Add(Color.HSVToRGB(H, 1, 1));
                continue;
            }
            colors.Add(Random.ColorHSV());
        }
        return colors;
    }

    private void TestRandomColors()
    {
        var colors = GetRandomColorList(_testColorCount);
        UpdateCustomizations(colors); 
    }

    public void UpdateCustomizations(List<Color> colors)
    {
        _gemsMeshRenderer.gameObject.SetActive(false);
        _fourthMeshRenderer.gameObject.SetActive(false);
        _fifthMeshRenderer.gameObject.SetActive(false);
        _sixthMeshRenderer.gameObject.SetActive(false);

        //Default
        if (colors.Count <= 0)
        {
            SetBaseColor(_defaultCrownBase, 0);
            SetTrimColor(_defaultCrownBase, 0);
        }
        //If 1 colors set, set the trim the same
        else if (colors.Count == 1)
        {
            SetBaseColor(colors.First(), 0);
            SetTrimColor(colors.First(), 0);
            return;


        }
        //If 2 colors set, set the trim different
        else if (colors.Count == 2)
        {

            SetBaseColor(colors[0], 0);
            SetTrimColor(colors[1], 0);
            return;
        }
        //If 3 colors set
        else if (colors.Count == 3)
        {

            SetBaseColor(colors[0], 25);
            SetTrimColor(colors[1], 25);
            SetGemsColor(colors[2], 25);
            _gemsMeshRenderer.gameObject.SetActive(true);
            return;
        }
        //If 4 Colors Set
        else if (colors.Count == 4)
        {
            SetBaseColor(colors[0], 25);
            SetTrimColor(colors[1], 25);
            SetGemsColor(colors[2], 25);
            _gemsMeshRenderer.gameObject.SetActive(true);
            _fourthMeshRenderer.gameObject.SetActive(true);
            return;
        }
        //If 5 Colors Set
        else if (colors.Count == 5)
        {
            SetBaseColor(colors[0], 25);
            SetTrimColor(colors[1], 25);
            SetGemsColor(colors[2], 25);
            _gemsMeshRenderer.gameObject.SetActive(true);
            _fourthMeshRenderer.gameObject.SetActive(true);
            _fifthMeshRenderer.gameObject.SetActive(true);
            return;
        }
        //If 6 Colors Set
        else if (colors.Count >= 6)
        {
            SetBaseColor(colors[0], 25);
            SetTrimColor(colors[1], 25);
            SetGemsColor(colors[2], 25);
            _gemsMeshRenderer.gameObject.SetActive(true);
            _fourthMeshRenderer.gameObject.SetActive(true);
            _fifthMeshRenderer.gameObject.SetActive(true);
            _sixthMeshRenderer.gameObject.SetActive(true);
            return;
        }
    }

    private void SetBaseColor(Color color, float emissionIntensity)
    {
        _basePropBlock.SetColor("_BaseColor", color);

        _basePropBlock.SetColor("_EmissionColor", color * emissionIntensity);

        _crownMeshRenderer.SetPropertyBlock(_basePropBlock, 0); 
    }

    private void SetTrimColor(Color color, float emissionIntensity)
    {
        _trimPropBlock.SetColor("_BaseColor", color);

        _trimPropBlock.SetColor("_EmissionColor", color * emissionIntensity);

        _crownMeshRenderer.SetPropertyBlock(_trimPropBlock, 1);
    }

    private void SetGemsColor(Color color, float emissionIntensity)
    {
        _gemsPropBlock.SetColor("_BaseColor", color);

        _gemsPropBlock.SetColor("_EmissionColor", color * emissionIntensity);

        _gemsMeshRenderer.SetPropertyBlock(_gemsPropBlock);
    }
}


public static class CrownSerializer
{
    public static List<Color> GetColorListFromJSON(string crownJSON)
    {
        if (string.IsNullOrEmpty(crownJSON))
            return new List<Color>();

        List<MyColor> myColors = JsonConvert.DeserializeObject<List<MyColor>>(crownJSON);
        List<Color> colorList = new List<Color>();

        for (int i = 0; i < myColors.Count; i++)
            colorList.Add(myColors[i].GetColor());

        return colorList;
    }

    public static string GetJSONFromColorList(List<Color> colors)
    {
        List<MyColor> serializeableColorList = new List<MyColor>();

        for (int i = 0; i < colors.Count; i++)
            serializeableColorList.Add(new MyColor(colors[i]));

        return JsonConvert.SerializeObject(serializeableColorList);
    }
}

[Serializable]
public class MyColor
{
    public float R;
    public float G;
    public float B;

    public MyColor(Color color)
    {
        R = color.r;
        G = color.g;
        B = color.b;
    }

    public Color GetColor()
    {
        return new Color(R, G, B);
    }
}
