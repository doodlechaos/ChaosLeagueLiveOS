using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public static class MyUtil
{
    public static bool ExtractQuotedSubstring(string str, out string quote)
    {
        // Regex pattern to find a substring within double quotes
        string pattern = "\"([^\"]*)\"";

        // Match the pattern
        Match match = Regex.Match(str, pattern);
        if (match.Success)
        {
            // Return the first captured group, which is the content inside the quotes
            quote = match.Groups[1].Value;
            return true;
        }

        // Return null if no match is found
        quote = "";
        return false;
    }
    public static bool GetFirstLongFromString(string inputString, out long num)
    {
        num = 0; 
        string pattern = @"(?<=\s)\d+(?=\s|$)"; // Matches digits surrounded by spaces or at the end of the string
        Match match = Regex.Match(inputString, pattern);

        if (!match.Success)
        {
            return false;
        }

        if (!long.TryParse(match.Groups[0].Value, out num))
        {
            return false;
        }
        return true;
    }
    public static bool GetUsernameFromString(string inputString, out string username)
    {
        username = ""; 
        string pattern = @"@([\w.-]+)";
        Match match = Regex.Match(inputString, pattern);

        if (!match.Success)
            return false; 
        
        username = match.Groups[1].Value; //group 0 includes the @ symbol

        if (string.IsNullOrEmpty(username))
            return false;

        return true;
    }

    public static string ColorToHexString(this Color color)
    {
        int r = Mathf.RoundToInt(color.r * 255f);
        int g = Mathf.RoundToInt(color.g * 255f);
        int b = Mathf.RoundToInt(color.b * 255f);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    public static Color SetColorSaveAlpha(Color color, Color alpha)
    {
        color.a = alpha.a;
        return color;
    }
    public static string GetMinuteSecString(int totalSeconds)
    {
        var timespan = TimeSpan.FromSeconds(totalSeconds);
        return timespan.ToString(@"mm\:ss");
    }

    public static string GetHourMinSecString(int totalSeconds)
    {
        var timespan = TimeSpan.FromSeconds(totalSeconds);
        if (timespan.Hours > 0)
        {
            return timespan.ToString(@"hh\:mm\:ss");
        }
        return timespan.ToString(@"mm\:ss");
    }

    public static string FormatDurationDHMS(int totalSeconds)
    {
        StringBuilder result = new StringBuilder();
        var timespan = TimeSpan.FromSeconds(totalSeconds);

        if (timespan.Days > 0)
        {
            result.Append($"{timespan.Days} day{(timespan.Days > 1 ? "s" : "")}, ");
        }

        if (timespan.Hours > 0 || timespan.Days > 0)  // Include hours if days is non-zero, even if hours is zero
        {
            result.Append($"{timespan.Hours} hour{(timespan.Hours == 1 ? "" : "s")}, ");
        }

        if (timespan.Minutes > 0 || timespan.Hours > 0 || timespan.Days > 0)  // Include minutes if hours or days is non-zero, even if minutes is zero
        {
            result.Append($"{timespan.Minutes} minute{(timespan.Minutes == 1 ? "" : "s")}, ");
        }

        result.Append($"{timespan.Seconds} sec{(timespan.Seconds == 1 ? "" : "s")}");

        return result.ToString();
    }

    public static string GetLabel(PBEffect effect, float value, int _zoneMultiplier)
    {
        string label = AbbreviateNum3Char((int)value);

        if(effect.HasFlag(PBEffect.Add) || effect.HasFlag(PBEffect.Subtract))
        {}
        else if (effect.HasFlag(PBEffect.Zero))
        {
            if(_zoneMultiplier > 0)
                label = "\U0001F607"; //Angel if in zone
            else
                label = "\U0000221E"; //Infinity if not in zone
        }
        else if (effect.HasFlag(PBEffect.Multiply))
            label = "x" + value;
        else if (effect.HasFlag(PBEffect.None))
            label = ""; 

        return label;
    }
    public static (Color meshColor, Color _labelColor) GetColorsFromValue(long value)
    {
        if (value < 0)
            return (Color.yellow, Color.black);
        else if (value <= 10)
            return (Color.HSVToRGB(0.31f, 1, Mathf.Lerp(1f, 0.7f, value / 10f)), Color.black); //Green
        else if(value <= 30)
            return (Color.HSVToRGB(0.49f, 1, Mathf.Lerp(1f, 0.7f, value / 30f)), Color.white); //Blue
        else if (value <= 100)
            return (Color.HSVToRGB(0.80f, 1, Mathf.Lerp(1f, 0.7f, value / 100f)), Color.white); //Purple
        else
        {
            if(value > 1000)
                value = 1000;
            return (Color.HSVToRGB(0.14f, 1, Mathf.Lerp(1f, 0.7f, value / 1000f)), Color.black); //Orange

        }
    }
    public static Vector3 FindClosestPointOnLine(LineRenderer lineRenderer, Vector3 nearbyPos)
    {
        Vector3 closestPoint = Vector3.zero;
        float closestDistance = float.MaxValue;

        Vector3[] linePositions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(linePositions);

        for (int i = 0; i < linePositions.Length - 1; i++)
        {
            Vector3 lineStart = linePositions[i];
            Vector3 lineEnd = linePositions[i + 1];

            // Calculate the closest point on the line segment
            Vector3 closestPointOnSegment = GetClosestPointOnSegment(lineStart, lineEnd, nearbyPos);

            // Calculate the distance between the target position and the closest point on the segment
            float distance = Vector3.Distance(nearbyPos, closestPointOnSegment);

            // Update the closest point if this point is closer
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = closestPointOnSegment;
            }
        }

        return closestPoint;
    }

    private static Vector3 GetClosestPointOnSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLength = lineDirection.magnitude;
        lineDirection.Normalize();

        Vector3 pointDirection = point - lineStart;
        float dotProduct = Vector3.Dot(pointDirection, lineDirection);

        if (dotProduct <= 0)
        {
            return lineStart;
        }
        else if (dotProduct >= lineLength)
        {
            return lineEnd;
        }
        else
        {
            return lineStart + lineDirection * dotProduct;
        }
    }

    // Given a position, find the percentage along the line.
    public static float CalculatePercentageAlongLine(LineRenderer lineRenderer, Vector3 position)
    {
        Vector3[] linePositions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(linePositions);

        float totalLength = 0f;

        // Calculate the total length of the line.
        for (int i = 0; i < linePositions.Length; i++)
        {
            Vector3 start = linePositions[i];
            Vector3 end = (i < linePositions.Length - 1) ? linePositions[i + 1] : start;
            totalLength += Vector3.Distance(start, end);
        }

        // Find the segment containing the position.
        float currentLength = 0f;
        for (int i = 0; i < linePositions.Length - 1; i++)
        {
            Vector3 start = linePositions[i];
            Vector3 end = linePositions[i + 1]; 
            float segmentLength = Vector3.Distance(start, end);

            if (segmentLength >= Vector3.Distance(start, position))
            {
                // Calculate the percentage along this segment.
                float percentage = (Vector3.Distance(position, start) + currentLength) / totalLength;
                return percentage;
            }

            currentLength += segmentLength;
        }

        return 0f; // Default to 0% if the position is not on the line.
    }

    public static string AbbreviateNum3Char(long number)
    {
        string[] suffixes = { "", "K", "M", "B", "t", "q", "Q", "", "" }; // You can extend this list if needed

        //999
        if(number < 1000)
            return number.ToString();

        int suffixIndex = 0;
        long numberCopy = number;
        while (Math.Abs(numberCopy) >= 1000 && suffixIndex < suffixes.Length - 1)
        {
            numberCopy /= 1000;
            suffixIndex++;
        }
        string numString = number.ToString();

        //Debug.Log($"NumString: {numString} numberCopy: {numberCopy}");

        // 0 - 9
        if(numberCopy < 10)
            return $"{numString[0]}.{numString[1]}{suffixes[suffixIndex]}";

        // 10 - 99
        if (numberCopy < 100)
            return $"{numString.Substring(0, 2)}{suffixes[suffixIndex]}";

        // 100 - 999
        return $"0.{numString[0]}{suffixes[suffixIndex + 1]}";

    }

    // 1.00K    123K   11.2k 999k    1.00m

    public static string AbbreviateNum4Char(long number)
    {
        string numberString = number.ToString();
        if (number < 1000)
            return numberString;

        int mag = (int)(Math.Floor(Math.Log10(number)) / 3); // Truncates to 6, divides to 2

        double divisor = Math.Pow(10, mag * 3);

        double shortNumber = number / divisor;


        //Debug.Log($"ShortNumber: {shortNumber} mag: {mag} divisor: {divisor} ");

        string suffix;
        switch (mag)
        {
            case 0:
                suffix = string.Empty;
                break;
            case 1:
                suffix = "K";
                break;
            case 2:
                suffix = "M";
                break;
            case 3:
                suffix = "B";
                break;
            case 4:
                suffix = "t";
                break;
            case 5:
                suffix = "q";
                break;
            case 6:
                suffix = "Q";
                break;
            default:
                suffix = ""; 
                break;
        }

        // 0 - 9
        if (shortNumber < 10)
            return $"{numberString[0]}.{numberString.Substring(1,2)}{suffix}";

        // 10 - 99
        if (shortNumber < 100)
            return $"{numberString.Substring(0,2)}.{numberString[2]}{suffix}";

        // 100 - 999
        return $"{numberString.Substring(0, 3)}{suffix}";

    }


    public static bool IsValidJson(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        try
        {
            JToken.Parse(input);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static T CreateDeepCopy<T>(T obj)
    {
        using (var ms = new MemoryStream())
        {
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, obj);
            ms.Seek(0, SeekOrigin.Begin);
            return (T)formatter.Deserialize(ms);
        }
    }

    public static Vector2 GetRandomPointInRect(Vector2 topL, Vector2 bottomR)
    {
        float randomX = UnityEngine.Random.Range(topL.x, bottomR.x);
        float randomY = UnityEngine.Random.Range(bottomR.y, topL.y);
        return new Vector2(randomX, randomY);   
    }
    public static T MapFloatToListVal<T>(float value, List<T> list)
    {
        // Calculate the index based on the normalized value
        int index = Mathf.FloorToInt(value * list.Count);

        // Clamp the index within the valid range
        index = Mathf.Clamp(index, 0, list.Count - 1);

        return list[index];
    }

    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        // Calculate the normalized position of the value within the from range
        float normalizedValue = Mathf.InverseLerp(fromMin, fromMax, value);

        // Remap the normalized value to the to range
        float remappedValue = Mathf.Lerp(toMin, toMax, normalizedValue);

        return remappedValue;
    }

    
    public static string CensorStringV2(string input, List<string> bannedWords)
    {
        string censoredString = input;
        foreach (string bannedWord in bannedWords)
        {
            censoredString = censoredString.Replace(bannedWord, new string('*', bannedWord.Length), StringComparison.OrdinalIgnoreCase);
        }

        return censoredString;
    }


    public static bool IsLayerInMask(int layer, LayerMask layerMask)
    {
        int layerMaskValue = layerMask.value;
        int layerBitValue = 1 << layer;
        return (layerMaskValue & layerBitValue) == layerBitValue;
    }
    public static int ConvertLayerMaskToLayerNumber(LayerMask layerMask)
    {
        int layerNumber = Mathf.RoundToInt(Mathf.Log(layerMask.value, 2));
        return layerNumber;
    }

    //Zero through 9
    public static bool StartsWithNumber(string input, out int number)
    {
        number = -1;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        char firstChar = input[0];
        if (char.IsDigit(firstChar))
        {
            int.TryParse(firstChar.ToString(), out number);
            return true;
        }

        return false;
    }

    //Ignores message if @ symbol found before number
    public static int ExtractFirstSingleDigit(string input)
    {
        int result = -1;
        bool ignoreDigits = false;

        foreach (char c in input)
        {
            if (char.IsDigit(c))
            {
                if (ignoreDigits)
                {
                    continue; // Ignore digits enclosed by '<' and '>'
                }

                int digit = int.Parse(c.ToString());
                if (digit >= 0 && digit <= 9)
                {
                    result = digit;
                    break; // Found the first single digit, exit the loop
                }
            }
            else if (c == '@')
                return -1;
            else if (c == '<')
            {
                ignoreDigits = true; // Start ignoring digits inside '<' and '>'
            }
            else if (c == '>')
            {
                ignoreDigits = false; // Stop ignoring digits inside '<' and '>'
            }
        }

        return result;
    }

    public static float CalculateLineLength(LineRenderer lr)
    {
        float totalLength = 0f;

        for (int i = 1; i < lr.positionCount; i++)
        {
            Vector3 startPoint = lr.GetPosition(i - 1);
            Vector3 endPoint = lr.GetPosition(i);
            totalLength += Vector3.Distance(startPoint, endPoint);
        }

        return totalLength;
    }

    public static Color GetColorFromHex(string hex, Color _default)
    {
        if(string.IsNullOrEmpty(hex))
            return _default;

        hex = hex.Contains('#') ? hex : "#" + hex;

        if (ColorUtility.TryParseHtmlString(hex, out Color color))
            return color;
        else
            return _default; 
    }
}
