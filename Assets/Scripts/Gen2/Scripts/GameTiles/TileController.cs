using Amazon.Runtime.Internal.Transform;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;
using UnityEngine;
using UnityEngine.UIElements;

public class TileController : MonoBehaviour
{
    [SerializeField] private GameManager _gm; 
    [SerializeField] private KingController _kingController;
    [SerializeField] private AutoPredictions _autoPredictions;
    [SerializeField] public Podium Podium;
    [SerializeField] private TwitchClient _twitchClient;
    [SerializeField] private RebellionController _rebellionController;
    [SerializeField] private GoldDistributor _goldDistributor;

    [SerializeField] private float _commonRarity;
    [SerializeField] private float _rareRarity;
    [SerializeField] private float _epicRarity;
    [SerializeField] private float _legendaryRarity;
    [SerializeField] private float _mythicRarity;
    [SerializeField] private float _etherealRarity;
    [SerializeField] private float _cosmicRarity;

    [SerializeField] private List<GameTile> AllRarities; //Adds one to each rarity 
    [SerializeField] private List<GameTile> CommonTiles; //69%      // 50%
    [SerializeField] private List<GameTile> RareTiles; // 25%       // 30%
    [SerializeField] private List<GameTile> EpicTiles; // 5%        // 17%
    [SerializeField] private List<GameTile> LegendaryTiles; // 1%   // 2.7%
    [SerializeField] private List<GameTile> MythicTiles; // X       // 0.27%
    [SerializeField] private List<GameTile> EtherealTiles; // X     // 0.027%
    [SerializeField] private List<GameTile> CosmicTiles; // X       // 0.003%

    public Transform HoldingPen;
    public Transform TilesRoot;

    public Transform LeftTileCenter;
    public Transform CenterTileCenter;
    public Transform RightTileCenter;

    [SerializeField] public BidHandler BidHandler;

    [SerializeField] public GameTile GameplayTile;
    [SerializeField] public GameTile CurrentBiddingTile; 
    [SerializeField] public GameTile NextBiddingTile;
    [SerializeField] private GameTile ForceThisTileNext;
    [SerializeField] private RarityType _forceThisRarity;
    [SerializeField] private bool _forceGolden; 

    private Dictionary<int, ObjectPool<GameTile>> _tilePools = new Dictionary<int, ObjectPool<GameTile>>(); 

    [SerializeField] private float _rotationDurationS = 10;
    [SerializeField] private int _animeTileCountMin;
    [SerializeField] private int _animeTileCountMax;
    [SerializeField] private float _animeRotateDegreesPerTile;

    [SerializeField] private AnimationCurve _animeSpeedCurve;

    [SerializeField] private List<Color> _pipeColors;
    [Space(5)]
    [SerializeField] private Color _commonStartColor;
    [SerializeField] private Color _commonEndColor;
    [SerializeField] private Color _commonTrimColor;
    [Space(5)]
    [SerializeField] private Color _rareStartColor;
    [SerializeField] private Color _rareEndColor;
    [SerializeField] private Color _rareTrimColor;
    [Space(5)]
    [SerializeField] private Color _epicStartColor;
    [SerializeField] private Color _epicEndColor;
    [SerializeField] private Color _epicTrimColor;
    [Space(5)]
    [SerializeField] private Color _legendaryStartColor;
    [SerializeField] private Color _legendaryEndColor;
    [SerializeField] private Color _legendaryTrimColor;
    [Space(5)]
    [SerializeField] private Color _mythicStartColor;
    [SerializeField] private Color _mythicEndColor;
    [SerializeField] private Color _mythicTrimColor;
    [Space(5)]
    [SerializeField] private Color _etherealStartColor;
    [SerializeField] private Color _etherealEndColor;
    [SerializeField] private Color _etherealTrimColor;
    [Space(5)]
    [SerializeField] private Color _cosmicStartColor;
    [SerializeField] private Color _cosmicEndColor;
    [SerializeField] private Color _cosmicTrimColor;

    private int _tileCounter = 0; 
    private int _promptIndex = 0; 

