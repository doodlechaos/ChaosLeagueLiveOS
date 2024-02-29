using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Game : PlayerReceiveable
{
    [SerializeField] public GameTile _gt;

    public bool IsGameStarted = false; 
    public bool DoneWithGameplay = false; 
    public virtual void OnTilePreInit()
    {
        throw new NotImplementedException();
    }
    public virtual void OnTileInitInPos()
    {

    }
    public virtual void StartGame()
    {
        throw new NotImplementedException();
    }

    public virtual List<TravelingIndicatorIO> GetDoublingTargets()
    {
        return new List<TravelingIndicatorIO>(); 
    }

    public virtual void CleanUpGame()
    {
        throw new NotImplementedException();
    }


    public virtual void ProcessGameplayCommand(string messageId, TwitchClient twitchClient, PlayerHandler ph, string msg, string rawEmotesRemoved)
    {
        
    }
}
