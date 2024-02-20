using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SuddenDeath : MonoBehaviour
{
    [SerializeField] private GameTile _gt;

    [SerializeField] private GameObject _deathBallPrefab;
    [SerializeField] private float _dbRetractedDuration = 3f; 
    [SerializeField] private TextMeshPro _intervalText;
    [SerializeField] private float _doubleTimeInterval = 15;
    [SerializeField] private MeshRenderer _progressRing;
    [SerializeField] private Transform _deathBallsRoot;
    [SerializeField] private Transform _deathBallSpawnPoint;
    [SerializeField] private Color _ringColor;
    [SerializeField] private Color _deathBallWarningColor;
    [SerializeField] private bool NoMultiply;

    //[SerializeField] private int _doublingDeathBallInterval = 3;

    private ObjectPool<DeathBall> _deathBallPool;
    private List<DeathBall> _activeDeathBalls = new List<DeathBall>();

    private int _interval_Index = 0;
    [SerializeField] private List<bool> _deathballPattern; 

    private MaterialPropertyBlock _ringMatPropBlock;

    private void Awake()
    {
        _deathBallPool = new ObjectPool<DeathBall>(DeathBallFactory, TurnOnDeathBall, TurnOffDeathBall); 
    }
    public DeathBall DeathBallFactory()
    {
        DeathBall db = Instantiate(_deathBallPrefab, _deathBallsRoot).GetComponent<DeathBall>();
        return db;
    }
    public void TurnOnDeathBall(DeathBall db)
    {
        db.gameObject.SetActive(true);
    }

    public void TurnOffDeathBall(DeathBall db)
    {
        db.gameObject.SetActive(false);
        db.transform.position = _gt.GetTileController().HoldingPen.position;
    }

    public void OnTilePreInit()
    {
        _interval_Index = 0; 
        _ringMatPropBlock = new MaterialPropertyBlock();
        _ringMatPropBlock.SetColor("_RingColor", _ringColor);
        _ringMatPropBlock.SetFloat("_InnerRing", 0.78f);
        _ringMatPropBlock.SetFloat("_FillAmount", 0);
        _progressRing.SetPropertyBlock(_ringMatPropBlock);
    }

    public void StartSuddenDeath()
    {
        StartCoroutine(Run()); 
    }

    private IEnumerator Run()
    {
        SetRingColor();

        //Run the timer for the interval, when it finishes, send out of wave of X2's from the top position
        float timer = 0;

        while (_gt.AlivePlayers.Count > 0 || _gt.ConveyorBelt.Count > 0)
        {
            yield return null;

            float t = timer / _doubleTimeInterval;
            //_gt.SetBackgroundShader(1 - t);

            _ringMatPropBlock.SetFloat("_FillAmount", t);
            _progressRing.SetPropertyBlock(_ringMatPropBlock); 

            if (t >= 1)
            {
                timer = 0;
                if (NoMultiply)
                    SpawnDeathBall();
                else
                ActivateDoubling(); 
            }

            timer += Time.deltaTime;
        }

    }

    private void ActivateDoubling()
    {
        //Get all the effectors we want to double from the game and the tile
        List<TravelingIndicatorIO> doublingTargets = new List<TravelingIndicatorIO>();
        if (_gt.GetGame() != null)
            doublingTargets.AddRange(_gt.GetGame().GetDoublingTargets());

        foreach(var effector in _gt.Effectors)
        {
            if (effector.GetEffect() == PBEffect.None)
                continue;
            else if (effector.GetEffect() == PBEffect.Explode)
                continue;
            else if ((effector.GetEffect() & PBEffect.Zero) == PBEffect.Zero)
                continue;
            doublingTargets.Add(effector); 
        }


        foreach (var doubleTarget in doublingTargets)
            TextPopupMaster.Inst.CreateTravelingIndicator("x2", 2, _intervalText.transform.position, doubleTarget, 0.13f, Color.magenta, null, TI_Type.Multiply);

        SetRingColor();


        if (_interval_Index > _deathballPattern.Count - 1 || _deathballPattern[_interval_Index])
            SpawnDeathBall();

        AudioController.inst.PlaySound(AudioController.inst.MultiplyPoints, 0.95f, 1.05f);
        _interval_Index++;
    }

    private void SetRingColor()
    {
        //If there is a death ball coming next, change the ring color to as a warning
        if (_interval_Index + 1 > _deathballPattern.Count - 1 || _deathballPattern[_interval_Index + 1])
            _ringMatPropBlock.SetColor("_RingColor", _deathBallWarningColor);
        else
            _ringMatPropBlock.SetColor("_RingColor", _ringColor);
        _progressRing.SetPropertyBlock(_ringMatPropBlock);
    }

    public void SpawnDeathBall()
    {
        //Spawn death ball
        DeathBall db = _deathBallPool.GetObject();
        _activeDeathBalls.Add(db);
        db.InitDB(_dbRetractedDuration);
        db._rb.transform.position = _deathBallSpawnPoint.position;

        AudioController.inst.PlaySound(AudioController.inst.SpikesExtend, 0.95f, 1.05f);
    }

    public int GetCurrInterval()
    {
        return _interval_Index;
    }

    public void CleanUp()
    {
        for (int i = _activeDeathBalls.Count - 1; i >= 0; i--)
            _deathBallPool.ReturnObject(_activeDeathBalls[i]);

        _activeDeathBalls.Clear();
    }
}
