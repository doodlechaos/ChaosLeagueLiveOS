using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;


public enum RarityType { Common, Rare, Epic, Legendary, Mythic, Ethereal, Cosmic }
public enum DurationTYpe { Timer, Manual }
public enum Side { Left, Center, Right}
public enum TileState { Inactive, LockedInPos, Bidding, Gameplay}

public class GameTile : MonoBehaviour
{
    private TileController _tc;
    public int TileIDNum;
    [SerializeField] private Game _game;
    [SerializeField] private SuddenDeath _suddenDeath; 
    [SerializeField] private CleaningBarController _cleaningBarController;
    [SerializeField] private Transform _releaseBar;
    [SerializeField] private TextMeshPro _tileNameText;
    [SerializeField] private TextMeshPro _rarityText;
    [SerializeField] private TextMeshPro _timerText;
    [SerializeField] private TextMeshPro _ticketBonusAmountText;
    public int TicketBonusAmount;

    public TileState TileState = TileState.Inactive;
    public RarityType RarityType;
    [SerializeField] public DurationTYpe DurationTYpe;
    [SerializeField] public bool IsGolden; //1% chance
    [SerializeField] public bool IsShop;
    [SerializeField] private CycleMode _cycleMode;
    [SerializeField] private bool _updateCycleModeButton;
    [SerializeField] private ContactWarp _contactWarp; 

    [SerializeField] private Transform _resetablesRoot;

    [SerializeField] private MeshRenderer _background;

    [SerializeField] private int _tileDurationS;

    [Range(0, 8)]
    [SerializeField] public int MinAuctionSlots;
    [Range(0, 8)]
    [SerializeField] public int MaxAuctionSlots;
    [Range(0, 8)]
    [SerializeField] public int RaffleSlots;
    [SerializeField] public int AuctionDuration = 60; 

    [SerializeField] private bool finishTileButton;
    [SerializeField] public PipeReleaser EntrancePipe;
    [SerializeField] private GoldenVisuals _goldenVisuals;
    [SerializeField] private List<MeshRenderer> _colorTrimsByRarity; 

    public List<PlayerHandler> ConveyorBelt = new List<PlayerHandler>();

    private bool _forceEndGameplay = false; 
    private float _tileGameplayTimeElapsed = 0; 
    private float _timer;

    //public bool TileActive;

    private MaterialPropertyBlock _mpb;
    private MaterialPropertyBlock _trimMpb;

    [SerializeField] [HideInInspector] private Color _backgroundStartColor;
    [SerializeField] [HideInInspector] private Color _backgroundEndColor;
    [SerializeField][HideInInspector] private Color _trimColor;

    public PBEffector[] Effectors;

    public Side CurrentSide;

    //public int TotalPlayers;
    public List<PlayerHandler> Players = new List<PlayerHandler>();
    public List<PlayerHandler> AlivePlayers = new List<PlayerHandler>();
    public List<PlayerHandler> EliminatedPlayers = new List<PlayerHandler>();

    [Header("Wait for All Players Released Before Starting Game")]
    [SerializeField] private bool _waitForAll = true;
    [SerializeField] private bool _waitForAllDead = false; 

    //private int _playersReleased = 0;

    private DateTime _tileStartTime;
    private long _playerPointsSumStart; 

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _trimMpb = new MaterialPropertyBlock();

        //SetBackgroundShader(1);

        _background.GetPropertyBlock(_mpb);
        _mpb.SetColor("_StartColor", _backgroundStartColor);
        _mpb.SetColor("_EndColor", _backgroundEndColor);
        _background.SetPropertyBlock(_mpb);

        _trimMpb.SetColor("_BaseColor", _trimColor);
        foreach (var meshRenderer in _colorTrimsByRarity)
            meshRenderer.SetPropertyBlock(_trimMpb);

/*        foreach (var resetable in _resetablesRoot.GetComponentsInChildren<IResetable>())
            resetable.MyReset();*/

        Effectors = GetComponentsInChildren<PBEffector>();