    private void Start()
    {

        //Set all the tile ID numbers
        int tileID = 0;
        foreach (GameTile tile in AllRarities)
        {
            tile.TileIDNum = tileID++;
        }
        foreach (GameTile tile in CommonTiles)
        {
            tile.TileIDNum = tileID++;
            tile.SetRarity(RarityType.Common, _commonStartColor, _commonEndColor, _commonTrimColor); 
        }
        foreach (GameTile tile in RareTiles)
        {
            tile.TileIDNum = tileID++;
            tile.SetRarity(RarityType.Rare, _rareStartColor, _rareEndColor, _rareTrimColor);
        }
        foreach (GameTile tile in EpicTiles)
        {
            tile.TileIDNum = tileID++;
            tile.SetRarity(RarityType.Epic, _epicStartColor, _epicEndColor, _epicTrimColor);
        }
        foreach (GameTile tile in LegendaryTiles)
        {
            tile.TileIDNum = tileID++;
            tile.SetRarity(RarityType.Legendary, _legendaryStartColor, _legendaryEndColor, _legendaryTrimColor);
        }
        foreach (GameTile tile in MythicTiles)
        {
            tile.TileIDNum = tileID++;
            tile.SetRarity(RarityType.Mythic, _mythicStartColor, _mythicEndColor, _mythicTrimColor);
        }
        foreach (GameTile tile in EtherealTiles)
        {
            tile.TileIDNum = tileID++;
            tile.SetRarity(RarityType.Ethereal, _etherealStartColor, _etherealEndColor, _etherealTrimColor);
        }
        foreach (GameTile tile in CosmicTiles)
        {
            tile.TileIDNum = tileID++;
            tile.SetRarity(RarityType.Cosmic, _cosmicStartColor, _cosmicEndColor, _cosmicTrimColor);
        }
        List<GameTile> tilePossibilities = new List<GameTile>();
        tilePossibilities = tilePossibilities.Concat(AllRarities).Concat(CommonTiles).Concat(RareTiles).Concat(EpicTiles).Concat(LegendaryTiles).Concat(MythicTiles).Concat(EtherealTiles).Concat(CosmicTiles).ToList(); 

        //Create a pool for each tile type
        foreach (GameTile tile in tilePossibilities)
            _tilePools.Add(tile.TileIDNum, new ObjectPool<GameTile>(() => {
                GameTile newTile = Instantiate(tile.gameObject).GetComponent<GameTile>();
                newTile.gameObject.name = "tile" + _tileCounter++;
                newTile.TileState = TileState.Inactive;
                return newTile; 
            }, 
            PoolTurnOnGT, 
            PoolTurnOffGT));


        CurrentBiddingTile = SpawnOriginTile(LeftTileCenter.position, Side.Left, false);
        CurrentBiddingTile.PreInitTile(this, _forceGolden);
        CurrentBiddingTile.InitTileInPos(); 

        StartCoroutine(BidHandler.RunBiddingOn(CurrentBiddingTile)); 
        
        GameplayTile = SpawnOriginTile(RightTileCenter.position, Side.Right, false);
        GameplayTile.PreInitTile(this, _forceGolden); 
        GameplayTile.InitTileInPos();
        StartCoroutine(GameplayTile.RunTile());
    }

    public void PoolTurnOnGT(GameTile tile)
    {
        tile.gameObject.SetActive(true);

        tile.TogglePhysics(false);
    }

    public void PoolTurnOffGT(GameTile tile)
    {
        //tile.transform.position = _tc.HoldingPen.position;
        tile.gameObject.SetActive(false);
        tile.transform.position = new Vector3(100, 0, 0);

    }
    public void ProcessMsgInTiles(PlayerHandler ph)
    {
/*        //Receive the stream event in each of the tiles 
        foreach (GameTile tile in _activeTiles)
            tile.ProcessPBMsg(ph, se);*/
    }

    public GameTile SpawnOriginTile(Vector3 pos, Side side, bool spinNew) 
    {
        GameTile gt; 
        if (ForceThisTileNext != null)
        {
            gt = GetTileByTileID(ForceThisTileNext.TileIDNum, _forceThisRarity, side);
            ForceThisTileNext = null;
        }
        else
            gt = GetRandomTile(null, side);

        gt.transform.position = pos; 
        if(spinNew)
            return SpinNewTile(gt);

        return gt;
    }

    //Have to start the coroutine here. If I start it in the gametile, when the tile is disabled, it will stop the coroutine
    public GameTile SpinNewTile(GameTile gt)
    {
        //Don't allow the same tile to spin, or a match for any other active file
        List<int> blacklistedTiles = new List<int>
            {
                gt.TileIDNum,
                CurrentBiddingTile.TileIDNum
            };
        if (GameplayTile != null)
            blacklistedTiles.Add(GameplayTile.TileIDNum);

        GameTile nextTile;
        if(ForceThisTileNext != null)
        {
            nextTile = GetTileByTileID(ForceThisTileNext.TileIDNum, _forceThisRarity, gt.CurrentSide);
            ForceThisTileNext = null; 
        } 
        else
             nextTile = GetRandomTile(blacklistedTiles, gt.CurrentSide);
        NextBiddingTile = nextTile;
        GameplayTile = null; 
        StartCoroutine(CSpinNewTile(gt, nextTile));

        return nextTile; 
    }


