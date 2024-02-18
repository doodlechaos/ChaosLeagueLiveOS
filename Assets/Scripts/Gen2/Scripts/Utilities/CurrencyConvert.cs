using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class CurrencyConvert
{

    private static Dictionary<string, FloatRate> exchangeRates; 

    public static void Init()
    {
        //Load the rates from the file
        string currencyRatesPath = Path.Combine(Application.dataPath, "StreamingAssets", "usdExchangeRates.json");
        string usdExchangeRatesJSON = File.ReadAllText(currencyRatesPath);

        exchangeRates = JsonConvert.DeserializeObject<Dictionary<string, FloatRate>>(usdExchangeRatesJSON);
        exchangeRates = CapitalizeKeys(exchangeRates); 
    }

    public static float ConvertToUSD(float amount, string currencyCode)
    {
        if (currencyCode == "USD")
        {
            return amount; // No conversion needed if already in USD
        }

        // Convert the value to USD using the current exchange rate
        float exchangeRate = GetExchangeRate(currencyCode);
        float usdValue = amount / exchangeRate;

        return usdValue;
    }

    private static float GetExchangeRate(string currency)
    {
        if(exchangeRates == null)
        {
            Debug.LogError("Exchange rates not loaded.");
            return 1; 
        }

        if (!exchangeRates.ContainsKey(currency))
        {
            Debug.LogError("File doesn't contain rate for currency: " + currency);
            return 1;
        }

        return exchangeRates[currency].rate;
    }

    static Dictionary<string, FloatRate> CapitalizeKeys(Dictionary<string, FloatRate> dictionary)
    {
        Dictionary<string, FloatRate> capitalizedDictionary = new Dictionary<string, FloatRate>();

        foreach (var kvp in dictionary)
        {
            string capitalizedKey = kvp.Key.ToUpper(); // Capitalize the key
            capitalizedDictionary[capitalizedKey] = kvp.Value;
        }

        return capitalizedDictionary;
    }
}

public class FloatRate
{
    public string code;
    public string alphaCode;
    public string numericCode;
    public string name;
    public float rate;
    public string date;
    public float inverseRate;
}