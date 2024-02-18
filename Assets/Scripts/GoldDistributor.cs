using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using TwitchLib.Unity;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class GoldDistributor : MonoBehaviour, TravelingIndicatorIO
{
    [SerializeField] private KingController _kingController;
    [SerializeField] private TextMeshPro _storedGoldText;
    [SerializeField] private TextMeshPro _distributeTimerText;

    [SerializeField] private TwitchApi _twitchAPI;
    [SerializeField] private HoldingPen _holdingPen;
    [SerializeField] private Transform _goldCoinsRoot; 

    [SerializeField] private ParticleSystem _particleSys;
    [SerializeField] private MeshRenderer _pieTimer;
    [SerializeField] private SpriteRenderer _goldChest;

    [SerializeField] private Vector2 _coinAmountColorMap; 
    [SerializeField] private Gradient _coinColor;

    [SerializeField] private Color _startColor;
    [SerializeField] private Color _endColor;

    [SerializeField] private GameObject _goldCoinPrefab;

    [SerializeField] private int testDistributeAmount = 30;
    [SerializeField] private bool testDistributeButton;

    [SerializeField] private Vector2 _spawnVelMin;
    [SerializeField] private Vector2 _spawnVelMax; 

    private ObjectPool<GoldCoin> _goldCoinPool;

    [SerializeField] private int _baseGoldPerTile = 100; 

    private int _storedGold; 
    //private int _liveViewCount;
    //private CancellationTokenSource _cts;
    private MaterialPropertyBlock _mpb;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();

        SetBackgroundColor(_startColor, _endColor);

        //_cts = new CancellationTokenSource();

        //_ = UpdateLiveViewCount(_cts);
        _storedGold = 0; 
        UpdateGoldStoredText();


        StartCoroutine(ViewerGoldFaucet());

        _goldCoinPool = new ObjectPool<GoldCoin>(GoldCoinFactory, GoldCoinTurnOn, GoldCoinTurnOff);
    }

    private void OnValidate()
    {
        if (testDistributeButton)
        {
            testDistributeButton = false;
            _storedGold = testDistributeAmount; 
            DistributeGold(); 
        }
    }

    private GoldCoin GoldCoinFactory()
    {
        return Instantiate(_goldCoinPrefab, _goldCoinsRoot).GetComponent<GoldCoin>();
    }

    private void GoldCoinTurnOn(GoldCoin goldCoin)
    {
        goldCoin.gameObject.SetActive(true); 
    }

    private void GoldCoinTurnOff(GoldCoin goldCoin)
    {
        goldCoin.gameObject.SetActive(false);
        goldCoin.transform.position = _holdingPen.transform.position;
    }


    public IEnumerator ViewerGoldFaucet()
    {
        while (true)
        {
            int _payoutWaitTime = AppConfig.inst.GetI("GoldPayoutInterval");
            for (int i = 0; i < _payoutWaitTime; i++)
            {
                _distributeTimerText.SetText(MyUtil.GetMinuteSecString(_payoutWaitTime - i));

                float t = i / (float)_payoutWaitTime;
                SetBackgroundShader(t);

                //_storedGold += 1;
                //UpdateGoldStoredText();

                yield return new WaitForSeconds(1);
            }

            DistributeGold(); 

            yield return null;
        }
    }

    public void SpawnCoin(Vector3 origin, Vector2 spawnVel, TravelingIndicatorIO target, int coinValue)
    {
        Color coinColor = _coinColor.Evaluate(coinValue / _coinAmountColorMap.y);

        GoldCoin newCoin = _goldCoinPool.GetObject();
        newCoin.InitializeCoin(this, origin, spawnVel, target, coinValue, coinColor);
    }

    private void DistributeGold()
    {
        if (_kingController.currentKing == null)
            return;

        while (_storedGold > 0)
        {
            int coinValue = Mathf.Min(_storedGold, 10);
            Vector3 spawnVel = new Vector2(Random.Range(_spawnVelMin.x, _spawnVelMax.x), Random.Range(_spawnVelMin.y, _spawnVelMax.y));
            SpawnCoin(Get_TI_IO_Position(), spawnVel, _kingController.currentKing.Ph, coinValue);
            _storedGold -= coinValue;
        }

        UpdateGoldStoredText(); 
    }

    public void SpawnGoldFromTileRarity(GameTile gt)
    {
        int mult = AppConfig.GetMult(gt.RarityType);
        int totalGold = _baseGoldPerTile * mult;
        if (gt.IsGolden)
            totalGold *= AppConfig.inst.GetI("GoldenTileMultiplier");

        while (totalGold > 0)
        {
            int coinValue = Mathf.Min(totalGold, 10);
            if (totalGold > 10000)
                coinValue = 100;

            float randomX = Mathf.Pow(Random.Range(0f, 1f), 2) * 10f;
            float randomY = Mathf.Pow(Random.Range(0f, 1f), 2) * (-20f - -10f) + -10f;

            Vector2 spawnVel = new Vector2(randomX, randomY);

            SpawnCoin(gt.GetGoldSpawnPos(), spawnVel, this, coinValue);
            //newCoin.InitializeCoin(this, gt.GetGoldSpawnPos(), spawnVel, this, coinValue, coinColor);
            totalGold -= coinValue;
        }
    }

    public void SpawnGoldFromDefenseBrick(Vector3 brickPos, int totalGold, TravelingIndicatorIO target)
    {
        while (totalGold > 0)
        {
            int coinValue = Mathf.Min(totalGold, 10);
            if (totalGold > 10000)
                coinValue = 100;

            Vector2 spawnVel = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));

            SpawnCoin(brickPos, spawnVel, target, coinValue);
            totalGold -= coinValue;
        }
    }

    private void UpdateGoldStoredText()
    {
        _storedGoldText.SetText(_storedGold.ToString()); 
    }

    public void NewKingSignal()
    {
        //Give gold to the king equal to the viewer count
        //if (_kingController.currentKing != null)
        //    TextPopupMaster.Inst.CreateTravelingIndicator($"{_liveViewCount}", _liveViewCount, this, _kingController.currentKing.Ph, 0.05f, MyColors.Gold, null, TI_Type.GiveGold);
        //DistributeGold(_liveViewCount);
    }

