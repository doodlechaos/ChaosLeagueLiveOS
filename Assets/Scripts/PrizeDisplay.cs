using System.Collections;
using System.Collections.Generic;
using TMPro;
using TwitchLib.Api.Helix.Models.Charity.GetCharityCampaign;
using UnityEngine;

public class PrizeDisplay : MonoBehaviour
{

    [SerializeField] private TextMeshPro _winnerPrizeTextFront;
    [SerializeField] private TextMeshPro _winnerPrizeTextBack;

    [SerializeField] private Transform _prizeRotationRoot; 

    //[SerializeField] private TextMeshPro _multiplierText;
    //[SerializeField] private ParticleSystem _multiplierHypeParticles;
    //[SerializeField] private MeshRenderer _backgroundPanel;

    [SerializeField] private GameObject _giftBox;
    [SerializeField] private MeshRenderer _giftBoxBodyColor;

    [SerializeField] private float _animateDuration;
    [SerializeField] private Vector3 _maxScale;
    [SerializeField] private AnimationCurve _scaleCurve;

    [SerializeField] private Gradient _totalPrizeGradient;
    [SerializeField] private int _maxPrizeGradientAmount;

    private int _winnerPrizeAmount = 0;

    public void HideVisuals()
    {
        _prizeRotationRoot.gameObject.SetActive(false); 
/*        _giftBox.SetActive(false);
        _winnerPrizeTextFront.enabled = false;
        _winnerPrizeTextBack.enabled = false;*/
    }

    public void EnableVisuals()
    {
        _prizeRotationRoot.gameObject.SetActive(true);

        /*        _giftBox.SetActive(true); 
                _winnerPrizeTextFront.enabled = true;
                _winnerPrizeTextBack.enabled = true;*/

    }

    public void SetWinnerPrize(int total)
    {
        //Don't do anything if it is unchanged or less than
        if (total <= _winnerPrizeAmount)
            return;

        _winnerPrizeAmount = total;
        SetColorByValue();

        string prizeText = $"{MyUtil.AbbreviateNum4Char(_winnerPrizeAmount)}";
        _winnerPrizeTextFront.SetText(prizeText);
        _winnerPrizeTextBack.SetText(prizeText);

        StartCoroutine(AnimateWinnerPrizeText());
    }

    public int GetWinnerPrize()
    {
        return _winnerPrizeAmount;
    }
    public void ResetWinnerPrize()
    {
        _winnerPrizeAmount = 0;
        SetColorByValue();
        _winnerPrizeTextFront.SetText($"{_winnerPrizeAmount}");
        _winnerPrizeTextBack.SetText($"{_winnerPrizeAmount}");
    }

    private void SetColorByValue()
    {
        float t = _winnerPrizeAmount / (float) _maxPrizeGradientAmount;
        _giftBoxBodyColor.material.color = _totalPrizeGradient.Evaluate(t); 
    }

    private IEnumerator AnimateWinnerPrizeText()
    {
        float timer = 0;
        while(timer < _animateDuration)
        {
            float t = timer / _animateDuration;
            t = _scaleCurve.Evaluate(t);

            _prizeRotationRoot.localScale = Vector3.Lerp(Vector3.one, _maxScale, t);

            timer += Time.deltaTime; 
            yield return null;
        }
        _prizeRotationRoot.localScale = Vector3.one;

    }
}
