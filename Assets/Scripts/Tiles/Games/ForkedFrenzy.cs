using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForkedFrenzy : Game
{
    [SerializeField] private PipeReleaser _releasePipe; 

    public List<PlayerHandler> _releaseOrder = new List<PlayerHandler>();
    private int _releaseIndex = 0;

    public List<PlayerHandler> PlayersReadyToRecycle = new List<PlayerHandler>();

    [SerializeField] private float _releaseTimeInterval = 0.5f;
    private float _releaseTimer = 0; 

    public override void OnTilePreInit()
    {
    }

    public void DetectedPB(PlayerBall pb)
    {
        if (_releaseOrder.Contains(pb.Ph))
            return;
        _releaseOrder.Add(pb.Ph);
    }

    public override void StartGame()
    {
        _releaseOrder.Clear();
        PlayersReadyToRecycle.Clear();
        _releaseIndex = 0;

        StartCoroutine(RunGame());
    }

    public IEnumerator RunGame()
    {
        while (_gt.ConveyorBelt.Count > 0)
            yield return null;

        while (_gt.AlivePlayers.Count > 0)
        {
            yield return null; 
        }

        DoneWithGameplay = true;
    }



    public override void ProcessGameplayCommand(string messageId, TwitchClient twitchClient, PlayerHandler ph, string msg, string rawEmotesRemoved)
    {


    }

    private void Update()
    {
        if(_releaseTimer > 0)
        {
            _releaseTimer -= Time.deltaTime;
            return;
        }

        if (_releaseOrder.Count <= 0)
            return;


        PlayerHandler nextPh = _releaseOrder[_releaseIndex]; 
        //If the next valid player to be release was eliminated, dequeue them
        if (_gt.EliminatedPlayers.Contains(nextPh))
        {
            _releaseIndex = (_releaseIndex + 1) % _releaseOrder.Count;
            return;
        }

        if (PlayersReadyToRecycle.Count <= 0)
            return;

        //If they're ready to be released, release them
        if (PlayersReadyToRecycle.Contains(nextPh))
        {
            PlayersReadyToRecycle.Remove(nextPh);
            _releaseIndex = (_releaseIndex + 1) % _releaseOrder.Count;

            _releasePipe.ReceivePlayer(nextPh.GetPlayerBall());
            Debug.Log("Releasing player: " + nextPh.pp.TwitchUsername); 
            _releaseTimer = _releaseTimeInterval; 
        }


    }

    public override void CleanUpGame()
    {
        IsGameStarted = false;
    }

    public override Vector3 GetReceivePosition()
    {
        return _gt.GetTileController().HoldingPen.position;
    }

    public override void ReceivePlayer(PlayerBall pb)
    {
        pb._rb2D.simulated = true;

        if(!PlayersReadyToRecycle.Contains(pb.Ph))
            PlayersReadyToRecycle.Add(pb.Ph); 
    }
}
