using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static ContactWarp;

public enum CycleMode { verticalWrap, specificPos, receiver }

public class ContactWarp : MonoBehaviour
{
    [SerializeField] public LayerMask validLayers;
    [SerializeField] public float gravityAfterWarp = 1;
    [SerializeField] private int addPointsToPlayer = 0;


    public CycleMode targetMode;
    public PlayerReceiveable playerReceiver;
    public Transform targetPos;
    [SerializeField] private Transform verticalWrapHeight; 

    private StringBuilder _sb = new StringBuilder();

    private void DetectedObj(Rigidbody2D rb)
    {
        if (!MyUtil.IsLayerInMask(rb.gameObject.layer, validLayers))
            return;

        rb.gravityScale = gravityAfterWarp;

        if (targetMode == CycleMode.specificPos && targetPos != null)
        {
            rb.transform.position = targetPos.position;
            return;
        }
        else if (targetMode == CycleMode.receiver && playerReceiver != null)
        {
            PlayerBall pb = rb.gameObject.GetComponentInParent<PlayerBall>();

            if (pb == null)
            {
                DeathBall db = rb.gameObject.GetComponentInParent<DeathBall>();
                if (db == null)
                    return;

                playerReceiver.ReceiveDeathBall(db);
                return;
            }

            if (addPointsToPlayer > 0)
            {
                pb.Ph.AddPoints(addPointsToPlayer);

                _sb.Clear();
                _sb.Append("+")
                    .Append(addPointsToPlayer.ToString());
                TextPopupMaster.Inst.CreateTextPopup(pb._rb2D.position, Vector3.down, _sb.ToString(), Color.yellow, randomSpreadDist: 0f);
            }

            playerReceiver.ReceivePlayer(pb);
        }
        else if(targetMode == CycleMode.verticalWrap)
        {
            rb.transform.position = new Vector2(rb.transform.position.x, verticalWrapHeight.position.y); 
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        DetectedObj(collision.attachedRigidbody);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        DetectedObj(collision.rigidbody);
    }

}
