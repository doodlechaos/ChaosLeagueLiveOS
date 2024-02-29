using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class SimonSaysGame : Game
{
    [SerializeField] private int _maxGameDuration = 90;

    //[SerializeField] private int _secPerDecisionTimer = 20;

    [SerializeField] private Transform _playerPositionsTop;
    [SerializeField] private Transform _playerPositionsBottom;

    [SerializeField] private int _baseSurvivalReward = 25; //50, 100, 200, 400
    [SerializeField] private PBEffector _survivalReward;

    public override void OnTilePreInit()
    {
        int survivalReward = AppConfig.GetMult(_gt.RarityType) * _baseSurvivalReward;

        if (_gt.IsGolden)
            survivalReward *= AppConfig.inst.GetI("GoldenTileMultiplier");

        _survivalReward.SetCurrValue(survivalReward);
    }

    public override void StartGame()
    {

        foreach (var ph in _gt.AlivePlayers)
        {
            var rb2D = ph.pb._rb2D;
            rb2D.gravityScale = 0;
            rb2D.drag = 1f;
            rb2D.angularDrag = 0.1f;
            rb2D.velocity = Vector2.zero;
            rb2D.angularVelocity = 0;
        }

        //Spread eachplayer out on a line between the defined top and bottom positions
        for (int i = 0; i < _gt.AlivePlayers.Count; i++)
        {
            PlayerHandler ph = _gt.AlivePlayers[i];

            //Lock their speech bubble direcitions based on the side
            ph.GetPlayerBall().LockSpeechBubbleAngle = true;
            ph.GetPlayerBall().LockedSpeechBubbleAngle = 180;

            Vector2 step = (_playerPositionsBottom.position - _playerPositionsTop.position) / (_gt.AlivePlayers.Count - 1);

            Vector2 targetPos = _playerPositionsTop.position + (Vector3)step * i;

            ph.GetPlayerBall().AddPriorityWaypoint(targetPos, 0.15f);
            Debug.Log($"Moving player {ph.pp.TwitchUsername} to position {targetPos}");
        }

        _survivalReward.IncrementCurrValue(_gt.TicketBonusAmount / 10);
        StartCoroutine(RunGame());

    }

    public IEnumerator RunGame()
    {
        float timer = 0;
        while (_gt.AlivePlayers.Count > 1)
        {
            timer += Time.deltaTime;

            if (timer >= _maxGameDuration)
                break;

            _gt.SetBackgroundShader(1 - (timer / _maxGameDuration));

            yield return null;
        }

        for(int i = _gt.AlivePlayers.Count - 1; i >= 0; i--)
            _gt.EliminatePlayer(_gt.AlivePlayers[i], int.MaxValue);

    }

    /*    public IEnumerator RunGame()
        {
            int prevPlayersAlive = _gt.AlivePlayers.Count;
            float timer = 0;
            while (_gt.AlivePlayers.Count > 1)
            {
                //If the number of players alive changes, reset the timer
                if (_gt.AlivePlayers.Count != prevPlayersAlive)
                    timer = 0;

                timer += Time.deltaTime;

                //If the timer surpases the decision time limit and the king hasn't made a decision, kill a random player
                if (timer >= _secPerDecisionTimer)
                {
                    PlayerHandler randomPh = _gt.AlivePlayers[Random.Range(0, _gt.AlivePlayers.Count)];
                    KillPlayer(randomPh);
                    timer = 0;
                }

                _gt.SetBackgroundShader(1 - (timer / _secPerDecisionTimer));

                prevPlayersAlive = _gt.AlivePlayers.Count;
                yield return null;
            }

        }*/

    public override void ProcessGameplayCommand(string messageId, TwitchClient twitchClient, PlayerHandler ph, string msg, string rawEmotesRemoved)
    {
        string commandKey = msg.ToLower();

        //If you're in the tile, you can use TTS
        if (_gt.AlivePlayers.Contains(ph))
        {
            if (msg.StartsWith("!"))
                return;

            MyTTS.inst.SpeechMaster(rawEmotesRemoved, Amazon.Polly.VoiceId.Joey, MyTTS.AudioPitch.High, false);
            return;
        }

        if (!ph.IsKing())
            return;

        //Parse the username out from after 
        var mentions = msg.Split(' ').Where(word => word.StartsWith("@")).Select(word => word.Substring(1)).ToList();

        if (mentions.Count <= 0)
            return;

        //Get the target player handler that matches the username
        PlayerHandler targetPh = _gt.AlivePlayers.Find(x => string.Equals(x.pp.TwitchUsername, mentions[0], System.StringComparison.OrdinalIgnoreCase));

        if (targetPh == null)
        {
            Debug.Log("TargetPlayerHandler is null");
            twitchClient.ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Your command failed to find a valid target username in play. The correct format is: @username");
            return;
        }

        if (targetPh.pb == null || targetPh.pb.IsExploding)
            return;

        DrawLineMaster.Inst.DrawLine(ph.GetBallPos(), 0, targetPh.GetBallPos(), 1, 2f, Color.red);
        KillPlayer(targetPh);
    }

    private void KillPlayer(PlayerHandler ph)
    {
        ph.SubtractPoints((long)_survivalReward.GetZoneMultiplyAppliedValue(), false, true);
        _gt.EliminatePlayer(ph, true);
        AudioController.inst.PlaySound(AudioController.inst.Beheading, 0.9f, 1.1f);
        AudioController.inst.PlaySound(AudioController.inst.DeathScream, 0.9f, 1.5f);
        //Give points to the surviving players
        foreach (var player in _gt.AlivePlayers)
            player.AddPoints((long)_survivalReward.GetZoneMultiplyAppliedValue(), true, Vector3.up);

        _survivalReward.MultiplyCurrValue(2);
    }

    public override void CleanUpGame()
    {
        IsGameStarted = false;
    }

}
