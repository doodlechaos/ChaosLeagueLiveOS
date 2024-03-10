using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class DangerZoneGame : Game, TravelingIndicatorIO
{
    private enum GameState { waiting, collectingCommands, pulling, gridDividing, spinning}
    private GameState _state;

    [SerializeField] private List<DangerZone> _dangerZones;
    [SerializeField] private int _rounds = 2;

    [SerializeField] private int _commandCollectionDuration = 20;
    [SerializeField] private float _spinDuration = 4.5f;
    [SerializeField] private float _pullForceDuration = 4f; 

    [SerializeField] private TextMeshPro _roundTimer;
    [SerializeField] private TextMeshPro _roundsRemainingText;
    [SerializeField] private GameObject _gridDividersRoot;
    [SerializeField] private TextMeshPro _stateText;

    private List<Arrow> _activeArrows = new List<Arrow>();

    private int _currZonePosIndex = 3;

    [SerializeField] private PBEffector _roundRewardEffector;
    [SerializeField] private int _baseSurvivalReward = 100;

    //TODO: Write "Prize is divided among final survivors" If they both die on the last round they split it

    public override void OnTilePreInit()
    {
        //Debug.Log("On tile init visuals in Danger Zone");
        int survivalReward = AppConfig.GetMult(_gt.RarityType) * _baseSurvivalReward;

        if (_gt.IsGolden)
            survivalReward *= AppConfig.inst.GetI("GoldenTileMultiplier");

        _roundRewardEffector.SetCurrValue(survivalReward);
    }

    public override void StartGame()
    {
        //Debug.Log("Starting Danger Zone Game"); 
        //Set all the balls to zero gravity

        _roundRewardEffector.IncrementCurrValue(_gt.TicketBonusAmount / 10);

        SetState(GameState.waiting); 
        //Spread the balls out evenly on the _danger zone tiles
        StartCoroutine(RunGame(_gt)); 
    }

    public IEnumerator RunGame(GameTile gt)
    {
        while (_gt.ConveyorBelt.Count > 0)
            yield return null;

        foreach (var ph in gt.AlivePlayers)
        {
            var rb2D = ph.pb._rb2D;
            rb2D.gravityScale = 0;
            rb2D.drag = 1f;
            rb2D.angularDrag = 0.1f;
            rb2D.velocity = Vector2.zero;
            rb2D.angularVelocity = 0;
        }

        for (int round = 0; round < _rounds; round++)
        {
            if (gt.AlivePlayers.Count <= 1)
                break;

            StartRound(round, gt.AlivePlayers);

            //Wait for all the players to arrive in position, then turn on simulation for them
            yield return new WaitForSeconds(3);
            foreach (var ph in gt.AlivePlayers)
                ph.pb.EnableDynamicPhysicsMode(); 

            SetState(GameState.collectingCommands); 
            //Wait for timer to finish
            for (int s = 0; s < _commandCollectionDuration; s++)
            {
                _roundTimer.SetText(MyUtil.GetMinuteSecString(_commandCollectionDuration - s));
                yield return new WaitForSeconds(1);
            }
            _roundTimer.SetText(MyUtil.GetMinuteSecString(0));

            SetState(GameState.pulling);
            //Apply the attractive force based on all the arrows
            float pullTimer = 0;
            while(pullTimer < _pullForceDuration)
            {
                foreach(var arrow in _activeArrows)
                {
                    PlayerBall pb = arrow.StartPoint.GetComponentInParent<PlayerBall>();
                    if (pb == null)
                        continue;
                    Vector2 pullForce = arrow.EndPoint.position - arrow.StartPoint.position;
                    pb._rb2D.AddForce(pullForce);
                }
                pullTimer += Time.deltaTime; 
                yield return null; 
            }

            SetState(GameState.gridDividing); 

            _gridDividersRoot.SetActive(true);
            yield return new WaitForSeconds(2);

            SetState(GameState.spinning);

            //Delete all the arrows
            foreach (var arrow in _activeArrows)
                arrow.DestroyArrow();
            _activeArrows.Clear();

            foreach (var zone in _dangerZones)
                StartCoroutine(zone.Spin(round, _spinDuration));

            yield return new WaitForSeconds(_spinDuration);

            //Delete all the arrows
            foreach (var arrow in _activeArrows)
                arrow.DestroyArrow();
            _activeArrows.Clear();

            //Wait a beat after the players explode
            yield return new WaitForSeconds(1f);

            int roundSurvivalReward = (int)_roundRewardEffector.GetZoneMultiplyAppliedValue(); //Mathf.Max(1, (_gt.TicketBonusAmount / 10) * (round + 1));

            //Give points to the surviving players
            foreach (var player in _gt.AlivePlayers)
                player.AddPoints(roundSurvivalReward, true, Vector3.up);
            
            yield return new WaitForSeconds(1);

            if (_gt.AlivePlayers.Count == 1)
            {
                _gt.AlivePlayers.First().pb.ExplodeBall();
                DoneWithGameplay = true;
                yield break;
            }
            //Starting new round, double the reward
            _roundRewardEffector.MultiplyCurrValue(2); 
        }

        DoneWithGameplay = true; 
    }

    private void StartRound(int roundNum, List<PlayerHandler> phs)
    {
        Debug.Log("Starting round with total player count: " + phs.Count);
        _roundsRemainingText.SetText($"Round: {roundNum + 1}/{_rounds}");

        //Randomize the order of the alive players;
        phs = phs.OrderBy(x => Random.value).ToList();

        _currZonePosIndex = 0; 
        foreach (var ph in phs)
        {
            Vector3 targetPos = GetNextOpenZonePos();
            ph.pb.AddPriorityWaypoint(targetPos, 0.2f);
        }

        _gridDividersRoot.SetActive(false);

        //Remaining tiles are random
        foreach (var zone in _dangerZones)
            zone.InitZone(Random.Range(0f, 1f));

        //Pick a random tile that has a player on it to be 100% life
        int safeTileIndex = Random.Range(0, phs.Count);
        safeTileIndex %= 6;
        _dangerZones[safeTileIndex].InitZone(0);

        //Pick a random tile that has a player on it to be 100% death 
        int deathTileIndex;
        do
        {
            deathTileIndex = Random.Range(0, phs.Count);
            deathTileIndex %= 6;
        } while (deathTileIndex == safeTileIndex);

        _dangerZones[deathTileIndex].InitZone(1);

    }

    private void DrawPullArrow(PlayerHandler puller, PlayerHandler target)
    {
        //Check if the player has already drawn an arrow, if so, remove it
        for(int i = _activeArrows.Count - 1; i  >= 0; i--)
        {
            var arrow = _activeArrows[i];
            if (arrow.EndPoint.position == puller.GetPlayerBall()._rb2D.transform.position)
            {
                arrow.DestroyArrow();
                _activeArrows.RemoveAt(i);
                break;
            }
        }

        //Draw an arrow from the target to the puller
        Transform startPos = target.GetPlayerBall()._rb2D.transform;
        Transform endPos = puller.GetPlayerBall()._rb2D.transform;
        float ballRadius = target.GetPlayerBall()._rb2D.transform.localScale.x / 2;
        Arrow newArrow = DrawArrowMaster.Inst.DrawArrow(startPos, ballRadius, endPos, ballRadius, false, puller.NameTextColor);
        _activeArrows.Add(newArrow);
    }


    public override void ProcessGameplayCommand(string messageId, TwitchClient twitchClient, PlayerHandler ph, string msg, string rawEmotesRemoved)
    {
        string commandKey = msg.ToLower();

        //You must be a player to use the command
        if (!_gt.Players.Contains(ph))
            return;

        //Parse the username out from after 
        var mentions = msg.Split(' ').Where(word => word.StartsWith("@")).Select(word => word.Substring(1)).ToList();

        if (mentions.Count <= 0)
            return;

        if (_state != GameState.collectingCommands && _state != GameState.gridDividing)
        {
            twitchClient.ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You can only pull while the tile is in 'Collecting commands' mode.");
            return;
        }

        Debug.Log($"first username: [{mentions[0]}]");

        //Get the target player handler that matches the username
        var targetPlayerHandler = _gt.AlivePlayers.Find(x => string.Equals(x.pp.TwitchUsername, mentions[0], System.StringComparison.OrdinalIgnoreCase));

        if(targetPlayerHandler == null)
        {
            Debug.Log("TargetPlayerHandler is null"); 
            twitchClient.ReplyToPlayer(messageId, ph.pp.TwitchUsername, "Your command failed to find a valid target username in play. The correct format is: @username");
            return;
        }

        if(targetPlayerHandler.pp.TwitchUsername == ph.pp.TwitchUsername)
        {
            twitchClient.ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You can't pull yourself doofus. I thought of everything... hehehe.");
            return;
        }

        DrawPullArrow(ph, targetPlayerHandler);
        
    }

    private void SetState(GameState state)
    {
        _state = state;

        if (state == GameState.waiting)
            _stateText.SetText("Waiting...");
        else if (state == GameState.collectingCommands)
            _stateText.SetText("Collecting commands...");
        else if(state == GameState.pulling)
            _stateText.SetText("Pulling players...");
        else if(state == GameState.gridDividing)
            _stateText.SetText("Grid dividing...");
        else if(state == GameState.spinning)
            _stateText.SetText("Spinning...");

    }

    public override void CleanUpGame()
    {
        //In case there was a bug, delete all the arrows again
        foreach (var arrow in _activeArrows)
            arrow.DestroyArrow();
        _activeArrows.Clear();

        IsGameStarted = false;
        _roundsRemainingText.SetText($"Round: 1/{_rounds}");
        SetState(GameState.waiting);
    }

    private Vector3 GetNextOpenZonePos()
    {
        bool secondPos = false;
        if (_currZonePosIndex >= 6)
            secondPos = true;

        Vector3 targetPos = _dangerZones[_currZonePosIndex % 6].GetSpawnPos(secondPos);
        _currZonePosIndex = (_currZonePosIndex + 1) % 12;
        return targetPos;
    }

    public void ReceiveTravelingIndicator(TravelingIndicator TI)
    {
        throw new System.NotImplementedException();
    }

    public Vector3 Get_TI_IO_Position()
    {
        return _roundsRemainingText.transform.position;
    }

/*    public GameObject GetGameObject()
    {
        return gameObject;
    }*/
}
