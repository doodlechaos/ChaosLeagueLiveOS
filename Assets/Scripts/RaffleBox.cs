using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RaffleBox : PlayerReceiveable, TravelingIndicatorIO
{
    [SerializeField] private Transform _receivePlayerPos;
    [SerializeField] private TextMeshPro _raffleCountText;

    public void SetRaffleCountText(string text)
    {
        _raffleCountText.SetText(text);
    }

    public override Vector3 GetReceivePosition()
    {
        return _receivePlayerPos.position;
    }

    public override void ReceivePlayer(PlayerBall pb)
    {
        //Disable the player ball and set to minimal mode
        pb.TempDeactivate();
    }

    public void ReceiveTravelingIndicator(TravelingIndicator TI)
    {
        throw new System.NotImplementedException();
    }

    public Vector3 Get_TI_IO_Position()
    {
        return _receivePlayerPos.position; 
    }

/*    public GameObject GetGameObject()
    {
        return gameObject; 
    }*/
}