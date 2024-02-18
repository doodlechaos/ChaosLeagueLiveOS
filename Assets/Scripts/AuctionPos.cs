using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class AuctionPos : PlayerReceiveable
{
    public bool IsValid = false;

    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private Vector3 _receiveOffset;
    public override Vector3 GetReceivePosition()
    {
        return transform.position + _receiveOffset;
    }

    public void SetValid(bool valid)
    {
        IsValid = valid;
    }


    public override void ReceivePlayer(PlayerBall pb)
    {
        //_meshRenderer.material.color = Color.gray;

    }

    public Vector3 PBTargetPos()
    {
        return (Vector2)transform.position;
    }

    public IEnumerator ColorSpinY_C(float duration, Color color)
    {

        _meshRenderer.material.color = color;

        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float rot = Mathf.Lerp(0, 360, t);
            transform.eulerAngles = new Vector3(90, rot, 0);

            yield return null;
        }
    }
}
