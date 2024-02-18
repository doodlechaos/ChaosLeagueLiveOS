using System.Collections;
using System.Collections.Generic;
using TMPro;
using TwitchLib.Api.Helix.Models.Soundtrack;
using TwitchLib.PubSub.Events;
using UnityEngine;

public class PodiumEntry : MonoBehaviour, TravelingIndicatorIO
{
    [SerializeField] private TextMeshPro _rankText;
    [SerializeField] private MeshRenderer _targetDisk;
    [SerializeField] private TextMeshPro _rewardText;
    public int Rank; 
    private int _reward;
    [SerializeField] private TextMeshPro _ROI_Text;

    private Vector3 _localTargetPos;

    public PlayerHandler Ph; 

    public void InitEntry(PlayerHandler ph, int rank, Vector3 localTargetPos)
    {
        Rank = rank;
        _rankText.SetText(rank.ToString() + ".");
        _rewardText.SetText("");
        //_doneWithSlideIn = false;

        if (ph.TilePointsROI >= 0)
            _ROI_Text.color = Color.green;
        else
            _ROI_Text.color = Color.red;
        
        _ROI_Text.SetText($"{MyUtil.AbbreviateNum4Char(ph.TilePointsROI).TrimStart('-')}");

        /*        if (string.IsNullOrEmpty(ph.pp.InvitedByID))
                    _ROI_Text.SetText("");
                else
                    StartCoroutine(SetInvitedByText(ph.GetGameManager(), ph.pp.InvitedByID));
        */
        Ph = ph;

        _localTargetPos = localTargetPos;
    }

/*    private IEnumerator SetInvitedByText(GameManager gm, string invitedByID)
    {
        CoroutineResult<PlayerHandler> coResult = new CoroutineResult<PlayerHandler>();
        yield return gm.GetPlayerHandler(invitedByID, coResult);

        PlayerHandler ph = coResult.Result;

        if (ph == null)
            yield break;

        _ROI_Text.SetText($"Invited by {ph.pp.TwitchUsername}");
    }*/

    public void ShiftLocalTargetPos(Vector3 shift)
    {
        _localTargetPos += shift; 
    }

    public void SetReward(int reward)
    {
        _reward = reward;
        _rewardText.SetText("+" + MyUtil.AbbreviateNum4Char(reward));
    }

    private void Update()
    {
        //Force the hologram to the disk
        if(Ph.pbh.gameObject.activeSelf)
            Ph.pbh.transform.position = _targetDisk.transform.position;

        //Ph.GetPlayerBall()._rb2D.transform.position = _targetDisk.transform.position;
        //Ph.GetPlayerBall()._rb2D.simulated = false;


        transform.localPosition = Vector3.Lerp(transform.localPosition, _localTargetPos, 0.5f); //Vector3.MoveTowards(transform.position, _targetPos, 0.5f); 
    }

    public Vector3 GetRewardTextPos()
    {
        return _rewardText.transform.position; 
    }

    public void ReceiveTravelingIndicator(TravelingIndicator TI)
    {
        throw new System.NotImplementedException();
    }

    public Vector3 Get_TI_IO_Position()
    {
        return GetRewardTextPos();
    }

/*    public GameObject GetGameObject()
    {
        return _rewardText.gameObject;
    }*/
}
