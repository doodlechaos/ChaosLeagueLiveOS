using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class TrailShop : Game
{
    [SerializeField] private List<TrailShopEntry> _entries;


    [SerializeField] private int _buyTime = 45; 


    public override void OnTilePreInit()
    {
        _entries[0].InitEntry(GetRandomGradient(1), 1, (_gt.IsGolden) ? 750 : 1_000);
        _entries[1].InitEntry(GetRandomGradient(2), 2, (_gt.IsGolden) ? 7500 : 10_000);
        _entries[2].InitEntry(GetRandomGradient(3), 3, (_gt.IsGolden) ? 75000 : 100_000);

        foreach (var entry in _entries)
            entry.HideCommandText(); 
    }

    private Gradient GetRandomGradient(int numColors)
    {
        Gradient gradient = new Gradient();
        gradient.mode = GradientMode.PerceptualBlend;

        // Create color keys
        GradientColorKey[] colorKeys = new GradientColorKey[numColors];

        float startAlpha;
        if (numColors >= 3)
            startAlpha = 0.5f;
        else if (numColors >= 2)
            startAlpha = 0.35f;
        else
            startAlpha = 0.2f;

        // Create alpha keys
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(startAlpha, 0); // Alpha starts at 1
        alphaKeys[1] = new GradientAlphaKey(0, 1); // Alpha ends at 0

        // Assign random colors at random positions for each color key
        for (int i = 0; i < numColors; i++)
        {
            colorKeys[i].color = Color.HSVToRGB(Random.value, 1, 1);
            colorKeys[i].time = Mathf.Lerp(0, 0.66f, i / (numColors - 1f)); // Distribute the colors across the gradient
        }

        // Set the color and alpha keys
        gradient.SetKeys(colorKeys, alphaKeys);

        return gradient;
    }

    public override void StartGame()
    {
        StartCoroutine(_gt.RunTimer(_buyTime));
        StartCoroutine(KillAllAfterDelay(_buyTime));

        if (_gt.Players.Count <= 0)
            return;

        foreach (var entry in _entries)
            entry.ShowCommandText();
    }

    public IEnumerator KillAllAfterDelay(int secDelay)
    {
        yield return new WaitForSeconds(secDelay); 
        _gt.EliminatePlayers(_gt.AlivePlayers.ToList(), false); 
    }


    public override void CleanUpGame()
    {
        foreach (var entry in _entries)
            entry.HideCommandText();
    }

    public override void ProcessGameplayCommand(string messageId, TwitchClient twitchClient, PlayerHandler ph, string msg, string rawEmotesRemoved)
    {
        foreach(var entry in _entries)
        {
            foreach(string buyCommand in entry.BuyCommands)
            {
                if (msg.ToLower().StartsWith(buyCommand))
                {
                    if (ph.pp.Gold < entry.GoldCost)
                    {
                        twitchClient.ReplyToPlayer(messageId, ph.pp.TwitchUsername, $"You don't have enough gold! Your current gold: {ph.pp.Gold}");
                        return;
                    }

                    //If the player handler is king, allow them to attempt the purchase without moving
                    if (ph.IsKing())
                        entry.AttemptPurchase(ph.pb);
                    else //If the player is a normal ball, move them to the entry before making the purchase
                        ph.ReceivableTarget = entry;

                    break;
                }
            }

        }
         
    }


}
