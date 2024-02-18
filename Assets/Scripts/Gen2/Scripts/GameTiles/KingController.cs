using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KingController : MonoBehaviour, TravelingIndicatorIO
{
    public PlayerBall currentKing = null;

    [SerializeField] private GameManager _gm;
    [SerializeField] private TileController _tileController;
    [SerializeField] private MeshRenderer kingInitialPlaceholder;
    [SerializeField] private Transform kingTransform;
    [SerializeField] private PBDetector _pbCollisionDetector;
    [SerializeField] private CleaningBarController _cleaningBarController;
    [SerializeField] private ParticleSystem confetti;
    [SerializeField] private TextMeshPro winnerNameText;
    [SerializeField] private TextMeshPro _tollRateText;
    [SerializeField] private AutoPredictions _autoPredictions;
    [SerializeField] private GoldDistributor _liveViewCount;
    [SerializeField] private Crown _crown;
    [SerializeField] private MyCameraController _myCameraController;

    [SerializeField] private TextMeshPro _kingPointsText;
    [SerializeField] private TextMeshPro _kingGoldText;

    [SerializeField] private DefaultDefenseV2 _defaultDefenseV2;

    private float _previousKingDuration = 0;
    private float _currentKingTimer = 0;

    [SerializeField] private GameObject _newKingBlockade;
    [SerializeField] private TextMeshPro _newKingBlockadeTimer;
    [SerializeField] private Material _throneTileTrim;
    [SerializeField] private bool _enableNewKingBlockade = false; 

    public int TollRate = 0;


    public void Awake()
    {
        _newKingBlockade.SetActive(false);
        _newKingBlockadeTimer.enabled = false;
    }

    public void UpdateTollRate(int rate)
    {
        if (currentKing == null)
        {
            CLDebug.Inst.Log("Failed to update toll rate. No currentKing");
            return;
        }

        if (rate == TollRate)
            return;

        //MyTTS.inst.Announce($"{currentKing.Ph.pp.TwitchUsername} changed the toll rate to {rate}");
        currentKing.Ph.SpeechBubble($"I decree a new toll rate: {rate}"); 

        TollRate = rate;
        
        _tileController.GameplayTile?.EntrancePipe.SetTollCost(rate);
        _tileController.CurrentBiddingTile?.EntrancePipe.SetTollCost(rate);
        _tileController.NextBiddingTile?.EntrancePipe.SetTollCost(rate);
    }

    private void Update()
    {
        _currentKingTimer += Time.deltaTime;
    }


    public IEnumerator ThroneNewKing(PlayerBall pb)
    {
        if(_enableNewKingBlockade)
            StartCoroutine(NewKingBlockade());

        //_myCameraController.KingFocusCameraMove(); 

        CleanupCurrentKing();

        _crown.UpdateCustomizations(CrownSerializer.GetColorListFromJSON(pb.Ph.pp.CrownJSON)); 

        string newKingUsername = pb.Ph.pp.TwitchUsername;
        pb.Ph.pp.ThroneCaptures += 1; 
        _autoPredictions.NewKingSignal(newKingUsername, (int)_previousKingDuration);

        MyTTS.inst.Announce($"Throne captured by {newKingUsername}");
        winnerNameText.SetText(newKingUsername);

        winnerNameText.color = pb._usernameText.color;
        _throneTileTrim.color = pb._usernameText.color;

        pb.SetupAsKing(kingTransform.localScale);
        pb.UpdateBidCountText(); //Hides the bid count while king
        pb._rb2D.transform.position = kingTransform.position;
        pb._rb2D.transform.eulerAngles = kingTransform.eulerAngles;
        pb.OverridePointsTextTarget(_kingPointsText);
        //pb.pointsText.rectTransform = _kingPoints.rectTransform;

        AudioController.inst.PlaySound(AudioController.inst.NewKingThroned, 1f, 1f);
        AudioController.inst.PlaySound(AudioController.inst.Beheading, 1f, 1f);
        confetti.Play(); 

        //pointPopUpTimer = 0;

        currentKing = pb;
        UpdateGoldText();

        yield return StartCoroutine(_cleaningBarController.RunCleaningBar());

        UpdateCurrExponentScale();

        _defaultDefenseV2.ResetDefense(DefaultDefenseMode.Random); 

        //Force spending half of points on defense
        long halfOfPoints = pb.Ph.pp.SessionScore / 2;
        if (halfOfPoints > 0)
            _defaultDefenseV2.AddBonusDefense(halfOfPoints, pb.Ph);

        _liveViewCount.NewKingSignal(); 
    }

    private IEnumerator NewKingBlockade()
    {
        _newKingBlockade.SetActive(true);
        _newKingBlockadeTimer.enabled = true;
        int timer = 0;
        int duration = AppConfig.inst.GetI("NewKingBlockadeDuration");
        while (timer < duration)
        {
            _newKingBlockadeTimer.SetText((duration - timer).ToString());
            yield return new WaitForSeconds(1);
            timer++;
        }
        _newKingBlockade.SetActive(false);
        _newKingBlockadeTimer.enabled = false;
    }

    private void CleanupCurrentKing()
    {
        _previousKingDuration = _currentKingTimer;
        _currentKingTimer = 0;

        kingInitialPlaceholder.enabled = false; //Only needs to happen for first king

        //Destroy the previous king and send them the points they've earned for holding the position
        if (currentKing != null)
        {
            currentKing.Ph.SetState(PlayerHandlerState.Idle);

            //currentKing.Ph.pp.Gold += ((int)_previousKingDuration);
            currentKing.Ph.pp.TimeOnThrone += ((int)_previousKingDuration);
            currentKing.ExplodeBall();
            currentKing.ResetAndUpdatePointsTextTarget();

            currentKing._sbcV2.transform.localScale = Vector3.one;

            currentKing._usernameText.enabled = true;
            currentKing._usernameBackgroundHighlight.enabled = true;
            currentKing._inviterIndicator.enabled = true;

        }
    }

    public void UpdateGoldText()
    {
        _kingGoldText.SetText($"{MyUtil.AbbreviateNum4Char(currentKing.Ph.pp.Gold)} Gold"); 
    }

    private void UpdateCurrExponentScale()
    {
        if (_previousKingDuration < AppConfig.inst.GetF("TargetKingDuration"))
            _defaultDefenseV2.ExponentScale += 1;
        else
        {
            _defaultDefenseV2.ExponentScale -= 1;
            if (_defaultDefenseV2.ExponentScale < 1)
                _defaultDefenseV2.ExponentScale = 1;
        }
    }

    public void DetectedPB(PlayerBall pb)
    {
        if (_cleaningBarController.Activated)
            return;

        StartCoroutine(ThroneNewKing(pb)); 
    }


    public void ReceiveTravelingIndicator(TravelingIndicator TI)
    {
        return;
    }

    public Vector3 Get_TI_IO_Position()
    {
        return _tollRateText.transform.position;
    }


}
