using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldingPen : PlayerReceiveable, TravelingIndicatorIO, IPlayerReceiveable
{
    [SerializeField] private GameManager _gm;

    public override Vector3 GetReceivePosition()
    {
        return transform.position;
    }
    public override void ReceivePlayer(PlayerBall pb)
    {
        _gm.DestroyPlayerBall(pb); //Destroy the player ball and don't do anything with the points
    }

    public Vector3 Get_TI_IO_Position()
    {
        return transform.position;
    }

    public void ReceiveTravelingIndicator(TravelingIndicator TI)
    {
        // Do nothing
    }


}