    //Can pass in null for the forceThisTileNext if you don't want to 
    private IEnumerator CSpinNewTile(GameTile gt, GameTile nextTile)
    {
        _rebellionController.OnNewTileSpin(); 
        //Choose amount of random tiles and put them in a list
        List<GameTile> animeTiles = new List<GameTile>{ gt };//Add the current tile to the animatino

        int _animeTileCount = Random.Range(_animeTileCountMin, _animeTileCountMax);
        for(int i = 0; i < _animeTileCount; i++)
            animeTiles.Add(GetRandomTile(null, gt.CurrentSide));

        animeTiles.Add(nextTile);

        //Init all the tiles visually except for the first one (that just finished gameplay)
        for (int i = 1; i < animeTiles.Count; i++)
        {
            GameTile tile = animeTiles[i];
            bool isGolden = false;
            float random = Random.Range(0f, 100f);
            if (random <= AppConfig.inst.GetI("GoldenTilePercentChance"))
                isGolden = true;

            if (_forceGolden)
                isGolden = true; 

            tile.PreInitTile(this, isGolden);
        }

        Vector3 finalTilePos = gt.transform.position;
        Quaternion finalTileRot = gt.transform.rotation;

        Vector3 rotatePoint = finalTilePos + Vector3.forward * 50;

        //Play the spin animation
        yield return SpinAnimation(animeTiles, rotatePoint, finalTilePos);

        //Move all the random animation tiles back to the holding pen, and init the selected one
        for(int i = 0; i < animeTiles.Count; i++)
        {
            GameTile tile = animeTiles[i];

            if (i == animeTiles.Count - 1) //If the last tile in the animation
            {
                tile.transform.SetPositionAndRotation(finalTilePos, finalTileRot);
                tile.InitTileInPos();
                _goldDistributor.SpawnGoldFromTileRarity(tile); 
                if (tile.RarityType == RarityType.Legendary)
                    _autoPredictions.LegendarySignal(); 
                else if (tile.RarityType == RarityType.Mythic)
                    _autoPredictions.LegendarySignal();
                else if (tile.RarityType == RarityType.Ethereal)
                    _autoPredictions.LegendarySignal();
                else if (tile.RarityType == RarityType.Cosmic)
                    _autoPredictions.LegendarySignal();
                continue;
            }
            _tilePools[tile.TileIDNum].ReturnObject(tile);
        }

    }

    public IEnumerator SpinAnimation(List<GameTile> animeTiles, Vector3 rotatePoint, Vector3 finalTilePos)
    {

        float rotationTimer = 0;
        float totalRotation = (animeTiles.Count - 1) * _animeRotateDegreesPerTile;
        float t = 0;

        AudioController.inst.PlaySound(AudioController.inst.TileSpinStart, 0.95f, 1.05f);

        while (rotationTimer < _rotationDurationS)
        {
            for (int i = 0; i < animeTiles.Count; i++)
            {
                GameTile tile = animeTiles[i];
                tile.transform.SetPositionAndRotation(finalTilePos, Quaternion.identity);
                float degrees = i * _animeRotateDegreesPerTile - (totalRotation * t);

                //Cull if out of view
                if (degrees > -25 && degrees < 25)
                    tile.gameObject.SetActive(true);
                else
                    tile.gameObject.SetActive(false);

                tile.transform.RotateAround(rotatePoint, Vector3.right, degrees);
            }

            rotationTimer += Time.deltaTime;

            t = _animeSpeedCurve.Evaluate(rotationTimer / _rotationDurationS);

            yield return null;
        }
        //Debug.Log("DONE WITH spin animation with animeTiles count: " + animeTiles.Count + "names: " + debugString);

    }

