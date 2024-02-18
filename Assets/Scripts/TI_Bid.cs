using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class TI_Bid : MonoBehaviour
{
    public BidHandler BidHandler;
    public PlayerHandler Ph;

    [SerializeField] private SpriteRenderer _channelPointsSprite;
    [SerializeField] private SpriteRenderer _bitsSprite;
    [SerializeField] private SpriteRenderer _newPlayerSprite;
    [SerializeField] private SpriteRenderer _newSubSprite;

    public BidType BidType;

    [SerializeField] private AnimationCurve _slideIn;
    [SerializeField] private AnimationCurve _moveToTarget;
    [SerializeField] private float _slideInDuration;
    [SerializeField] private float _moveToTargetDuration;
    [SerializeField] private TextMeshPro _ticketCountText;
    [SerializeField] private TextMeshPro _usernameText;
    private float _moveToTargetTimer;

    private Vector3 _startPos;
    private TI_Bid_IO _target;
    public int Amount;
    [SerializeField] private float _zDepth = -1; 
    [SerializeField] private Vector3 slideOffset;
    [SerializeField] private MeshRenderer _background;
    [SerializeField] private Color _channelPointsBackgroundColor;
    [SerializeField] private Color _bitsBackgroundColor;
    [SerializeField] private Color _newPlayerBonusBackgroundColor;
    [SerializeField] private Color _newSubBonusBackgroundColor;


    public void Init(BidHandler bh, Vector3 startPos, TI_Bid_IO target, PlayerHandler ph, int count, BidType bidType)
    {
        BidHandler = bh;

        _moveToTargetTimer = 0;

        _startPos = startPos;
        transform.position = startPos;

        if (ph == null)
            Debug.LogError("target is null inside TI_ticket init");

        _target = target;
        Ph = ph;

        _channelPointsSprite.sprite = bh.GetGameManager().CommunityPointSprite; 

        _usernameText.SetText(ph.pp.TwitchUsername);
        _usernameText.color = ph.NameTextColor;

        BidType = bidType;
        DecorateByType(bidType); 

        Amount = count;
        _ticketCountText.SetText(MyUtil.AbbreviateNum4Char(count));

        StartCoroutine(SlideOntoScreenAnimation());
    }

    private void DecorateByType(BidType bidType)
    {
        _channelPointsSprite.enabled = false;
        _bitsSprite.enabled = false;
        _newPlayerSprite.enabled = false;
        _newSubSprite.enabled = false;

        if (bidType == BidType.NewSubBonus)
        {
            _newSubSprite.enabled = true;
            _background.material.color = _newSubBonusBackgroundColor;
        }
        else if (BidType == BidType.Bits)
        {
            _bitsSprite.enabled = true;
            _background.material.color = _bitsBackgroundColor;
        }
        else if (BidType == BidType.NewPlayerBonus)
        {
            _newPlayerSprite.enabled = true;
            _background.material.color = _newPlayerBonusBackgroundColor;
        }
        else
        {
            _channelPointsSprite.enabled = true;
            _background.material.color = _channelPointsBackgroundColor;
        }
    }

    public IEnumerator SlideOntoScreenAnimation()
    {

        float slideInDuration = 1.5f;
        float slideInTimer = 0;
        Vector3 targetPos = _startPos;
        _startPos = _startPos + (_startPos.x < 0 ? slideOffset : -slideOffset);
        while (slideInTimer < slideInDuration)
        {
            slideInTimer += Time.deltaTime;
            float t = slideInTimer / slideInDuration;
            t = _slideIn.Evaluate(t);
            Vector3 nextPos = Vector3.LerpUnclamped(_startPos, targetPos, t);
            nextPos.z = _zDepth;
            transform.position = nextPos;
            yield return null;
        }

        yield return MoveToTarget(transform.position);
    }

    public IEnumerator MoveToTarget(Vector3 startPos)
    {
        while (true)
        {
            _moveToTargetTimer += Time.deltaTime;

            float t = _moveToTargetTimer / _moveToTargetDuration;

            t = _moveToTarget.Evaluate(t);

            Vector3 nextTicketPos = Vector3.LerpUnclamped(startPos, _target.Get_Ticket_IO_Position(), t);
            nextTicketPos.z = _zDepth;
            transform.position = nextTicketPos;

            if (t >= 1)
            {
                _target.ReceiveBid(this);
                BidHandler.DestroyBidTI(this);
                yield break;
            }

            yield return null;
        }

    }

}

public interface TI_Bid_IO
{
    public void ReceiveBid(TI_Bid TI_Ticket);
    public Vector3 Get_Ticket_IO_Position();

}