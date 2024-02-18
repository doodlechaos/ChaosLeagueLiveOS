using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ColorExtensions
{
    public static Color WithAlpha(this Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    public static Color BlackOrWhiteHighestContrast(this Color color)
    {
        // Calculate luminance
        float luminance = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;

        // Determine if the color is closer to black or white
        if (luminance >= 0.5f)
        {
            // Closer to white, return black
            return Color.black;
        }
        else
        {
            // Closer to black, return white
            return Color.white;
        }

    }
}
