using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Podium : MonoBehaviour
{
    [SerializeField] private GameObject _podiumEntryPrefab;
    [SerializeField] private BidHandler _ticketHandler;

    private ObjectPool<PodiumEntry> _podiumEntryPool;
    private List<PodiumEntry> _activePodiumEntries = new List<PodiumEntry>();


    [SerializeField] private TextMeshPro _travelingTicketBonusText;
    [SerializeField] private float _travelingTicketBonusTextSpeed = 0.1f;

    [SerializeField] private Transform _podiumLeftPos;
    [SerializeField] private Transform _podiumRightPos;
    [SerializeField] private Transform _entriesRoot;
    [SerializeField] private Transform _entrySpawnPos;
    [SerializeField] private Transform _entryRankOnePos;
    [SerializeField] private Transform _podiumTile; 
    [SerializeField] private Vector3 _entrySeparation;
    [SerializeField] private float _timeBetweenEntrySpawns;
    [SerializeField] private Transform _podiumLeftSpawnPos;

    [SerializeField] private AnimationCurve _slideIn;
    [SerializeField] private float _slideInOutDuration; 

    private void Awake()
    {
        _podiumEntryPool = new ObjectPool<PodiumEntry>(PodiumEntryFactory, PodiumEntryTurnOn, PodiumEntryTurnOff);
    }

    private PodiumEntry PodiumEntryFactory()
    {
        return Instantiate(_podiumEntryPrefab, _entriesRoot).GetComponent<PodiumEntry>();
    }

    private void PodiumEntryTurnOn(PodiumEntry pe)
    {
        pe.gameObject.SetActive(true);
    }

    private void PodiumEntryTurnOff(PodiumEntry pe)
    {
        pe.gameObject.SetActive(false);
    }

    public IEnumerator RunPodium(GameTile gt, List<PlayerHandler> players)
    {
        _podiumTile.gameObject.SetActive(true);

        Vector3 podiumStartPos = new Vector3(_podiumLeftSpawnPos.position.x * (gt.CurrentSide == Side.Left ? 1 : -1), _podiumLeftSpawnPos.position.y, _podiumLeftSpawnPos.position.z); 

        Vector3 podiumTargetPos =  gt.CurrentSide == Side.Left ? _podiumLeftPos.position : _podiumRightPos.position;

        yield return SlidePodiumIntoPosition(podiumStartPos, podiumTargetPos);

        //Explode all remaining alive players so they can begin bidding on next tile
        foreach (var ph in gt.AlivePlayers)
        {
            if (ph.pb != null)
                ph.pb.ExplodeBall();
        }


        //Mirror the entry spawn pos based on the side
        Vector3 entrySpawnPos = _entrySpawnPos.localPosition;
        entrySpawnPos.x = Mathf.Abs(entrySpawnPos.x) * (gt.CurrentSide == Side.Left ? -1 : 1);
        _entrySpawnPos.localPosition = entrySpawnPos;

        yield return SpawnPodiumEntries(players);

        Debug.Log("done with spawn podium entries");

        yield return TI_FromTicketBonusToShowEachPodiumEntryEarningsFraction(gt);

        Debug.Log("done with TI bonus");


        yield return ShowInvitedByRewards();

        yield return new WaitForSeconds(3f);

        Debug.Log("done with wait for 3 sec"); 

        //Slide podium off screen
        yield return SlidePodiumIntoPosition(podiumTargetPos, podiumStartPos);

        ClearEntries(); //Clear podium display


        _podiumTile.gameObject.SetActive(false);
    }

    private IEnumerator SlidePodiumIntoPosition(Vector3 startPos, Vector3 targetPos)
    {
        float slideInDuration = _slideInOutDuration;
        float slideInTimer = 0;

        while (slideInTimer < slideInDuration)
        {
            slideInTimer += Time.deltaTime;
            float t = slideInTimer / slideInDuration;
            t = _slideIn.Evaluate(t);
            _podiumTile.transform.position = Vector3.LerpUnclamped(startPos, targetPos, t);
            yield return null;
        }
    }

    public IEnumerator SpawnPodiumEntries(List<PlayerHandler> rankScoredPlayers)
    {
        if (rankScoredPlayers.Count <= 0)
            yield break;

        rankScoredPlayers = rankScoredPlayers.OrderByDescending(x => x.GetRankScore()).ToList();

        int currRank = 1;
        int prevRankScore = rankScoredPlayers[0].GetRankScore();
        List<(PlayerHandler ph, int rank)> entries = new List<(PlayerHandler ph, int rank)>();

        for (int i = 0; i < rankScoredPlayers.Count; i++)
        {
            PlayerHandler ph = rankScoredPlayers[i];
            
            if(ph.GetRankScore() != prevRankScore && i > 0)
                currRank++;

            entries.Add((ph, currRank));
            prevRankScore = ph.GetRankScore();
        }
        
        //Spawn them in reverse order so the #1 appears at the top of the screen
        for(int i = entries.Count - 1; i >= 0; i--)
        {
            var entry = entries[i];
            SpawnEntry(entry.ph, entry.rank);
            yield return new WaitForSeconds(_timeBetweenEntrySpawns);
        }

    }

    private void SpawnEntry(PlayerHandler ph, int rank)
    {
        //Slide the previous podium entries down
        foreach(var entry in _activePodiumEntries)
            entry.ShiftLocalTargetPos(_entrySeparation);

        PodiumEntry pe = _podiumEntryPool.GetObject();
        pe.InitEntry(ph, rank, _entryRankOnePos.localPosition);
        ph.EnableHologram();
        pe.transform.localPosition = _entrySpawnPos.localPosition;

        _activePodiumEntries.Add(pe);
    }

    public IEnumerator TI_FromTicketBonusToShowEachPodiumEntryEarningsFraction(GameTile gt)
    {
        _travelingTicketBonusText.enabled = true;
        _travelingTicketBonusText.transform.position = gt.GetTicketBonusAmountPos();
        _travelingTicketBonusText.SetText(gt.TicketBonusAmount.ToString()); 

        for (int i =  _activePodiumEntries.Count - 1; i >= 0; i--)
        {
            PodiumEntry targetEntry = _activePodiumEntries[i];

            int reward = gt.TicketBonusAmount / targetEntry.Rank;
            

            if (reward <= 0)
                continue;

            _travelingTicketBonusText.SetText("+" + reward.ToString()); 

            while (Vector3.Distance(targetEntry.GetRewardTextPos(), _travelingTicketBonusText.transform.position) > 0.1f)
            {
                Vector3 nextPos = Vector3.MoveTowards(_travelingTicketBonusText.transform.position, targetEntry.GetRewardTextPos(), _travelingTicketBonusTextSpeed);
                nextPos.z = _podiumTile.transform.position.z;
                _travelingTicketBonusText.transform.position = nextPos;
                yield return null; 
            }
            TextPopupMaster.Inst.CreateTravelingIndicator("+" + MyUtil.AbbreviateNum4Char(reward), reward, targetEntry, targetEntry.Ph, 0.1f, Color.yellow, null); 
            targetEntry.SetReward(reward);
        }

        _travelingTicketBonusText.enabled = false;

    }

    //Doesn't actually send 
    public IEnumerator ShowInvitedByRewards()
    {
        yield return null;
    }

    private void ClearEntries()
    {
        foreach(var entry in _activePodiumEntries)
        {
            /*            entry.Ph.GetPlayerBall().ExplodeBall();
                        entry.Ph.State = PlayerHandlerState.Idle; */
            entry.Ph.DisableHologram(); 

            _podiumEntryPool.ReturnObject(entry);
        }

        _activePodiumEntries.Clear(); 
    }
}
