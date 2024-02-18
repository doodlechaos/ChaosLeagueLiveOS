using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChoiceSplitter : MonoBehaviour, IResetable
{
    [SerializeField] private PipeReleaser _leftPipe;
    [SerializeField] private PipeReleaser _rightPipe;
    [SerializeField] private int _rightPipeTicketCost = 1;

    [SerializeField] private SpriteRenderer _communityPointSprite;
    [SerializeField] private GameTile _gt; 


    public void MyReset()
    {
        _communityPointSprite.sprite = _gt.GetTileController().GetGameManager().CommunityPointSprite;
    }

    public void DetectedPB(PlayerBall pb)
    {
        int bid = pb.Ph.GetBid();

        if (bid > 0)
        {
            _rightPipe.ReceivePlayer(pb);
            pb.Ph.DecrementBid(_rightPipeTicketCost); 
        }
        else
        {
            _leftPipe.ReceivePlayer(pb);
        }

    }


}
