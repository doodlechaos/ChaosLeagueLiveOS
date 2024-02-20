using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using TwitchLib.Api.Helix.Models.Polls.CreatePoll;
using UnityEngine;

//MAX PLAYERS: 5, Due to poll limitation on twitch
public class QuipBattleGame : Game
{
    [SerializeField] private TextMeshPro _promptText;
    [SerializeField] private Transform _playerPositionsTop;
    [SerializeField] private Transform _playerPositionsBottom;

    [SerializeField] private int _pollDurationSeconds;

    [SerializeField] private Transform _voteCountTextsRoot;

    private TextMeshPro[] _voteCountTexts;

    [SerializeField] private int _baseVoteReward = 50;
    //private int _currVoteReward = 0;

    [SerializeField] private PBEffector _voteValueEffector;

    //[SerializeField] private QuipBattleGameQuestions qBGQ = new();

    private void Awake()
    {
        _voteCountTexts = _voteCountTextsRoot.GetComponentsInChildren<TextMeshPro>();
    }

    public override void OnTilePreInit()
    {
        string prompt = AppConfig.QuipBattleQuestions.GetPrompt();
        //string prompt = QuipBattlePrompts.inst.prompts[_gt.GetTileController().GetNextPromptIndex()];
        _promptText.SetText(prompt);
        //Debug.Log($"Setting next prompt to {prompt}");

        foreach (var voteCount in _voteCountTexts)
            voteCount.SetText("");

        int currVoteReward = AppConfig.GetMult(_gt.RarityType) * _baseVoteReward;

        if (_gt.IsGolden)
            currVoteReward *= AppConfig.inst.GetI("GoldenTileMultiplier");

        _voteValueEffector.SetCurrValue(currVoteReward);
    }

    public override void StartGame()
    {
        Debug.Log("Starting Quip battle");

        _voteValueEffector.IncrementCurrValue(_gt.TicketBonusAmount / 10); 

        //Spread eachplayer out on a line between the defined top and bottom positions
        for (int i = 0; i < _gt.AlivePlayers.Count; i++)
        {
            PlayerHandler ph = _gt.AlivePlayers[i];

            //Lock their speech bubble direcitions based on the side
            ph.GetPlayerBall().LockSpeechBubbleAngle = true;
            ph.GetPlayerBall().LockedSpeechBubbleAngle = 0; 

            Vector2 step = (_playerPositionsBottom.position - _playerPositionsTop.position) / (_gt.AlivePlayers.Count - 1);

            Vector2 targetPos = _playerPositionsTop.position + (Vector3)step * i;

            //Move the vote count text to position based on the ball
            _voteCountTexts[i].transform.position = new Vector2(_voteCountTexts[i].transform.position.x, targetPos.y);
            _voteCountTexts[i].SetText("Votes: 0"); 

            ph.GetPlayerBall().AddPriorityWaypoint(targetPos, 0.15f);
            Debug.Log($"Moving player {ph.pp.TwitchUsername} to position {targetPos}");
        }

        //Clear any voteCountTexts that are unused
        for (int i = _gt.AlivePlayers.Count; i < _voteCountTexts.Length; i++)
            _voteCountTexts[i].SetText(""); 

        List<Choice> pollChoices = new List<Choice>();
        for(int i = 0; i < _gt.AlivePlayers.Count; i++)
        {
            pollChoices.Add(new Choice { Title = _gt.AlivePlayers[i].pp.TwitchUsername });
        }

        _ = TwitchApi.StartPoll("Who has the funniest response?", pollChoices, _pollDurationSeconds);

        StartCoroutine(RunGame(_gt)); 
    }

    private IEnumerator GetVotesListV2(CoroutineResult<List<(int votes, string username)>> coResult)
    {
        Debug.Log("Getting poll results");
        var t = Task.Run(async () =>
        {
            try
            {
                return await TwitchApi.GetPollResults();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching poll results: {ex.Message}");
                return null; // Indicate failure
            }
        });

        float timeout = 4f; // 10 seconds timeout
        float startTime = Time.time;

        // Wait for task completion or timeout
        while (!t.IsCompleted && Time.time - startTime < timeout)
        {
            yield return null;
        }

        // Handle task completion or timeout
        if (t.IsCompleted)
        {
            // Check if the task result is null (indicating an error occurred)
            if (t.Result == null)
            {
                coResult.Complete(null); // Complete with null or appropriate error result
            }
            else
            {
                var choices = t.Result;

                List<(int votes, string username)> votesList = new List<(int votes, string username)>();

                foreach (var choice in choices)
                    votesList.Add((choice.Votes, choice.Title));
                
                coResult.Complete(votesList); // Complete with actual results
            }
        }
        else
        {
            Debug.LogError("Timeout occurred while waiting for poll results");
            coResult.Complete(null); // Complete with null or timeout indication
        }
    }