        _tileNameText.SetText(gameObject.name.Replace("(Clone)",""));
    }

    public void SetRarity(RarityType rarityType, Color backgroundStartColor, Color backgroundEndColor, Color trimColor)
    {
        RarityType = rarityType;

        _rarityText.SetText(System.Enum.GetName(typeof(RarityType), rarityType));

        _backgroundStartColor = backgroundStartColor;
        _backgroundEndColor = backgroundEndColor;
        _trimColor = trimColor;

    }

    public RarityType GetRarity()
    {
        return RarityType; 
    }

    private void OnValidate()
    {
        if (finishTileButton) 
        {
            finishTileButton = false;
            FinishTile();
        }
        if (_updateCycleModeButton)
        {
            _updateCycleModeButton = false;
            InitCycleMode(); 
        }
    }

    private void InitCycleMode()
    {
        _contactWarp.targetMode = _cycleMode;

        if (_cycleMode == CycleMode.receiver)
        {
            EntrancePipe.gameObject.SetActive(true); 
            _contactWarp.playerReceiver = EntrancePipe;
            EntrancePipe.transform.localPosition = new Vector3(0, EntrancePipe.transform.localPosition.y, EntrancePipe.transform.localPosition.z);
            EntrancePipe.transform.localEulerAngles = new Vector3(0, 0, 180); 
        }
        else if(_cycleMode == CycleMode.verticalWrap)
        {
            EntrancePipe.gameObject.SetActive(false);
            int side = (CurrentSide == Side.Left) ? 1 : -1;
            EntrancePipe.transform.localPosition = new Vector3((_background.transform.localScale.x / 2.1f) * side, EntrancePipe.transform.localPosition.y, EntrancePipe.transform.localPosition.z); //Extra .1 to get past wall barrier
            EntrancePipe.transform.localEulerAngles = new Vector3(0, 0, 90 * side); 
        }
    }

    public void TogglePhysics(bool toggle)
    {
        foreach (var rb in GetComponentsInChildren<Rigidbody2D>())
            rb.simulated = toggle;

        foreach(var col in GetComponentsInChildren<Collider2D>())
            col.enabled = toggle;

        foreach (var oscillators in GetComponentsInChildren<OscillatorV2>())
            oscillators.ToggleOnOff(toggle); 
    }
    public void PreInitTile(TileController tc, bool isGolden)
    {
        if (_mpb == null)
            _mpb = new MaterialPropertyBlock();
        
        _tc = tc;

        InitCycleMode(); 

        Effectors = GetComponentsInChildren<PBEffector>();

        IsGolden = isGolden;
        _timer = 0;
        UpdateTileTimer();
        ResetTicketBonus();

        EntrancePipe.LockIcon.enabled = true;   

        foreach (var resetable in _resetablesRoot.GetComponentsInChildren<IResetable>())
            resetable.MyReset();

        foreach (var effector in Effectors)
        {
            effector.ResetEffector();

            effector.MultiplyCurrValue(AppConfig.GetMult(RarityType)); 

            if (isGolden)
                effector.MultiplyCurrValue(AppConfig.inst.GetI("GoldenTileMultiplier"));
        }
        
        EntrancePipe.SetTollCost(tc.GetGameManager().GetKingController().TollRate * AppConfig.GetMult(RarityType));

        if (_game != null)
        {
            _game.DoneWithGameplay = false;
            _game.IsGameStarted = false;
            _game.OnTilePreInit();
        }

        if (_suddenDeath.gameObject.activeSelf)
            _suddenDeath.OnTilePreInit(); 

        if (isGolden)
            _goldenVisuals.gameObject.SetActive(true);
        else
            _goldenVisuals.gameObject.SetActive(false);

        _mpb.SetColor("_StartColor", _backgroundStartColor);
        _mpb.SetColor("_EndColor", _backgroundEndColor);
        _background.SetPropertyBlock(_mpb);

        _trimMpb.SetColor("_BaseColor", _trimColor);
        foreach (var meshRenderer in _colorTrimsByRarity)
            meshRenderer.SetPropertyBlock(_trimMpb);

        //SetBackgroundShader(0);
        UpdateTileTimer();

    }

    public void InitTileInPos()
    {
        TileState = TileState.LockedInPos; 
        //TileActive = true;
        _timer = 0;
        //_playersReleased = 0;

        Players.Clear();
        AlivePlayers.Clear();
        EliminatedPlayers.Clear();

        if (_releaseBar != null)
            _releaseBar.gameObject.SetActive(true);

        if (_game != null)
            _game.OnTileInitInPos();
        

        TogglePhysics(true);

        UpdateTileTimer();

    }

    public void OnPipeReleasePlayer(PlayerBall pb)
    {
        //_playersReleased++; 
    }

    public IEnumerator RunTile()
    {
        TileState = TileState.Gameplay; 
        EntrancePipe.LockIcon.enabled = false;
        _tileStartTime = DateTime.Now;

        _playerPointsSumStart = 0;
        foreach (var player in Players)
        {
            player.TilePointsROI = 0; 
            _playerPointsSumStart += player.pp.SessionScore;
        }

        if (_waitForAll)
        {
            //wait until all the players are off the conveyor belt
            while (ConveyorBelt.Count > 0)
                yield return null;
        }

        if (_suddenDeath.gameObject.activeSelf)
            _suddenDeath.StartSuddenDeath();

        if (_releaseBar != null)
            _releaseBar.gameObject.SetActive(false); 
        
        if(_game != null)
        {
            _game.StartGame();
            _game.IsGameStarted = true;
        }

        _forceEndGameplay = false;

        _tileGameplayTimeElapsed = 0; 
        //Run the gameplay until we either get a signal from the game that it's done, or there is only one player left alive
        while (true)
        {
            //Stop if the game signals it's done
            if (_game != null && _game.DoneWithGameplay)
                break;

            //Stop if the timer runs out
            if (_timer > _tileDurationS)
                break;

            //Stop if there is only one player left alive and none on the belt
            if (!IsShop && AlivePlayers.Count <= ((_waitForAllDead) ? 0 : 1) && ConveyorBelt.Count <= 0)
                break;

            if (IsShop && AlivePlayers.Count <= 0 && ConveyorBelt.Count <= 0)
                break;

            if (_forceEndGameplay)
                break;

            if(_tileGameplayTimeElapsed > 240)
            {
                Debug.LogError("Finishing tile due exciting gameplay duration limit"); 
                break;
            }

            _tileGameplayTimeElapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"about to start podium: tileTimeElapsed: {_timer} tileDuration:{_tileDurationS} alivePlayers:{AlivePlayers.Count} ConveyorBelt:{ConveyorBelt.Count}");

        //Freeze the physics on any winning survivors so that they don't die while the podium is going up
        foreach(var ph in AlivePlayers)
        {
            if (ph.pb != null)
                ph.pb.EnableKinematicMode(); 
        }

        long _playerPointsSumFinish = 0;
        foreach (var player in Players)
            _playerPointsSumFinish += player.pp.SessionScore;

        if(Players.Count > 0)
            Debug.Log($"Tile {name} Rarity {RarityType} Gameplay finished in {(DateTime.Now - _tileStartTime).ToString(@"hh\:mm\:ss")}\n" +
                      $"Total Points Distributed: {(_playerPointsSumFinish - _playerPointsSumStart)}\n" + 
                      $"Average Points Gained Per Player: {(_playerPointsSumFinish - _playerPointsSumStart) / Players.Count}");


        if (!IsShop && Players.Count > 0)
            yield return _tc.Podium.RunPodium(this, Players);

        yield return null; //Wait one frame to allow time for bid handler to switch in case the tile has no bidders and instantly spins after countdown
        FinishTile(); 
    }

    public void EliminatePlayer(PlayerHandler ph, bool setRankScoreByElimOrder)
    {
        int rankScore = -1;
        if(setRankScoreByElimOrder)
            rankScore = EliminatedPlayers.Count;

        EliminatePlayer(ph, rankScore); 
    }

    public void EliminatePlayer(PlayerHandler ph, int rankScoreOverride = -1)
    {
        ph.SetState(PlayerHandlerState.Idle);

        //You can only be eliminated once
        if (!EliminatedPlayers.Contains(ph))
            EliminatedPlayers.Add(ph);

        if (rankScoreOverride != -1)
            ph.SetRankScore(rankScoreOverride);

        AlivePlayers.Remove(ph);
        ConveyorBelt.Remove(ph); //In case the player somehow died on the belt due to bug

        ph.GetPlayerBall().ExplodeBall();

    }

    public void EliminatePlayers(List<PlayerHandler> phs, bool setRankScoreByElimOrder)
    {
        int rankScore = -1;
        if(setRankScoreByElimOrder)
            rankScore = Players.Count - AlivePlayers.Count;

        foreach(var ph in phs)
            EliminatePlayer(ph, rankScore); 
    }

    public void ProcessGameplayCommand(string messageId, TwitchClient twitchClient, PlayerHandler ph, string msg, string rawEmotesRemoved)
    {
        if (_game == null)
            return;

        //If the game is not started, don't process any gameplay commands
        if (!_game.IsGameStarted)
            return;

        _game.ProcessGameplayCommand(messageId, twitchClient, ph, msg, rawEmotesRemoved);
    }


    //TODO: Backup timer, if all but one player isn't eliminated by the time this timer runs out, make the remaining players tie for first
    public IEnumerator RunTimer(int durationS)
    {
        _timer = 0;
        while (_timer < durationS)
        {
            float t = _timer / durationS;
            SetBackgroundShader(1 - t);
            _timerText.SetText( MyUtil.GetMinuteSecString((int)(durationS - _timer)));

            yield return new WaitForSeconds(1);

            _timer++;
        }
        SetBackgroundShader(0);
        _timerText.SetText(MyUtil.GetMinuteSecString(0));

    }

    private void UpdateTileTimer()
    {
        float t = _timer / _tileDurationS;

        if(t >= 1)
        {
            FinishTile();
            return;
        }

        SetBackgroundShader(1 - t); 
    }

    public void SetBackgroundShader(float t)
    {
        _background.GetPropertyBlock(_mpb);
        _mpb.SetFloat("_FillAmount", t);
        _background.SetPropertyBlock(_mpb);
    }

    public PlayerHandler GetAlivePlayerViaUsername(string twitchUsername)
    {
        //Check if the usernames match, ignore upper vs lowercase
        return AlivePlayers.Find(ph => string.Equals(ph.pp.TwitchUsername, twitchUsername, System.StringComparison.OrdinalIgnoreCase));
    }

    public void FinishTile()
    {
        //TileActive = false;
        TileState = TileState.Inactive; 
        if (_game != null)
        {
            _game.CleanUpGame();
            _game.IsGameStarted = false;
        }

        _suddenDeath.CleanUp();

        Players.Clear();
        AlivePlayers.Clear();
        EliminatedPlayers.Clear();

        //Once the gameplay tile finishes, spin it to a new tile
        _tc.SpinNewTile(this);
    }

    public void SetTicketBonus(int count)
    {
        if (IsShop)
            return;

        TicketBonusAmount = count;
        UpdateTicketBonusText();
        _ticketBonusAmountText.enabled = true; 
    }

    private void ResetTicketBonus()
    {
        TicketBonusAmount = 0;
        _ticketBonusAmountText.enabled = false;
        UpdateTicketBonusText(); 
    }

    private void UpdateTicketBonusText()
    {
        _ticketBonusAmountText.SetText("Win Prize: " + MyUtil.AbbreviateNum4Char(TicketBonusAmount)); 
    }

    public Vector3 GetTicketBonusAmountPos()
    {
        return _ticketBonusAmountText.transform.position;
    }
    public TileController GetTileController()
    {
        return _tc;
    }
    public Game GetGame()
    {
        return _game;
    }
    public SuddenDeath GetSuddenDeath()
    {
        return _suddenDeath;
    }

    public void ForceEndGameplay()
    {
        _forceEndGameplay = true; 
    }

    public Vector3 GetGoldSpawnPos()
    {
        return _rarityText.transform.position;
    }
}
