using Cysharp.Threading.Tasks.Triggers;
using SpotifyAPI.Web;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TwitchLib.Api.Core.Extensions.System;
using UnityEngine;

public class InvitePromo : MonoBehaviour
{

    private Queue<(PlayerHandler invitor, PlayerHandler invited)> announcementQ = new Queue<(PlayerHandler, PlayerHandler)> ();

    [SerializeField] private GameManager _gm;
    [SerializeField] private AnimationCurve _scaleAnimation;
    [SerializeField] private GameObject _popupPanel;
    [SerializeField] private float _scaleAnimationDuration;

    [SerializeField] private MeshRenderer _inviterBody;
    [SerializeField] private TextMeshPro _inviterUsername;
    [SerializeField] private TextMeshPro _inviterPoints;

    [SerializeField] private MeshRenderer _invitedBody;
    [SerializeField] private MeshRenderer _invitedInviterIndicator;
    [SerializeField] private TextMeshPro _invitedUsername;
    [SerializeField] private TextMeshPro _invitedPoints;

    [SerializeField] private float _popSpinAnimationDuration = 1;
    [SerializeField] private float _popSpinRotations = 1;
    [SerializeField] private float _popUpDistance = 1;

    [SerializeField] private AnimationCurve _popSpinSpeed;


    //private WaitForSeconds waitOneSec = new WaitForSeconds(1);

    [SerializeField] private bool testButton;

    [SerializeField] private float announcmentBackupTimerDuration = 5;
    private float announcmentBackupTimer = 0;
    private bool _animatingAnnouncment = false; 

    private void Start()
    {
        _popupPanel.SetActive(false); 
    }

    private void OnValidate()
    {
        if (testButton)
        {
            testButton = false;
            AnnounceNewInvite("493342634", "169321608"); 
        }
    }

    public void AnnounceNewInvite(string inviterId,  string invitedId)
    {
        StartCoroutine(AddToQ(inviterId, invitedId));
    }
    public void AnnounceNewInvite(PlayerHandler inviter, PlayerHandler invited)
    {
        announcementQ.Enqueue((inviter, invited)); 
    }

    private IEnumerator AddToQ(string inviterId, string invitedId)
    {
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return _gm.GetPlayerHandler(inviterId, coResult);
        PlayerHandler inviter = coResult.Result;

        coResult.Reset();
        yield return _gm.GetPlayerHandler(invitedId, coResult);
        PlayerHandler invited = coResult.Result;

        announcementQ.Enqueue((inviter, invited)); 
    }

    private void Update()
    {
        //Wait we're animating, wait
        if (_animatingAnnouncment && announcmentBackupTimer > 0)
        {
            announcmentBackupTimer -= Time.deltaTime;
            return;
        }
        //If the coroutine encountered an error and we finish the backup timer, continue

        if (announcementQ.Count <= 0)
            return;

        StartCoroutine(AnnounceAnimation()); 
    }

    public IEnumerator AnnounceAnimation()
    {
        announcmentBackupTimer = announcmentBackupTimerDuration;
        _animatingAnnouncment = true;

        (PlayerHandler inviter, PlayerHandler invited) = announcementQ.Dequeue();

        //Set the textures and text
        _inviterUsername.SetText(inviter.pp.TwitchUsername);
        _inviterUsername.color = inviter.NameTextColor;
        _inviterPoints.SetText(MyUtil.AbbreviateNum3Char(inviter.pp.SessionScore));

        if (inviter.PfpTexture == null)
            yield return inviter.LoadBallPfp();
        _inviterBody.material.mainTexture = inviter.PfpTexture;
        _invitedInviterIndicator.material.mainTexture = inviter.PfpTexture;

        _invitedUsername.SetText(invited.pp.TwitchUsername);
        _invitedUsername.color = invited.NameTextColor;
        _invitedPoints.SetText(MyUtil.AbbreviateNum3Char(invited.pp.SessionScore));

        if (invited.PfpTexture == null)
            yield return invited.LoadBallPfp();
        _invitedBody.material.mainTexture = invited.PfpTexture;

        //Animate the promo page onto the screen
        _popupPanel.SetActive(true);
        _invitedInviterIndicator.gameObject.SetActive(false);

        MyTTS.inst.Announce($"{inviter.pp.TwitchUsername} invited {invited.pp.TwitchUsername} to the stream!"); 

        float timer = 0; 
        while(timer < _scaleAnimationDuration)
        {
            float t = timer / _scaleAnimationDuration;
            t = _scaleAnimation.Evaluate(t); 

            _popupPanel.transform.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, t);

            timer += Time.deltaTime; 
            yield return null; 
        }

        yield return new WaitForSeconds(0.3f);


        //Do an animation showing the pfp of the inviter going onto the player
        _invitedInviterIndicator.gameObject.SetActive(true);
        AudioController.inst.PlaySound(AudioController.inst.NewInviteFlipIndicator, 0.95f, 1.05f);
        Vector3 startPos = _invitedInviterIndicator.transform.localPosition;
        timer = 0;
        while(timer < _popSpinAnimationDuration)
        {
            float t = timer / _popSpinAnimationDuration;

            t = _popSpinSpeed.Evaluate(t); 
            _invitedInviterIndicator.transform.localPosition = startPos + (Vector3.up * _popUpDistance * t);

            if (t > 0.5)
                t = 1 - t;

            _invitedInviterIndicator.transform.eulerAngles = new Vector3(_invitedInviterIndicator.transform.eulerAngles.x, _popSpinRotations * 360 * t, _invitedInviterIndicator.transform.eulerAngles.z);


            timer += Time.deltaTime;
            yield return null;
        }
        _invitedInviterIndicator.transform.localPosition = startPos;

        yield return new WaitForSeconds(0.3f);

        //Animate the promo off the screen
        timer = 0;
        while (timer < _scaleAnimationDuration)
        {
            float t = timer / _scaleAnimationDuration;
            t = _scaleAnimation.Evaluate(1 - t);

            _popupPanel.transform.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, t);

            timer += Time.deltaTime;
            yield return null;
        }

        _popupPanel.SetActive(false);
        _animatingAnnouncment = false; 
    }

}