    public (int, RarityType) GetRandomIDandRarity(List<int> blacklistedTiles)
    {
        GameTile tile;
        RarityType rarity;
        do
        {
            //Select the tile
            float t = Random.Range(0f, 1f);
            if (t <= _cosmicRarity)
            {
                int index = Random.Range(0, CosmicTiles.Count + AllRarities.Count);
                tile = (index < CosmicTiles.Count) ? CosmicTiles[index] : AllRarities[index - CosmicTiles.Count];
                rarity = RarityType.Cosmic;
            }
            else if (t <= _cosmicRarity + _etherealRarity)
            {
                int index = Random.Range(0, EtherealTiles.Count + AllRarities.Count);
                tile = (index < EtherealTiles.Count) ? EtherealTiles[index] : AllRarities[index - EtherealTiles.Count];
                rarity = RarityType.Ethereal;
            }
            else if (t <= _cosmicRarity + _etherealRarity + _mythicRarity)
            {
                int index = Random.Range(0, MythicTiles.Count + AllRarities.Count);
                tile = (index < MythicTiles.Count) ? MythicTiles[index] : AllRarities[index - MythicTiles.Count];
                rarity = RarityType.Mythic;
            }
            else if (t <= _cosmicRarity + _etherealRarity + _mythicRarity + _legendaryRarity)
            {
                int index = Random.Range(0, LegendaryTiles.Count + AllRarities.Count);
                tile = (index < LegendaryTiles.Count) ? LegendaryTiles[index] : AllRarities[index - LegendaryTiles.Count];
                rarity = RarityType.Legendary;
            }
            else if (t <= _cosmicRarity + _etherealRarity + _mythicRarity + _legendaryRarity + _epicRarity)
            {
                int index = Random.Range(0, EpicTiles.Count + AllRarities.Count);
                tile = (index < EpicTiles.Count) ? EpicTiles[index] : AllRarities[index - EpicTiles.Count];
                rarity = RarityType.Epic;
            }
            else if (t <= _cosmicRarity + _etherealRarity + _mythicRarity + _legendaryRarity + _epicRarity + _rareRarity)
            {
                int index = Random.Range(0, RareTiles.Count + AllRarities.Count);
                tile = (index < RareTiles.Count) ? RareTiles[index] : AllRarities[index - RareTiles.Count];
                rarity = RarityType.Rare;
            }
            else
            {
                int index = Random.Range(0, CommonTiles.Count + AllRarities.Count);
                tile = (index < CommonTiles.Count) ? CommonTiles[index] : AllRarities[index - CommonTiles.Count];
                rarity = RarityType.Common;
            }
        } while (blacklistedTiles != null && blacklistedTiles.Any(t => t == tile.TileIDNum));

        return (tile.TileIDNum, rarity);
    }

    public GameTile GetRandomTile(List<int> blacklistedTiles, Side side)
    {
        (int TileIDNum, RarityType rarity) = GetRandomIDandRarity(blacklistedTiles);

        GameTile tile = GetTileByTileID(TileIDNum, rarity, side);

        return tile;
    }

    private GameTile GetTileByTileID(int tileID, RarityType rarity, Side side)
    {
        //Get or create it from the pool
        _tilePools.TryGetValue(tileID, out ObjectPool<GameTile> pool);

        if (pool == null)
            Debug.LogError($"Unable to find pool with ID {tileID}");
        

        GameTile tile = pool.GetObject();
        tile.transform.SetParent(TilesRoot);
        tile.CurrentSide = side;

        if (rarity == RarityType.Common)
            tile.SetRarity(RarityType.Common, _commonStartColor, _commonEndColor, _commonTrimColor);
        else if(rarity == RarityType.Rare)
            tile.SetRarity(RarityType.Rare, _rareStartColor, _rareEndColor, _rareTrimColor);
        else if(rarity == RarityType.Epic)
            tile.SetRarity(RarityType.Epic, _epicStartColor, _epicEndColor, _epicTrimColor);
        else if (rarity == RarityType.Legendary)
            tile.SetRarity(RarityType.Legendary, _legendaryStartColor, _legendaryEndColor, _legendaryTrimColor);
        else if (rarity == RarityType.Mythic)
            tile.SetRarity(RarityType.Mythic, _mythicStartColor, _mythicEndColor, _mythicTrimColor);
        else if (rarity == RarityType.Ethereal)
            tile.SetRarity(RarityType.Ethereal, _etherealStartColor, _etherealEndColor, _etherealTrimColor);
        else
            tile.SetRarity(RarityType.Cosmic, _cosmicStartColor, _cosmicEndColor, _cosmicTrimColor);
        return tile;
    }

    public int GetNextPromptIndex()
    {
        int nextIndex = _promptIndex;
        _promptIndex = (_promptIndex + 1) % AppConfig.QuipBattleQuestions.list.Count; 
        return nextIndex;
    }

    public GameManager GetGameManager() { return _gm; }

}