/*    public async Task UpdateLiveViewCount(CancellationTokenSource cts)
    {
        int sequentialFailCount = 0; 
        while (true)
        {
            await Task.Delay(10_000);

            if (cts.IsCancellationRequested)
                return;

            try
            {
                var stream = await _twitchAPI.GetStream();
                if (stream == null)
                {
                    _liveViewCountText.SetText("1*");
                    _liveViewCount = 1; 
                    continue;
                }
                Debug.Log($"stream title: {stream.Title} stream viewer count: {stream.ViewerCount} stream date: {stream.StartedAt} ");
                _liveViewCountText.SetText(MyUtil.AbbreviateNum4Char(stream.ViewerCount));
                _liveViewCount = stream.ViewerCount;
                sequentialFailCount = 0; 
            }
            catch (Exception ex)
            {
                sequentialFailCount++;
                Debug.Log($"Failed to update live view count {sequentialFailCount} times in a row.\n" + ex.Message);
            }

        }
    }*/

    private void SetBackgroundShader(float t)
    {
        _pieTimer.GetPropertyBlock(_mpb);
        _mpb.SetFloat("_FillAmount", 1 - t);
        _pieTimer.SetPropertyBlock(_mpb);
    }

    private void SetBackgroundColor(Color startColor, Color endColor)
    {
        _pieTimer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_StartColor", startColor);
        _mpb.SetColor("_EndColor", endColor);
        _pieTimer.SetPropertyBlock(_mpb);
    }

/*    private void OnApplicationQuit()
    {
        _cts?.Cancel();
    }*/

    public void ReceiveTravelingIndicator(TravelingIndicator TI)
    {
        if (TI.TI_Type != TI_Type.GiveGold && TI.TI_Type != TI_Type.GiveGoldDoBonus)
            return;

        _storedGold += (int)TI.value;
        AudioController.inst.PlaySound(AudioController.inst.CollectGold, 0.88f, 1.0f);

        UpdateGoldStoredText(); 
    }

    public Vector3 Get_TI_IO_Position()
    {
        return _goldChest.transform.position;
    }

    public void ReturnGoldToPool(GoldCoin coin)
    {
        _goldCoinPool.ReturnObject(coin); 
    }
}
