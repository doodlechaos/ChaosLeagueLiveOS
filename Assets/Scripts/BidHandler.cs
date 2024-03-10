using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BidHandler : MonoBehaviour
{
    [SerializeField] private GameManager _gm;
    [SerializeField] private TileController _tileController;
    [SerializeField] private KingController _kingController;

    [SerializeField] private ParticleSystem _channelPointParticles;
    [SerializeField] private ParticleSystem _bitParticles;

    [SerializeField] private TwitchClient _twitchClient;
    [SerializeField] private Transform _auctionPositionsRoot;
    [SerializeField] private GameObject _TI_Ticket_Prefab;
    [SerializeField] private Transform _TI_Tickets_Root;

    [SerializeField] private MeshRenderer _bidSpawnZoneEndcap1;
    [SerializeField] private MeshRenderer _bidSpawnZoneEndcap2;
    [SerializeField] private int _bidTI_totalRows = 8;
    [SerializeField] public Transform TicketIO_Target;
    [SerializeField] public RaffleBox RaffleBox;
    [SerializeField] private TextMeshPro _auctionTimerText;
    [SerializeField] private Transform _beltWaypoint;
    [SerializeField] private float _rotateSpeed = 0.01f;
    [SerializeField] private float _timeBetweenRaffleReleases = 0.4f;
    [SerializeField] private Transform _drawingIndicatorsRoot;
    [SerializeField] private PrizeDisplay _winnerPrizeText;
    [SerializeField] public BidQueueOrigin BidQueueOrigin;

    [SerializeField] private Transform MirrorFromRaffleOrigin;

    [SerializeField] private Color _minAuctionPosColor;
    [SerializeField] private Color _maxAuctionPosColor;
    [SerializeField] private Color _invalidAuctionPosColor;

    [SerializeField] private AudioSource _countdownAudioSource;

    private int _BidTI_rowCounter = 0;

    private AuctionPos[] _auctionPositions;
    private RaffleDrawingIndicator[] _raffleDrawIndicators;

    private List<PlayerHandler> _biddingQ = new List<PlayerHandler>();
    private List<PlayerHandler> _raffleWinners = new List<PlayerHandler>();

    private ObjectPool<TI_Bid> _TI_BidPool;

    [SerializeField] private int _commonBasePrize = 100;
    [SerializeField] private int _rareBasePrize = 200;
    [SerializeField] private int _epicBasePrize = 1_000;
    [SerializeField] private int _legendaryBasePrize = 4_000;
    private int _rarityBasePrize = 0;

    [SerializeField] private List<SpriteRenderer> _communityPointSpriteRenderers; 

    private void Awake()
    {
        _auctionPositions = _auctionPositionsRoot.GetComponentsInChildren<AuctionPos>();

        _raffleDrawIndicators = _drawingIndicatorsRoot.GetComponentsInChildren<RaffleDrawingIndicator>();

        _bidSpawnZoneEndcap1.enabled = false;
        _bidSpawnZoneEndcap2.enabled = false;

        _TI_BidPool = new ObjectPool<TI_Bid>(TI_BidFactory, TI_BidTurnOn, TI_BidTurnOff);

    }

    private void Start()
    {
        //Load the sprite
        foreach (var sr in _communityPointSpriteRenderers)
            sr.sprite = _gm.CommunityPointSprite;

        _channelPointParticles.GetComponent<ParticleSystemRenderer>().material.mainTexture = _gm.CommunityPointSprite.texture; 
    }

    public IEnumerator RunBiddingOn(GameTile gt) //GameTile tile
    {
        _tileController.CurrentBiddingTile = gt;
        _tileController.NextBiddingTile = null;

        _winnerPrizeText.ResetWinnerPrize(); 
        SetBasePrizeByRarity(gt.RarityType); 

        Debug.Log($"Starting new round in ticket handler: {gt.name}");
        //Immediately set which auction positions are valid, even before doing the spinning animation.
        for (int a = 0; a < _auctionPositions.Length; a++)
        {
            if (a < gt.MaxAuctionSlots)
                _auctionPositions[a].SetValid(true);
            else
                _auctionPositions[a].SetValid(false);
        }

        if (gt.IsShop)
            _winnerPrizeText.HideVisuals();
        else
            _winnerPrizeText.EnableVisuals();


        //Rotate to the opposite side
        yield return RotateToThisTile(gt);
        gt.TileState = TileState.Bidding;

        //Set the available number of Auction spots based on what the tile defines
        int i = 0;
        while (i < _auctionPositions.Length)
        {
            if (i < gt.MinAuctionSlots)
            {
                StartCoroutine(_auctionPositions[i].ColorSpinY_C(1, _minAuctionPosColor));

                float t = i / (float)_auctionPositions.Length;
                float pitch = Mathf.Lerp(0.9f, 1.1f, t);
                AudioController.inst.PlaySound(AudioController.inst.AuctionPosOpen, pitch, pitch);
            }
            else if (i < gt.MaxAuctionSlots)
            {
                StartCoroutine(_auctionPositions[i].ColorSpinY_C(1, _maxAuctionPosColor));

                float t = i / (float)_auctionPositions.Length;
                float pitch = Mathf.Lerp(0.9f, 1.1f, t);
                AudioController.inst.PlaySound(AudioController.inst.AuctionPosOpen, pitch, pitch);
            }
            else
            {
                StartCoroutine(_auctionPositions[i].ColorSpinY_C(1, _invalidAuctionPosColor));

                float t = i / (float)_auctionPositions.Length;
                float pitch = Mathf.Lerp(1.1f, 0.9f, t) - 0.3f;
                AudioController.inst.PlaySound(AudioController.inst.AuctionPosOpen, pitch, pitch);
            }

            yield return new WaitForSeconds(0.2f);

            i++;
        }

        SetAllRaffleIndicatorsUnlit();
        i = 0;
        while (i <= _raffleDrawIndicators.Length)
        {
            if (i <= gt.RaffleSlots)
            {
                UpdateRaffleDrawIndicatorsCount(i);

                //Prevents making a sound on index zero when clearing all indicators 
                if (i > 0)
                {
                    float t = i / (float)_raffleDrawIndicators.Length;
                    float pitch = Mathf.Lerp(0.9f, 1.1f, t);
                    AudioController.inst.PlaySound(AudioController.inst.RaffleSpotOpen, pitch, pitch);
                }

            }

            yield return new WaitForSeconds(0.2f);
            i++;
        }
        yield return ReleasePlayersWhenReady(gt);

    }

    public IEnumerator RotateToThisTile(GameTile gt)
    {
        AudioController.inst.PlaySound(AudioController.inst.BidQSwitchSide, 0.9f, 1.1f);

        //_winnerPrizeText.SetSide(gt.CurrentSide); 
        float kingBitFluidsStartX = Mathf.Abs(MirrorFromRaffleOrigin.position.x);
        float kingBitFluidsTargetX = kingBitFluidsStartX;

        Vector3 startAngle = Vector3.zero;
        Vector3 targetAngle = Vector3.zero;
        Vector3 _timerTextPos = _auctionTimerText.transform.position;

        _timerTextPos.x = Mathf.Abs(_timerTextPos.x) * -1;

        if (gt.CurrentSide == Side.Right)
        {

            targetAngle = new Vector3(0, -180, 0);
            _timerTextPos.x = Mathf.Abs(_timerTextPos.x);

            Vector3 ticketSpawnCap = _bidSpawnZoneEndcap1.transform.position;
            ticketSpawnCap.x = Mathf.Abs(ticketSpawnCap.x);
            _bidSpawnZoneEndcap1.transform.position = ticketSpawnCap;

            ticketSpawnCap = _bidSpawnZoneEndcap2.transform.position;
            ticketSpawnCap.x = Mathf.Abs(ticketSpawnCap.x);
            _bidSpawnZoneEndcap2.transform.position = ticketSpawnCap;

            kingBitFluidsTargetX *= -1;
        }
        else
        {

            startAngle = new Vector3(0, -180, 0);
            Vector3 ticketSpawnCap = _bidSpawnZoneEndcap1.transform.position;
            ticketSpawnCap.x = Mathf.Abs(ticketSpawnCap.x) * -1;
            _bidSpawnZoneEndcap1.transform.position = ticketSpawnCap;

            ticketSpawnCap = _bidSpawnZoneEndcap2.transform.position;
            ticketSpawnCap.x = Mathf.Abs(ticketSpawnCap.x) * -1;
            _bidSpawnZoneEndcap2.transform.position = ticketSpawnCap;

            kingBitFluidsStartX *= -1;
        }

        _auctionTimerText.transform.position = _timerTextPos;

        Debug.Log($"Side:{gt.CurrentSide} Start angle: {startAngle} target angle: {targetAngle}. Start fluid pos: {kingBitFluidsStartX} target fluid pos: {kingBitFluidsTargetX}");

        float t = 0;
        while (t <= 1)
        {
            transform.eulerAngles = Vector3.Lerp(startAngle, targetAngle, t);
            RaffleBox.transform.localEulerAngles = Vector3.Lerp(startAngle, targetAngle, t);
            _winnerPrizeText.transform.localEulerAngles = Vector3.Lerp(startAngle, targetAngle, t);
            t += _rotateSpeed;

            //Move king bit fluids to opposite side 
            Vector3 kingBitFluidsPos = MirrorFromRaffleOrigin.position;
            kingBitFluidsPos.x = Mathf.Lerp(kingBitFluidsStartX, kingBitFluidsTargetX, EasingFunction.EaseInOutQuad(0, 1, t));
            MirrorFromRaffleOrigin.position = kingBitFluidsPos;

            yield return null;
        }
        transform.eulerAngles = targetAngle;
        MirrorFromRaffleOrigin.position = new Vector3(kingBitFluidsTargetX, MirrorFromRaffleOrigin.position.y, MirrorFromRaffleOrigin.position.z);
    }

    public IEnumerator ReleasePlayersWhenReady(GameTile gt)
    {
        _auctionTimerText.SetText("");

        //Wait for gameplay on other tile to finish
        while (_tileController.GameplayTile != null && _tileController.GameplayTile.TileState != TileState.Inactive)
            yield return null;

        int auctionTimeElapsed = 0;

        //Wait to have enough players, and for countdown
        while (auctionTimeElapsed <= gt.AuctionDuration)
        {
            yield return new WaitForSeconds(1);

            //If we don't have enough players in the queue now due to !cancelbid or there just isn't enough, stop the timer
            if (_biddingQ.Count < gt.MinAuctionSlots)
            {
                auctionTimeElapsed = 0; 
                _auctionTimerText.SetText("");
                if (_countdownAudioSource.isPlaying)
                    _countdownAudioSource.Stop(); 
                continue; 
            }

            float t = auctionTimeElapsed / (float)gt.AuctionDuration;
            _auctionTimerText.SetText(MyUtil.GetMinuteSecString(gt.AuctionDuration - auctionTimeElapsed));

            int secRemaining = gt.AuctionDuration - auctionTimeElapsed;


            if (secRemaining == 3)
            {
                _countdownAudioSource.pitch = Random.Range(0.95f, 1.05f); 
                _countdownAudioSource.Play();
            }

            auctionTimeElapsed++;
        }

        int raffleWinnersChosen = 0;
        //Select all necessary raffle winners
        while (raffleWinnersChosen < gt.RaffleSlots && _biddingQ.Count > gt.MaxAuctionSlots)
        {
            //Select a random ticket from the raffle
            PlayerHandler ph = _biddingQ.Skip(gt.MaxAuctionSlots).RandomElementByWeight(e => e.pp.CurrentBid);
            ph.ReceivableTarget = null;
            _raffleWinners.Add(ph);
            gt.ConveyorBelt.Add(ph);
            gt.Players.Add(ph);

            ph.ResetBid();

            PlayerBall pb = ph.GetPlayerBall();
            pb.Reactivate();
            pb._rb2D.transform.position = TicketIO_Target.position;
            //pb.AddPriorityWaypoint(TicketIO_Target.position, 0.1f);

            _biddingQ.Remove(ph);
            ph.SetState(PlayerHandlerState.Gameplay);

            //Update Total Raffle Tickets
            UpdateBiddingQ();

            //Update the raffle drawing indicators
            raffleWinnersChosen++;
            UpdateRaffleDrawIndicatorsCount(gt.RaffleSlots - raffleWinnersChosen);

        }

        Vector3 beltWayPointPos = _beltWaypoint.transform.position; //It moves as soon as the Q flips to the other side, so cache the position
        ReleasePlayersIntoTile(gt, beltWayPointPos);
        gt.SetTicketBonus(_winnerPrizeText.GetWinnerPrize());

        //Immediately after this timer runs out, I need to finalize all players that made it in
        //Because I need to start populating the next tile from ticket requests
        StartCoroutine(RunBiddingOn(_tileController.NextBiddingTile));

        //Move the raffle winners when ready
        foreach (var ph in _raffleWinners)
        {
            //Traverse belt
            ph.pb.AddPriorityWaypoint(beltWayPointPos, 0.1f);
            ph.ReceivableTarget = gt.EntrancePipe;

            yield return new WaitForSeconds(_timeBetweenRaffleReleases);
        }
        _raffleWinners.Clear();

        _auctionTimerText.SetText(MyUtil.GetMinuteSecString(0));

    }
    public void ReleasePlayersIntoTile(GameTile gt, Vector3 beltWayPointPos)
    {
        _tileController.GameplayTile = gt;

        //Select and move the players on the auction slots
        for (int i = 0; i < gt.MaxAuctionSlots && _biddingQ.Count > 0; i++)
        {
            PlayerHandler ph = _biddingQ[0];
            //Traverse belt
            ph.pb.AddPriorityWaypoint(beltWayPointPos, 0.1f);
            ph.ReceivableTarget = gt.EntrancePipe;
            ph.ResetBid();

            gt.ConveyorBelt.Add(ph);
            gt.Players.Add(ph);

            _biddingQ.Remove(ph);
            ph.SetState(PlayerHandlerState.Gameplay);
        }

        //Initialize and start running the tile based on the player count coming
        //int totalPlayersComing = gt.ConveyorBelt.Count(); //Math.Min(_biddingQ.Count + _raffleWinners.Count, gt.MaxAuctionSlots + _raffleWinners.Count);
        //gt.InitForPlayerCount(totalPlayersComing);
        StartCoroutine(gt.RunTile());
        Debug.Log($"total players coming: {gt.Players.Count} from biddingQcount: {_biddingQ.Count} raffleWinnersCount: {_raffleWinners.Count}");


        //Clear bidding Q
        int totalBidsLeftover = 0;
        for(int i = _biddingQ.Count - 1; i >= 0; i--)
        {
            PlayerHandler ph = _biddingQ[i]; 
            totalBidsLeftover += ph.pp.CurrentBid;
            ClearFromQ(ph, false); 
        }

        //Send the leftover tickets as points to the king
        if (_kingController.currentKing != null)
            TextPopupMaster.Inst.CreateTravelingIndicator(totalBidsLeftover.ToString(), totalBidsLeftover, RaffleBox, _kingController.currentKing.Ph, 0.08f, Color.white, null);

        _biddingQ.Clear();
        UpdateRaffleDrawIndicatorsCount(0); 
        UpdateBiddingQ();
    }

    public void ClearFromQ(PlayerHandler ph, bool updateQ, bool unbid = false)
    {
        ph.ResetBid();

        //If there was no ph to remove from the bidding Q, cancel the clear
        if (!_biddingQ.Remove(ph))
            return;

        ph.SetState(PlayerHandlerState.Idle);
        ph.ReceivableTarget = null; //Prevent bug where players would move to raffle after attacking and get stuck

        if (ph.pb != null)
            ph.pb.ExplodeBall();

        CancelTicketsUsed(ph, unbid);

        if (updateQ)
            UpdateBiddingQ();
    }

    private TI_Bid TI_BidFactory()
    {
        return Instantiate(_TI_Ticket_Prefab, _TI_Tickets_Root).GetComponent<TI_Bid>();
    }

    private void TI_BidTurnOn(TI_Bid TI_Bid)
    {
        TI_Bid.gameObject.SetActive(true);
    }

    private void TI_BidTurnOff(TI_Bid TI_Bid)
    {
        TI_Bid.gameObject.SetActive(false);
    }

    public void SpawnTI_Bid(PlayerHandler ph, TI_Bid_IO target, int count, BidType bidType)
    {
        TI_Bid TI_Bid = _TI_BidPool.GetObject();


        _BidTI_rowCounter = (_BidTI_rowCounter + 1) % _bidTI_totalRows;
        float t = _BidTI_rowCounter / (float)_bidTI_totalRows;
        Vector3 _ticketSpawnPos = Vector3.Lerp(_bidSpawnZoneEndcap1.transform.position, _bidSpawnZoneEndcap2.transform.position, t);

        float pitch = Mathf.Lerp(0.8f, 1.2f, t);
        //if (bidType == BidType.ChannelPoints)
        //    AudioController.inst.PlaySound(AudioController.inst.CommunityPointBidSlideIn, pitch - 0.5f, pitch + 0.5f); 

        TI_Bid.Init(this, _ticketSpawnPos, target, ph, count, bidType); 
    }

    public void DestroyBidTI(TI_Bid TI_Bid)
    {
        _TI_BidPool.ReturnObject(TI_Bid);
    }

    public void BidRedemption(PlayerHandler ph, int bidAmount, BidType bidType, string redemptionID = null, string rewardID = null)
    {
        SpawnTI_Bid(ph, target:ph, bidAmount, bidType);

        if (redemptionID == null)
            return;
        if (rewardID == null)
            return;

        List<string> redemptionsIds;
        ph.redemptionsIds.TryGetValue(rewardID, out redemptionsIds);
        if (redemptionsIds == null)
            redemptionsIds = new List<string>();

        redemptionsIds.Add(redemptionID);
        ph.redemptionsIds[rewardID] = redemptionsIds;
    }

    public void TryAddToBiddingQ(PlayerHandler ph)
    {
        //Don't interact with the bidding Q if the player is already spawned, but saves the bid amount for the next time they go in
        if (ph.GetState() == PlayerHandlerState.Gameplay)
            return;

        if (ph.IsKing())
            return;

        //Add player to the bidding queue if necessary and resort
        if (!_biddingQ.Contains(ph))
        {
            _biddingQ.Add(ph);
            ph.SetState(PlayerHandlerState.BiddingQ);
        }

        UpdateBiddingQ();
    }

    //Sets the target positions of all players in the Q, and updates the text totals of the raffle and prize.
    public void UpdateBiddingQ()
    {
        _biddingQ = _biddingQ.OrderByDescending(ph => ph.pp.CurrentBid).ToList();

        int totalRaffleTickets = 0;
        int totalAuctionSlotTickets = 0;
        int totalPlayersInRaffle = 0; 
        //If a player is in the top auction slots, set their mode to full ball
        for (int i = 0; i < _biddingQ.Count; i++)
        {
            if (i < _auctionPositions.Count() && _auctionPositions[i].IsValid)
            {
                _biddingQ[i].SetTicketQPos(_auctionPositions[i], false, this);
                totalAuctionSlotTickets += _biddingQ[i].pp.CurrentBid; 
            }
            else
            {
                //Debug.Log($"setting receivable target for {_biddingQ[i].pp.TwitchUsername} to raffle box");

                _biddingQ[i].SetTicketQPos(RaffleBox, true, this);
                totalRaffleTickets += _biddingQ[i].pp.CurrentBid;
                totalPlayersInRaffle++; 
            }
        }

        if(totalPlayersInRaffle <= _raffleDrawIndicators.Length + 1)
            SetRaffleDrawIndicatorsLit(totalPlayersInRaffle);
        

        _winnerPrizeText.SetWinnerPrize(_rarityBasePrize + totalAuctionSlotTickets + totalRaffleTickets);

        RaffleBox.SetRaffleCountText(MyUtil.AbbreviateNum4Char(totalRaffleTickets)); 
    }

    private void UpdateRaffleDrawIndicatorsCount(int count)
    {
        for (int i = 0; i < _raffleDrawIndicators.Length; i++)
        {
            if (i < count)
                _raffleDrawIndicators[i].gameObject.SetActive(true);
            else
                _raffleDrawIndicators[i].gameObject.SetActive(false);
        }
    }

    private void SetRaffleDrawIndicatorsLit(int count)
    {
        int raffleSlots = 0;

        foreach (var raffleIndicator in _raffleDrawIndicators)
            if(raffleIndicator.gameObject.activeSelf)
                raffleSlots++;

        

        //For each of the raffle indicators, light up the dots for each player in the raffle. 
        for (int i = _raffleDrawIndicators.Length - 1; i >= 0; i--)
        {
            if (count > raffleSlots)
                _raffleDrawIndicators[i].SetFull(); 
            else if (i < count)
                _raffleDrawIndicators[i].SetLit();
            else
                _raffleDrawIndicators[i].SetUnlit();
            
        }
    }

    private void SetAllRaffleIndicatorsUnlit()
    {
        //For each of the raffle indicators, light up the dots for each player in the raffle. 
        for (int i = _raffleDrawIndicators.Length - 1; i >= 0; i--)
            _raffleDrawIndicators[i].SetUnlit(); 
    }


    public void BurstCommunityPointParticles(Vector3 position, int count)
    {

        _channelPointParticles.transform.position = position;

        var main = _channelPointParticles.main;

        //float t = _countIncreaseChangeRate.Evaluate(Mathf.Clamp01(count / 100_000f));
        float t = count / 100_000f;

        float maxSpeed = Mathf.Lerp(8, 100, t);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1, maxSpeed);

        _channelPointParticles.Emit(count);


        float pitch = Mathf.Lerp(0.9f, 1.1f, t);
        AudioController.inst.PlaySound(AudioController.inst.CommunityPointParticlePop, pitch - 0.02f, pitch + 0.02f); 

    }

    public void BurstBitParticles(Vector3 position, int count)
    {
        _bitParticles.transform.position = position;

        var main = _bitParticles.main;

        float t = count / 100_000f;

        float maxSpeed = Mathf.Lerp(8, 100, t);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1, maxSpeed);

        _bitParticles.Emit(count);

        float pitch = Mathf.Lerp(0.9f, 1.1f, t);
        AudioController.inst.PlaySound(AudioController.inst.BitsParticlePop, pitch - 0.02f, pitch + 0.02f);
    }

    private void SetBasePrizeByRarity(RarityType rarity)
    {
        if (rarity == RarityType.Common)
            _rarityBasePrize = _commonBasePrize;
        else if (rarity == RarityType.Rare)
            _rarityBasePrize = _rareBasePrize;
        else if (rarity == RarityType.Epic)
            _rarityBasePrize = _epicBasePrize;
        else
            _rarityBasePrize = _legendaryBasePrize;

        UpdateBiddingQ(); 
    }

    private async void CancelTicketsUsed(PlayerHandler ph, bool unbid = false)
    {
        if (unbid)
        {
            foreach (var rewardID in ph.redemptionsIds.Keys)
            {
                List<string> redemptionsIds = ph.redemptionsIds[rewardID];
                await TwitchApi.RejectRewardRedemption(rewardID, redemptionsIds);
            }
        }

        ph.redemptionsIds.Clear();
    }

    public GameManager GetGameManager()
    {
        return _gm; 
    }
}
