using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using TMPro;
using TwitchLib.Api.Helix;
using TwitchLib.Client.Events;
using UnityEngine;
using UnityEngine.UI;

public class UnitTesting : MonoBehaviour
{
    [SerializeField] private TwitchClient _twitchClient;
    [SerializeField] private TwitchPubSub _twitchPubSub;
    [SerializeField] private TileController _tileController;

    [SerializeField] private bool incrementUserId;
    [SerializeField] private int idIncrementor = 0;
    [SerializeField] private string testUserId;
    [SerializeField] private string testUsername;
    [SerializeField] private string userInput;
    [SerializeField] private Color nameColor;
    [SerializeField] private bool randomizeNameColor = true; 
    [SerializeField] private int rewardCost;
    [SerializeField] private string rewardTitle; 
    [SerializeField] private int bits;
    [SerializeField] private bool isAdmin = true; 
    [SerializeField] private bool isMod = true; 
    [SerializeField] private bool isVIP = true; 

    [SerializeField] private bool RegularMessageButton;
    [SerializeField] private bool RedeemRewardButton;
    [SerializeField] private bool SendBitsButton;
    [SerializeField] private bool SendSubButton;
    [SerializeField] private bool SendGiftedSubButton;
    [SerializeField] private string SubGiftRecipientId;
    [SerializeField] private string SubGiftRecipientUsername;
    [SerializeField] private int MultiMonthDuration = 1;
    [SerializeField] private TwitchLib.PubSub.Enums.SubscriptionPlan SubPlan;

    [SerializeField] private PredictionObj testPredObj;
    [SerializeField] private bool testPrediction;
    [SerializeField] private bool isSubscriber;
    [SerializeField] private bool isFirstMessage;

    [SerializeField] private bool autoTest;
    [SerializeField] private int autoTestSecInterval;

    [SerializeField] private bool testRandomTiles;

    [SerializeField] private bool JobexiTest;


