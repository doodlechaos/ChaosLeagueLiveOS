using System.Collections;
using System.Collections.Generic;
using System.Data;
using TMPro;
using UnityEngine;

public class RoyalExecutionGame : Game
{
    private enum GameState { waiting, spikeSquishing, movingPlatform }
    private GameState _state;

    [SerializeField] private float _commandEnterTimer = 20f;

    [SerializeField] private GameObject _commandsTextLeft;
    [SerializeField] private GameObject _commandsTextRight;

    [SerializeField] private BoxCollider2D _spikeCollider; //disable when not squishing

    [SerializeField] private Transform _baseHoldingBar;
    [SerializeField] private Transform _baseHoldingBarTopPos;
    [SerializeField] private Transform _baseHoldingBarBottomPos;
    [SerializeField] private float _baseHoldingBarMotionDuration = 3f;

    [SerializeField] private Transform _spikeWallRoot;
    [SerializeField] private Transform _spikeWallTopPos;
    [SerializeField] private Transform _spikeWallBottomPos;

    [SerializeField] private Transform _spikeWallLeftRotation;
    [SerializeField] private Transform _spikeWallRightRotation;

    [SerializeField] private AnimationCurve _spikeMotionSpeed;
    [SerializeField] private float _spikeMotionDuration = 5;

    public Side SpikeSide = Side.Left;
    private bool _spikesRotating = false;

    public override void OnTilePreInit()
    {
        _commandsTextLeft.SetActive(false);
        _commandsTextRight.SetActive(false);
    }

    public override void StartGame()
    {

        StartCoroutine(RunGame());
    }

    public IEnumerator RunGame()
    {
        while (_gt.ConveyorBelt.Count > 0)
            yield return null; 

        while (_gt.AlivePlayers.Count >= 2)
        {

            _state = GameState.waiting;
            _spikeCollider.enabled = false;
            _commandsTextLeft.SetActive(true);
            _commandsTextRight.SetActive(true);


            //Wait for all the players to spawn in through the splitter, and for the king to finalize the side they choose
            StartCoroutine(_gt.RunTimer((int)_commandEnterTimer)); 
            yield return new WaitForSeconds(_commandEnterTimer);

            _state = GameState.spikeSquishing;
            _commandsTextLeft.SetActive(false);
            _commandsTextRight.SetActive(false);



            //Wait for the spikes to finish rotating if they're in the middle of it
            while (_spikesRotating)
                yield return null;

            _spikeCollider.enabled = true;


            //Animate the spikes down to kill the side
            float timer = 0;
            float halfDuration = _spikeMotionDuration / 2; 
            while(timer < _spikeMotionDuration)
            {

                float t;

                if (timer < halfDuration)
                    t = timer / halfDuration;
                else
                    t = 1 - ((timer - halfDuration) / halfDuration);

                t = _spikeMotionSpeed.Evaluate(t);

                Vector3 nextPos = Vector3.Lerp(_spikeWallTopPos.position, _spikeWallBottomPos.position, t);
                _spikeWallRoot.position = nextPos;

                timer += Time.deltaTime;
                yield return null;
            }

            //Teleport the baseboard up to the top and animate it down back to the starting position
            _state = GameState.movingPlatform;
            timer = 0;
            while(timer < _baseHoldingBarMotionDuration)
            {
                float t = timer / _baseHoldingBarMotionDuration;
                t = EasingFunction.EaseInOutCubic(0, 1, t); 
                Vector3 nextPos = Vector3.Lerp(_baseHoldingBarTopPos.position, _baseHoldingBarBottomPos.position, t);
                _baseHoldingBar.position = nextPos; 
                timer += Time.deltaTime; 
                yield return null; 
            }

            //Randomize the side
            bool randomBool = Random.value > 0.5f;
            _commandsTextLeft.SetActive(false);
            _commandsTextRight.SetActive(false);
            _spikeCollider.enabled = false;

            if (_gt.AlivePlayers.Count <= 1)
                break;

            if (randomBool)
                RotateSpikeToSide(Side.Left);
            else
                RotateSpikeToSide(Side.Right);
        }

        DoneWithGameplay = true;
    }



    public override void ProcessGameplayCommand(string messageId, TwitchClient twitchClient, PlayerHandler ph, string msg, string rawEmotesRemoved)
    {
        string commandKey = msg.ToLower();

        if (_state != GameState.waiting)
            return;

        if (commandKey.Contains("!left"))
        {
            if (!ph.IsKing())
            {
                twitchClient.ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You must hold the throne to use this command.");
                return;
            }

            DrawLineMaster.Inst.DrawLine(ph.GetBallPos(), 0, _commandsTextLeft.transform.position, 1, 2f, Color.yellow); 
            RotateSpikeToSide(Side.Left);
            AudioController.inst.PlaySound(AudioController.inst.Switch2, 0.98f, 0.98f);
        }
        else if (commandKey.Contains("!right"))
        {
            if (!ph.IsKing())
            {
                twitchClient.ReplyToPlayer(messageId, ph.pp.TwitchUsername, "You must hold the throne to use this command.");
                return;
            }

            DrawLineMaster.Inst.DrawLine(ph.GetBallPos(), 0, _commandsTextRight.transform.position, 1, 2f, Color.yellow);
            RotateSpikeToSide(Side.Right);
            AudioController.inst.PlaySound(AudioController.inst.Switch2, 1.02f, 1.02f);

        }

    }

    private void RotateSpikeToSide(Side side)
    {
        SpikeSide = side;
    }

    private void Update()
    {
        Vector3 currEulerAngles = _spikeWallRoot.eulerAngles;

        _spikesRotating = false;
        if (SpikeSide == Side.Left && Mathf.RoundToInt(currEulerAngles.y) == Mathf.RoundToInt(_spikeWallLeftRotation.eulerAngles.y))
            return;

        if (SpikeSide == Side.Right && Mathf.RoundToInt(currEulerAngles.y) == Mathf.RoundToInt(_spikeWallRightRotation.eulerAngles.y))
            return;

        _spikesRotating = true;

        if (SpikeSide == Side.Left)
            _spikeWallRoot.rotation = Quaternion.RotateTowards(_spikeWallRoot.rotation, _spikeWallLeftRotation.rotation, 1); 
        else
            _spikeWallRoot.rotation = Quaternion.RotateTowards(_spikeWallRoot.rotation, _spikeWallRightRotation.rotation, 1);


    }

    public override void CleanUpGame()
    {
        IsGameStarted = false;
    }


}
