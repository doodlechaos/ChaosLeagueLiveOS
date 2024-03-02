using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ChaosPlinko : Game, IObsSpawner
{
    [SerializeField] private SpriteRenderer _obsSpawnRegion;
    [SerializeField] private GameObject _obstaclePrefab;
    [SerializeField] private GameObject _deathBallPrefab;

    [SerializeField] private LayerMask _invalidOverlapLayers;

    [SerializeField] private List<int> _spawnRates;

    [SerializeField] private int _targetObsCount;
    [SerializeField] private Transform _chaosObsRoot;
    //[SerializeField] private Transform _deathBallsRoot;
    //[SerializeField] private Transform _deathBallSpawnPoint;
    [SerializeField] private float _spawnInterval; 

    private ObjectPool<ChaosObs> _obstaclePool;
    //private ObjectPool<DeathBall> _deathBallPool;

    private List<ChaosObs> _activeObs = new List<ChaosObs>();
    ///private List<DeathBall> _activeDeathBalls = new List<DeathBall>(); 

    private Collider2D[] overlapResults = new Collider2D[20];

    //[SerializeField] private TextMeshPro _intervalText; 
    //[SerializeField] private float _doubleTimeInterval = 15;


    //private int _currInterval = 0; 

    private void Awake()
    {
        _obstaclePool = new ObjectPool<ChaosObs>(PlayerObstacleFactory, TurnOnObstacle, TurnOffObstacle);
    }

    public ChaosObs PlayerObstacleFactory()
    {
        GameObject playerObstacle = Instantiate(_obstaclePrefab, _chaosObsRoot);
        ChaosObs pov2 = playerObstacle.GetComponentInChildren<ChaosObs>();
        return pov2;
    }

    private void TurnOnObstacle(ChaosObs obs)
    {
        obs.gameObject.SetActive(true);
    }

    private void TurnOffObstacle(ChaosObs obs)
    {
        _activeObs.Remove(obs); 
        obs.gameObject.SetActive(false);
        obs.transform.position = _gt.GetTileController().HoldingPen.position; 
    }

    public override void OnTilePreInit()
    {
        _obsSpawnRegion.enabled = false;
        //_currInterval = 0;
    }

    public override void OnTileInitInPos()
    {
        _gt.GetSuddenDeath().SpawnDeathBall();

       /* //Set the target obstacle count based on the rarity
        if (_gt.GetRarity() == RarityType.Common)
            _targetObsCount = 15;
        else if (_gt.GetRarity() == RarityType.Rare)
            _targetObsCount = 15;
        else if (_gt.GetRarity() == RarityType.Epic)
            _targetObsCount = 15;
        else if (_gt.GetRarity() == RarityType.Legendary)
            _targetObsCount = 15; */

        StartCoroutine(AutoPopulateObs()); 
    }

    private IEnumerator AutoPopulateObs()
    {
        //Pre spawn obstacles with no animation
        while (true)
        {
            yield return new WaitForSeconds(_spawnInterval);

            if (_activeObs.Count >= _targetObsCount)
                continue;

            if(DoneWithGameplay)
                break;

            TrySpawnObs();
        }
    }


    private bool TrySpawnObs()
    {
        ChaosObsType obsType = GetRandomObsType();
        float radius = ChaosObs.GetRadiusOfType(obsType);

        Physics2D.SyncTransforms(); 
        if (!GetValidSpawnPosInRegion(radius, out Vector3 pos))
            return false;

        ChaosObs newObs = _obstaclePool.GetObject();
        newObs.Init(_gt.RarityType, this, obsType, radius);

        if (_gt.IsGolden)
            newObs.MultiplyCurrValue(AppConfig.inst.GetI("GoldenTileMultiplier"));

        if (_gt.GetSuddenDeath().GetCurrInterval() > 0)
            newObs.MultiplyCurrValue(2 * _gt.GetSuddenDeath().GetCurrInterval()); 
        _activeObs.Add(newObs);

        newObs.transform.position = pos;
        return true; 
    }

    private ChaosObsType GetRandomObsType()
    {
        // Calculate the total sum of spawn rates
        float totalSpawnRate = 0;
        foreach (int rate in _spawnRates)
            totalSpawnRate += rate;
        
        // Generate a random value within the total spawn rate
        float randomValue = Random.Range(0f, totalSpawnRate);

        // Find the corresponding index of the chosen spawnable object
        int typeIndex = 0;
        for (int i = 0; i < _spawnRates.Count; i++)
        {
            randomValue -= _spawnRates[i];
            if (randomValue <= 0)
            {
                typeIndex = i;
                break;
            }
        }
        return (ChaosObsType)typeIndex; 
    }

    public override void StartGame()
    {
        //StartCoroutine(RunGame()); 
    }


    public override List<TravelingIndicatorIO> GetDoublingTargets()
    {
        return _activeObs.Cast<TravelingIndicatorIO>().ToList();
    }

    public override void ProcessGameplayCommand(string messageId, TwitchClient twitchClient, PlayerHandler ph, string msg, string rawEmotesRemoved)
    {

    }
    public override void CleanUpGame()
    {
        DoneWithGameplay = true; 

        //throw new NotImplementedException();
        for (int i = _activeObs.Count - 1; i >= 0; i--)
            ReturnObsToPool(_activeObs[i]);

        _activeObs.Clear();
    }

    private bool GetValidSpawnPosInRegion(float objRadius, out Vector3 pos)
    {
        pos = Vector3.zero;

        //Pick a random point in the range that is not within the pegScale distance from the wall so there is no overlap leaking from the zone
        Vector3 spriteSize = _obsSpawnRegion.bounds.size;
        Vector3 spriteCenter = _obsSpawnRegion.bounds.center;

        // Calculate top-left and bottom-right positions
        Vector3 bottomLeft = spriteCenter - spriteSize * 0.5f;
        Vector3 topRight = spriteCenter + spriteSize * 0.5f;

        Debug.DrawRay(bottomLeft, -Vector3.forward, Color.white, 10f);
        Debug.DrawRay(topRight, -Vector3.forward, Color.white, 10f);

        //Adjust for the peg scale
        bottomLeft += new Vector3(objRadius, objRadius, 0);
        topRight += new Vector3(-objRadius, -objRadius, 0);
        Debug.DrawRay(bottomLeft, -Vector3.forward, Color.green, 10f);
        Debug.DrawRay(topRight, -Vector3.forward, Color.green, 10f);

        int failures = 0;
        //Check if that point would overlap with any other pegs or undesired locations at the current scale, if there is a collision, try again.

        while (failures < 15)
        {
            pos = MyUtil.GetRandomPointInRect(bottomLeft, topRight);
            Debug.DrawRay(pos, -Vector3.forward, Color.yellow, 10f);
            //Check for overlap with other obstacles
            // = Physics2D.OverlapCircleAll(pos, pegScale / 2, InvalidOverlapLayers);
            int overlapCount = Physics2D.OverlapCircleNonAlloc(pos, objRadius, overlapResults, _invalidOverlapLayers);
            //debugCircleCenter = pos;
            //debugCircleRadius = pegScale / 2;
            if (overlapCount <= 0)
                return true;
            //Debug.Log($"Detedted collision with: {overlapCount} example: {overlapResults[0].name}");
            failures++;

        }

        CLDebug.Inst.Log("Failed to find valid spawn position for obstacle in " + gameObject.name);
        return false;
    }

    public void ReturnObsToPool(ChaosObs obs)
    {
        //Debug.Log("returning obs to pool in plinko"); 
        _activeObs.Remove(obs);
        _obstaclePool.ReturnObject(obs);
    }
}