    private float autoTestTimer = 0;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                for (int i = 0; i < 4; i++)
                    RedeemReward();
                return;
            }
            RedeemReward();
        }
        if(Input.GetKeyDown(KeyCode.M)) 
        {
            RegularMessage();
        }
        if(Input.GetKeyDown(KeyCode.B)) 
        {
            SendBits(); 
        }
        if (autoTest)
        {
            autoTestTimer += Time.deltaTime;

            if(autoTestTimer > autoTestSecInterval)
            {
                autoTestTimer = 0; 

                string userID = GetUserId();
                string username = testUsername + userID;
                StartCoroutine(_twitchPubSub.HandleOnChannelPointsRedeemed(userID, username, rewardTitle, userInput, rewardCost)); //Pubsub activate both
                StartCoroutine(_twitchClient.HandleMessage(null, userID, username, GetNameColor(), userInput, emotes:null, isSubscriber, isFirstMessage, bits, isAdmin, isMod, isVIP));
            }
        }
    }
    private void OnValidate()
    {
        if (JobexiTest)
        {
            JobexiTest = false;
            CurrentTest();
        }

        if (RegularMessageButton)
        {
            RegularMessageButton = false;
            RegularMessage();
        }

        if (RedeemRewardButton)
        {
            RedeemRewardButton = false;
            RedeemReward(); 
        }

        if (SendBitsButton)
        {
            SendBitsButton = false;
            SendBits();
        }

        if (SendSubButton)
        {
            SendSubButton = false;
            string userID = GetUserId();
            string username = testUsername + userID;
            StartCoroutine(_twitchPubSub.HandleOnSubscription(userID, username, MultiMonthDuration, SubPlan)); 
        }

        if (SendGiftedSubButton)
        { 
            SendGiftedSubButton = false;
            string userID = GetUserId();
            string username = testUsername + userID;
            StartCoroutine(_twitchPubSub.HandleGiftSubscription(userID, username, SubGiftRecipientId, SubGiftRecipientUsername, MultiMonthDuration, SubPlan));
        }

        if (testPrediction)
        {
            testPrediction = false;

            _ = TwitchApi.StartPrediction(testPredObj); 
        }

        if (testRandomTiles)
        {
            testRandomTiles = false;
            TestRandomTiles();
        }
    }
    

    private void CurrentTest()
    {
        string userID = GetUserId();
        string username = testUsername + userID;
        StartCoroutine(_twitchClient.HandleMessage(null, userID, username, GetNameColor(), userInput, emotes: null, isSubscriber, isFirstMessage, bits, isAdmin, isMod, isVIP));
    }

    private void RegularMessage()
    {
        string userID = GetUserId();
        string username = testUsername + userID;
        StartCoroutine(_twitchClient.HandleMessage(null, userID, username, GetNameColor(), userInput, emotes: null, isSubscriber, isFirstMessage, bits, isAdmin, isMod, isVIP));
    }
    private void RedeemReward()
    {
        string userID = GetUserId();
        string username = testUsername + userID;
        StartCoroutine(_twitchPubSub.HandleOnChannelPointsRedeemed(userID, username, rewardTitle, userInput, rewardCost)); //Pubsub activate both
        StartCoroutine(_twitchClient.HandleMessage(null, userID, username, GetNameColor(), userInput, emotes: null, isSubscriber, isFirstMessage, bits, isAdmin, isMod, isVIP));
    }
    private void SendBits()
    {
        string userID = GetUserId();
        string username = testUsername + userID;
        StartCoroutine(_twitchPubSub.HandleOnBitsReceived(userID, username, userInput, bits)); //Pubsub activate both
        StartCoroutine(_twitchClient.HandleMessage(null, userID, username, GetNameColor(), userInput, emotes: null, isSubscriber, isFirstMessage, bits, isAdmin, isMod, isVIP));
    }
    private string GetUserId()
    {
        if (!incrementUserId)
            return testUserId;

        idIncrementor++;
        return testUserId + idIncrementor.ToString();

    }

    private Color GetNameColor()
    {
        if (randomizeNameColor)
            return UnityEngine.Random.ColorHSV();

        return nameColor;
    }

    public class TileTest
    {
        public Dictionary<RarityType, int> rarityCounts= new Dictionary<RarityType, int>() { {RarityType.Common, 0 }, {RarityType.Rare, 0 }, {RarityType.Epic, 0 }, {RarityType.Legendary, 0 } };
    }

    private void TestRandomTiles()
    {
        int totalTiles = 10_000_000;
        Dictionary<int, TileTest> tileCounts = new Dictionary<int, TileTest>();

        for (int i = 0; i < totalTiles; i++)
        {
            (int idNum, RarityType rarity) = _tileController.GetRandomIDandRarity(null);
            if (tileCounts.ContainsKey(idNum))
                tileCounts[idNum].rarityCounts[rarity]++;
            else
            {
                tileCounts[idNum] = new TileTest();
                tileCounts[idNum].rarityCounts[rarity] = 1;
            }
        }

        int totalCommons = 0;
        foreach (var id in tileCounts.Keys)
            totalCommons += tileCounts[id].rarityCounts[RarityType.Common];

        int totalRares = 0;
        foreach (var id in tileCounts.Keys)
            totalRares += tileCounts[id].rarityCounts[RarityType.Rare];

        int totalEpics = 0;
        foreach (var id in tileCounts.Keys)
            totalEpics += tileCounts[id].rarityCounts[RarityType.Epic];

        int totalLegendaries = 0;
        foreach (var id in tileCounts.Keys)
            totalLegendaries += tileCounts[id].rarityCounts[RarityType.Legendary];


        Debug.Log($"TOTAL TILES SPAWN RATES OUT OF {totalTiles} \n" +
                    $"commons: {totalCommons / (float)totalTiles * 100}% \n"
                  + $"rares: {totalRares / (float)totalTiles * 100}% \n"
                  + $"epics: {totalEpics / (float)totalTiles * 100}% \n"
                  + $"legendaries: {totalLegendaries / (float)totalTiles * 100}% \n");

        //For each tile id, print out its percent in each rarity
        foreach (var id in tileCounts.Keys)
        {
            var tile = tileCounts[id];
            int allRarities = tile.rarityCounts[RarityType.Common] 
                + tile.rarityCounts[RarityType.Rare]
                + tile.rarityCounts[RarityType.Epic]
                + tile.rarityCounts[RarityType.Legendary];

            Debug.Log($"TILE ID: {id}   Total: ({allRarities}) \n" +
                        $"COMMON     ({tile.rarityCounts[RarityType.Common]})      {Math.Round(tile.rarityCounts[RarityType.Common] / (float)allRarities * 100, 2)}% \n"
                      + $"RARE:      ({tile.rarityCounts[RarityType.Rare]})      {Math.Round(tile.rarityCounts[RarityType.Rare] / (float)allRarities * 100, 2)}% \n"
                      + $"EPIC:      ({tile.rarityCounts[RarityType.Epic]})      {Math.Round(tile.rarityCounts[RarityType.Epic] / (float)allRarities * 100, 2)}% \n"
                      + $"LEGENDARY: ({tile.rarityCounts[RarityType.Legendary]})      {Math.Round(tile.rarityCounts[RarityType.Legendary] / (float)allRarities * 100, 2)}% \n");
        }

    }
}
