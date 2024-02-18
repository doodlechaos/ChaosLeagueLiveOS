using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableGradient
{
    public MyColorKey[] ColorKeys;
    public MyAlphaKey[] AlphaKeys;

    public GradientMode Mode;

    public SerializableGradient(Gradient gradient)
    {
        if (gradient == null)
            return;

        if(gradient.colorKeys == null)
            ColorKeys = new MyColorKey[0];
        else
        {
            ColorKeys = new MyColorKey[gradient.colorKeys.Length];
            for (int i = 0; i < gradient.colorKeys.Length; i++)
            {
                GradientColorKey currKey = gradient.colorKeys[i];
                ColorKeys[i] = new MyColorKey() { Time = currKey.time, R = currKey.color.r, G = currKey.color.g, B = currKey.color.b };
            }
        }

        if(gradient.alphaKeys == null)
            AlphaKeys = new MyAlphaKey[0];
        else
        {
            AlphaKeys = new MyAlphaKey[gradient.alphaKeys.Length];
            for (int i = 0; i < gradient.alphaKeys.Length; i++)
            {
                GradientAlphaKey currKey = gradient.alphaKeys[i];
                AlphaKeys[i] = new MyAlphaKey() { Time = currKey.time, Alpha = currKey.alpha };
            }
        }



        Mode = gradient.mode;
    }

    public Gradient GetGradient()
    {
        GradientColorKey[] colorKeys_g = new GradientColorKey[ColorKeys.Length];
        for (int i = 0; i < colorKeys_g.Length; i++)
            colorKeys_g[i] = ColorKeys[i].GetKey();
        

        GradientAlphaKey[] alphaKeys_g = new GradientAlphaKey[AlphaKeys.Length]; 

        for (int i = 0; i < alphaKeys_g.Length; i++)
            alphaKeys_g[i] = AlphaKeys[i].GetKey();

        Gradient gradient = new Gradient
        {
            colorKeys = colorKeys_g,
            alphaKeys = alphaKeys_g,

            mode = Mode
        };
        return gradient; 
    }
}

[Serializable]
public class MyColorKey
{
    public float Time;
    public float R;
    public float G;
    public float B;

    public GradientColorKey GetKey()
    {
        GradientColorKey key = new GradientColorKey()
        {
            time = Time,
            color = new Color(R, G, B),
        }; 
        return key;
    }
}

[Serializable]
public class MyAlphaKey
{
    public float Time;
    public float Alpha; 

    public GradientAlphaKey GetKey()
    {
        GradientAlphaKey key = new GradientAlphaKey()
        {
            time = Time,
            alpha = Alpha,
        };
        return key;
    }
}

public static class GradientSerializer
{
    public static string SerializeGradient(Gradient gradient)
    {
        if(gradient == null)
        {
            Debug.LogError("Failed to serialize null gradient");
            return null;
        }

        SerializableGradient serializableGradient = new SerializableGradient(gradient);

        return JsonConvert.SerializeObject(serializableGradient);
    }

    public static Gradient DeserializeGradient(string json)
    {
        SerializableGradient serializableGradient = JsonConvert.DeserializeObject<SerializableGradient>(json);
        
        return serializableGradient.GetGradient();
    }
}