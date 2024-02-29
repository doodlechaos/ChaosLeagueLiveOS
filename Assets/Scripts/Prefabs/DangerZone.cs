using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class DangerZone : PlayerReceiveable
{
    [SerializeField] private MeshRenderer _background;

    [SerializeField] private Transform SpawnPos1;
    [SerializeField] private Transform SpawnPos2;

    [SerializeField] private Color _killStartColor;
    [SerializeField] private Color _killEndColor;
    [SerializeField] private Color _safeTileColor;

    [SerializeField] private Transform _spinner;

    private MaterialPropertyBlock _mpb;

    [SerializeField] private AnimationCurve _spinSpeed;
    private float _percentChanceKill = 0;

    [SerializeField] private bool testSpin;
    [SerializeField] private float testDeathPercent;

    [SerializeField] private GameTile _gt;    

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock(); 
    }

    //TEMP
    private void OnValidate()
    {
        if (testSpin)
        {
            testSpin = false;

            InitZone(testDeathPercent);

            StartCoroutine(Spin(0, 4));
        }
    }

    public void InitZone(float percentChanceKill)
    {
        _percentChanceKill = percentChanceKill;
        _spinner.eulerAngles = Vector3.zero;
        _spinner.gameObject.SetActive(false);

        if (percentChanceKill <= 0)
            SetBackgroundColor(_safeTileColor, _safeTileColor);
        else
            SetBackgroundColor(_killStartColor, _killEndColor);

        SetBackgroundShader(percentChanceKill); 
    }

    public IEnumerator Spin(int round, float spinDuration)
    {
        if(_percentChanceKill <= 0)
            yield break;

        if(_percentChanceKill >= 1)
        {
            KillTouchingPlayers(round);
            yield break;
        }

        _spinner.gameObject.SetActive(true);

        float targetVal = Random.Range(0f, 1f);

        int fullRotations = Random.Range(3, 6);

        float timer = 0;
        Vector3 spinnerRot = Vector3.zero;
        while(timer < spinDuration)
        {
            float t = timer / spinDuration;

            spinnerRot.z = Mathf.Lerp(0, -(targetVal + fullRotations) * 360, _spinSpeed.Evaluate(t));  
            _spinner.eulerAngles = spinnerRot;

            timer += Time.deltaTime;
            yield return null; 
        }

        if(targetVal < _percentChanceKill)
            KillTouchingPlayers(round); 

    }

    public void KillTouchingPlayers(int round)
    {
        var overlaps = Physics2D.OverlapBoxAll(_background.transform.position, new Vector2(_background.transform.localScale.x, _background.transform.localScale.y), 0);

        foreach(var overlap in overlaps)
        {
            var pb = overlap.GetComponentInParent<PlayerBall>();
            if(pb == null)
                continue;

            pb.Ph.SetRankScore(round);
            _gt.EliminatePlayer(pb.Ph, false);
            pb.ExplodeBall();
        }

        Debug.Log("Killing touching players"); 
    }

    private void SetBackgroundShader(float t)
    {
        _background.GetPropertyBlock(_mpb);
        _mpb.SetFloat("_FillAmount", 1 - t);
        _background.SetPropertyBlock(_mpb);
    }

    private void SetBackgroundColor(Color startColor,  Color endColor)
    {
        _background.GetPropertyBlock(_mpb);
        _mpb.SetColor("_StartColor", endColor);
        _mpb.SetColor("_EndColor", startColor); 
        _background.SetPropertyBlock(_mpb);
    }

    public Vector3 GetSpawnPos(bool isSecondPos)
    {
        if (isSecondPos)
            return SpawnPos2.transform.position;

        return SpawnPos1.transform.position;
    }

}
