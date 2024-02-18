using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BidQueueOrigin : PlayerReceiveable
{
    public override Vector3 GetReceivePosition()
    {
        Vector3 pos = transform.position;
        pos.z = 1; 
        return pos;
    }

    public override void ReceivePlayer(PlayerBall pb)
    {
        Debug.Log("Queue Origin receiving player"); 
    }
}