    private IEnumerator RunGame(GameTile gt)
    {
        while (gt.ConveyorBelt.Count > 0)
            yield return null;

        var coResult = new CoroutineResult<List<(int votes, string username)>>();

        List<(int votes, string username)> prevVotes = new List<(int votes, string username)>();

        //Update the background shader
        int secondsPassed = 0;
        StartCoroutine(gt.RunTimer(_pollDurationSeconds));
        while(secondsPassed < _pollDurationSeconds)
        {
            yield return new WaitForSeconds(1);
            secondsPassed++;
            //Every 5 seconds, fetch the poll voting results
            if (secondsPassed % 5 != 0)
                continue;

            coResult.Reset();
            yield return GetVotesListV2(coResult);

            var voteList1 = coResult.Result;
            if (voteList1 == null)
            {
                Debug.LogError("Failed to get voteList in quip battle game"); 
                continue;
            }

            for (int i = 0; i < voteList1.Count && i < _voteCountTexts.Length; i++)
            {
                _voteCountTexts[i].SetText($"Votes: {voteList1[i].votes}");

                int voteChange = voteList1[i].votes;
                if (prevVotes.Count >= voteList1.Count) //If it's the first time we read the poll, just take whatever the vote amount as the change so long as it's zero, because the prevList isn't populated yet
                    voteChange = voteList1[i].votes - prevVotes[i].votes;
                
                if (voteChange > 0)
                {
                    //int votesPointBonus = Mathf.Max(1, voteChange * (gt.TicketBonusAmount / 10));
                    
                    int votesPointBonus = Mathf.Max(1, voteChange * (int)_voteValueEffector.GetZoneMultiplyAppliedValue());
                    TextPopupMaster.Inst.CreateTextPopup(_voteCountTexts[i].transform.position, Vector2.up, $"votes +{voteChange}", Color.cyan);

                    PlayerHandler ph = gt.GetAlivePlayerViaUsername(voteList1[i].username);
                    if(ph != null)
                        TextPopupMaster.Inst.CreateTravelingIndicator(MyUtil.AbbreviateNum4Char(votesPointBonus), votesPointBonus, _voteCountTexts[i].transform.position, ph, 0.1f, Color.cyan, null, TI_Type.GivePoints);

                }
            }

            prevVotes = voteList1.ToList(); 
        }

        coResult.Reset();

        yield return GetVotesListV2(coResult);

        if(coResult.Result == null)
        {
            Debug.LogError("Failed to get final vote list in quip battle game. Eliminating all players");
            gt.EliminatePlayers(gt.AlivePlayers.ToList(), false);
            yield break;
        }

        var votesList = coResult.Result.OrderBy(v => v.votes).ToList();

        //Kill all the players in the order of the choices
        foreach (var item in votesList)
        {
            PlayerHandler ph = gt.GetAlivePlayerViaUsername(item.username);

            if(ph == null)
            {
                Debug.Log($"Failed to find player handler for {item.username} in quip battle.");
                continue;
            }
            ph.SetRankScore(item.votes);
        }

        gt.EliminatePlayers(gt.AlivePlayers.ToList(), false);
    }


    public override void ProcessGameplayCommand(string messageId, TwitchClient twitchClient, PlayerHandler ph, string msg, string rawEmotesRemoved)
    {
        if (!_gt.Players.Contains(ph))
            return;

        if (msg.StartsWith("!"))
            return;

        MyTTS.inst.SpeechMaster(rawEmotesRemoved, Amazon.Polly.VoiceId.Joey, MyTTS.AudioPitch.High, false);
    }

    public override void CleanUpGame()
    {
        IsGameStarted = false;

    }
}
